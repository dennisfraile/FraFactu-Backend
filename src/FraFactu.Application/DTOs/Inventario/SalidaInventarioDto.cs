namespace FraFactu.Application.DTOs.Inventario;

/// <summary>
/// DTO para registrar salida de inventario (ventas manuales, devoluciones a proveedores)
/// </summary>
public class SalidaInventarioDto
{
    public int ProductoId { get; set; }
    public int BodegaId { get; set; }

    /// <summary>
    /// Cantidad que sale (siempre positiva)
    /// </summary>
    public decimal Cantidad { get; set; }

    /// <summary>
    /// Tipo de documento: VENTA, DEVOLUCION_PROVEEDOR, MERMA, USO_INTERNO, OTRO
    /// </summary>
    public string TipoDocumento { get; set; } = "VENTA";

    public string? NumeroDocumento { get; set; }
    public int? DocumentoId { get; set; }
    public string? Observaciones { get; set; }
}
