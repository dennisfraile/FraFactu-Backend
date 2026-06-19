namespace FraFactu.Application.DTOs.Inventario;

/// <summary>
/// DTO para categorías
/// </summary>
public class CategoriaDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? CategoriaPadreId { get; set; }
    public string? CategoriaPadreNombre { get; set; }
    public int TotalProductos { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class CrearCategoriaDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? CategoriaPadreId { get; set; }
}
