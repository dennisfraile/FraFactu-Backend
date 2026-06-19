using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Exceptions;
using FraFactu.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FraFactu.API.Controllers;

/// <summary>
/// Plan B Hub-as-Emisor — Fase 2 Task 15.
///
/// Receiver de webhooks fiscales push de SmartHub. Cuando un admin edita la
/// data fiscal del Hub o de una Sucursal en SmartHub, SmartHub nos llama con
/// el payload nuevo y refrescamos el cache local del Emisor / Sucursal.
///
/// Autenticación: X-Api-Key con <see cref="SmartHubSettings.ApiKey"/>, la misma
/// que validan los otros endpoints internos llamados por SmartHub
/// (SyncEmisorController, HubWebhookController, etc.).
///
/// Idempotente: si el Emisor / Sucursal no existe, 404. Si existe, sincroniza
/// los campos identitarios. Los campos Smartix-only (Mh*, Smtp*, Gmail*, Logo,
/// ambiente) se preservan intactos.
/// </summary>
[ApiController]
[Route("api/internal")]
[AllowAnonymous]
public class InternalFiscalSyncController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly SmartHubSettings _settings;
    private readonly ILogger<InternalFiscalSyncController> _logger;

    public InternalFiscalSyncController(
        ApplicationDbContext context,
        IOptions<SmartHubSettings> settings,
        ILogger<InternalFiscalSyncController> logger)
    {
        _context = context;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Refresca el cache del Emisor identificándolo por <c>HubId</c>. 404 si el
    /// Hub no está vinculado a ningún Emisor en Smartix (caso típico: SmartHub
    /// envió webhook pero admin aún no completó el flujo de "Vincular Hub con
    /// Emisor" de PR #76).
    /// </summary>
    [HttpPost("sync-emisor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SyncEmisor([FromBody] HubFiscalSyncRequest request, CancellationToken ct)
    {
        if (!ValidarApiKey()) return Unauthorized(new { error = "API key invalida." });

        var emisor = await _context.Emisores.FirstOrDefaultAsync(e => e.HubId == request.HubId, ct);
        if (emisor is null)
        {
            _logger.LogWarning(
                "[FISCAL-SYNC-RECEIVER] Hub {HubId} no esta vinculado a ningun Emisor en Smartix. " +
                "Webhook ignorado; admin debe vincular el Hub primero via UI admin.", request.HubId);
            return NotFound(new { error = $"No existe Emisor en Smartix vinculado al Hub {request.HubId}." });
        }

        var (catDepId, catMunId, catTipoId, catDistritoId) = await ResolverCatalogosAsync(
            request.CodDepartamento, request.CodMunicipio, request.CodTipoEstablecimiento, request.CodDistrito, ct);

        // Solo campos fiscales identitarios. NO tocar Mh*, MhProd*, Smtp*, Gmail*,
        // LogoUrl, CatAmbienteDestinoId, HubId — esos son secretos / config Smartix-only.
        emisor.Nit = request.Nit;
        emisor.Nrc = request.Nrc;
        emisor.NombreRazonSocial = request.NombreRazonSocial;
        emisor.NombreComercial = request.NombreComercial;
        emisor.CodigoActividad = request.CodActividadEconomica;
        emisor.DescripcionActividad = request.DescActividadEconomica;
        emisor.CatDepartamentoId = catDepId;
        emisor.CatMunicipioId = catMunId;
        emisor.CatTipoEstablecimientoId = catTipoId;
        emisor.CatDistritoId = catDistritoId;
        emisor.Direccion = request.DireccionComplemento;
        if (!string.IsNullOrWhiteSpace(request.TelefonoFiscal))
            emisor.Telefono = request.TelefonoFiscal;
        if (!string.IsNullOrWhiteSpace(request.CorreoFiscal))
            emisor.CorreoElectronico = request.CorreoFiscal;

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation(
            "[FISCAL-SYNC-RECEIVER] Emisor refrescado via webhook. EmisorId={EmisorId} HubId={HubId}.",
            emisor.Id, request.HubId);
        return NoContent();
    }

    /// <summary>
    /// Refresca el cache de la Sucursal identificándola por <c>HubSucursalId</c>.
    /// 404 si la Sucursal Smartix no existe (admin aún no la creó o aún no se
    /// auto-creó via el flujo de prefill de SmartCare).
    /// </summary>
    [HttpPost("sync-sucursal")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SyncSucursal([FromBody] SucursalFiscalSyncRequest request, CancellationToken ct)
    {
        if (!ValidarApiKey()) return Unauthorized(new { error = "API key invalida." });

        var sucursal = await _context.Sucursales.FirstOrDefaultAsync(s => s.HubSucursalId == request.HubSucursalId, ct);
        if (sucursal is null)
        {
            _logger.LogWarning(
                "[FISCAL-SYNC-RECEIVER] HubSucursal {Id} no esta vinculado a ninguna Sucursal en Smartix. " +
                "Webhook ignorado; se auto-creara en el proximo facturado desde SmartCare.", request.HubSucursalId);
            return NotFound(new { error = $"No existe Sucursal en Smartix vinculada al HubSucursalId {request.HubSucursalId}." });
        }

        var (catDepId, catMunId, catTipoId, catDistritoId) = await ResolverCatalogosAsync(
            request.CodDepartamento, request.CodMunicipio, request.CodTipoEstablecimiento, request.CodDistrito, ct);

        sucursal.Nombre = request.Nombre;
        sucursal.CodigoEstablecimiento = request.CodigoEstablecimientoMH;
        sucursal.CatDepartamentoId = catDepId;
        sucursal.CatMunicipioId = catMunId;
        // Sucursal.CatTipoEstablecimientoId NO es nullable (a diferencia del
        // Emisor): si SH manda vacio preservamos el valor previo. Si nunca tuvo
        // valor lo dejamos en 0 (caso edge raro; la entity se cre+a con default).
        if (catTipoId.HasValue) sucursal.CatTipoEstablecimientoId = catTipoId.Value;
        sucursal.CatDistritoId = catDistritoId;
        sucursal.Direccion = request.DireccionComplemento;
        if (!string.IsNullOrWhiteSpace(request.TelefonoSucursal))
            sucursal.Telefono = request.TelefonoSucursal;
        if (!string.IsNullOrWhiteSpace(request.CorreoSucursal))
            sucursal.CorreoElectronico = request.CorreoSucursal;
        sucursal.ContingenciaNombreResponsable = request.ContingenciaNombreResponsable;
        sucursal.ContingenciaTipoDocResponsable = request.ContingenciaTipoDocResponsable;
        sucursal.ContingenciaNumeroDocResponsable = request.ContingenciaNumeroDocResponsable;

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation(
            "[FISCAL-SYNC-RECEIVER] Sucursal refrescada via webhook. SucursalId={Id} HubSucursalId={HubId}.",
            sucursal.Id, request.HubSucursalId);
        return NoContent();
    }

    private bool ValidarApiKey()
    {
        var apiKey = Request.Headers["X-Api-Key"].FirstOrDefault() ?? string.Empty;
        return !string.IsNullOrEmpty(_settings.ApiKey) && apiKey == _settings.ApiKey;
    }

    private async Task<(int catDepartamentoId, int catMunicipioId, int? catTipoEstablecimientoId, int? catDistritoId)>
        ResolverCatalogosAsync(string codDep, string codMun, string? codTipo, string? codDistrito, CancellationToken ct)
    {
        var catDepId = await _context.CatDepartamentos
            .Where(d => d.Codigo == codDep)
            .Select(d => (int?)d.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new ValidationException("codDepartamento",
                $"Codigo de departamento '{codDep}' no existe en catalogo MH.");

        var catMunId = await _context.CatMunicipios
            .Where(m => m.CodigoDepartamento == codDep && m.Codigo == codMun)
            .Select(m => (int?)m.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new ValidationException("codMunicipio",
                $"Codigo de municipio '{codMun}' (depto '{codDep}') no existe en catalogo MH.");

        // CodTipoEstablecimiento es OPCIONAL en el Hub (mismo patron que el
        // HubFiscalDataValidator de SmartHub-BE: si el admin no lo provee, el DTE
        // toma tipoEstablecimiento de la Sucursal en lugar del Emisor). La columna
        // Emisor.CatTipoEstablecimientoId es nullable, asi que podemos asignar
        // NULL. Para Sucursal el caller debe preservar el valor previo (la columna
        // Sucursal.CatTipoEstablecimientoId NO es nullable).
        int? catTipoId = null;
        if (!string.IsNullOrEmpty(codTipo))
        {
            catTipoId = await _context.CatTiposEstablecimiento
                .Where(t => t.Codigo == codTipo)
                .Select(t => (int?)t.Id)
                .FirstOrDefaultAsync(ct);
            if (catTipoId is null)
            {
                throw new ValidationException("codTipoEstablecimiento",
                    $"Codigo de tipo de establecimiento '{codTipo}' no existe en catalogo MH.");
            }
        }

        // Distrito es OPCIONAL durante la migración incremental. Si SH aún no lo
        // tiene seteado, guardamos null y MH rechazará el DTE con [096] al facturar
        // — esto NO rompe la sincronización identitaria del emisor/sucursal.
        int? catDistritoId = null;
        if (!string.IsNullOrEmpty(codDistrito))
        {
            catDistritoId = await _context.CatDistritos
                .Where(d => d.CodigoDepartamento == codDep
                         && d.CodigoMunicipio == codMun
                         && d.Codigo == codDistrito)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync(ct);
            if (catDistritoId is null)
            {
                throw new ValidationException("codDistrito",
                    $"Codigo de distrito '{codDistrito}' (depto '{codDep}' munic '{codMun}') no existe en catalogo MH.");
            }
        }

        return (catDepId, catMunId, catTipoId, catDistritoId);
    }
}

/// <summary>
/// Payload del webhook SmartHub → Smartix. Espejo del HubFiscalSyncRequest del
/// SmartHub-BackEnd (PR #10). Si SmartHub mergea primero, este shape debe alinearse.
/// </summary>
public class HubFiscalSyncRequest
{
    public int HubId { get; set; }
    public string Nit { get; set; } = string.Empty;
    public string Nrc { get; set; } = string.Empty;
    public string NombreRazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public string CodActividadEconomica { get; set; } = string.Empty;
    public string DescActividadEconomica { get; set; } = string.Empty;
    public string CodTipoEstablecimiento { get; set; } = string.Empty;
    public string CodDepartamento { get; set; } = string.Empty;
    public string CodMunicipio { get; set; } = string.Empty;
    /// <summary>Código CAT-008 del distrito. Vacío si SmartHub aún no lo tiene configurado.</summary>
    public string CodDistrito { get; set; } = string.Empty;
    public string DireccionComplemento { get; set; } = string.Empty;
    public string? TelefonoFiscal { get; set; }
    public string? CorreoFiscal { get; set; }
}

public class SucursalFiscalSyncRequest
{
    public int HubSucursalId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string CodigoEstablecimientoMH { get; set; } = string.Empty;
    public string CodTipoEstablecimiento { get; set; } = string.Empty;
    public string CodDepartamento { get; set; } = string.Empty;
    public string CodMunicipio { get; set; } = string.Empty;
    /// <summary>Código CAT-008 del distrito. Vacío si SmartHub aún no lo tiene configurado.</summary>
    public string CodDistrito { get; set; } = string.Empty;
    public string DireccionComplemento { get; set; } = string.Empty;
    public string? TelefonoSucursal { get; set; }
    public string? CorreoSucursal { get; set; }
    public string? ContingenciaNombreResponsable { get; set; }
    public string? ContingenciaTipoDocResponsable { get; set; }
    public string? ContingenciaNumeroDocResponsable { get; set; }
}
