using System.Collections.Generic;

namespace FraFactu.Application.DTOs.Common
{
    public class ResultadoEnvio
    {
        public bool Exitoso { get; set; }
        public bool EsRechazoMH { get; set; } // True si MH respondió pero rechazó (no es contingencia)
        public bool EsContingencia { get; set; } // True si falló conexión tras reintentos
        public string? CodigoMsg { get; set; } // Código de rechazo MH (ej: "004")
        public string? DescripcionMsg { get; set; } // Descripción del rechazo MH
        public string MensajeError { get; set; } = string.Empty;
        public DiagnosticoConectividad? Diagnostico { get; set; }

        // Datos de éxito
        public string? SelloRecibido { get; set; }
        public string? DocumentoFirmado { get; set; }
        public List<string>? Observaciones { get; set; }
    }
}
