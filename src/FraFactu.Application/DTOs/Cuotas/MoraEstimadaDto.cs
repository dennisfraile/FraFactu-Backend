namespace FraFactu.Application.DTOs.Cuotas
{
    public class MoraEstimadaDto
    {
        public int Numero { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaPactada { get; set; }
        public int DiasAtraso { get; set; }
        public decimal InteresMora { get; set; }
        public bool MoraHabilitada { get; set; }
    }
}
