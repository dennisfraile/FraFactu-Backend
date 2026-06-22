using System.ComponentModel.DataAnnotations;

namespace FraFactu.Application.DTOs.ProductosServicios
{
    /// <summary>
    /// Datos para dar de baja (soft-delete) un producto/servicio. F3 (G2).
    /// </summary>
    public class DesactivarProductoServicioDto
    {
        /// <summary>
        /// Motivo de la baja. Obligatorio.
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Motivo { get; set; } = string.Empty;
    }
}
