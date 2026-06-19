using AutoMapper;
using FraFactu.Application.DTOs.DtesRecibidos;
using FraFactu.Application.Interfaces;
using FraFactu.Application.Services;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Services;

/// <summary>
/// Servicio para gestionar DTEs recibidos
/// </summary>
public class DteRecibidoService : IDteRecibidoService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IDteIngestaService _ingesta;
    private readonly IDteParserService _dteParser;
    private readonly ILogger<DteRecibidoService> _logger;

    public DteRecibidoService(
        ApplicationDbContext context,
        IMapper mapper,
        IDteIngestaService ingesta,
        IDteParserService dteParser,
        ILogger<DteRecibidoService> logger)
    {
        _context = context;
        _mapper = mapper;
        _ingesta = ingesta;
        _dteParser = dteParser;
        _logger = logger;
    }

    public async Task<(List<DteRecibidoResumenDto> items, int total)> ListarAsync(
        int emisorId,
        int pagina = 1,
        int tamanoPagina = 20,
        string? estado = null,
        string? tipoDte = null,
        string? emisorNit = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        string? search = null,
        string? sortBy = null,
        bool sortDesc = false)
    {
        var query = AplicarFiltros(
            _context.DtesRecibidos.AsNoTracking(),
            emisorId, estado, tipoDte, emisorNit, fechaDesde, fechaHasta, search);

        var total = await query.CountAsync();

        // Ordenamiento
        query = sortBy?.ToLower() switch
        {
            "fechaemision" => sortDesc ? query.OrderByDescending(d => d.FechaEmision) : query.OrderBy(d => d.FechaEmision),
            "total" => sortDesc ? query.OrderByDescending(d => d.Total) : query.OrderBy(d => d.Total),
            "emisornombre" => sortDesc ? query.OrderByDescending(d => d.EmisorNombre) : query.OrderBy(d => d.EmisorNombre),
            "estado" => sortDesc ? query.OrderByDescending(d => d.Estado) : query.OrderBy(d => d.Estado),
            _ => query.OrderByDescending(d => d.FechaCreacion)
        };

        var items = await query
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        return (_mapper.Map<List<DteRecibidoResumenDto>>(items), total);
    }

    /// <summary>
    /// F5: filtros compartidos entre <see cref="ListarAsync"/> y
    /// <see cref="ObtenerEstadisticasAsync"/>. Antes la logica estaba duplicada;
    /// extraerla evita que las tarjetas de estadisticas se desincronicen con
    /// la tabla cuando se agregue/cambie un filtro nuevo.
    /// </summary>
    private static IQueryable<DteRecibido> AplicarFiltros(
        IQueryable<DteRecibido> query,
        int emisorId,
        string? estado,
        string? tipoDte,
        string? emisorNit,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        string? search)
    {
        query = query.Where(d => d.EmisorId == emisorId);

        if (!string.IsNullOrEmpty(estado)
            && Enum.TryParse<EstadoDteRecibido>(estado, ignoreCase: true, out var estadoFiltro))
            query = query.Where(d => d.Estado == estadoFiltro);

        if (!string.IsNullOrEmpty(tipoDte))
            query = query.Where(d => d.TipoDte == tipoDte);

        if (!string.IsNullOrEmpty(emisorNit))
            query = query.Where(d => d.EmisorNit == emisorNit);

        if (fechaDesde.HasValue)
        {
            var desdeUtc = DateTime.SpecifyKind(fechaDesde.Value.Date, DateTimeKind.Utc);
            query = query.Where(d => d.FechaEmision >= desdeUtc);
        }

        if (fechaHasta.HasValue)
        {
            var hastaUtc = DateTime.SpecifyKind(fechaHasta.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(d => d.FechaEmision <= hastaUtc);
        }

        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(d =>
                d.EmisorNombre.ToLower().Contains(searchLower) ||
                d.EmisorNit.ToLower().Contains(searchLower) ||
                d.CodigoGeneracion.ToLower().Contains(searchLower) ||
                (d.NumeroControl != null && d.NumeroControl.ToLower().Contains(searchLower)));
        }

        return query;
    }

    public async Task<DteRecibidoDto> ObtenerPorIdAsync(int id, int emisorId)
    {
        var dte = await _context.DtesRecibidos
            .FirstOrDefaultAsync(d => d.Id == id && d.EmisorId == emisorId)
            ?? throw new KeyNotFoundException($"DTE recibido con ID {id} no encontrado");

        return _mapper.Map<DteRecibidoDto>(dte);
    }

    public async Task DescartarAsync(int id, DescartarDteDto dto, int emisorId)
    {
        var dte = await _context.DtesRecibidos
            .FirstOrDefaultAsync(d => d.Id == id && d.EmisorId == emisorId)
            ?? throw new KeyNotFoundException($"DTE recibido con ID {id} no encontrado");

        if (dte.Estado == EstadoDteRecibido.VINCULADO)
            throw new InvalidOperationException("No se puede descartar un DTE que ya está vinculado a una compra");

        if (dte.Estado == EstadoDteRecibido.DESCARTADO)
            throw new InvalidOperationException("Este DTE ya fue descartado");

        dte.Estado = EstadoDteRecibido.DESCARTADO;
        dte.MotivoDescarte = dto.Motivo;
        dte.FechaActualizacion = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("DTE {CodigoGeneracion} descartado. Motivo: {Motivo}", dte.CodigoGeneracion, dto.Motivo);
    }

    public async Task<DteRecibidoEstadisticasDto> ObtenerEstadisticasAsync(
        int emisorId,
        string? estado = null,
        string? tipoDte = null,
        string? emisorNit = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        string? search = null)
    {
        // F5: las tarjetas respetan los mismos filtros que la tabla. Cuando se
        // omiten (caso anterior a F5) se obtiene el total global del emisor.
        var query = AplicarFiltros(
            _context.DtesRecibidos.AsNoTracking(),
            emisorId, estado, tipoDte, emisorNit, fechaDesde, fechaHasta, search);

        var dtes = await query
            .GroupBy(d => d.Estado)
            .Select(g => new { Estado = g.Key, Count = g.Count(), MontoTotal = g.Sum(d => d.Total) })
            .ToListAsync();

        var emisor = await _context.Emisores
            .AsNoTracking()
            .Where(e => e.Id == emisorId)
            .Select(e => e.UltimaLecturaCorreo)
            .FirstOrDefaultAsync();

        var pendientes = dtes.FirstOrDefault(d => d.Estado == EstadoDteRecibido.PENDIENTE);
        var vinculados = dtes.FirstOrDefault(d => d.Estado == EstadoDteRecibido.VINCULADO);
        var descartados = dtes.FirstOrDefault(d => d.Estado == EstadoDteRecibido.DESCARTADO);

        return new DteRecibidoEstadisticasDto
        {
            TotalPendientes = pendientes?.Count ?? 0,
            TotalVinculados = vinculados?.Count ?? 0,
            TotalDescartados = descartados?.Count ?? 0,
            Total = dtes.Sum(d => d.Count),
            MontoTotalPendientes = pendientes?.MontoTotal ?? 0,
            UltimaLectura = emisor
        };
    }

    public async Task<int> CrearCompraDesdeAsync(int dteRecibidoId, int emisorId, int sucursalId)
    {
        var dte = await _context.DtesRecibidos
            .FirstOrDefaultAsync(d => d.Id == dteRecibidoId && d.EmisorId == emisorId)
            ?? throw new KeyNotFoundException($"DTE recibido con ID {dteRecibidoId} no encontrado");

        if (dte.Estado == EstadoDteRecibido.VINCULADO)
            throw new InvalidOperationException("Este DTE ya está vinculado a una compra");

        if (dte.Estado == EstadoDteRecibido.DESCARTADO)
            throw new InvalidOperationException("No se puede crear una compra desde un DTE descartado");

        // Verificar que no exista una compra con el mismo código de generación DTE
        var compraExistente = await _context.ComprasExternas
            .AnyAsync(c => c.CodigoGeneracionDte == dte.CodigoGeneracion);

        if (compraExistente)
            throw new InvalidOperationException($"Ya existe una compra asociada al DTE con código {dte.CodigoGeneracion}");

        // F4: toda la operacion (proveedor + compra + gastos + vincular DTE) en
        // una unica transaccion. Antes habia 4 SaveChangesAsync sueltos: si la
        // creacion de gastos fallaba, la compra quedaba huerfana y el DTE no
        // vinculado. Ahora o se persiste todo o se rollback completo.
        var executionStrategy = _context.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync();

            // Buscar o crear proveedor
            var proveedor = await _context.Proveedores
                .FirstOrDefaultAsync(p => p.NIT == dte.EmisorNit && p.EmisorId == emisorId && p.Activo);

            if (proveedor == null)
            {
                proveedor = new Proveedor
                {
                    NIT = dte.EmisorNit,
                    Nombre = dte.EmisorNombre,
                    EmisorId = emisorId,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };
                _context.Proveedores.Add(proveedor);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Proveedor creado automáticamente desde DTE: {NIT} - {Nombre}", proveedor.NIT, proveedor.Nombre);
            }

            // Crear compra con trazabilidad DTE
            var compra = new CompraExterna
            {
                ProveedorId = proveedor.Id,
                SucursalId = sucursalId,
                NumeroFactura = dte.NumeroControl ?? dte.CodigoGeneracion[..20],
                FechaEmision = DateTime.SpecifyKind(dte.FechaEmision, DateTimeKind.Utc),
                FechaRegistro = DateTime.UtcNow,
                Subtotal = dte.SubTotal,
                IVA = dte.IVA,
                Total = dte.Total,
                Estado = "BORRADOR",
                Observaciones = $"Creada desde DTE {dte.TipoDte} | Código: {dte.CodigoGeneracion}",
                Origen = "DTE",
                CodigoGeneracionDte = dte.CodigoGeneracion,
                SelloRecibidoDte = dte.SelloRecibido,
                TipoDte = dte.TipoDte,
                NumeroControlDte = dte.NumeroControl,
                FechaCreacion = DateTime.UtcNow
            };

            _context.ComprasExternas.Add(compra);
            await _context.SaveChangesAsync();

            // Crear gastos administrativos desde los items del DTE. F4: si el
            // parseo o el guardado falla, la transaccion abortara: no queda
            // compra sin items.
            var dteParsed = _dteParser.ParsearDteJson(dte.JsonDte);
            foreach (var item in dteParsed.Items)
            {
                var monto = item.VentaGravada + item.VentaExenta + item.VentaNoSujeta;
                if (monto <= 0) monto = item.PrecioUnitario * item.Cantidad;

                var gasto = new GastoAdministrativo
                {
                    CompraExternaId = compra.Id,
                    Descripcion = $"{item.Descripcion} (x{item.Cantidad})",
                    Monto = monto,
                    FechaCreacion = DateTime.UtcNow
                };
                _context.GastosAdministrativos.Add(gasto);
            }

            // Vincular DTE
            dte.Estado = EstadoDteRecibido.VINCULADO;
            dte.CompraExternaId = compra.Id;
            dte.FechaActualizacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            _logger.LogInformation("Compra {CompraId} creada desde DTE {CodigoGeneracion}", compra.Id, dte.CodigoGeneracion);

            return compra.Id;
        });
    }

    public async Task<MapearDteCompraResponseDto> MapearYCrearCompraAsync(
        int dteRecibidoId, MapearDteCompraDto dto, int emisorId, int? cargadoPorUsuarioId = null)
    {
        // 1. Validaciones de estado del DTE (mismas que CrearCompraDesdeAsync).
        var dte = await _context.DtesRecibidos
            .FirstOrDefaultAsync(d => d.Id == dteRecibidoId && d.EmisorId == emisorId)
            ?? throw new KeyNotFoundException($"DTE recibido con ID {dteRecibidoId} no encontrado");

        if (dte.Estado == EstadoDteRecibido.VINCULADO)
            throw new InvalidOperationException("Este DTE ya está vinculado a una compra");

        if (dte.Estado == EstadoDteRecibido.DESCARTADO)
            throw new InvalidOperationException("No se puede crear una compra desde un DTE descartado");

        var compraExistente = await _context.ComprasExternas
            .AnyAsync(c => c.CodigoGeneracionDte == dte.CodigoGeneracion);
        if (compraExistente)
            throw new InvalidOperationException(
                $"Ya existe una compra asociada al DTE con código {dte.CodigoGeneracion}");

        // 2. Sucursal pertenece al emisor.
        var sucursal = await _context.Sucursales
            .FirstOrDefaultAsync(s => s.Id == dto.SucursalId && s.EmisorId == emisorId)
            ?? throw new KeyNotFoundException(
                $"Sucursal con ID {dto.SucursalId} no encontrada para el emisor");

        // 3. Productos existentes pertenecen al emisor.
        var productoIds = dto.Items
            .Where(i => i.Accion == AccionMapeoItem.ProductoExistente && i.ProductoId.HasValue)
            .Select(i => i.ProductoId!.Value)
            .Distinct()
            .ToList();
        if (productoIds.Count > 0)
        {
            var encontrados = await _context.ProductosServicios
                .Where(p => productoIds.Contains(p.Id) && p.EmisorId == emisorId)
                .Select(p => p.Id)
                .ToListAsync();
            var faltantes = productoIds.Except(encontrados).ToList();
            if (faltantes.Count > 0)
                throw new KeyNotFoundException(
                    $"Productos no encontrados o no pertenecen al emisor: {string.Join(", ", faltantes)}");
        }

        // 4. Bodegas pertenecen a la sucursal seleccionada.
        var bodegaIds = dto.Items
            .Where(i => i.Accion != AccionMapeoItem.Gasto && i.BodegaId.HasValue)
            .Select(i => i.BodegaId!.Value)
            .Distinct()
            .ToList();
        if (bodegaIds.Count > 0)
        {
            var bodegasValidas = await _context.Bodegas
                .Where(b => bodegaIds.Contains(b.Id) && b.SucursalId == dto.SucursalId)
                .CountAsync();
            if (bodegasValidas != bodegaIds.Count)
                throw new InvalidOperationException(
                    "Una o más bodegas no pertenecen a la sucursal seleccionada");
        }

        // 5. Codigos de productos nuevos no colisionan con catalogo.
        //    Solo validamos los que el usuario ingreso manualmente; los vacios se
        //    autogeneran (PROD-/SERV-) al crear el producto y no pueden colisionar.
        var codigosNuevos = dto.Items
            .Where(i => i.Accion == AccionMapeoItem.ProductoNuevo && i.ProductoNuevo != null
                && !string.IsNullOrWhiteSpace(i.ProductoNuevo!.Codigo))
            .Select(i => i.ProductoNuevo!.Codigo.Trim())
            .ToList();
        if (codigosNuevos.Count > 0)
        {
            // Duplicados dentro del propio payload.
            var duplicadosInternos = codigosNuevos
                .GroupBy(c => c, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicadosInternos.Count > 0)
                throw new InvalidOperationException(
                    $"Códigos de producto nuevo duplicados en el payload: {string.Join(", ", duplicadosInternos)}");

            var yaExisten = await _context.ProductosServicios
                .Where(p => p.EmisorId == emisorId && codigosNuevos.Contains(p.Codigo))
                .Select(p => p.Codigo)
                .ToListAsync();
            if (yaExisten.Count > 0)
                throw new InvalidOperationException(
                    $"Los códigos ya existen en el catálogo: {string.Join(", ", yaExisten)}");
        }

        // 6. Totales mapeados deben cuadrar con el DTE (tolerancia 0.01).
        var totalMapeado = dto.Items.Sum(i => CalcularTotalItemMapeado(i, dte));
        if (Math.Abs(totalMapeado - dte.Total) > 0.01m)
            throw new InvalidOperationException(
                $"El total mapeado ({totalMapeado:F2}) no coincide con el total del DTE ({dte.Total:F2}). " +
                $"Diferencia: {totalMapeado - dte.Total:F2}. Ajuste cantidades y costos antes de crear la compra.");

        // 7. Persistir todo en una transaccion (proveedor + compra + items + DTE).
        var executionStrategy = _context.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync();

            var proveedor = await ObtenerOCrearProveedorAsync(dte, emisorId);
            var compra = CrearCompraDesdeDte(dte, proveedor.Id, dto.SucursalId);
            _context.ComprasExternas.Add(compra);
            await _context.SaveChangesAsync();

            var itemsProductos = 0;
            var itemsGastos = 0;
            var productosCreados = 0;

            foreach (var item in dto.Items)
            {
                if (item.Accion == AccionMapeoItem.Gasto)
                {
                    _context.GastosAdministrativos.Add(new GastoAdministrativo
                    {
                        CompraExternaId = compra.Id,
                        Descripcion = item.DescripcionDte,
                        Monto = item.MontoDte,
                        CatTipoGastoId = item.CatTipoGastoId,
                        FechaCreacion = DateTime.UtcNow
                    });
                    itemsGastos++;
                    continue;
                }

                int productoId;
                if (item.Accion == AccionMapeoItem.ProductoNuevo)
                {
                    var tipoInventario = (Domain.Enums.TipoInventario)item.ProductoNuevo!.TipoInventario;

                    // Autogenera PROD-/SERV- cuando el wizard no envia codigo (mismo
                    // formato que el flujo normal de productos). Si el usuario ingreso
                    // un SKU manual se respeta. El SaveChanges por iteracion garantiza
                    // que el correlativo del siguiente producto nuevo sea correcto.
                    var codigoProducto = string.IsNullOrWhiteSpace(item.ProductoNuevo.Codigo)
                        ? await ProductoCodigoGenerator.GenerarSiguienteCodigoAsync(
                            _context, emisorId, item.ProductoNuevo.CatTipoItemId)
                        : item.ProductoNuevo.Codigo.Trim();

                    var nuevo = new ProductoServicio
                    {
                        Codigo = codigoProducto,
                        Nombre = item.ProductoNuevo.Nombre.Trim(),
                        Descripcion = item.DescripcionDte,
                        CatTipoItemId = item.ProductoNuevo.CatTipoItemId,
                        CatUnidadMedidaId = item.ProductoNuevo.CatUnidadMedidaId,
                        CategoriaId = item.ProductoNuevo.CategoriaId,
                        EmisorId = emisorId,
                        PrecioVenta = 0m,
                        PrecioCosto = item.CostoUnitario,
                        AccesoTodasSucursales = true,
                        TipoInventario = tipoInventario,
                        FechaCreacion = DateTime.UtcNow
                    };

                    // F2: para activos fijos, sembrar los datos contables a partir
                    // de la compra. El usuario puede ajustarlos despues editando el
                    // producto. Vida util y residual quedan en lo que mando el wizard
                    // (suele ser null hasta que el usuario complete).
                    if (tipoInventario == Domain.Enums.TipoInventario.MobiliarioEquipo)
                    {
                        nuevo.FechaAdquisicion = DateTime.SpecifyKind(dte.FechaEmision, DateTimeKind.Utc);
                        nuevo.ValorActual = item.CostoUnitario;
                        nuevo.AniosVidaUtil = item.ProductoNuevo.AniosVidaUtil;
                        nuevo.ValorResidual = item.ProductoNuevo.ValorResidual;
                    }

                    _context.ProductosServicios.Add(nuevo);
                    await _context.SaveChangesAsync();
                    productoId = nuevo.Id;
                    productosCreados++;
                }
                else
                {
                    productoId = item.ProductoId!.Value;
                }

                var subtotal = item.Cantidad!.Value * item.CostoUnitario!.Value;
                var iva = ProrratearIva(subtotal, dte);

                _context.CompraExternaDetalles.Add(new CompraExternaDetalle
                {
                    CompraExternaId = compra.Id,
                    ProductoId = productoId,
                    BodegaId = item.BodegaId!.Value,
                    Cantidad = item.Cantidad.Value,
                    CostoUnitario = item.CostoUnitario.Value,
                    Subtotal = subtotal,
                    IVA = iva,
                    Total = subtotal + iva,
                    EsParaInventario = true,
                    FechaCreacion = DateTime.UtcNow
                });
                itemsProductos++;
            }

            dte.Estado = EstadoDteRecibido.VINCULADO;
            dte.CompraExternaId = compra.Id;
            dte.FechaActualizacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            _logger.LogInformation(
                "[MapearYCrearCompra] Compra {CompraId} creada desde DTE {CodigoGen} con {Prod} productos ({Nuevos} nuevos) y {Gastos} gastos (usuario {UsuarioId})",
                compra.Id, dte.CodigoGeneracion, itemsProductos, productosCreados, itemsGastos, cargadoPorUsuarioId);

            return new MapearDteCompraResponseDto
            {
                CompraId = compra.Id,
                TotalMapeado = totalMapeado,
                TotalDte = dte.Total,
                ItemsProductos = itemsProductos,
                ItemsGastos = itemsGastos,
                ProductosCreados = productosCreados
            };
        });
    }

    /// <summary>
    /// Calcula el total de un item del wizard tal como se persistira: para
    /// productos es Cantidad x CostoUnitario + IVA prorrateado; para gastos
    /// es el monto declarado por el usuario.
    /// </summary>
    private static decimal CalcularTotalItemMapeado(MapearItemDto item, DteRecibido dte)
    {
        if (item.Accion == AccionMapeoItem.Gasto)
            return item.MontoDte;

        var subtotal = (item.Cantidad ?? 0m) * (item.CostoUnitario ?? 0m);
        return subtotal + ProrratearIva(subtotal, dte);
    }

    /// <summary>
    /// Prorratea el IVA del DTE entre los items en proporcion a su subtotal.
    /// Si el DTE no trae base gravable (SubTotal=0) no se aplica IVA al item.
    /// </summary>
    private static decimal ProrratearIva(decimal subtotal, DteRecibido dte)
    {
        if (dte.SubTotal <= 0m || dte.IVA <= 0m || subtotal <= 0m)
            return 0m;
        return Math.Round(subtotal * (dte.IVA / dte.SubTotal), 2);
    }

    /// <summary>
    /// Reutiliza el proveedor existente por NIT o lo crea al vuelo. Mismo
    /// criterio que <see cref="CrearCompraDesdeAsync"/>.
    /// </summary>
    private async Task<Proveedor> ObtenerOCrearProveedorAsync(DteRecibido dte, int emisorId)
    {
        var proveedor = await _context.Proveedores
            .FirstOrDefaultAsync(p => p.NIT == dte.EmisorNit && p.EmisorId == emisorId && p.Activo);

        if (proveedor != null) return proveedor;

        proveedor = new Proveedor
        {
            NIT = dte.EmisorNit,
            Nombre = dte.EmisorNombre,
            EmisorId = emisorId,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _context.Proveedores.Add(proveedor);
        await _context.SaveChangesAsync();
        _logger.LogInformation(
            "Proveedor creado automáticamente desde DTE: {NIT} - {Nombre}",
            proveedor.NIT, proveedor.Nombre);
        return proveedor;
    }

    /// <summary>
    /// Construye el header de la compra (BORRADOR) con la trazabilidad del DTE.
    /// </summary>
    private static CompraExterna CrearCompraDesdeDte(DteRecibido dte, int proveedorId, int sucursalId)
        => new()
        {
            ProveedorId = proveedorId,
            SucursalId = sucursalId,
            NumeroFactura = dte.NumeroControl ?? dte.CodigoGeneracion[..Math.Min(20, dte.CodigoGeneracion.Length)],
            FechaEmision = DateTime.SpecifyKind(dte.FechaEmision, DateTimeKind.Utc),
            FechaRegistro = DateTime.UtcNow,
            Subtotal = dte.SubTotal,
            IVA = dte.IVA,
            Total = dte.Total,
            Estado = "BORRADOR",
            Observaciones = $"Creada desde DTE {dte.TipoDte} | Código: {dte.CodigoGeneracion}",
            Origen = "DTE",
            CodigoGeneracionDte = dte.CodigoGeneracion,
            SelloRecibidoDte = dte.SelloRecibido,
            TipoDte = dte.TipoDte,
            NumeroControlDte = dte.NumeroControl,
            FechaCreacion = DateTime.UtcNow
        };

    public async Task VincularACompraAsync(int dteRecibidoId, int compraId, int emisorId)
    {
        var dte = await _context.DtesRecibidos
            .FirstOrDefaultAsync(d => d.Id == dteRecibidoId && d.EmisorId == emisorId)
            ?? throw new KeyNotFoundException($"DTE recibido con ID {dteRecibidoId} no encontrado");

        if (dte.Estado == EstadoDteRecibido.VINCULADO)
            throw new InvalidOperationException("Este DTE ya está vinculado a una compra");

        var compra = await _context.ComprasExternas
            .Include(c => c.Proveedor)
            .FirstOrDefaultAsync(c => c.Id == compraId && c.Proveedor.EmisorId == emisorId)
            ?? throw new KeyNotFoundException($"Compra con ID {compraId} no encontrada");

        dte.Estado = EstadoDteRecibido.VINCULADO;
        dte.CompraExternaId = compraId;
        dte.FechaActualizacion = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("DTE {CodigoGeneracion} vinculado a compra {CompraId}", dte.CodigoGeneracion, compraId);
    }

    public async Task<CargaMasivaDtesResponseDto> CargarDtesManualmenteAsync(
        int emisorId,
        IEnumerable<ArchivoAIngestar> archivos,
        int? cargadoPorUsuarioId = null,
        CancellationToken ct = default)
    {
        var emisor = await _context.Emisores
            .FirstOrDefaultAsync(e => e.Id == emisorId, ct)
            ?? throw new KeyNotFoundException($"Emisor con ID {emisorId} no encontrado");

        var response = new CargaMasivaDtesResponseDto();

        foreach (var archivo in archivos)
        {
            response.TotalArchivos++;
            try
            {
                var ingesta = await _ingesta.IngestarAsync(archivo.Contenido, new IngestaContexto
                {
                    EmisorId = emisorId,
                    EmisorNit = emisor.Nit,
                    Fuente = FuenteRecepcionDte.CARGA_MANUAL,
                    CargadoPorUsuarioId = cargadoPorUsuarioId,
                }, ct);

                response.Resultados.Add(new CargaArchivoResultadoDto
                {
                    Archivo = archivo.NombreArchivo,
                    Resultado = ingesta.Resultado.ToString(),
                    CodigoGeneracion = ingesta.CodigoGeneracion,
                    Mensaje = ingesta.Mensaje
                });

                if (ingesta.Resultado == ResultadoIngestaDte.Cargado) response.Cargados++;
                else if (ingesta.Resultado == ResultadoIngestaDte.Duplicado) response.Duplicados++;
                else response.Rechazados++;
            }
            catch (Exception ex)
            {
                // El pipeline no debería lanzar — esto es defensa por si llega
                // un archivo con un problema inesperado (encoding, etc.).
                _logger.LogWarning(ex,
                    "[CargaManual] Error inesperado procesando archivo {Archivo} (emisor {EmisorId})",
                    archivo.NombreArchivo, emisorId);
                response.Resultados.Add(new CargaArchivoResultadoDto
                {
                    Archivo = archivo.NombreArchivo,
                    Resultado = "ErrorLectura",
                    Mensaje = $"No se pudo procesar el archivo: {ex.Message}"
                });
                response.Rechazados++;
            }
        }

        _logger.LogInformation(
            "[CargaManual] Emisor {EmisorId}: {Cargados} cargados, {Duplicados} duplicados, {Rechazados} rechazados de {Total} archivos",
            emisorId, response.Cargados, response.Duplicados, response.Rechazados, response.TotalArchivos);

        return response;
    }
}
