using FraFactu.Application.DTOs.Cuotas;

namespace FraFactu.Application.Interfaces
{
    public interface IAgingService
    {
        /// <summary>Genera el reporte de antigüedad de saldos del emisor (fecha de corte = hoy).</summary>
        Task<AgingReporteDto> GenerarAsync(int emisorId);
    }
}
