using FraFactu.Domain.Entities;

namespace FraFactu.Application.Interfaces
{
    /// <summary>
    /// Sincroniza el detalle del lote de contingencia con el estado de la factura
    /// (extraído de FacturaService, Fase 3). Colaborador compartido (núcleo + invalidación).
    /// </summary>
    public interface IFacturaLoteSync
    {
        Task SincronizarLoteDetalleAsync(FacturaElectronica factura, string? motivoRechazo = null);
    }
}
