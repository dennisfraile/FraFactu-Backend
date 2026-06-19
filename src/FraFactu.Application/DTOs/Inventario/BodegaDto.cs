namespace FraFactu.Application.DTOs.Inventario;

/// <summary>
/// DTO para bodegas
/// </summary>
public class BodegaDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public int? SucursalId { get; set; }
    public string? SucursalNombre { get; set; }
    public bool EsPrincipal { get; set; }
    public bool Activa { get; set; }

    // Información agregada
    public int TotalProductos { get; set; }
    public decimal ValorInventarioTotal { get; set; }
}

public class CrearBodegaDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public int? SucursalId { get; set; }
    public bool EsPrincipal { get; set; }
}
