namespace FraFactu.Application.DTOs.Facturas
{
    /// <summary>
    /// Resultado de la detección de contingencias que requieren presentar el "Informe Técnico
    /// de Contingencia" (Normativa DTE, regla 13.2.1.1): cuando la contingencia de un sujeto
    /// pasivo persiste por más de 3 días consecutivos, debe presentarlo a Hacienda antes de
    /// transmitir el Evento de Contingencia. Solo detecta y alerta; no transmite nada.
    /// </summary>
    public class InformeTecnicoContingenciaResultadoDto
    {
        /// <summary>Cantidad de emisores cuya contingencia en curso supera los 3 días consecutivos.</summary>
        public int EmisoresQueRequierenInforme { get; set; }
    }
}
