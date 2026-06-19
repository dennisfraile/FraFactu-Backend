namespace FraFactu.Application.DTOs.Compras;

/// <summary>
/// DTO para mostrar una compra externa completa
/// </summary>
public class CompraExternaDto
{
    public int Id { get; set; }

    // Proveedor
    /// <summary>
    /// ID del proveedor
    /// </summary>
    public int ProveedorId { get; set; }
    public string ProveedorNIT { get; set; } = string.Empty; // Keeping this as it was not explicitly removed by the instruction's snippet
    /// <summary>
    /// Nombre del proveedor
    /// </summary>
    public string ProveedorNombre { get; set; } = string.Empty;

    // Sucursal
    /// <summary>
    /// ID de la sucursal
    /// </summary>
    public int SucursalId { get; set; }

    /// <summary>
    /// Nombre de la sucursal
    /// </summary>
    public string SucursalNombre { get; set; } = string.Empty;

    // Documento
    /// <summary>
    /// Número de la factura del proveedor
    /// </summary>
    public string NumeroFactura { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public DateTime FechaRegistro { get; set; }

    // Montos
    public decimal Subtotal { get; set; }
    public decimal IVA { get; set; }
    public decimal Total { get; set; }

    // Estado
    public string Estado { get; set; } = string.Empty; // BORRADOR, CONFIRMADA, ANULADA
    public DateTime? FechaConfirmacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }

    public string? Observaciones { get; set; }

    // Origen y trazabilidad DTE
    public string Origen { get; set; } = "MANUAL";
    public string? CodigoGeneracionDte { get; set; }
    public string? SelloRecibidoDte { get; set; }
    public string? TipoDte { get; set; }
    public string? NumeroControlDte { get; set; }

    // Detalles
    public List<CompraDetalleDto> Detalles { get; set; } = new();

    // Gastos
    public List<GastoAdministrativoDto> Gastos { get; set; } = new();

    // Totales calculados
    public decimal TotalProductosInventario { get; set; }
    public decimal TotalGastosAdministrativos { get; set; }
    public int CantidadItems { get; set; }
}
