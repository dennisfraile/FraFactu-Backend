using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities;

/// <summary>
/// Stock actual de un producto en una bodega específica
/// Tabla de resumen para consultas rápidas
/// </summary>
public class StockBodega : BaseEntity
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

    // ==========================================
    // CANTIDADES
    // ==========================================

    /// <summary>
    /// Cantidad disponible para venta
    /// </summary>
    public decimal CantidadDisponible { get; set; }

    /// <summary>
    /// Cantidad reservada (en facturas no firmadas)
    /// </summary>
    public decimal CantidadReservada { get; set; }

    /// <summary>
    /// Cantidad total = Disponible + Reservada
    /// </summary>
    public decimal CantidadTotal => CantidadDisponible + CantidadReservada;

    // ==========================================
    // COSTOS Y VALORIZACIÓN
    // ==========================================

    /// <summary>
    /// Costo promedio ponderado del producto en esta bodega
    /// Se actualiza con cada entrada
    /// </summary>
    public decimal CostoPromedio { get; set; }

    /// <summary>
    /// Valor total del inventario (CantidadTotal × CostoPromedio)
    /// </summary>
    public decimal ValorInventario => CantidadTotal * CostoPromedio;

    // ==========================================
    // METADATA
    // ==========================================

    /// <summary>
    /// Fecha de última actualización del stock
    /// </summary>
    public DateTime UltimaActualizacion { get; set; } = DateTime.UtcNow;

    // ==========================================
    // CONCURRENCIA
    // ==========================================

    /// <summary>
    /// Token de concurrencia optimista de PostgreSQL (columna de sistema xmin).
    /// Cambia automáticamente con cada UPDATE. EF Core detecta conflictos comparando este valor.
    /// </summary>
    public uint xmin { get; set; }
}
