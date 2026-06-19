namespace FraFactu.Domain.Entities;

/// <summary>
/// Tabla join many-to-many: asigna sucursales específicas a un vendedor
/// </summary>
public class VendedorSucursal
{
    public int VendedorId { get; set; }
    public Vendedor Vendedor { get; set; } = null!;

    public int SucursalId { get; set; }
    public Sucursal Sucursal { get; set; } = null!;
}
