namespace FraFactu.Application.DTOs.Common
{
    public class DiagnosticoConectividad
    {
        public int? TipoContingenciaSugerido { get; set; }  // 1 o 3, null si no determinó
        public string Razon { get; set; } = string.Empty;   // Explicación técnica
        public bool EsCerteza { get; set; }                 // true = seguro, false = sugerencia
        public string DetalleError { get; set; } = string.Empty; // Mensaje original del error
    }
}
