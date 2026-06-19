using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities;

public class HistorialUsuarioCaja : BaseEntity
{
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int CajaId { get; set; }
    public Caja Caja { get; set; } = null!;

    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    public int UsuarioAsignadorId { get; set; }
    public Usuario UsuarioAsignador { get; set; } = null!;
}
