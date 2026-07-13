using FraFactu.Application.DTOs.Invalidacion;

namespace FraFactu.Application.Interfaces
{
    /// <summary>
    /// Anulación / invalidación / descarte de Facturas Electrónicas (extraído de FacturaService, Fase 3).
    /// </summary>
    public interface IFacturaInvalidacionService
    {
        Task<bool> AnularAsync(int facturaId, int emisorId, string motivo);
        Task<bool> ActualizarEstadoHaciendaAsync(int facturaId, int emisorId, string estado, string? selloRecepcion = null);
        Task<InvalidacionDto> InvalidarFacturaAsync(int facturaId, AnularFacturaDto dto, int emisorId);
        Task DescartarFacturaRechazadaAsync(int facturaId, int emisorId);
    }
}
