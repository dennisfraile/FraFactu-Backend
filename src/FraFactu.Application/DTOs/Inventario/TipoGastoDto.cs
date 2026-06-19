namespace FraFactu.Application.DTOs.Inventario;

/// <summary>
/// DTO para tipos de gasto
/// </summary>
public class TipoGastoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}
