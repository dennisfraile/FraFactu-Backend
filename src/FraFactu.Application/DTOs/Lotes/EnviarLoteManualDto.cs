using System.ComponentModel.DataAnnotations;

namespace FraFactu.Application.DTOs.Lotes;

/// <summary>
/// DTO para envío manual de lote con selección de facturas
/// </summary>
public class EnviarLoteManualDto
{
    /// <summary>
    /// IDs de las facturas a incluir en el lote
    /// </summary>
    [Required(ErrorMessage = "Debe especificar al menos una factura")]
    [MinLength(1, ErrorMessage = "Debe incluir al menos una factura")]
    public List<int> FacturaIds { get; set; } = new();

    /// <summary>
    /// Descripción opcional del lote
    /// </summary>
    [MaxLength(500)]
    public string? Descripcion { get; set; }
}
