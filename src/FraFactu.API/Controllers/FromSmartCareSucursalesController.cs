using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FraFactu.API.Controllers;

/// <summary>
/// Endpoints catalogo-only que SmartCare consume server-to-server para construir
/// la UI de configuracion de la clinica (ej. modulo Facturacion electronica:
/// dropdown de servicios disponibles para mapear con la "consulta" SmartCare).
///
/// Auth X-Api-Key (SmartCareSettings.ApiKey), mismo patron que FromSmartCareController.
/// </summary>
[ApiController]
[Route("api/from-smartcare/sucursales")]
[AllowAnonymous]
public class FromSmartCareSucursalesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly SmartCareSettings _settings;

    public FromSmartCareSucursalesController(
        ApplicationDbContext db,
        IOptions<SmartCareSettings> settings)
    {
        _db = db;
        _settings = settings.Value;
    }

    /// <summary>
    /// Lista productos/servicios activos de la Sucursal Smartix indicada para que
    /// SmartCare los muestre en el dropdown del modal "Agregar mapeo". Incluye los
    /// asignados explicitamente a la sucursal (ProductosServiciosSucursales) y los
    /// que tienen AccesoTodasSucursales=true en el Emisor padre.
    ///
    /// 404 si la sucursal no existe, esta inactiva, o ya no esta vinculada a un
    /// Hub-sucursal (HubSucursalId IS NULL). El ultimo caso es defensivo:
    /// cubre race conditions con el unlink Hub - Emisor y el cache de 5 min del
    /// smartHubClient en SmartCare, para no servir catalogo cross-app cuando la
    /// cadena ya se rompio en Hub.
    /// </summary>
    [HttpGet("{sucursalId:int}/servicios")]
    [ProducesResponseType(typeof(IEnumerable<ServicioParaMapeoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListarServicios(int sucursalId, CancellationToken ct)
    {
        if (!ValidarApiKey()) return Unauthorized(new { error = "API key invalida." });

        var sucursal = await _db.Sucursales
            .AsNoTracking()
            .Where(s => s.Id == sucursalId && s.Activo)
            .Select(s => new { s.Id, s.EmisorId, s.HubSucursalId })
            .FirstOrDefaultAsync(ct);
        if (sucursal is null)
            return NotFound(new { error = $"Sucursal {sucursalId} no existe o esta inactiva." });

        // Sucursal Smartix ya no esta vinculada a ningun Hub-sucursal: SmartCare
        // no deberia estar pidiendo este catalogo. Defensive 404 para evitar
        // exponer datos cross-app cuando la cadena Hub -> Emisor -> Sucursal se
        // rompio (ver cascade-unlink-cross-app, task 6 / 2026-06-03).
        if (sucursal.HubSucursalId is null)
            return NotFound(new { error = $"Sucursal {sucursalId} no esta vinculada a un Hub-sucursal activo." });

        // ProductosServiciosSucursales = tabla puente (Id producto + Id sucursal).
        // Si el producto tiene AccesoTodasSucursales=true se considera disponible
        // sin necesidad de fila en la tabla puente.
        //
        // Bug #1 IVA (2026-05-29): SmartCare consume PrecioUnitario como "precio al
        // cliente final" (con IVA incluido). Por convencion el catalogo de SmartCare
        // asume precios con IVA y su builder divide /1.13 antes de mandar al prefill.
        // Smartix guarda PrecioVenta como BASE (sin IVA, ver ProductoServicio.cs:50),
        // entonces devolverlo crudo perdia un factor 1.13 en la cadena
        // (Smartix base $44.25 -> SmartCare $44.25/1.13 = $39.16 -> Smartix *1.13 = $44.25
        // mostrado como Total, cuando el cliente esperaba $50 = base * 1.13).
        // Fix: sumar IVA aca cuando TipoImpuesto = Gravado para que SmartCare reciba
        // el "precio al cliente". Exento/NoSujeto no llevan IVA, devolver tal cual.
        var query = from p in _db.ProductosServicios.AsNoTracking()
                    where p.EmisorId == sucursal.EmisorId && p.Activo
                          && (p.AccesoTodasSucursales
                              || _db.ProductosServiciosSucursales.Any(ps =>
                                    ps.ProductoServicioId == p.Id && ps.SucursalId == sucursalId))
                    orderby p.Nombre
                    select new
                    {
                        p.Id,
                        p.Codigo,
                        p.Nombre,
                        p.PrecioVenta,
                        p.TipoImpuesto,
                        p.PorcentajeIVA,
                        p.CatTipoItemId
                    };

        var raw = await query.ToListAsync(ct);
        var items = raw.Select(p => new ServicioParaMapeoDto
        {
            Id = p.Id,
            Codigo = p.Codigo,
            Nombre = p.Nombre,
            PrecioUnitario = p.TipoImpuesto == TipoImpuesto.Gravado && p.PorcentajeIVA.HasValue
                ? Math.Round(p.PrecioVenta * (1m + p.PorcentajeIVA.Value / 100m), 2, MidpointRounding.AwayFromZero)
                : p.PrecioVenta,
            CatTipoItemId = p.CatTipoItemId
        }).ToList();

        return Ok(items);
    }

    private bool ValidarApiKey() =>
        Request.Headers.TryGetValue("X-Api-Key", out var key) &&
        !string.IsNullOrEmpty(_settings.ApiKey) &&
        key.FirstOrDefault() == _settings.ApiKey;
}
