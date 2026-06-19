namespace FraFactu.Domain.Entities;

/// <summary>
/// Tabla join many-to-many: asigna sucursales específicas a un producto/servicio
/// </summary>
public class ProductoServicioSucursal
{
    public int ProductoServicioId { get; set; }
    public ProductoServicio ProductoServicio { get; set; } = null!;

    public int SucursalId { get; set; }
    public Sucursal Sucursal { get; set; } = null!;
}
