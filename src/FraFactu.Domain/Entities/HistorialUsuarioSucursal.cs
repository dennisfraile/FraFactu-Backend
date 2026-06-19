using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities;

public class HistorialUsuarioSucursal : BaseEntity
{
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int SucursalId { get; set; }
    public Sucursal Sucursal { get; set; } = null!;

    public DateTime FechaAcceso { get; set; } // Fecha de inicio de sesión o asignación
    public string? Accion { get; set; } // "Login", "Trabajo", "Asignacion"
}
