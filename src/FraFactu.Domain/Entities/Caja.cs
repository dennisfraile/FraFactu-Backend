using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities;

public class Caja : BaseEntity
{
    public string Codigo { get; set; } = string.Empty; // Código interno o de Hacieda para Punto de Venta
    public string Nombre { get; set; } = string.Empty; // "Caja 1", "Caja Principal"

    public string CodPuntoVenta { get; set; } = string.Empty;
    public string CodPuntoVentaMH { get; set; } = string.Empty;

    // Relación con Sucursal
    public int SucursalId { get; set; }
    public Sucursal Sucursal { get; set; } = null!;

    // Si queremos restringir cajas activas
    // BaseEntity ya tiene Activo, pero a veces se oculta. Usaremos el de base.

    // Relación con Facturas
    public ICollection<FacturaElectronica> Facturas { get; set; } = new List<FacturaElectronica>();
}
