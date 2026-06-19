namespace FraFactu.Application.DTOs.Cuotas
{
    /// <summary>Definición de una cuota al crear el plan. Se usa Monto O Porcentaje.</summary>
    public class DefinicionCuotaDto
    {
        public int Numero { get; set; }
        public decimal? Monto { get; set; }
        public decimal? Porcentaje { get; set; }
        public DateTime FechaPactada { get; set; }
    }
}
