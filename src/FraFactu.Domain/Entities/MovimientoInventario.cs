using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities;

/// <summary>
/// Registro de movimientos de inventario (entradas, salidas, ajustes, traslados)
/// Auditoría completa de todos los cambios en el stock
/// </summary>
public class MovimientoInventario : BaseEntity
{
    /// <summary>
    /// FK al producto
    /// </summary>
    public int ProductoId { get; set; }
    public ProductoServicio Producto { get; set; } = null!;

    /// <summary>
    /// FK a la bodega
    /// </summary>
    public int BodegaId { get; set; }
    public Bodega Bodega { get; set; } = null!;

    /// <summary>
    /// FK al usuario que realizó el movimiento
    /// </summary>
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    // ==========================================
    // TIPO DE MOVIMIENTO
    // ==========================================

    /// <summary>
    /// Tipo de movimiento: ENTRADA, SALIDA, AJUSTE, TRASLADO
    /// </summary>
    public string TipoMovimiento { get; set; } = string.Empty;

    /// <summary>
    /// Cantidad del movimiento (positiva para entrada, negativa para salida)
    /// </summary>
    public decimal Cantidad { get; set; }

    // ==========================================
    // COSTOS
    // ==========================================

    /// <summary>
    /// Costo unitario del producto en este movimiento
    /// </summary>
    public decimal CostoUnitario { get; set; }

    /// <summary>
    /// Costo total del movimiento
    /// </summary>
    public decimal CostoTotal => Math.Abs(Cantidad) * CostoUnitario;

    // ==========================================
    // REFERENCIA AL DOCUMENTO ORIGEN
    // ==========================================

    /// <summary>
    /// Tipo de documento que generó el movimiento
    /// FACTURA, COMPRA, AJUSTE, TRASLADO, DEVOLUCION
    /// </summary>
    public string? TipoDocumento { get; set; }

    /// <summary>
    /// ID del documento origen (FacturaId, CompraId, etc.)
    /// </summary>
    public int? DocumentoId { get; set; }

    /// <summary>
    /// Número del documento origen para referencia
    /// </summary>
    public string? NumeroDocumento { get; set; }

    // ==========================================
    // TRASLADOS (si aplica)
    // ==========================================

    /// <summary>
    /// Bodega destino (solo para traslados)
    /// </summary>
    public int? BodegaDestinoId { get; set; }
    public Bodega? BodegaDestino { get; set; }

    // ==========================================
    // INFORMACIÓN ADICIONAL
    // ==========================================

    /// <summary>
    /// Fecha y hora del movimiento
    /// </summary>
    public DateTime FechaMovimiento { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Observaciones del movimiento
    /// </summary>
    public string? Observaciones { get; set; }

    /// <summary>
    /// Usuario que registró el movimiento
    /// </summary>
    public string? UsuarioRegistro { get; set; }

    // ==========================================
    // SALDOS (para kardex)
    // ==========================================

    /// <summary>
    /// Saldo anterior de cantidad
    /// </summary>
    public decimal? SaldoAnterior { get; set; }

    /// <summary>
    /// Nuevo saldo después del movimiento
    /// </summary>
    public decimal? NuevoSaldo { get; set; }

    /// <summary>
    /// Costo promedio anterior
    /// </summary>
    public decimal? CostoPromedioAnterior { get; set; }

    /// <summary>
    /// Nuevo costo promedio
    /// </summary>
    public decimal? NuevoCostoPromedio { get; set; }
}
