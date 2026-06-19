namespace FraFactu.Application.DTOs.Inventario;

/// <summary>
/// DTO para actualizar un producto existente
/// </summary>
public class ActualizarProductoDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int CategoriaId { get; set; }
    public int? MarcaId { get; set; }
    public int CatUnidadMedidaId { get; set; }
    public decimal StockMinimo { get; set; }
    public decimal StockMaximo { get; set; }
    public decimal PuntoReorden { get; set; }
    public decimal PrecioCosto { get; set; }
    public decimal PrecioVenta { get; set; }
    public bool AplicaIVA { get; set; }
    public decimal PorcentajeIVA { get; set; }
    public bool PermiteVentaSinStock { get; set; }
    public bool EsServicio { get; set; }
    public bool Activo { get; set; }
}
