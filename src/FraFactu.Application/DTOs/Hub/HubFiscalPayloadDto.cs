namespace FraFactu.Application.DTOs.Hub;

/// <summary>
/// Plan B Hub-as-Emisor — Fase 2 Task 16 (Bloque D, SSO refresh).
///
/// Respuesta del endpoint <c>GET /api/internal/hubs/{id}/fiscal-payload</c> de
/// SmartHub. Espejo de <c>FiscalPayloadDto</c> en SmartHub-BackEnd (PR #9).
///
/// AuthService consume este shape al hacer SSO hub-login para refrescar el
/// cache del Emisor antes de emitir el JWT. <see cref="Sucursal"/> es siempre
/// null para el scope hub (el endpoint hub-only no incluye sucursal); aquí se
/// deja por compatibilidad con la deserialización del shape común.
/// </summary>
public class HubFiscalPayloadDto
{
    public EmisorFiscalPayloadDto Emisor { get; set; } = null!;
    public SucursalFiscalPayloadDto? Sucursal { get; set; }
}

public class EmisorFiscalPayloadDto
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
    public string DireccionComplemento { get; set; } = string.Empty;
    public string? TelefonoFiscal { get; set; }
    public string? CorreoFiscal { get; set; }
}

public class SucursalFiscalPayloadDto
{
    public int HubSucursalId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string CodigoEstablecimientoMH { get; set; } = string.Empty;
    public string CodTipoEstablecimiento { get; set; } = string.Empty;
    public string CodDepartamento { get; set; } = string.Empty;
    public string CodMunicipio { get; set; } = string.Empty;
    public string DireccionComplemento { get; set; } = string.Empty;
    public string? TelefonoSucursal { get; set; }
    public string? CorreoSucursal { get; set; }
    public ContingenciaPayloadDto? Contingencia { get; set; }
}

public class ContingenciaPayloadDto
{
    public string NombreResponsable { get; set; } = string.Empty;
    public string TipoDocResponsable { get; set; } = string.Empty;
    public string NumeroDocResponsable { get; set; } = string.Empty;
}
