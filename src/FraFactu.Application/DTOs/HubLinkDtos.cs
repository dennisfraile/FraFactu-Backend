namespace FraFactu.Application.DTOs;

/// <summary>
/// Plan B Hub-as-Emisor — Endpoints internos de vinculación cross-app.
/// SmartHub consume estos DTOs al armar el modal "Vincular con Smartix".
/// </summary>

/// <summary>Item del listado de Emisores Smartix que se ofrecen al SuperAdmin para vincular con un Hub.</summary>
public class EmisorParaVincularDto
{
    public int Id { get; set; }
    public string Nit { get; set; } = string.Empty;
    public string NombreRazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }

    /// <summary>HubId actual al que está vinculado. Null si está libre.</summary>
    public int? HubId { get; set; }
}

/// <summary>Item del listado de Sucursales Smartix que se ofrecen al admin para vincular con una Sucursal Hub.</summary>
public class SucursalParaVincularDto
{
    public int Id { get; set; }
    public int EmisorId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string? CodigoEstablecimiento { get; set; }

    /// <summary>HubSucursalId actual. Null si está libre.</summary>
    public int? HubSucursalId { get; set; }
}

/// <summary>Body del PATCH /api/internal/emisores/{id}/hub-link.</summary>
public record PatchEmisorHubLinkRequest(int? HubId);

/// <summary>Body del PATCH /api/internal/sucursales/{id}/hub-link.</summary>
public record PatchSucursalHubLinkRequest(int? HubSucursalId);

/// <summary>
/// Snapshot fiscal del Emisor devuelto en la respuesta del PATCH hub-link
/// cuando se VINCULA (HubId != null). SmartHub lo usa para precargar los
/// datos fiscales del Hub recien vinculado (solo si el Hub los tiene vacios:
/// politica "no sobrescribir"). Codigos MH (TipoEstablecimiento/Departamento/
/// Municipio) ya resueltos desde los catalogos de Smartix.
/// </summary>
public class EmisorFiscalSnapshotDto
{
    public int Id { get; set; }
    public string Nit { get; set; } = string.Empty;
    public string Nrc { get; set; } = string.Empty;
    public string NombreRazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public string CodigoActividad { get; set; } = string.Empty;
    public string DescripcionActividad { get; set; } = string.Empty;
    /// <summary>Codigo MH del tipo de establecimiento (ej "01"). Null si el catalogo no resolvio.</summary>
    public string? CodTipoEstablecimientoMH { get; set; }
    /// <summary>Codigo MH del departamento (ej "06"). Null si el catalogo no resolvio.</summary>
    public string? CodDepartamentoMH { get; set; }
    /// <summary>Codigo MH del municipio (ej "23"). Null si el catalogo no resolvio.</summary>
    public string? CodMunicipioMH { get; set; }
    /// <summary>CAT-008 Distrito MH del emisor. Null si el emisor no tiene CatDistritoId.</summary>
    public string? CodDistritoMH { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;
}

/// <summary>
/// Snapshot fiscal de la Sucursal devuelto al VINCULAR (HubSucursalId != null).
/// SmartHub precarga los campos vacios de su Sucursal local. Incluye los 3
/// campos de contingencia (van juntos o no van).
/// </summary>
public class SucursalFiscalSnapshotDto
{
    public int Id { get; set; }
    public int EmisorId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? CodigoEstablecimientoMH { get; set; }
    public string? CodTipoEstablecimientoMH { get; set; }
    public string? CodDepartamentoMH { get; set; }
    public string? CodMunicipioMH { get; set; }
    /// <summary>CAT-008 Distrito MH de la sucursal. Null si la sucursal no tiene CatDistritoId.</summary>
    public string? CodDistritoMH { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? CorreoElectronico { get; set; }
    public string? ContingenciaNombreResponsable { get; set; }
    public string? ContingenciaTipoDocResponsable { get; set; }
    public string? ContingenciaNumeroDocResponsable { get; set; }
}
