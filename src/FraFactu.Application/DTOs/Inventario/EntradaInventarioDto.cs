namespace FraFactu.Application.DTOs.Inventario;

/// <summary>
/// DTO para registrar entrada de inventario (compras, devoluciones de clientes)
/// </summary>
public class EntradaInventarioDto
{
    /// <summary>
    /// ID del producto
    /// </summary>
    public int ProductoId { get; set; }

    /// <summary>
    /// ID de la bodega donde ingresa
    /// </summary>
    public int BodegaId { get; set; }

    /// <summary>
    /// Cantidad que ingresa
    /// </summary>
    public decimal Cantidad { get; set; }

    /// <summary>
    /// Costo unitario del producto
    /// </summary>
    public decimal CostoUnitario { get; set; }

    /// <summary>
    /// Tipo de documento que origina la entrada
    /// COMPRA, DEVOLUCION_CLIENTE, PRODUCCION, OTRO
    /// </summary>
    public string TipoDocumento { get; set; } = "COMPRA";

    /// <summary>
    /// Número del documento origen
    /// </summary>
    public string? NumeroDocumento { get; set; }

    /// <summary>
    /// ID del documento origen (si aplica)
    /// </summary>
    public int? DocumentoId { get; set; }

    /// <summary>
    /// Observaciones del movimiento
    /// </summary>
    public string? Observaciones { get; set; }
}
