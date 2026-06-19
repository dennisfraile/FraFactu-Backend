namespace FraFactu.Application.Interfaces;

/// <summary>
/// Interfaz para exportación de datos
/// </summary>
public interface IExportService
{
    Task<byte[]> ExportarFacturasAExcelAsync(DateTime? fechaInicio, DateTime? fechaFin);
    Task<byte[]> ExportarInventarioAExcelAsync();
    Task<byte[]> ExportarVentasAExcelAsync(DateTime fechaInicio, DateTime fechaFin);
    Task<byte[]> GenerarPlantillaProductosExcelAsync(int emisorId);
    Task<byte[]> GenerarPlantillaServiciosExcelAsync(int emisorId, List<int>? sucursalIds = null);
}
