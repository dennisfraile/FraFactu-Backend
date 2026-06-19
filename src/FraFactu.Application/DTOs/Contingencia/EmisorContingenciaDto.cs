namespace FraFactu.Application.DTOs.Contingencia;

/// <summary>
/// DTO para datos del emisor en el evento de contingencia
/// Corresponde a la sección "emisor" del schema JSON v3
/// </summary>
public class EmisorContingenciaDto
{
    /// <summary>
    /// NIT del emisor (9 o 14 dígitos sin guiones)
    /// </summary>
    public string Nit { get; set; } = string.Empty;

    /// <summary>
    /// Nombre, denominación o razón social del emisor
    /// Min: 5, Max: 250
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del responsable del establecimiento (OBLIGATORIO)
    /// Min: 5, Max: 100
    /// </summary>
    public string NombreResponsable { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de documento del responsable (CAT-22)
    /// 36=NIT, 13=DUI, 02=Carnet, 03=Pasaporte, 37=Otro
    /// </summary>
    public string TipoDocResponsable { get; set; } = string.Empty;

    /// <summary>
    /// Número de documento del responsable
    /// Min: 5, Max: 25
    /// </summary>
    public string NumeroDocResponsable { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de establecimiento
    /// 01, 02, 04, 07, 20
    /// </summary>
    public string TipoEstablecimiento { get; set; } = string.Empty;

    /// <summary>
    /// Código de establecimiento otorgado por MH (4 caracteres, opcional)
    /// </summary>
    public string? CodEstableMH { get; set; }

    public string? CodEstable { get; set; }

    /// <summary>
    /// Código de punto de venta del contribuyente (1-15 caracteres, opcional)
    /// </summary>
    public string? CodPuntoVenta { get; set; }

    public string? CodPuntoVentaMH { get; set; }

    /// <summary>
    /// Teléfono del emisor
    /// Min: 8, Max: 30
    /// </summary>
    public string Telefono { get; set; } = string.Empty;

    /// <summary>
    /// Correo electrónico del emisor
    /// Min: 3, Max: 100
    /// </summary>
    public string Correo { get; set; } = string.Empty;
}
