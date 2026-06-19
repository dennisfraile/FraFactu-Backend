namespace FraFactu.Application.DTOs.Inventario;

/// <summary>
/// DTO para marcas
/// </summary>
public class MarcaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int TotalProductos { get; set; }
    public bool Activa { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class CrearMarcaDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}
