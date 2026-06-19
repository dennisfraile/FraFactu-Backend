namespace FraFactu.Application.DTOs.Contingencia;

/// <summary>
/// DTO para cada documento en el array detalleDTE
/// Representa un DTE incluido en el evento de contingencia
/// </summary>
public class DetalleDocumentoDto
{
    /// <summary>
    /// Número correlativo del item (1 a 1000)
    /// </summary>
    public int NoItem { get; set; }

    /// <summary>
    /// Código de generación (UUID) del documento reportado
    /// Formato: XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX
    /// </summary>
    public string CodigoGeneracion { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de Documento Tributario Electrónico
    /// Valores: "01" a "15"
    /// </summary>
    public string TipoDoc { get; set; } = string.Empty;
}
