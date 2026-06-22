using Microsoft.EntityFrameworkCore;
using FraFactu.Application.Common.Interfaces;
using FraFactu.Application.Interfaces;
using Microsoft.Extensions.Logging;
using FraFactu.Application.Services;
using FraFactu.Application.DTOs.Inventario;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;

namespace FraFactu.Infrastructure.Services;

public class InventarioIntegrationService : IInventarioIntegrationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InventarioIntegrationService> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITelemetryService _telemetry;

    // Usar UTC para almacenar en PostgreSQL (Azure usa UTC)
    private static DateTime GetUtcNow() => DateTime.UtcNow;

    /// <summary>
    /// Lee el stock de un producto en una bodega con bloqueo pesimista (SELECT ... FOR UPDATE).
    /// La fila queda bloqueada hasta que la transacción actual haga commit/rollback.
    /// REQUIERE estar dentro de una transacción explícita.
    ///
    /// Implementación: hace 2 pasos por una limitación de EF Core + Postgres.
    /// Combinar <c>FromSqlInterpolated(... FOR UPDATE)</c> con <c>.Include(...)</c> obliga
    /// a EF Core a envolver el SQL en una derived table (<c>FROM (...) AS f</c>) para
    /// agregar los JOINs de las navegaciones; pero como la entidad <c>StockBodega</c>
    /// tiene la propiedad <c>xmin</c> mapeada como concurrency token, EF Core agrega
    /// <c>f.xmin</c> al SELECT externo, y las derived tables de Postgres NO exponen
    /// columnas de sistema (<c>42703: column f.xmin does not exist</c>).
    ///
    /// Solución: primero adquirimos el lock pesimista con un <c>SELECT 1 ... FOR UPDATE</c>
    /// vía <c>ExecuteSqlInterpolatedAsync</c> (no materializa entity, no envuelve en
    /// derived table). Luego leemos la fila ya bloqueada con un query EF Core normal
    /// que sí puede incluir las navegaciones y el xmin sin romper. Dentro de la misma
    /// transacción, Postgres garantiza que la lectura ve la fila ya lockeada.
    /// </summary>
    protected virtual async Task<StockBodega?> ObtenerStockConBloqueoAsync(int productoId, int bodegaId)
    {
        // 1. Adquirir lock pesimista sin materializar entity (evita el wrap en derived table).
        //    Si la fila no existe, este SELECT 1 no devuelve nada y no lockea — el caller
        //    debe manejar el null que devuelve el read de abajo (mismo comportamiento que antes).
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $@"SELECT 1 FROM stock_bodegas
               WHERE ""ProductoId"" = {productoId}
               AND ""BodegaId"" = {bodegaId}
               FOR UPDATE");

        // 2. Leer la entity ya bloqueada con EF Core normal (Includes + xmin tracking funcionan).
        return await _context.StocksBodega
            .Include(s => s.Producto)
            .Include(s => s.Bodega)
            .FirstOrDefaultAsync(s => s.ProductoId == productoId && s.BodegaId == bodegaId);
    }

    /// <summary>
    /// Adquiere un bloqueo pesimista sobre una fila de factura usando SELECT ... FOR UPDATE.
    /// En tests con InMemory se puede omitir (override con no-op).
    /// </summary>
    protected virtual async Task BloquearFacturaAsync(int facturaId)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $@"SELECT 1 FROM ""Facturas"" WHERE ""Id"" = {facturaId} FOR UPDATE");
    }

    /// <summary>
    /// Adquiere un bloqueo pesimista sobre una fila de invalidación usando SELECT ... FOR UPDATE.
    /// </summary>
    protected virtual async Task BloquearInvalidacionAsync(int invalidacionId)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $@"SELECT 1 FROM invalidaciones WHERE ""Id"" = {invalidacionId} FOR UPDATE");
    }

    /// <summary>
    /// Ejecuta una operación con reintentos automáticos ante conflictos de concurrencia (DbUpdateConcurrencyException).
    /// Si el token xmin detecta que otra transacción modificó la fila, limpia el change tracker y reintenta desde cero.
    /// Con transacción externa activa, no reintenta (el caller maneja los conflictos).
    /// </summary>
    private async Task EjecutarConReintentoAsync(Func<Task> operacion, int maxReintentos = 3)
    {
        var reintentos = _context.Database.CurrentTransaction != null ? 1 : maxReintentos;

        for (int intento = 0; intento < reintentos; intento++)
        {
            try
            {
                await operacion();
                return;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(
                    "[CONCURRENCIA] Conflicto detectado, intento {Intento}/{Max}. Entidades afectadas: {Count}",
                    intento + 1, reintentos, ex.Entries.Count);

                if (intento == reintentos - 1)
                    throw;

                // Limpiar change tracker para reiniciar con datos frescos en el siguiente intento
                _context.ChangeTracker.Clear();
            }
        }
    }

    /// <summary>
    /// Versión genérica para operaciones que retornan un valor.
    /// </summary>
    private async Task<T> EjecutarConReintentoAsync<T>(Func<Task<T>> operacion, int maxReintentos = 3)
    {
        var reintentos = _context.Database.CurrentTransaction != null ? 1 : maxReintentos;

        for (int intento = 0; intento < reintentos; intento++)
        {
            try
            {
                return await operacion();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(
                    "[CONCURRENCIA] Conflicto detectado, intento {Intento}/{Max}. Entidades afectadas: {Count}",
                    intento + 1, reintentos, ex.Entries.Count);

                if (intento == reintentos - 1)
                    throw;

                _context.ChangeTracker.Clear();
            }
        }

        throw new InvalidOperationException("Se agotaron los reintentos de concurrencia");
    }

    public InventarioIntegrationService(
        ApplicationDbContext context,
        ILogger<InventarioIntegrationService> logger,
        ICurrentUserService currentUserService,
        ITelemetryService telemetry)
    {
        _context = context;
        _logger = logger;
        _currentUserService = currentUserService;
        _telemetry = telemetry;
    }

    /// <summary>
    /// Valida que haya stock disponible para todos los productos de una factura.
    /// IMPORTANTE: Requiere una transacción externa activa para que los locks FOR UPDATE persistan.
    /// Sin transacción, los locks se liberan inmediatamente al retornar cada fila.
    /// </summary>
    public async Task<bool> ValidarStockDisponibleAsync(int facturaId)
    {
        var factura = await _context.Facturas
            .Include(f => f.Detalles)
            .FirstOrDefaultAsync(f => f.Id == facturaId);

        if (factura == null)
            throw new KeyNotFoundException($"Factura con ID {facturaId} no encontrada");

        // Ordenar por (ProductoId, BodegaId) para adquirir locks en orden consistente y evitar deadlocks
        var detallesOrdenados = factura.Detalles
            .Where(d => d.ProductoId.HasValue && d.BodegaId.HasValue)
            .OrderBy(d => d.ProductoId).ThenBy(d => d.BodegaId)
            .ToList();

        foreach (var detalle in detallesOrdenados)
        {
            // Usar FOR UPDATE para bloquear la fila durante la validación
            var stock = await ObtenerStockConBloqueoAsync(detalle.ProductoId!.Value, detalle.BodegaId!.Value);

            if (stock == null || stock.CantidadDisponible < detalle.Cantidad)
            {
                _logger.LogWarning("[INVENTARIO-VALIDACION] Stock insuficiente - ProductoId={ProductoId}, BodegaId={BodegaId}, Requerido={Requerido}, Disponible={Disponible}",
                    detalle.ProductoId, detalle.BodegaId, detalle.Cantidad, stock?.CantidadDisponible ?? 0);

                _telemetry.TrackEvent("smartix.inventario.consultado",
                    properties: new Dictionary<string, string>
                    {
                        ["facturaId"] = facturaId.ToString(),
                        ["resultado"] = "stock_insuficiente",
                        ["productoId"] = detalle.ProductoId?.ToString() ?? string.Empty,
                        ["bodegaId"] = detalle.BodegaId?.ToString() ?? string.Empty
                    },
                    measurements: new Dictionary<string, double>
                    {
                        ["cantidadRequerida"] = (double)detalle.Cantidad,
                        ["cantidadDisponible"] = (double)(stock?.CantidadDisponible ?? 0)
                    });
                return false;
            }
        }

        _logger.LogDebug("[INVENTARIO-VALIDACION] Stock disponible para todos los productos de factura {FacturaId}", facturaId);

        _telemetry.TrackEvent("smartix.inventario.consultado",
            properties: new Dictionary<string, string>
            {
                ["facturaId"] = facturaId.ToString(),
                ["resultado"] = "stock_disponible",
                ["detallesValidados"] = detallesOrdenados.Count.ToString()
            });

        return true;
    }

    public async Task ReservarStockAsync(int facturaId) => await EjecutarConReintentoAsync(async () =>
    {
        var isExternalTransaction = _context.Database.CurrentTransaction != null;
        var transaction = isExternalTransaction ? null : await _context.Database.BeginTransactionAsync();

        try
        {
            var factura = await _context.Facturas
                .Include(f => f.Detalles)
                .FirstOrDefaultAsync(f => f.Id == facturaId);

            if (factura == null)
                throw new KeyNotFoundException($"Factura con ID {facturaId} no encontrada");

            // Ordenar por (ProductoId, BodegaId) para adquirir locks en orden consistente y evitar deadlocks
            var detallesOrdenados = factura.Detalles
                .Where(d => d.ProductoId.HasValue && d.BodegaId.HasValue)
                .OrderBy(d => d.ProductoId).ThenBy(d => d.BodegaId)
                .ToList();

            foreach (var detalle in detallesOrdenados)
            {

                var stock = await ObtenerStockConBloqueoAsync(detalle.ProductoId!.Value, detalle.BodegaId!.Value);

                if (stock == null)
                    throw new InvalidOperationException(
                        $"No existe stock para el producto {detalle.ProductoId} en la bodega {detalle.BodegaId}");

                if (stock.CantidadDisponible < detalle.Cantidad)
                    throw new InvalidOperationException(
                        $"Stock insuficiente. Disponible: {stock.CantidadDisponible}, Requerido: {detalle.Cantidad}");

                // Reservar stock
                stock.CantidadDisponible -= detalle.Cantidad;
                stock.CantidadReservada += detalle.Cantidad;

                _logger.LogDebug("[INVENTARIO-RESERVA] ProductoId={ProductoId}, BodegaId={BodegaId}: Reservado={Cantidad}, NuevoDisponible={Disponible}, NuevoReservado={Reservado}",
                    detalle.ProductoId, detalle.BodegaId, detalle.Cantidad, stock.CantidadDisponible, stock.CantidadReservada);
            }

            await _context.SaveChangesAsync();

            if (transaction != null)
                await transaction.CommitAsync();

            _logger.LogInformation("[INVENTARIO-RESERVA] Stock reservado exitosamente para factura {FacturaId}", facturaId);
        }
        catch
        {
            if (transaction != null)
                await transaction.RollbackAsync();
            throw;
        }
    });

    public async Task ConfirmarVentaAsync(int facturaId) => await EjecutarConReintentoAsync(async () =>
    {
        var isExternalTransaction = _context.Database.CurrentTransaction != null;
        var transaction = isExternalTransaction ? null : await _context.Database.BeginTransactionAsync();

        try
        {
            // Bloquear la fila de la factura para serializar operaciones concurrentes sobre la misma factura
            await BloquearFacturaAsync(facturaId);

            // Verificar idempotencia: si ya existen movimientos para esta factura, no duplicar
            var yaExistenMovimientos = await _context.MovimientosInventario
                .AnyAsync(m => m.TipoDocumento == "FACTURA_EMITIDA" && m.DocumentoId == facturaId);

            if (yaExistenMovimientos)
            {
                _logger.LogDebug("[INVENTARIO-CONFIRMACION] Ya existen movimientos para factura {FacturaId}, omitiendo", facturaId);

                if (transaction != null)
                    await transaction.CommitAsync();
                return;
            }

            var factura = await _context.Facturas
                .Include(f => f.Detalles)
                .ThenInclude(d => d.Producto)
                .Include(f => f.Detalles)
                .ThenInclude(d => d.Bodega)
                .FirstOrDefaultAsync(f => f.Id == facturaId);

            if (factura == null)
                throw new KeyNotFoundException($"Factura con ID {facturaId} no encontrada");

            // Ordenar por (ProductoId, BodegaId) para adquirir locks en orden consistente y evitar deadlocks
            var detallesOrdenados = factura.Detalles
                .Where(d => d.ProductoId.HasValue && d.BodegaId.HasValue)
                .OrderBy(d => d.ProductoId).ThenBy(d => d.BodegaId)
                .ToList();

            foreach (var detalle in detallesOrdenados)
            {

                var stock = await ObtenerStockConBloqueoAsync(detalle.ProductoId!.Value, detalle.BodegaId!.Value);

                if (stock == null)
                    throw new InvalidOperationException($"Stock no encontrado para producto {detalle.ProductoId}");

                // Capturar saldo total ANTES de modificar para auditoría exacta
                var saldoAnterior = stock.CantidadTotal;

                // Convertir de reservado a salida (evitar valores negativos)
                stock.CantidadReservada = Math.Max(0, stock.CantidadReservada - detalle.Cantidad);

                // Registrar movimiento de inventario
                var movimiento = new MovimientoInventario
                {
                    ProductoId = detalle.ProductoId!.Value,
                    BodegaId = detalle.BodegaId!.Value,
                    TipoMovimiento = "SALIDA",
                    Cantidad = -detalle.Cantidad, // Negativo porque es salida
                    CostoUnitario = detalle.CostoUnitario,
                    TipoDocumento = "FACTURA_EMITIDA",
                    DocumentoId = facturaId,
                    NumeroDocumento = factura.NumeroControl,
                    FechaMovimiento = GetUtcNow(),
                    Observaciones = $"Venta confirmada - MH aprobado",
                    SaldoAnterior = saldoAnterior,
                    NuevoSaldo = stock.CantidadTotal,
                    CostoPromedioAnterior = stock.CostoPromedio,
                    NuevoCostoPromedio = stock.CostoPromedio,
                    UsuarioId = factura.UsuarioId
                };

                _context.MovimientosInventario.Add(movimiento);

                _logger.LogDebug("[INVENTARIO-CONFIRMACION] ProductoId={ProductoId}, BodegaId={BodegaId}: Venta confirmada, Cantidad={Cantidad}, CostoUnitario={CostoUnitario}",
                    detalle.ProductoId, detalle.BodegaId, detalle.Cantidad, detalle.CostoUnitario);
            }

            await _context.SaveChangesAsync();

            if (transaction != null)
                await transaction.CommitAsync();

            _logger.LogInformation("[INVENTARIO-CONFIRMACION] Venta confirmada para factura {FacturaId}", facturaId);
        }
        catch
        {
            if (transaction != null)
                await transaction.RollbackAsync();
            throw;
        }
    });

    public async Task LiberarReservasAsync(int facturaId) => await EjecutarConReintentoAsync(async () =>
    {
        var isExternalTransaction = _context.Database.CurrentTransaction != null;
        var transaction = isExternalTransaction ? null : await _context.Database.BeginTransactionAsync();

        try
        {
            var factura = await _context.Facturas
                .Include(f => f.Detalles)
                .FirstOrDefaultAsync(f => f.Id == facturaId);

            if (factura == null)
                throw new KeyNotFoundException($"Factura con ID {facturaId} no encontrada");

            // Ordenar por (ProductoId, BodegaId) para adquirir locks en orden consistente y evitar deadlocks
            var detallesOrdenados = factura.Detalles
                .Where(d => d.ProductoId.HasValue && d.BodegaId.HasValue)
                .OrderBy(d => d.ProductoId).ThenBy(d => d.BodegaId)
                .ToList();

            foreach (var detalle in detallesOrdenados)
            {

                var stock = await ObtenerStockConBloqueoAsync(detalle.ProductoId!.Value, detalle.BodegaId!.Value);

                if (stock == null)
                    continue; // Log warning pero no fallar

                // Liberar reserva (evitar valores negativos)
                var cantidadALiberar = Math.Min(detalle.Cantidad, stock.CantidadReservada);
                stock.CantidadReservada -= cantidadALiberar;
                stock.CantidadDisponible += cantidadALiberar;

                _logger.LogDebug("[INVENTARIO-LIBERACION] ProductoId={ProductoId}, BodegaId={BodegaId}: Liberado={Cantidad}, NuevoDisponible={Disponible}",
                    detalle.ProductoId, detalle.BodegaId, detalle.Cantidad, stock.CantidadDisponible);
            }

            await _context.SaveChangesAsync();

            if (transaction != null)
                await transaction.CommitAsync();

            _logger.LogInformation("[INVENTARIO-LIBERACION] Reservas liberadas para factura {FacturaId}", facturaId);
        }
        catch
        {
            if (transaction != null)
                await transaction.RollbackAsync();
            throw;
        }
    });

    public async Task RevertirVentaAsync(int invalidacionId) => await EjecutarConReintentoAsync(async () =>
    {
        var isExternalTransaction = _context.Database.CurrentTransaction != null;
        var transaction = isExternalTransaction ? null : await _context.Database.BeginTransactionAsync();

        try
        {
            // Bloquear la fila de invalidación para serializar reversiones concurrentes
            await BloquearInvalidacionAsync(invalidacionId);

            // Verificar idempotencia: si ya existen movimientos de reversión para esta invalidación, no duplicar
            var yaExistenMovimientos = await _context.MovimientosInventario
                .AnyAsync(m => m.DocumentoId == invalidacionId &&
                          m.TipoDocumento != null && m.TipoDocumento.StartsWith("DEVOLUCION"));

            if (yaExistenMovimientos)
            {
                _logger.LogDebug("[INVENTARIO-REVERSION] Ya existen movimientos de reversión para invalidación {InvalidacionId}, omitiendo", invalidacionId);

                if (transaction != null)
                    await transaction.CommitAsync();
                return;
            }

            var invalidacion = await _context.Invalidaciones
                .Include(i => i.FacturaElectronica)
                    .ThenInclude(f => f.Detalles)
                .FirstOrDefaultAsync(i => i.Id == invalidacionId);

            if (invalidacion == null)
                throw new KeyNotFoundException($"Invalidación con ID {invalidacionId} no encontrada");

            // Verificar si debe revertir inventario
            if (!invalidacion.RevirtiInventario)
            {
                if (transaction != null)
                    await transaction.CommitAsync();
                return;
            }

            var factura = invalidacion.FacturaElectronica;

            // Ordenar por (ProductoId, BodegaId) para adquirir locks en orden consistente y evitar deadlocks
            var detallesOrdenados = factura.Detalles
                .Where(d => d.ProductoId.HasValue && d.BodegaId.HasValue)
                .OrderBy(d => d.ProductoId).ThenBy(d => d.BodegaId)
                .ToList();

            foreach (var detalle in detallesOrdenados)
            {

                var stock = await ObtenerStockConBloqueoAsync(detalle.ProductoId!.Value, detalle.BodegaId!.Value);

                if (stock == null)
                    throw new InvalidOperationException($"Stock no encontrado para producto {detalle.ProductoId}");

                // Devolver stock
                stock.CantidadDisponible += detalle.Cantidad;

                // Registrar movimiento de devolución
                var movimiento = new MovimientoInventario
                {
                    ProductoId = detalle.ProductoId!.Value,
                    BodegaId = detalle.BodegaId!.Value,
                    TipoMovimiento = "ENTRADA",
                    Cantidad = detalle.Cantidad, // Positivo porque es entrada
                    CostoUnitario = detalle.CostoUnitario,
                    TipoDocumento = GetTipoDocumentoInvalidacion(invalidacion.TipoInvalidacion),
                    DocumentoId = invalidacionId,
                    NumeroDocumento = factura.NumeroControl,
                    FechaMovimiento = GetUtcNow(),
                    Observaciones = $"Devolución por invalidación - Tipo: {invalidacion.TipoInvalidacion}. Motivo: {invalidacion.MotivoAnulacion}",
                    SaldoAnterior = stock.CantidadTotal - detalle.Cantidad,
                    NuevoSaldo = stock.CantidadTotal,
                    CostoPromedioAnterior = stock.CostoPromedio,
                    NuevoCostoPromedio = stock.CostoPromedio,
                    UsuarioId = _currentUserService.GetUsuarioId()
                };

                _context.MovimientosInventario.Add(movimiento);

                _logger.LogDebug("[INVENTARIO-REVERSION] ProductoId={ProductoId}, BodegaId={BodegaId}: Devolución por invalidación, Cantidad={Cantidad}",
                    detalle.ProductoId, detalle.BodegaId, detalle.Cantidad);
            }

            await _context.SaveChangesAsync();

            if (transaction != null)
                await transaction.CommitAsync();

            _logger.LogInformation("[INVENTARIO-REVERSION] Inventario revertido para invalidación {InvalidacionId}", invalidacionId);
        }
        catch
        {
            if (transaction != null)
                await transaction.RollbackAsync();
            throw;
        }
    });

    private string GetTipoDocumentoInvalidacion(string? tipoInvalidacion)
    {
        return tipoInvalidacion switch
        {
            "ERROR_FACTURA" => "DEVOLUCION_ERROR_FACTURA",
            "DEVOLUCION_SIMPLE" => "DEVOLUCION_CLIENTE",
            "CAMBIO_PRODUCTO" => "DEVOLUCION_CAMBIO",
            _ => "DEVOLUCION_INVALIDACION"
        };
    }

    public async Task RevertirStockFacturaAsync(int facturaId) => await EjecutarConReintentoAsync(async () =>
    {
        var isExternalTransaction = _context.Database.CurrentTransaction != null;
        var transaction = isExternalTransaction ? null : await _context.Database.BeginTransactionAsync();

        try
        {
            // Bloquear la fila de la factura para serializar reversiones concurrentes
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $@"SELECT 1 FROM ""Facturas"" WHERE ""Id"" = {facturaId} FOR UPDATE");

            // Verificar idempotencia: si ya existen movimientos de reversión para esta factura, no duplicar
            var yaExistenMovimientos = await _context.MovimientosInventario
                .AnyAsync(m => m.TipoDocumento == "FACTURA_CANCELADA" && m.DocumentoId == facturaId);

            if (yaExistenMovimientos)
            {
                _logger.LogDebug("[INVENTARIO-REVERSION] Ya existen movimientos de reversión para factura {FacturaId}, omitiendo", facturaId);

                if (transaction != null)
                    await transaction.CommitAsync();
                return;
            }

            var factura = await _context.Facturas
                .Include(f => f.Detalles)
                .FirstOrDefaultAsync(f => f.Id == facturaId);

            if (factura == null)
                throw new KeyNotFoundException($"Factura con ID {facturaId} no encontrada");

            // Ordenar por (ProductoId, BodegaId) para adquirir locks en orden consistente y evitar deadlocks
            var detallesOrdenados = factura.Detalles
                .Where(d => d.ProductoId.HasValue && d.BodegaId.HasValue)
                .OrderBy(d => d.ProductoId).ThenBy(d => d.BodegaId)
                .ToList();

            foreach (var detalle in detallesOrdenados)
            {

                var stock = await ObtenerStockConBloqueoAsync(detalle.ProductoId!.Value, detalle.BodegaId!.Value);

                if (stock == null) continue;

                // Devolver stock
                stock.CantidadDisponible += detalle.Cantidad;
                stock.UltimaActualizacion = GetUtcNow();

                // Registrar movimiento de devolución
                var movimiento = new MovimientoInventario
                {
                    ProductoId = detalle.ProductoId!.Value,
                    BodegaId = detalle.BodegaId!.Value,
                    TipoMovimiento = "ENTRADA",
                    Cantidad = detalle.Cantidad,
                    CostoUnitario = detalle.CostoUnitario,
                    TipoDocumento = "FACTURA_CANCELADA",
                    DocumentoId = facturaId,
                    NumeroDocumento = factura.NumeroControl,
                    FechaMovimiento = GetUtcNow(),
                    Observaciones = "Reversión por factura rechazada/cancelada antes de envío",
                    SaldoAnterior = stock.CantidadTotal - detalle.Cantidad,
                    NuevoSaldo = stock.CantidadTotal,
                    CostoPromedioAnterior = stock.CostoPromedio,
                    NuevoCostoPromedio = stock.CostoPromedio,
                    UsuarioId = factura.UsuarioId
                };

                _context.MovimientosInventario.Add(movimiento);
            }

            await _context.SaveChangesAsync();

            if (transaction != null)
                await transaction.CommitAsync();

            _logger.LogInformation("[INVENTARIO-REVERSION] Stock revertido para factura rechazada {FacturaId}", facturaId);
        }
        catch
        {
            if (transaction != null)
                await transaction.RollbackAsync();
            throw;
        }
    });

    public async Task DescontarStockInmediatoAsync(int facturaId) => await EjecutarConReintentoAsync(async () =>
    {
        var isExternalTransaction = _context.Database.CurrentTransaction != null;
        var transaction = isExternalTransaction ? null : await _context.Database.BeginTransactionAsync();

        try
        {
            // Bloquear la fila de la factura para serializar operaciones concurrentes sobre la misma factura
            await BloquearFacturaAsync(facturaId);

            // Verificar idempotencia: si ya existen movimientos para esta factura, no duplicar (F3 G4)
            var yaExistenMovimientos = await _context.MovimientosInventario
                .AnyAsync(m => m.TipoDocumento == "FACTURA_EMITIDA" && m.DocumentoId == facturaId);

            if (yaExistenMovimientos)
            {
                _logger.LogDebug("[INVENTARIO-DESCUENTO-DIRECTO] Ya existen movimientos para factura {FacturaId}, omitiendo", facturaId);

                if (transaction != null)
                    await transaction.CommitAsync();
                return;
            }

            var factura = await _context.Facturas
                .Include(f => f.Detalles)
                .FirstOrDefaultAsync(f => f.Id == facturaId);

            if (factura == null)
                throw new KeyNotFoundException($"Factura con ID {facturaId} no encontrada");

            // Ordenar por (ProductoId, BodegaId) para adquirir locks en orden consistente y evitar deadlocks
            var detallesOrdenados = factura.Detalles
                .Where(d => d.ProductoId.HasValue && d.BodegaId.HasValue)
                .OrderBy(d => d.ProductoId).ThenBy(d => d.BodegaId)
                .ToList();

            foreach (var detalle in detallesOrdenados)
            {

                var stock = await ObtenerStockConBloqueoAsync(detalle.ProductoId!.Value, detalle.BodegaId!.Value);

                if (stock == null)
                    throw new InvalidOperationException(
                        $"No existe stock para el producto {detalle.ProductoId} en la bodega {detalle.BodegaId}");

                if (stock.CantidadDisponible < detalle.Cantidad)
                    throw new InvalidOperationException(
                        $"Stock insuficiente. Disponible: {stock.CantidadDisponible}, Requerido: {detalle.Cantidad}");

                // Descontar stock directamente
                stock.CantidadDisponible -= detalle.Cantidad;
                stock.UltimaActualizacion = GetUtcNow();

                // Registrar movimiento de inventario (SALIDA)
                var movimiento = new MovimientoInventario
                {
                    ProductoId = detalle.ProductoId!.Value,
                    BodegaId = detalle.BodegaId!.Value,
                    TipoMovimiento = "SALIDA",
                    Cantidad = -detalle.Cantidad, // Negativo
                    CostoUnitario = detalle.CostoUnitario,
                    TipoDocumento = "FACTURA_EMITIDA",
                    DocumentoId = facturaId,
                    NumeroDocumento = factura.NumeroControl,
                    FechaMovimiento = GetUtcNow(),
                    Observaciones = "Venta con descuento inmediato de stock",
                    SaldoAnterior = stock.CantidadTotal + detalle.Cantidad, // Total antes del descuento
                    NuevoSaldo = stock.CantidadTotal, // Total actual (ya descontado)
                    CostoPromedioAnterior = stock.CostoPromedio,
                    NuevoCostoPromedio = stock.CostoPromedio,
                    UsuarioId = factura.UsuarioId
                };

                _context.MovimientosInventario.Add(movimiento);

                _logger.LogInformation("[INVENTARIO-DESCUENTO-DIRECTO] ProductoId={ProductoId}, BodegaId={BodegaId}: Descontado={Cantidad}, NuevoDisponible={Disponible}",
                    detalle.ProductoId, detalle.BodegaId, detalle.Cantidad, stock.CantidadDisponible);
            }

            await _context.SaveChangesAsync();

            if (transaction != null)
                await transaction.CommitAsync();

            _logger.LogInformation("[INVENTARIO-DESCUENTO-DIRECTO] Stock descontado exitosamente para factura {FacturaId}", facturaId);
        }
        catch
        {
            if (transaction != null)
                await transaction.RollbackAsync();
            throw;
        }
    });
    public async Task<MovimientoInventario> RegistrarAjusteAsync(AjusteInventarioDto dto, int emisorId, int? usuarioId) => await EjecutarConReintentoAsync<MovimientoInventario>(async () =>
    {
        var isExternalTransaction = _context.Database.CurrentTransaction != null;
        var transaction = isExternalTransaction ? null : await _context.Database.BeginTransactionAsync();

        try
        {
            var stock = await ObtenerStockConBloqueoAsync(dto.ProductoId, dto.BodegaId);

            if (stock == null)
                throw new InvalidOperationException($"No existe registro de stock para el producto {dto.ProductoId} en la bodega {dto.BodegaId}");

            // Cargar relaciones necesarias para la validación de emisor
            await _context.Entry(stock).Reference(s => s.Bodega).LoadAsync();
            if (stock.Bodega != null)
                await _context.Entry(stock.Bodega).Reference(b => b.Sucursal).LoadAsync();

            if (stock.Bodega?.Sucursal?.EmisorId != emisorId)
                throw new UnauthorizedAccessException("El producto/bodega no pertenece al emisor");

            // Validar saldo suficiente si es decremento
            if (dto.Cantidad < 0 && stock.CantidadDisponible + dto.Cantidad < 0)
            {
                throw new InvalidOperationException($"Stock insuficiente para el ajuste. Disponible: {stock.CantidadDisponible}, Ajuste: {dto.Cantidad}");
            }

            // Actualizar stock
            var saldoAnterior = stock.CantidadTotal;
            stock.CantidadDisponible += dto.Cantidad;
            stock.UltimaActualizacion = GetUtcNow();

            // Registrar movimiento
            var movimiento = new MovimientoInventario
            {
                ProductoId = dto.ProductoId,
                BodegaId = dto.BodegaId,
                TipoMovimiento = dto.Cantidad >= 0 ? "AJUSTE_ENTRADA" : "AJUSTE_SALIDA",
                Cantidad = dto.Cantidad,
                CostoUnitario = stock.CostoPromedio, // Usar costo promedio actual
                TipoDocumento = dto.Motivo, // CORRECCION, MERMA, etc.
                DocumentoId = 0,
                NumeroDocumento = "AJUSTE-MANUAL",
                FechaMovimiento = GetUtcNow(),
                Observaciones = dto.Observaciones,
                SaldoAnterior = saldoAnterior,
                NuevoSaldo = stock.CantidadTotal, // Propiedad calculada
                CostoPromedioAnterior = stock.CostoPromedio,
                NuevoCostoPromedio = stock.CostoPromedio,
                UsuarioId = usuarioId
            };

            _context.MovimientosInventario.Add(movimiento);
            await _context.SaveChangesAsync();

            if (transaction != null)
                await transaction.CommitAsync();

            _logger.LogInformation("Ajuste de inventario registrado: Producto {ProductoId}, Bodega {BodegaId}, Cantidad {Cantidad}, NuevoSaldo {NuevoSaldo}",
                dto.ProductoId, dto.BodegaId, dto.Cantidad, stock.CantidadTotal);

            return movimiento;
        }
        catch
        {
            if (transaction != null)
                await transaction.RollbackAsync();
            throw;
        }
    });
    public async Task TrasladarAsync(TrasladoInventarioDto dto, int usuarioId) => await EjecutarConReintentoAsync(async () =>
    {
        if (dto.BodegaOrigenId == dto.BodegaDestinoId)
            throw new InvalidOperationException("La bodega origen y destino no pueden ser la misma");

        if (dto.Cantidad <= 0)
            throw new InvalidOperationException("La cantidad a trasladar debe ser mayor a cero");

        var isExternalTransaction = _context.Database.CurrentTransaction != null;
        var transaction = isExternalTransaction ? null : await _context.Database.BeginTransactionAsync();

        try
        {
            // Bloquear AMBOS stocks en orden consistente (menor BodegaId primero) para evitar deadlocks
            var primerBodegaId = Math.Min(dto.BodegaOrigenId, dto.BodegaDestinoId);
            var segundaBodegaId = Math.Max(dto.BodegaOrigenId, dto.BodegaDestinoId);

            var stockPrimero = await ObtenerStockConBloqueoAsync(dto.ProductoId, primerBodegaId);
            var stockSegundo = await ObtenerStockConBloqueoAsync(dto.ProductoId, segundaBodegaId);

            // Asignar según rol (origen/destino)
            var stockOrigen = dto.BodegaOrigenId == primerBodegaId ? stockPrimero : stockSegundo;
            var stockDestino = dto.BodegaDestinoId == primerBodegaId ? stockPrimero : stockSegundo;

            if (stockOrigen == null)
                throw new InvalidOperationException($"No existe registro de stock para el producto {dto.ProductoId} en la bodega origen {dto.BodegaOrigenId}");

            if (stockOrigen.CantidadDisponible < dto.Cantidad)
                throw new InvalidOperationException($"Stock insuficiente en origen. Disponible: {stockOrigen.CantidadDisponible}, Requerido: {dto.Cantidad}");

            // Verificar bodega destino existe
            var bodegaDestino = await _context.Bodegas.FindAsync(dto.BodegaDestinoId);
            if (bodegaDestino == null)
                throw new InvalidOperationException($"La bodega destino {dto.BodegaDestinoId} no existe");

            // Si no existe stock destino, crearlo.
            // Usamos INSERT ... ON CONFLICT para manejar la race condition donde dos traslados
            // concurrentes intenten crear el mismo registro (protegido por IX_StockBodega_Producto_Bodega).
            if (stockDestino == null)
            {
                var costoPromedio = stockOrigen.CostoPromedio;
                var ahora = GetUtcNow();

                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $@"INSERT INTO stock_bodegas (""ProductoId"", ""BodegaId"", ""CantidadDisponible"", ""CantidadReservada"", ""CostoPromedio"", ""UltimaActualizacion"", ""FechaCreacion"", ""Activo"")
                       VALUES ({dto.ProductoId}, {dto.BodegaDestinoId}, 0, 0, {costoPromedio}, {ahora}, NOW(), true)
                       ON CONFLICT (""ProductoId"", ""BodegaId"") DO NOTHING");

                // Re-leer con FOR UPDATE para obtener la entidad trackeada (ya sea la recién creada o la existente)
                stockDestino = await ObtenerStockConBloqueoAsync(dto.ProductoId, dto.BodegaDestinoId);

                if (stockDestino == null)
                    throw new InvalidOperationException($"No se pudo crear/obtener stock destino para producto {dto.ProductoId} en bodega {dto.BodegaDestinoId}");
            }

            var fecha = GetUtcNow();

            // 2. Movimiento SALIDA en Origen
            stockOrigen.CantidadDisponible -= dto.Cantidad;
            stockOrigen.UltimaActualizacion = fecha;

            var movSalida = new MovimientoInventario
            {
                ProductoId = dto.ProductoId,
                BodegaId = dto.BodegaOrigenId,
                BodegaDestinoId = dto.BodegaDestinoId,
                TipoMovimiento = "SALIDA",
                Cantidad = -dto.Cantidad,
                CostoUnitario = stockOrigen.CostoPromedio,
                TipoDocumento = "TRASLADO",
                DocumentoId = 0,
                NumeroDocumento = "TRASLADO",
                FechaMovimiento = fecha,
                Observaciones = $"Traslado hacia bodega {bodegaDestino.Nombre} - {dto.Observaciones}",
                SaldoAnterior = stockOrigen.CantidadTotal + dto.Cantidad,
                NuevoSaldo = stockOrigen.CantidadTotal,
                CostoPromedioAnterior = stockOrigen.CostoPromedio,
                NuevoCostoPromedio = stockOrigen.CostoPromedio,
                UsuarioId = usuarioId
            };
            _context.MovimientosInventario.Add(movSalida);

            // 3. Movimiento ENTRADA en Destino
            var saldoAnteriorDestino = stockDestino.CantidadTotal;
            stockDestino.CantidadDisponible += dto.Cantidad;
            stockDestino.UltimaActualizacion = fecha;

            // Recalcular costo promedio destino (ponderado básico)
            if (stockDestino.CantidadTotal > 0)
            {
                var valAnterior = saldoAnteriorDestino * stockDestino.CostoPromedio;
                var valEntrada = dto.Cantidad * stockOrigen.CostoPromedio;
                stockDestino.CostoPromedio = (valAnterior + valEntrada) / stockDestino.CantidadTotal;
            }
            else
            {
                stockDestino.CostoPromedio = stockOrigen.CostoPromedio;
            }

            // Necesitamos el nombre de la bodega origen para el movimiento de entrada
            var bodegaOrigen = stockOrigen.Bodega ?? await _context.Bodegas.FindAsync(dto.BodegaOrigenId);

            var movEntrada = new MovimientoInventario
            {
                ProductoId = dto.ProductoId,
                BodegaId = dto.BodegaDestinoId,
                TipoMovimiento = "ENTRADA",
                Cantidad = dto.Cantidad,
                CostoUnitario = stockOrigen.CostoPromedio,
                TipoDocumento = "TRASLADO",
                DocumentoId = 0,
                NumeroDocumento = "TRASLADO",
                FechaMovimiento = fecha,
                Observaciones = $"Traslado desde bodega {bodegaOrigen?.Nombre} - {dto.Observaciones}",
                SaldoAnterior = saldoAnteriorDestino,
                NuevoSaldo = stockDestino.CantidadTotal,
                CostoPromedioAnterior = stockDestino.CostoPromedio,
                NuevoCostoPromedio = stockDestino.CostoPromedio,
                UsuarioId = usuarioId
            };
            _context.MovimientosInventario.Add(movEntrada);

            await _context.SaveChangesAsync();

            if (transaction != null)
                await transaction.CommitAsync();

            _logger.LogInformation("Traslado de inventario completado: Producto {Prod} de Bodega {Ori} a {Des}. Cant: {Cant}",
                dto.ProductoId, dto.BodegaOrigenId, dto.BodegaDestinoId, dto.Cantidad);
        }
        catch
        {
            if (transaction != null)
                await transaction.RollbackAsync();
            throw;
        }
    });
}
