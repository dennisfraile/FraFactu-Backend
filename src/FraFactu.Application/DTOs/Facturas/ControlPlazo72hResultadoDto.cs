namespace FraFactu.Application.DTOs.Facturas
{
    /// <summary>
    /// Resultado del control del plazo de 72 h para transmitir DTE diferidos/contingencia
    /// (Normativa DTE). Mide cuántas facturas en PENDIENTE_LOTE están vencidas o por vencer
    /// respecto a su FechaEmision. No cambia el estado de las facturas: solo marca y alerta.
    /// </summary>
    public class ControlPlazo72hResultadoDto
    {
        /// <summary>Facturas cuyo plazo de 72 h desde la emisión ya venció.</summary>
        public int Vencidas { get; set; }

        /// <summary>De las vencidas, las que se marcaron en esta pasada (no estaban marcadas antes).</summary>
        public int NuevasVencidas { get; set; }

        /// <summary>Facturas que aún no vencen pero entran en la ventana de aviso previa.</summary>
        public int PorVencer { get; set; }
    }
}
