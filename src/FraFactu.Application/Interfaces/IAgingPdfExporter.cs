using FraFactu.Application.DTOs.Cuotas;

namespace FraFactu.Application.Interfaces
{
    public interface IAgingPdfExporter
    {
        byte[] Generar(AgingReporteDto reporte);
    }
}
