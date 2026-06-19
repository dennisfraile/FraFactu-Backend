using FraFactu.Application.DTOs.Cuotas;

namespace FraFactu.Application.Interfaces
{
    public interface IAgingExcelExporter
    {
        byte[] Generar(AgingReporteDto reporte);
    }
}
