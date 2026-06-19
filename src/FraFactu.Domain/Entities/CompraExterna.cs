using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities;

/// <summary>
/// Compra externa (factura de proveedor)
/// Registra las compras realizadas a proveedores externos
/// </summary>
public class CompraExterna : BaseEntity
{
    // ==========================================
    // PROVEEDOR
    // ==========================================

    /// <summary>
    /// FK al proveedor
    /// </summary>
    public int ProveedorId { get; set; }
    public Proveedor Proveedor { get; set; } = null!;

    /// <summary>
    /// FK a la sucursal donde se realizó la compra
    /// </summary>
    public int SucursalId { get; set; }
    public Sucursal Sucursal { get; set; } = null!;

    // ==========================================
    // INFORMACIÓN DEL DOCUMENTO
    // ==========================================

    /// <summary>
    /// Número de la factura del proveedor
    /// </summary>
    public string NumeroFactura { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de emisión de la factura del proveedor
    /// </summary>
    public DateTime FechaEmision { get; set; }

    /// <summary>
    /// Fecha de registro en el sistema
    /// </summary>
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    // ==========================================
    // MONTOS
    // ==========================================

    /// <summary>
    /// Subtotal de la compra (sin IVA)
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// IVA de la compra
    /// </summary>
    public decimal IVA { get; set; }

    /// <summary>
    /// Total de la compra (Subtotal + IVA)
    /// </summary>
    public decimal Total { get; set; }

    // ==========================================
    // ESTADO Y CONTROL
    // ==========================================

    /// <summary>
    /// Estado de la compra: BORRADOR, CONFIRMADA, ANULADA
    /// </summary>
    public string Estado { get; set; } = "BORRADOR";

    /// <summary>
    /// Fecha de confirmación (cuando se afectó inventario)
    /// </summary>
    public DateTime? FechaConfirmacion { get; set; }

    /// <summary>
    /// Fecha de anulación (si fue cancelada)
    /// </summary>
    public DateTime? FechaAnulacion { get; set; }

    /// <summary>
    /// Observaciones de la compra
    /// </summary>
    public string? Observaciones { get; set; }

    // ==========================================
    // ORIGEN Y TRAZABILIDAD DTE
    // ==========================================

    /// <summary>
    /// Origen de la compra: MANUAL o DTE
    /// </summary>
    public string Origen { get; set; } = "MANUAL";

    /// <summary>
    /// Código de generación del DTE vinculado (si origen es DTE)
    /// </summary>
    public string? CodigoGeneracionDte { get; set; }

    /// <summary>
    /// Sello de recibido del MH del DTE vinculado
    /// </summary>
    public string? SelloRecibidoDte { get; set; }

    /// <summary>
    /// Tipo de DTE vinculado (01, 03, etc.)
    /// </summary>
    public string? TipoDte { get; set; }

    /// <summary>
    /// Número de control del DTE vinculado
    /// </summary>
    public string? NumeroControlDte { get; set; }

    // ==========================================
    // NAVEGACIÓN
    // ==========================================

    /// <summary>
    /// Detalles de productos comprados
    /// </summary>
    public ICollection<CompraExternaDetalle> Detalles { get; set; } = new List<CompraExternaDetalle>();

    /// <summary>
    /// Gastos administrativos asociados a esta compra
    /// </summary>
    public ICollection<GastoAdministrativo> Gastos { get; set; } = new List<GastoAdministrativo>();
}
