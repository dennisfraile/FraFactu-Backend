namespace FraFactu.Application.DTOs.Lotes;

/// <summary>
/// DTO para crear un nuevo lote de facturas
/// </summary>
public class CrearLoteDto
{
    /// <summary>
    /// IDs de las facturas a incluir en el lote (máximo 100)
    /// </summary>
    public List<int> FacturaIds { get; set; } = new();

    /// <summary>
    /// Indica si el lote es para documentos de contingencia
    /// (permite envío 24/7 sin restricción horaria)
    /// </summary>
    public bool EsContingencia { get; set; } = false;

    /// <summary>
    /// ID del evento de contingencia (OBLIGATORIO si EsContingencia = true)
    /// </summary>
    public int? EventoContingenciaId { get; set; }
}
