using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities;

/// <summary>
/// Bodega o almacén para gestión de inventario
/// </summary>
public class Bodega : BaseEntity
{
    /// <summary>
    /// Código único de la bodega
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Nombre de la bodega
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Dirección física de la bodega
    /// </summary>
    public string? Direccion { get; set; }

    /// <summary>
    /// FK a sucursal (opcional - bodega puede estar asociada a una sucursal)
    /// </summary>
    public int? SucursalId { get; set; }
    public Sucursal? Sucursal { get; set; }

    /// <summary>
    /// Indica si es la bodega principal
    /// </summary>
    public bool EsPrincipal { get; set; }

    /// <summary>
    /// Indica si la bodega está activa
    /// </summary>
    public bool Activa { get; set; } = true;

    /// <summary>
    /// Stock de productos en esta bodega
    /// </summary>
    public ICollection<StockBodega> Stocks { get; set; } = new List<StockBodega>();

    /// <summary>
    /// Movimientos de inventario de esta bodega
    /// </summary>
    public ICollection<MovimientoInventario> Movimientos { get; set; } = new List<MovimientoInventario>();
}
