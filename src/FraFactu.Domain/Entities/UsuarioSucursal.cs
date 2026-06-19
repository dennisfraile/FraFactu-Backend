using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities;

/// <summary>
/// Tabla join many-to-many: asigna sucursales específicas a un usuario
/// </summary>
public class UsuarioSucursal
{
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int SucursalId { get; set; }
    public Sucursal Sucursal { get; set; } = null!;
}
