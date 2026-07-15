using FraFactu.Application.DTOs.Facturas;

namespace FraFactu.Application.Interfaces
{
    /// <summary>
    /// Jobs de fondo de contingencia (Fase 4 del refactor de FacturaService).
    /// Orquestados por los BackgroundServices vía la fachada IFacturaService.
    /// </summary>
    public interface IFacturaJobsService
    {
        /// <summary>
        /// Controla el plazo de 72 h para transmitir DTE diferidos/contingencia desde su
        /// FechaEmision. Marca (con una observación) y alerta las facturas en PENDIENTE_LOTE
        /// que ya vencieron el plazo o están por vencer. No cambia el estado ni bloquea.
        /// Pensado para ejecutarse desde un background service.
        /// </summary>
        Task<ControlPlazo72hResultadoDto> MarcarDiferidosPorVencer72hAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Detecta las contingencias en curso (por emisor) que persisten más de 3 días consecutivos
        /// y que, por tanto, obligan a presentar el "Informe Técnico de Contingencia" a Hacienda antes
        /// de transmitir el Evento de Contingencia (regla 13.2.1.1). Solo detecta y alerta (log/telemetría);
        /// el informe se presenta por los medios que disponga la Administración Tributaria, fuera de la API.
        /// </summary>
        Task<InformeTecnicoContingenciaResultadoDto> DetectarContingenciasParaInformeTecnicoAsync(CancellationToken cancellationToken = default);
    }
}
