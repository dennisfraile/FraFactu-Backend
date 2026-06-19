namespace FraFactu.Domain.Entities
{
    public class UsuarioCaja
    {
        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;

        public int CajaId { get; set; }
        public Caja Caja { get; set; } = null!;
    }
}
