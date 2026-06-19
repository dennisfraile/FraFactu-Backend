namespace FraFactu.Application.DTOs.Compras;

/// <summary>
/// DTO para actualizar una compra externa en estado BORRADOR
/// </summary>
public class ActualizarCompraExternaDto
{
    /// <summary>
    /// ID del proveedor
    /// </summary>
    public int ProveedorId { get; set; }

    /// <summary>
    /// ID de la sucursal donde se realiza la compra
    /// </summary>
    public int SucursalId { get; set; }

    /// <summary>
    /// Número de la factura del proveedor
    /// </summary>
    public string NumeroFactura { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de emisión de la factura
    /// </summary>
    public DateTime FechaEmision { get; set; }

    /// <summary>
    /// Subtotal de la compra
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// IVA de la compra
    /// </summary>
    public decimal IVA { get; set; }

    /// <summary>
    /// Total de la compra
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// Observaciones de la compra
    /// </summary>
    public string? Observaciones { get; set; }

    /// <summary>
    /// Detalles de productos comprados
    /// </summary>
    public List<CrearCompraDetalleDto> Detalles { get; set; } = new();

    /// <summary>
    /// Gastos administrativos asociados
    /// </summary>
    public List<CrearGastoDto> Gastos { get; set; } = new();
}
