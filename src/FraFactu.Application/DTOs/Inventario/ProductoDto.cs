namespace FraFactu.Application.DTOs.Inventario;

/// <summary>
/// DTO para mostrar información de un producto
/// </summary>
public class ProductoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    // Clasificación
    public int CategoriaId { get; set; }
    public string CategoriaNombre { get; set; } = string.Empty;
    public int? MarcaId { get; set; }
    public string? MarcaNombre { get; set; }
    public int CatUnidadMedidaId { get; set; }
    public string UnidadMedidaNombre { get; set; } = string.Empty;

    // Stock
    public decimal StockMinimo { get; set; }
    public decimal StockMaximo { get; set; }
    public decimal PuntoReorden { get; set; }

    // Precios
    public decimal PrecioCosto { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal Margen { get; set; }

    // Impuestos
    public bool AplicaIVA { get; set; }
    public decimal PorcentajeIVA { get; set; }

    // Configuración
    public bool Activo { get; set; }
    public bool PermiteVentaSinStock { get; set; }
    public bool EsServicio { get; set; }

    // Stock actual (suma de todas las bodegas)
    public decimal StockTotal { get; set; }
    public decimal StockDisponible { get; set; }
    public decimal StockReservado { get; set; }

    // Valorización
    public decimal CostoPromedioGlobal { get; set; }
    public decimal ValorInventario { get; set; }

    // Metadata
    public DateTime FechaCreacion { get; set; }
}
