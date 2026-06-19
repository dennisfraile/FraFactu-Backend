using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities;

/// <summary>
/// Categoría para clasificación jerárquica de productos
/// </summary>
public class Categoria : BaseEntity
{
    /// <summary>
    /// Código único de la categoría
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Nombre de la categoría
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Descripción detallada
    /// </summary>
    public string? Descripcion { get; set; }

    /// <summary>
    /// ID del Emisor al que pertenece esta categoría
    /// </summary>
    public int EmisorId { get; set; }
    public Emisor? Emisor { get; set; }

    /// <summary>
    /// FK a categoría padre (para jerarquía)
    /// </summary>
    public int? CategoriaPadreId { get; set; }
    public Categoria? CategoriaPadre { get; set; }

    /// <summary>
    /// Subcategorías
    /// </summary>
    public ICollection<Categoria> Subcategorias { get; set; } = new List<Categoria>();

    /// <summary>
    /// Productos en esta categoría
    /// </summary>
    public ICollection<ProductoServicio> Productos { get; set; } = new List<ProductoServicio>();
}
