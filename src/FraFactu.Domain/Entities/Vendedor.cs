using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities;

public class Vendedor : BaseEntity
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Porcentaje de comisión sobre ventas (0-100)
    /// </summary>
    public decimal PorcentajeComision { get; set; } = 0;

    // Relación con Emisor (Tenant)
    public int EmisorId { get; set; }
    public Emisor Emisor { get; set; } = null!;

    /// <summary>
    /// Si es true, el vendedor tiene acceso a todas las sucursales del emisor.
    /// Si es false, solo tiene acceso a las sucursales asignadas en VendedorSucursales.
    /// </summary>
    public bool AccesoTodasSucursales { get; set; } = false;

    /// <summary>
    /// Sucursales asignadas al vendedor (many-to-many)
    /// </summary>
    public ICollection<VendedorSucursal> VendedorSucursales { get; set; } = new List<VendedorSucursal>();

    // Relación con Facturas
    public ICollection<FacturaElectronica> Facturas { get; set; } = new List<FacturaElectronica>();
}
