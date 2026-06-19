namespace FraFactu.Application.DTOs.Facturas
{
    /// <summary>
    /// Resultado de una pasada del reintento automático de facturas en estado ERROR
    /// (regla 13.2.1 de la Normativa DTE: reintentos de transmisión a Hacienda).
    /// </summary>
    public class ReintentoErrorResultadoDto
    {
        /// <summary>Facturas que se reintentaron (se reenviaron a Hacienda).</summary>
        public int Reintentadas { get; set; }

        /// <summary>Facturas que, tras agotar el tope de intentos, se escalaron a contingencia (PENDIENTE_LOTE).</summary>
        public int Escaladas { get; set; }

        /// <summary>Facturas cuyo reintento volvió a fallar en esta pasada.</summary>
        public int Fallidas { get; set; }

        /// <summary>Total de facturas en ERROR evaluadas como candidatas en esta pasada.</summary>
        public int Candidatas { get; set; }
    }
}
