using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FraFactu.Application.Interfaces;
using FraFactu.Application.DTOs.Compras;
using FraFactu.Application.Services;
using FraFactu.Application.Common;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;

namespace FraFactu.Infrastructure.Services;

public class CompraExternaService : ICompraExternaService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CompraExternaService>? _logger;

    // Usar UTC para almacenar en PostgreSQL (Azure usa UTC)
    private static DateTime GetUtcNow() => DateTime.UtcNow;

    public CompraExternaService(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<CompraExternaService>? logger = null)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<CompraExternaDto> CrearAsync(CrearCompraExternaDto dto, int emisorId)
    {
        // Validar que la sucursal pertenezca al emisor
        var sucursalExiste = await _context.Sucursales
            .AnyAsync(s => s.Id == dto.SucursalId && s.EmisorId == emisorId);

        if (!sucursalExiste)
            throw new InvalidOperationException("La sucursal no existe o no pertenece al emisor");
        // Validar que el proveedor exista
        var proveedorExiste = await _context.Proveedores
            .AnyAsync(p => p.Id == dto.ProveedorId && p.Activo);

        if (!proveedorExiste)
            throw new InvalidOperationException("El proveedor no existe o está inactivo");

        // Validar que no exista la misma factura del mismo proveedor
        var facturaExiste = await _context.ComprasExternas
            .AnyAsync(c => c.ProveedorId == dto.ProveedorId && c.NumeroFactura == dto.NumeroFactura);

        if (facturaExiste)
            throw new InvalidOperationException($"Ya existe una compra con el número de factura {dto.NumeroFactura} del mismo proveedor");

        // Crear la compra en estado BORRADOR
        var compra = new CompraExterna
        {
            ProveedorId = dto.ProveedorId,
            SucursalId = dto.SucursalId,
            NumeroFactura = dto.NumeroFactura,
            FechaEmision = dto.FechaEmision,
            FechaRegistro = GetUtcNow(),
            Subtotal = dto.Subtotal,
            IVA = dto.IVA,
            Total = dto.Total,
            Estado = "BORRADOR",
            Observaciones = dto.Observaciones
        };

        _context.ComprasExternas.Add(compra);
        await _context.SaveChangesAsync();

        // Crear detalles
        foreach (var detalleDto in dto.Detalles)
        {
            var detalle = new CompraExternaDetalle
            {
                CompraExternaId = compra.Id,
                ProductoId = detalleDto.ProductoId,
                BodegaId = detalleDto.BodegaId,
                Cantidad = detalleDto.Cantidad,
                CostoUnitario = detalleDto.CostoUnitario,
                Subtotal = detalleDto.Subtotal,
                IVA = detalleDto.IVA,
                Total = detalleDto.Total,
                EsParaInventario = detalleDto.EsParaInventario
            };

            _context.CompraExternaDetalles.Add(detalle);
        }

        // Crear gastos administrativos
        foreach (var gastoDto in dto.Gastos)
        {
            var gasto = new GastoAdministrativo
            {
                CompraExternaId = compra.Id,
                CatTipoGastoId = gastoDto.CatTipoGastoId,
                Descripcion = gastoDto.Descripcion,
                Monto = gastoDto.Monto,
                CentroCosto = gastoDto.CentroCosto,
                CuentaContable = gastoDto.CuentaContable
            };

            _context.GastosAdministrativos.Add(gasto);
        }

        await _context.SaveChangesAsync();

        return await ObtenerPorIdAsync(compra.Id, emisorId);
    }

    public async Task<CompraExternaDto> ActualizarAsync(int id, ActualizarCompraExternaDto dto, int emisorId)
    {
        // Obtener la compra con sus detalles y gastos
        var compra = await _context.ComprasExternas
            .Include(c => c.Detalles)
            .Include(c => c.Gastos)
            .Include(c => c.Sucursal)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (compra == null)
            throw new KeyNotFoundException($"Compra con ID {id} no encontrada");

        if (compra.Sucursal.EmisorId != emisorId)
            throw new UnauthorizedAccessException("No tiene permiso para actualizar esta compra");

        // Validar que esté en estado BORRADOR
        if (compra.Estado != "BORRADOR")
            throw new InvalidOperationException($"Solo se pueden actualizar compras en estado BORRADOR. Estado actual: {compra.Estado}");

        // Validar que el proveedor exista y esté activo
        var proveedorExiste = await _context.Proveedores
            .AnyAsync(p => p.Id == dto.ProveedorId && p.Activo);

        if (!proveedorExiste)
            throw new InvalidOperationException("El proveedor no existe o está inactivo");

        // Validar que no exista la misma factura del mismo proveedor (excluyendo la actual si cambia)
        if (compra.NumeroFactura != dto.NumeroFactura || compra.ProveedorId != dto.ProveedorId)
        {
            var facturaExiste = await _context.ComprasExternas
                .AnyAsync(c => c.Id != id &&
                             c.ProveedorId == dto.ProveedorId &&
                             c.NumeroFactura == dto.NumeroFactura);

            if (facturaExiste)
                throw new InvalidOperationException($"Ya existe una compra con el número de factura {dto.NumeroFactura} del mismo proveedor");
        }

        // Actualizar campos principales
        compra.ProveedorId = dto.ProveedorId;
        compra.NumeroFactura = dto.NumeroFactura;
        compra.FechaEmision = dto.FechaEmision;
        compra.Subtotal = dto.Subtotal;
        compra.IVA = dto.IVA;
        compra.Total = dto.Total;
        compra.Observaciones = dto.Observaciones;

        // Eliminar detalles anteriores
        _context.CompraExternaDetalles.RemoveRange(compra.Detalles);

        // Eliminar gastos anteriores
        _context.GastosAdministrativos.RemoveRange(compra.Gastos);

        // Agregar nuevos detalles
        foreach (var detalleDto in dto.Detalles)
        {
            var detalle = new CompraExternaDetalle
            {
                CompraExternaId = compra.Id,
                ProductoId = detalleDto.ProductoId,
                BodegaId = detalleDto.BodegaId,
                Cantidad = detalleDto.Cantidad,
                CostoUnitario = detalleDto.CostoUnitario,
                Subtotal = detalleDto.Subtotal,
                IVA = detalleDto.IVA,
                Total = detalleDto.Total,
                EsParaInventario = detalleDto.EsParaInventario
            };

            _context.CompraExternaDetalles.Add(detalle);
        }

        // Agregar nuevos gastos
        foreach (var gastoDto in dto.Gastos)
        {
            var gasto = new GastoAdministrativo
            {
                CompraExternaId = compra.Id,
                CatTipoGastoId = gastoDto.CatTipoGastoId,
                Descripcion = gastoDto.Descripcion,
                Monto = gastoDto.Monto,
                CentroCosto = gastoDto.CentroCosto,
                CuentaContable = gastoDto.CuentaContable
            };

            _context.GastosAdministrativos.Add(gasto);
        }

        await _context.SaveChangesAsync();

        return await ObtenerPorIdAsync(compra.Id, emisorId);
    }

    public async Task<CompraExternaDto> ConfirmarAsync(int id, int emisorId, ConfirmarCompraDto? dto = null)
    {
        var compra = await _context.ComprasExternas
            .Include(c => c.Detalles)
                .ThenInclude(d => d.Producto)
            .Include(c => c.Detalles)
                .ThenInclude(d => d.Bodega)
            .Include(c => c.Sucursal)
                .ThenInclude(s => s.Emisor)  // F3: necesario para chequear TieneSmartInventoryActiva
            .FirstOrDefaultAsync(c => c.Id == id);

        if (compra == null)
            throw new KeyNotFoundException($"Compra con ID {id} no encontrada");

        if (compra.Sucursal.EmisorId != emisorId)
            throw new UnauthorizedAccessException("No tiene permiso para confirmar esta compra");

        if (compra.Estado != "BORRADOR")
            throw new InvalidOperationException($"Solo se pueden confirmar compras en estado BORRADOR. Estado actual: {compra.Estado}");

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Procesar detalles que afectan inventario
            foreach (var detalle in compra.Detalles.Where(d => d.EsParaInventario))
            {
                // F2: los activos fijos (MobiliarioEquipo) NO entran al stock
                // circulante; solo se registra el movimiento de adquisicion
                // para trazabilidad. Smartix no calcula costo promedio ni
                // saldos de stock para estos items.
                var esActivoFijo = detalle.Producto?.TipoInventario
                    == Domain.Enums.TipoInventario.MobiliarioEquipo;

                var movimiento = new MovimientoInventario
                {
                    ProductoId = detalle.ProductoId,
                    BodegaId = detalle.BodegaId,
                    TipoMovimiento = "ENTRADA",
                    Cantidad = detalle.Cantidad,
                    CostoUnitario = detalle.CostoUnitario,
                    TipoDocumento = "COMPRA_EXTERNA",
                    DocumentoId = compra.Id,
                    NumeroDocumento = compra.NumeroFactura,
                    FechaMovimiento = GetUtcNow(),
                    Observaciones = esActivoFijo
                        ? $"Activo fijo - Proveedor: {compra.ProveedorId}"
                        : $"Compra externa - Proveedor: {compra.ProveedorId}",
                    UsuarioId = _currentUserService.GetUsuarioId()
                };

                if (esActivoFijo)
                {
                    // Activos fijos: solo movimiento, sin tocar StockBodega ni
                    // calcular saldos. La trazabilidad queda; el "stock" del
                    // activo se gestiona desde la vista de activos fijos.
                    _context.MovimientosInventario.Add(movimiento);
                    continue;
                }

                // Actualizar stock (solo Ventas / Insumos)
                var stock = await _context.StocksBodega
                    .FirstOrDefaultAsync(s => s.ProductoId == detalle.ProductoId && s.BodegaId == detalle.BodegaId);

                if (stock == null)
                {
                    // Crear stock si no existe
                    stock = new StockBodega
                    {
                        ProductoId = detalle.ProductoId,
                        BodegaId = detalle.BodegaId,
                        CantidadDisponible = detalle.Cantidad,
                        CantidadReservada = 0,
                        CostoPromedio = detalle.CostoUnitario
                    };
                    _context.StocksBodega.Add(stock);
                }
                else
                {
                    // Calcular nuevo costo promedio ponderado
                    var cantidadAnterior = stock.CantidadTotal;
                    var costoAnterior = stock.CostoPromedio;
                    var nuevaCantidad = cantidadAnterior + detalle.Cantidad;

                    stock.CostoPromedio = ((cantidadAnterior * costoAnterior) + (detalle.Cantidad * detalle.CostoUnitario)) / nuevaCantidad;
                    stock.CantidadDisponible += detalle.Cantidad;
                }

                // Guardar saldos en el movimiento
                movimiento.SaldoAnterior = stock.CantidadTotal - detalle.Cantidad;
                movimiento.NuevoSaldo = stock.CantidadTotal;
                movimiento.CostoPromedioAnterior = stock.CostoPromedio;
                movimiento.NuevoCostoPromedio = stock.CostoPromedio;

                _context.MovimientosInventario.Add(movimiento);
            }

            // Cambiar estado a CONFIRMADA
            compra.Estado = "CONFIRMADA";
            compra.FechaConfirmacion = GetUtcNow();

            if (dto != null && !string.IsNullOrWhiteSpace(dto.Observaciones))
            {
                compra.Observaciones += $"\n[Confirmación] {dto.Observaciones}";
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return await ObtenerPorIdAsync(compra.Id, emisorId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }


    public async Task<CompraExternaDto> AnularAsync(int id, AnularCompraDto dto, int emisorId)
    {
        var compra = await _context.ComprasExternas
            .Include(c => c.Detalles)
                .ThenInclude(d => d.Producto)  // F2: necesario para leer TipoInventario al revertir
            .Include(c => c.Sucursal)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (compra == null)
            throw new KeyNotFoundException($"Compra con ID {id} no encontrada");

        if (compra.Sucursal.EmisorId != emisorId)
            throw new UnauthorizedAccessException("No tiene permiso para anular esta compra");

        if (compra.Estado != "CONFIRMADA")
            throw new InvalidOperationException($"Solo se pueden anular compras en estado CONFIRMADA. Estado actual: {compra.Estado}");

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Revertir movimientos de inventario
            foreach (var detalle in compra.Detalles.Where(d => d.EsParaInventario))
            {
                // F2: activos fijos no tienen stock circulante. Al anular,
                // solo se registra el movimiento de SALIDA por trazabilidad;
                // el ProductoServicio queda en el catalogo (decision producto:
                // anular compra no borra el activo). El usuario puede desactivar
                // el producto manualmente si corresponde.
                var esActivoFijo = detalle.Producto?.TipoInventario
                    == Domain.Enums.TipoInventario.MobiliarioEquipo;

                if (esActivoFijo)
                {
                    _context.MovimientosInventario.Add(new MovimientoInventario
                    {
                        ProductoId = detalle.ProductoId,
                        BodegaId = detalle.BodegaId,
                        TipoMovimiento = "SALIDA",
                        Cantidad = -detalle.Cantidad,
                        CostoUnitario = detalle.CostoUnitario,
                        TipoDocumento = "ANULACION_COMPRA",
                        DocumentoId = compra.Id,
                        NumeroDocumento = compra.NumeroFactura,
                        FechaMovimiento = GetUtcNow(),
                        Observaciones = $"Anulación activo fijo. Motivo: {dto.Motivo}",
                        UsuarioId = _currentUserService.GetUsuarioId()
                    });
                    continue;
                }

                var stock = await _context.StocksBodega
                    .FirstOrDefaultAsync(s => s.ProductoId == detalle.ProductoId && s.BodegaId == detalle.BodegaId);

                if (stock == null)
                    throw new InvalidOperationException($"No se encontró stock para el producto {detalle.ProductoId} en bodega {detalle.BodegaId}");

                // Validar que hay suficiente stock para revertir
                if (stock.CantidadDisponible < detalle.Cantidad)
                    throw new InvalidOperationException($"Stock insuficiente para revertir. Disponible: {stock.CantidadDisponible}, necesario: {detalle.Cantidad}");

                // Restar cantidad
                stock.CantidadDisponible -= detalle.Cantidad;

                // Registrar movimiento de anulación
                var movimiento = new MovimientoInventario
                {
                    ProductoId = detalle.ProductoId,
                    BodegaId = detalle.BodegaId,
                    TipoMovimiento = "SALIDA",
                    Cantidad = -detalle.Cantidad,
                    CostoUnitario = detalle.CostoUnitario,
                    TipoDocumento = "ANULACION_COMPRA",
                    DocumentoId = compra.Id,
                    NumeroDocumento = compra.NumeroFactura,
                    FechaMovimiento = GetUtcNow(),
                    Observaciones = $"Anulación de compra. Motivo: {dto.Motivo}",
                    SaldoAnterior = stock.CantidadTotal + detalle.Cantidad,
                    NuevoSaldo = stock.CantidadTotal,
                    CostoPromedioAnterior = stock.CostoPromedio,
                    NuevoCostoPromedio = stock.CostoPromedio,
                    UsuarioId = _currentUserService.GetUsuarioId()
                };

                _context.MovimientosInventario.Add(movimiento);
            }

            // Cambiar estado a ANULADA
            compra.Estado = "ANULADA";
            compra.FechaAnulacion = GetUtcNow();
            compra.Observaciones += $"\n[Anulación] {dto.Motivo}";

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return await ObtenerPorIdAsync(compra.Id, emisorId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<CompraExternaDto> EditarConfirmadaAsync(int id, ActualizarCompraExternaDto dto, int emisorId)
    {
        var compra = await _context.ComprasExternas
            .Include(c => c.Detalles)
                .ThenInclude(d => d.Producto)
            .Include(c => c.Detalles)
                .ThenInclude(d => d.Bodega)
            .Include(c => c.Gastos)
            .Include(c => c.Sucursal)
                .ThenInclude(s => s.Emisor)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (compra == null)
            throw new KeyNotFoundException($"Compra con ID {id} no encontrada");

        if (compra.Sucursal.EmisorId != emisorId)
            throw new UnauthorizedAccessException("No tiene permiso para editar esta compra");

        if (compra.Estado != "CONFIRMADA")
            throw new InvalidOperationException($"Solo se pueden editar compras en estado CONFIRMADA. Estado actual: {compra.Estado}");

        if (compra.Origen != "MANUAL")
            throw new InvalidOperationException("Las compras de origen DTE no se pueden editar.");

        var proveedorExiste = await _context.Proveedores.AnyAsync(p => p.Id == dto.ProveedorId && p.Activo);
        if (!proveedorExiste)
            throw new InvalidOperationException("El proveedor no existe o está inactivo");

        if (compra.NumeroFactura != dto.NumeroFactura || compra.ProveedorId != dto.ProveedorId)
        {
            var facturaExiste = await _context.ComprasExternas
                .AnyAsync(c => c.Id != id && c.ProveedorId == dto.ProveedorId && c.NumeroFactura == dto.NumeroFactura);
            if (facturaExiste)
                throw new InvalidOperationException($"Ya existe una compra con el número de factura {dto.NumeroFactura} del mismo proveedor");
        }

        // Snapshot de items viejos (para detectar cambio y calcular el delta a SmartInventory).
        var itemsViejos = compra.Detalles
            .Where(d => d.EsParaInventario)
            .Select(d => (d.ProductoId, d.BodegaId, d.Cantidad))
            .ToList();

        var itemsCambian = DetallesCambiaron(compra.Detalles, dto.Detalles);

        // --- Rama solo-cabecera: no toca inventario ---
        if (!itemsCambian)
        {
            ActualizarCabeceraYGastos(compra, dto);
            await _context.SaveChangesAsync();
            return await ObtenerPorIdAsync(compra.Id, emisorId);
        }

        // --- Rama con inventario: revertir + reaplicar (atómico) ---
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Pre-chequeo de stock negativo ANTES de escribir nada.
            foreach (var detalle in compra.Detalles.Where(d => d.EsParaInventario))
            {
                if (detalle.Producto?.TipoInventario == Domain.Enums.TipoInventario.MobiliarioEquipo)
                    continue; // activos fijos no tienen stock circulante
                var stockActual = await _context.StocksBodega
                    .FirstOrDefaultAsync(s => s.ProductoId == detalle.ProductoId && s.BodegaId == detalle.BodegaId);
                var disponible = stockActual?.CantidadDisponible ?? 0m;
                if (disponible < detalle.Cantidad)
                    throw new InvalidOperationException(
                        $"No se puede editar: revertir '{detalle.Producto?.Nombre ?? detalle.ProductoId.ToString()}' " +
                        $"dejaría stock negativo (disponible: {disponible}, comprado: {detalle.Cantidad})");
            }

            // 2. Revertir inventario de items actuales (estilo Anular).
            foreach (var detalle in compra.Detalles.Where(d => d.EsParaInventario))
            {
                var esActivoFijo = detalle.Producto?.TipoInventario == Domain.Enums.TipoInventario.MobiliarioEquipo;
                if (esActivoFijo)
                {
                    _context.MovimientosInventario.Add(new MovimientoInventario
                    {
                        ProductoId = detalle.ProductoId, BodegaId = detalle.BodegaId,
                        TipoMovimiento = "SALIDA", Cantidad = -detalle.Cantidad,
                        CostoUnitario = detalle.CostoUnitario, TipoDocumento = "EDICION_COMPRA",
                        DocumentoId = compra.Id, NumeroDocumento = compra.NumeroFactura,
                        FechaMovimiento = GetUtcNow(),
                        Observaciones = "Edición de compra confirmada (reverso activo fijo)",
                        UsuarioId = _currentUserService.GetUsuarioId()
                    });
                    continue;
                }

                var stock = await _context.StocksBodega
                    .FirstAsync(s => s.ProductoId == detalle.ProductoId && s.BodegaId == detalle.BodegaId);
                stock.CantidadDisponible -= detalle.Cantidad;
                _context.MovimientosInventario.Add(new MovimientoInventario
                {
                    ProductoId = detalle.ProductoId, BodegaId = detalle.BodegaId,
                    TipoMovimiento = "SALIDA", Cantidad = -detalle.Cantidad,
                    CostoUnitario = detalle.CostoUnitario, TipoDocumento = "EDICION_COMPRA",
                    DocumentoId = compra.Id, NumeroDocumento = compra.NumeroFactura,
                    FechaMovimiento = GetUtcNow(),
                    Observaciones = "Edición de compra confirmada (reverso)",
                    SaldoAnterior = stock.CantidadTotal + detalle.Cantidad, NuevoSaldo = stock.CantidadTotal,
                    CostoPromedioAnterior = stock.CostoPromedio, NuevoCostoPromedio = stock.CostoPromedio,
                    UsuarioId = _currentUserService.GetUsuarioId()
                });
            }

            // 3. Reemplazar detalles y gastos.
            _context.CompraExternaDetalles.RemoveRange(compra.Detalles);
            _context.GastosAdministrativos.RemoveRange(compra.Gastos);
            await _context.SaveChangesAsync(); // materializa el borrado antes de reinsertar/leer stock

            var nuevosDetalles = dto.Detalles.Select(d => new CompraExternaDetalle
            {
                CompraExternaId = compra.Id, ProductoId = d.ProductoId, BodegaId = d.BodegaId,
                Cantidad = d.Cantidad, CostoUnitario = d.CostoUnitario, Subtotal = d.Subtotal,
                IVA = d.IVA, Total = d.Total, EsParaInventario = d.EsParaInventario
            }).ToList();
            _context.CompraExternaDetalles.AddRange(nuevosDetalles);

            foreach (var gastoDto in dto.Gastos)
            {
                _context.GastosAdministrativos.Add(new GastoAdministrativo
                {
                    CompraExternaId = compra.Id, CatTipoGastoId = gastoDto.CatTipoGastoId,
                    Descripcion = gastoDto.Descripcion, Monto = gastoDto.Monto,
                    CentroCosto = gastoDto.CentroCosto, CuentaContable = gastoDto.CuentaContable
                });
            }

            // 4. Reaplicar inventario de los nuevos items (estilo Confirmar).
            foreach (var detalle in nuevosDetalles.Where(d => d.EsParaInventario))
            {
                var producto = await _context.ProductosServicios.FindAsync(detalle.ProductoId);
                var esActivoFijo = producto?.TipoInventario == Domain.Enums.TipoInventario.MobiliarioEquipo;

                var movimiento = new MovimientoInventario
                {
                    ProductoId = detalle.ProductoId, BodegaId = detalle.BodegaId,
                    TipoMovimiento = "ENTRADA", Cantidad = detalle.Cantidad,
                    CostoUnitario = detalle.CostoUnitario, TipoDocumento = "EDICION_COMPRA",
                    DocumentoId = compra.Id, NumeroDocumento = compra.NumeroFactura,
                    FechaMovimiento = GetUtcNow(),
                    Observaciones = esActivoFijo
                        ? "Edición de compra confirmada (activo fijo)"
                        : "Edición de compra confirmada (entrada)",
                    UsuarioId = _currentUserService.GetUsuarioId()
                };

                if (esActivoFijo)
                {
                    _context.MovimientosInventario.Add(movimiento);
                    continue;
                }

                var stock = await _context.StocksBodega
                    .FirstOrDefaultAsync(s => s.ProductoId == detalle.ProductoId && s.BodegaId == detalle.BodegaId);
                if (stock == null)
                {
                    stock = new StockBodega
                    {
                        ProductoId = detalle.ProductoId, BodegaId = detalle.BodegaId,
                        CantidadDisponible = detalle.Cantidad, CantidadReservada = 0,
                        CostoPromedio = detalle.CostoUnitario
                    };
                    _context.StocksBodega.Add(stock);
                }
                else
                {
                    var cantidadAnterior = stock.CantidadTotal;
                    var costoAnterior = stock.CostoPromedio;
                    var nuevaCantidad = cantidadAnterior + detalle.Cantidad;
                    stock.CostoPromedio = nuevaCantidad == 0
                        ? costoAnterior
                        : ((cantidadAnterior * costoAnterior) + (detalle.Cantidad * detalle.CostoUnitario)) / nuevaCantidad;
                    stock.CantidadDisponible += detalle.Cantidad;
                }
                movimiento.SaldoAnterior = stock.CantidadTotal - detalle.Cantidad;
                movimiento.NuevoSaldo = stock.CantidadTotal;
                movimiento.CostoPromedioAnterior = stock.CostoPromedio;
                movimiento.NuevoCostoPromedio = stock.CostoPromedio;
                _context.MovimientosInventario.Add(movimiento);
            }

            // 5. Cabecera + totales.
            ActualizarCabeceraYGastos(compra, dto, reemplazarGastos: false);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return await ObtenerPorIdAsync(compra.Id, emisorId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>Compara los detalles actuales con los del DTO como multiconjunto.</summary>
    private static bool DetallesCambiaron(
        ICollection<CompraExternaDetalle> actuales,
        List<CrearCompraDetalleDto> nuevos)
    {
        if (actuales.Count != nuevos.Count) return true;
        static IEnumerable<(int, int, decimal, decimal, bool)> Orden(
            IEnumerable<(int p, int b, decimal c, decimal co, bool e)> src) =>
            src.OrderBy(x => x.p).ThenBy(x => x.b).ThenBy(x => x.c).ThenBy(x => x.co).ThenBy(x => x.e)
               .Select(x => (x.p, x.b, x.c, x.co, x.e));
        var a = Orden(actuales.Select(d => (d.ProductoId, d.BodegaId, d.Cantidad, d.CostoUnitario, d.EsParaInventario)));
        var n = Orden(nuevos.Select(d => (d.ProductoId, d.BodegaId, d.Cantidad, d.CostoUnitario, d.EsParaInventario)));
        return !a.SequenceEqual(n);
    }

    /// <summary>Actualiza campos de cabecera. Si reemplazarGastos, borra y reinserta gastos del DTO.</summary>
    private void ActualizarCabeceraYGastos(CompraExterna compra, ActualizarCompraExternaDto dto, bool reemplazarGastos = true)
    {
        compra.ProveedorId = dto.ProveedorId;
        compra.NumeroFactura = dto.NumeroFactura;
        compra.FechaEmision = dto.FechaEmision;
        compra.Subtotal = dto.Subtotal;
        compra.IVA = dto.IVA;
        compra.Total = dto.Total;
        compra.Observaciones = dto.Observaciones;

        if (reemplazarGastos)
        {
            _context.GastosAdministrativos.RemoveRange(compra.Gastos);
            foreach (var gastoDto in dto.Gastos)
            {
                _context.GastosAdministrativos.Add(new GastoAdministrativo
                {
                    CompraExternaId = compra.Id, CatTipoGastoId = gastoDto.CatTipoGastoId,
                    Descripcion = gastoDto.Descripcion, Monto = gastoDto.Monto,
                    CentroCosto = gastoDto.CentroCosto, CuentaContable = gastoDto.CuentaContable
                });
            }
        }
    }

    public async Task<CompraExternaDto> ObtenerPorIdAsync(int id, int emisorId)
    {
        var compra = await _context.ComprasExternas
            .Include(c => c.Proveedor)
            .Include(c => c.Sucursal) // Include Sucursal to check EmisorId
            .Include(c => c.Detalles)
                .ThenInclude(d => d.Producto)
            .Include(c => c.Detalles)
                .ThenInclude(d => d.Bodega)
            .Include(c => c.Gastos)
                .ThenInclude(g => g.TipoGasto)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (compra == null)
            throw new KeyNotFoundException($"Compra con ID {id} no encontrada");

        if (compra.Sucursal.EmisorId != emisorId)
            throw new UnauthorizedAccessException("No tiene permiso para ver esta compra");

        return MapToDto(compra);
    }

    public async Task<PagedResult<CompraExternaDto>> ListarAsync(
        int emisorId,
        int pagina = 1,
        int tamanoPagina = 20,
        int? proveedorId = null,
        int? bodegaId = null,
        int? sucursalId = null,
        string? estado = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        DateTime? fechaRegistroDesde = null,
        DateTime? fechaRegistroHasta = null,
        string? search = null,
        List<int>? sucursalIds = null,
        string? sortBy = null,
        bool sortDesc = true)
    {
        var query = _context.ComprasExternas
            .Include(c => c.Proveedor)
            .Include(c => c.Sucursal)
            .Include(c => c.Detalles)  // CRÍTICO: sin esto, Items = 0
            .Include(c => c.Gastos)
            .Where(c => c.Sucursal.EmisorId == emisorId) // Filtrar por emisor
            .AsQueryable();

        // Filtros
        if (proveedorId.HasValue)
            query = query.Where(c => c.ProveedorId == proveedorId.Value);

        if (bodegaId.HasValue)
            query = query.Where(c => c.Detalles.Any(d => d.BodegaId == bodegaId.Value));

        if (sucursalIds != null && sucursalIds.Any())
            query = query.Where(c => sucursalIds.Contains(c.SucursalId));
        else if (sucursalId.HasValue)
            query = query.Where(c => c.SucursalId == sucursalId.Value);

        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(c => c.Estado == estado);

        if (fechaDesde.HasValue)
            query = query.Where(c => c.FechaEmision >= fechaDesde.Value);

        if (fechaHasta.HasValue)
            query = query.Where(c => c.FechaEmision <= fechaHasta.Value);

        if (fechaRegistroDesde.HasValue)
            query = query.Where(c => c.FechaRegistro >= fechaRegistroDesde.Value);

        if (fechaRegistroHasta.HasValue)
            query = query.Where(c => c.FechaRegistro <= fechaRegistroHasta.Value);

        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(c =>
                c.NumeroFactura.ToLower().Contains(searchLower) ||
                (c.Proveedor != null && c.Proveedor.Nombre.ToLower().Contains(searchLower)) ||
                (c.Observaciones != null && c.Observaciones.ToLower().Contains(searchLower)) ||
                (c.Sucursal != null && c.Sucursal.Nombre.ToLower().Contains(searchLower)));
        }

        // Total
        var total = await query.CountAsync();

        // Ordenamiento dinámico
        IOrderedQueryable<Domain.Entities.CompraExterna> orderedQuery = sortBy?.ToLower() switch
        {
            "numerofactura" => sortDesc ? query.OrderByDescending(c => c.NumeroFactura) : query.OrderBy(c => c.NumeroFactura),
            "proveedor" => sortDesc ? query.OrderByDescending(c => c.Proveedor != null ? c.Proveedor.Nombre : "") : query.OrderBy(c => c.Proveedor != null ? c.Proveedor.Nombre : ""),
            "total" => sortDesc ? query.OrderByDescending(c => c.Total) : query.OrderBy(c => c.Total),
            "estado" => sortDesc ? query.OrderByDescending(c => c.Estado) : query.OrderBy(c => c.Estado),
            "observaciones" => sortDesc ? query.OrderByDescending(c => c.Observaciones) : query.OrderBy(c => c.Observaciones),
            "sucursal" => sortDesc ? query.OrderByDescending(c => c.Sucursal != null ? c.Sucursal.Nombre : "") : query.OrderBy(c => c.Sucursal != null ? c.Sucursal.Nombre : ""),
            _ => sortDesc ? query.OrderByDescending(c => c.FechaEmision) : query.OrderBy(c => c.FechaEmision)
        };

        // Paginación
        var compras = await orderedQuery
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        var items = compras.Select(MapToDto).ToList();

        return new PagedResult<CompraExternaDto>
        {
            Items = items,
            TotalItems = total,
            PageNumber = pagina,
            PageSize = tamanoPagina
        };
    }

    public async Task<List<CompraExternaDto>> ObtenerPorProveedorAsync(int proveedorId, int emisorId, int limite = 10)
    {
        var compras = await _context.ComprasExternas
            .Include(c => c.Proveedor)
            .Include(c => c.Sucursal)
            .Where(c => c.ProveedorId == proveedorId && c.Sucursal.EmisorId == emisorId)
            .OrderByDescending(c => c.FechaEmision)
            .Take(limite)
            .ToListAsync();

        return compras.Select(MapToDto).ToList();
    }

    public async Task<decimal> ObtenerTotalComprasAsync(DateTime desde, DateTime hasta, int emisorId, int? proveedorId = null, int? sucursalId = null)
    {
        var query = _context.ComprasExternas
            .Where(c => c.Estado == "CONFIRMADA")
            .Where(c => c.Sucursal.EmisorId == emisorId)
            .Where(c => c.FechaEmision >= desde && c.FechaEmision <= hasta);

        if (proveedorId.HasValue)
            query = query.Where(c => c.ProveedorId == proveedorId.Value);

        if (sucursalId.HasValue)
            query = query.Where(c => c.SucursalId == sucursalId.Value);

        return await query.SumAsync(c => c.Total);
    }

    private CompraExternaDto MapToDto(CompraExterna compra)
    {
        var detalles = compra.Detalles?.Select(d => new CompraDetalleDto
        {
            Id = d.Id,
            CompraExternaId = d.CompraExternaId,
            ProductoId = d.ProductoId,
            ProductoCodigo = d.Producto?.Codigo ?? "",
            ProductoNombre = d.Producto?.Nombre ?? "",
            BodegaId = d.BodegaId,
            BodegaNombre = d.Bodega?.Nombre ?? "",
            Cantidad = d.Cantidad,
            CostoUnitario = d.CostoUnitario,
            Subtotal = d.Subtotal,
            IVA = d.IVA,
            Total = d.Total,
            EsParaInventario = d.EsParaInventario
        }).ToList() ?? new();

        var gastos = compra.Gastos?.Select(g => new GastoAdministrativoDto
        {
            Id = g.Id,
            CompraExternaId = g.CompraExternaId,
            CatTipoGastoId = g.CatTipoGastoId,
            TipoGastoNombre = g.TipoGasto?.Nombre,
            Descripcion = g.Descripcion,
            Monto = g.Monto,
            CentroCosto = g.CentroCosto,
            CuentaContable = g.CuentaContable,
            FechaCreacion = g.FechaCreacion
        }).ToList() ?? new();

        return new CompraExternaDto
        {
            Id = compra.Id,
            ProveedorId = compra.ProveedorId,
            ProveedorNIT = compra.Proveedor?.NIT ?? "",
            ProveedorNombre = compra.Proveedor?.Nombre ?? "",
            SucursalId = compra.SucursalId,
            SucursalNombre = compra.Sucursal?.Nombre ?? "",
            NumeroFactura = compra.NumeroFactura,
            FechaEmision = compra.FechaEmision,
            FechaRegistro = compra.FechaRegistro,
            Subtotal = compra.Subtotal,
            IVA = compra.IVA,
            Total = compra.Total,
            Estado = compra.Estado,
            FechaConfirmacion = compra.FechaConfirmacion,
            FechaAnulacion = compra.FechaAnulacion,
            Observaciones = compra.Observaciones,
            Detalles = detalles,
            Gastos = gastos,
            TotalProductosInventario = detalles.Where(d => d.EsParaInventario).Sum(d => d.Total),
            TotalGastosAdministrativos = gastos.Sum(g => g.Monto),
            CantidadItems = detalles.Count + gastos.Count
        };
    }
}