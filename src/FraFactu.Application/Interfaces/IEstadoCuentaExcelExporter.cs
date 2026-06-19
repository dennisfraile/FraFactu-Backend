using FraFactu.Application.DTOs.Cuotas;

namespace FraFactu.Application.Interfaces
{
    public interface IEstadoCuentaExcelExporter
    {
        byte[] GenerarPlan(EstadoCuentaPlanDto plan);
        byte[] GenerarCliente(EstadoCuentaClienteDto cliente);
    }
}
