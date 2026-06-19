namespace FraFactu.Application.DTOs.Cuotas
{
    public class ActualizarConfiguracionCuotasDto
    {
        public decimal TasaMoraMensual { get; set; }
        public int DiasGracia { get; set; }
        public bool MoraHabilitada { get; set; }
        public bool RecordatoriosHabilitados { get; set; }
        public int DiasAntesRecordatorio { get; set; }
    }
}
