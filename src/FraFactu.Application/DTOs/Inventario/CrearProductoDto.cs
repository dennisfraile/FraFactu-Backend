namespace FraFactu.Application.DTOs.Inventario;

/// <summary>
/// DTO para crear un nuevo producto
/// </summary>
public class CrearProductoDto
{
    /// <summary>
    /// Código único del producto
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Código de barras (opcional)
    /// </summary>
    public string? CodigoBarras { get; set; }

    /// <summary>
    /// Nombre del producto
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Descripción detallada
    /// </summary>
    public string? Descripcion { get; set; }

    /// <summary>
    /// ID de la categoría
    /// </summary>
    public int CategoriaId { get; set; }

    /// <summary>
    /// ID de la marca (opcional)
    /// </summary>
    public int? MarcaId { get; set; }

    /// <summary>
    /// ID de unidad de medida del catálogo MH
    /// </summary>
    public int CatUnidadMedidaId { get; set; }

    /// <summary>
    /// Stock mínimo que debe mantenerse
    /// </summary>
    public decimal StockMinimo { get; set; }

    /// <summary>
    /// Stock máximo permitido
    /// </summary>
    public decimal StockMaximo { get; set; }

    /// <summary>
    /// Punto de reorden (genera alerta)
    /// </summary>
    public decimal PuntoReorden { get; set; }

    /// <summary>
    /// Precio de costo
    /// </summary>
    public decimal PrecioCosto { get; set; }

    /// <summary>
    /// Precio de venta
    /// </summary>
    public decimal PrecioVenta { get; set; }

    /// <summary>
    /// Aplica IVA
    /// </summary>
    public bool AplicaIVA { get; set; } = true;

    /// <summary>
    /// Porcentaje de IVA (13% por defecto)
    /// </summary>
    public decimal PorcentajeIVA { get; set; } = 13;

    /// <summary>
    /// Permite venta sin stock disponible
    /// </summary>
    public bool PermiteVentaSinStock { get; set; } = false;

    /// <summary>
    /// Es un servicio (no maneja stock físico)
    /// </summary>
    public bool EsServicio { get; set; } = false;
}
