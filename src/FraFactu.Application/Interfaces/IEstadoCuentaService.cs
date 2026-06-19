using FraFactu.Application.DTOs.Cuotas;

namespace FraFactu.Application.Interfaces
{
    public interface IEstadoCuentaService
    {
        Task<EstadoCuentaPlanDto?> GenerarPlanAsync(int planId, int emisorId);
        Task<EstadoCuentaClienteDto?> GenerarClienteAsync(int receptorId, int emisorId);
    }
}
