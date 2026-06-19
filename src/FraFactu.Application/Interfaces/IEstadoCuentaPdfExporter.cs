using FraFactu.Application.DTOs.Cuotas;

namespace FraFactu.Application.Interfaces
{
    public interface IEstadoCuentaPdfExporter
    {
        byte[] GenerarPlan(EstadoCuentaPlanDto plan);
        byte[] GenerarCliente(EstadoCuentaClienteDto cliente);
    }
}
