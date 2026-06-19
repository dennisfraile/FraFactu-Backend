using FraFactu.Application.DTOs.Import;

namespace FraFactu.Application.Interfaces;

/// <summary>
/// Interfaz para importación masiva de datos desde Excel
/// </summary>
public interface IImportService
{
    Task<ImportResultDto> ImportarProductosDesdeExcelAsync(Stream excelStream, int emisorId);
    Task<ImportResultDto> ImportarServiciosDesdeExcelAsync(Stream excelStream, int emisorId);
}
