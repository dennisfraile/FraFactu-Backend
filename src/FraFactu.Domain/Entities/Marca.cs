using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities;

/// <summary>
/// Marca de productos
/// </summary>
public class Marca : BaseEntity
{
    /// <summary>
    /// Nombre de la marca
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Descripción de la marca
    /// </summary>
    public string? Descripcion { get; set; }

    /// <summary>
    /// ID del Emisor al que pertenece esta marca
    /// </summary>
    public int EmisorId { get; set; }
    public Emisor? Emisor { get; set; }

    /// <summary>
    /// Indica si la marca está activa
    /// </summary>
    public bool Activa { get; set; } = true;

    /// <summary>
    /// Productos de esta marca
    /// </summary>
    public ICollection<ProductoServicio> Productos { get; set; } = new List<ProductoServicio>();
}
