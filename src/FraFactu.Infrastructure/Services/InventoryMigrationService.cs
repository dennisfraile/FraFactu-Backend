using FraFactu.Application.DTOs;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Services;

public class InventoryMigrationService : IInventoryMigrationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InventoryMigrationService> _logger;

    public InventoryMigrationService(ApplicationDbContext context, ILogger<InventoryMigrationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<InventoryExportResponseDto> ExportarInventarioAsync(int hubId, Guid migrationId)
    {
        var emisor = await _context.Emisores.FirstOrDefaultAsync(e => e.HubId == hubId)
            ?? throw new KeyNotFoundException($"No se encontró emisor para HubId {hubId}.");

        var productos = await _context.ProductosServicios
            .Include(p => p.UnidadMedida)
            .Where(p => p.EmisorId == emisor.Id && p.Activo)
            .ToListAsync();

        var stocks = await _context.StocksBodega
            .Include(sb => sb.Bodega)
                .ThenInclude(b => b.Sucursal)  // para mapear HubSucursalId al exportar
            .Include(sb => sb.Producto)
            .Where(sb => sb.Producto.EmisorId == emisor.Id && sb.Producto.Activo)
            .ToListAsync();

        // Bodegas activas del emisor: consulta directa, no derivar de stocks.
        // Si se derivan solo de stocks, una bodega recién creada sin productos
        // (común al recién vincular Inventario en SmartHub) queda fuera del export
        // y SI nunca la ve hasta que algún producto tenga stock allí.
        var bodegasActivas = await _context.Bodegas
            .Include(b => b.Sucursal)
            .Where(b => b.SucursalId != null && b.Sucursal!.EmisorId == emisor.Id && b.Activa)
            .ToListAsync();

        // Defensa-en-profundidad: si quedó stock en una bodega inactiva, la
        // exportamos también para no perder esos registros en SI.
        var bodegas = bodegasActivas
            .Concat(stocks.Select(sb => sb.Bodega))
            .DistinctBy(b => b.Id)
            .ToList();

        _logger.LogInformation(
            "Export inventario HubId={HubId} EmisorId={EmisorId} MigrationId={MigrationId}: {P} productos, {B} bodegas, {S} stocks",
            hubId, emisor.Id, migrationId, productos.Count, bodegas.Count, stocks.Count);

        return new InventoryExportResponseDto
        {
            Productos = productos.Select(p => new ProductoMigracionDto
            {
                // Identidad estable cross-app: el id del producto en Smartix.
                ProductoIdExterno = p.Id,
                Codigo = p.Codigo,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                Tipo = p.CatTipoItemId == 2 ? "Servicio" : "Bien",
                PrecioVenta = p.PrecioVenta,
                PrecioCosto = p.PrecioCosto,
                UnidadMedida = p.UnidadMedida?.Valor ?? "Unidad",
                StockMinimo = (int)(p.StockMinimo ?? 0),
                CodigoBarras = p.CodigoBarras
            }).ToList(),
            Bodegas = bodegas.Select(b => new BodegaMigracionDto
            {
                Nombre = b.Nombre,
                Descripcion = null,
                Ubicacion = b.Direccion,
                EsPrincipal = b.EsPrincipal,
                // Id de la sucursal en SmartHub: SmartInventory lo asigna a la bodega
                // importada para que no quede sin sucursal (filtro Sub-plan 8).
                SucursalIdExterno = b.Sucursal?.HubSucursalId
            }).ToList(),
            Stock = stocks.Select(sb => new StockMigracionDto
            {
                ProductoCodigo = sb.Producto.Codigo,
                BodegaNombre = sb.Bodega.Nombre,
                CantidadDisponible = (int)sb.CantidadDisponible,
                CostoPromedio = sb.CostoPromedio
            }).ToList()
        };
    }

    public async Task ImportarDesdeSmartInventoryAsync(InventoryImportRequestDto request)
    {
        // Idempotencia: si ya se procesó esta migración, no hacer nada
        if (await _context.MigracionesInventarioProcesadas
                .AnyAsync(m => m.MigrationId == request.MigrationId && m.Tipo == "import"))
        {
            _logger.LogInformation("MigrationId={MigrationId} ya procesada, ignorando.", request.MigrationId);
            return;
        }

        var emisor = await _context.Emisores.FirstOrDefaultAsync(e => e.HubId == request.HubId)
            ?? throw new KeyNotFoundException($"No se encontró emisor para HubId {request.HubId}.");

        // Catálogos necesarios
        var unidadesMedida = await _context.CatUnidadesMedida.ToListAsync();
        var unidadesMap = unidadesMedida.ToDictionary(u => u.Valor.ToLowerInvariant(), u => u.Id);
        int unidadDefault = unidadesMedida.FirstOrDefault()?.Id ?? 1;

        // CatTipoItem: 1=Bien, 2=Servicio (valores estándar del sistema)
        const int tipoItemBien = 1;
        const int tipoItemServicio = 2;

        // Upsert de productos
        foreach (var dto in request.Productos)
        {
            var existing = await _context.ProductosServicios
                .FirstOrDefaultAsync(p => p.Codigo == dto.Codigo && p.EmisorId == emisor.Id);

            int tipoItem = dto.Tipo.ToLowerInvariant() == "servicio" ? tipoItemServicio : tipoItemBien;
            int unidadId = unidadesMap.GetValueOrDefault(dto.UnidadMedida?.ToLowerInvariant() ?? "unidad", unidadDefault);

            if (existing == null)
            {
                _context.ProductosServicios.Add(new ProductoServicio
                {
                    Codigo = dto.Codigo,
                    Nombre = dto.Nombre,
                    Descripcion = dto.Descripcion,
                    PrecioVenta = dto.PrecioVenta,
                    PrecioCosto = dto.PrecioCosto,
                    StockMinimo = dto.StockMinimo,
                    CodigoBarras = dto.CodigoBarras,
                    EmisorId = emisor.Id,
                    CatTipoItemId = tipoItem,
                    CatUnidadMedidaId = unidadId,
                    TipoImpuesto = TipoImpuesto.Gravado,
                    AccesoTodasSucursales = true
                });
            }
            else
            {
                existing.Nombre = dto.Nombre;
                existing.Descripcion = dto.Descripcion;
                existing.PrecioVenta = dto.PrecioVenta;
                if (dto.PrecioCosto.HasValue) existing.PrecioCosto = dto.PrecioCosto;
                existing.StockMinimo = dto.StockMinimo;
                existing.CodigoBarras = dto.CodigoBarras;
                existing.CatUnidadMedidaId = unidadId;
            }
        }

        await _context.SaveChangesAsync();

        // Actualizar stock en la bodega principal del emisor
        var bodegaPrincipal = await _context.Bodegas
            .Include(b => b.Sucursal)
            .Where(b => b.SucursalId != null && b.Sucursal!.EmisorId == emisor.Id && b.EsPrincipal && b.Activa)
            .FirstOrDefaultAsync();

        if (bodegaPrincipal != null && request.Stock.Count > 0)
        {
            foreach (var stockDto in request.Stock)
            {
                var producto = await _context.ProductosServicios
                    .FirstOrDefaultAsync(p => p.Codigo == stockDto.ProductoCodigo && p.EmisorId == emisor.Id);

                if (producto == null) continue;

                var stockEntry = await _context.StocksBodega
                    .FirstOrDefaultAsync(sb => sb.ProductoId == producto.Id && sb.BodegaId == bodegaPrincipal.Id);

                if (stockEntry == null)
                {
                    _context.StocksBodega.Add(new StockBodega
                    {
                        ProductoId = producto.Id,
                        BodegaId = bodegaPrincipal.Id,
                        CantidadDisponible = stockDto.CantidadDisponible,
                        CantidadReservada = 0,
                        CostoPromedio = stockDto.CostoPromedio ?? 0,
                        UltimaActualizacion = DateTime.UtcNow
                    });
                }
                else
                {
                    stockEntry.CantidadDisponible = stockDto.CantidadDisponible;
                    if (stockDto.CostoPromedio.HasValue) stockEntry.CostoPromedio = stockDto.CostoPromedio.Value;
                    stockEntry.UltimaActualizacion = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
        }
        else if (bodegaPrincipal == null && request.Stock.Count > 0)
        {
            _logger.LogWarning(
                "Import HubId={HubId}: no se encontró bodega principal para EmisorId={EmisorId}. Stock no importado.",
                request.HubId, emisor.Id);
        }

        // Registrar como procesada
        _context.MigracionesInventarioProcesadas.Add(new MigracionInventarioProcesada
        {
            MigrationId = request.MigrationId,
            Tipo = "import",
            HubId = request.HubId,
            EmisorId = emisor.Id,
            ProcesadaEn = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Import completado HubId={HubId} EmisorId={EmisorId} MigrationId={MigrationId}: {P} productos procesados.",
            request.HubId, emisor.Id, request.MigrationId, request.Productos.Count);
    }
}
