using System.ComponentModel.DataAnnotations;

namespace FraFactu.Application.DTOs.Inventario;

public class AjusteInventarioDto
{
    [Required]
    public int ProductoId { get; set; }

    [Required]
    public int BodegaId { get; set; }

    [Required]
    public decimal Cantidad { get; set; } // Positiva = Incremento, Negativa = Decremento

    [Required]
    [StringLength(50)]
    public string Motivo { get; set; } = string.Empty; // CORRECCION, MERMA, FALTANTE, SOBRANTE, DAÑADO, OTRO

    [Required]
    [StringLength(1000, MinimumLength = 10)]
    public string Observaciones { get; set; } = string.Empty;
}
