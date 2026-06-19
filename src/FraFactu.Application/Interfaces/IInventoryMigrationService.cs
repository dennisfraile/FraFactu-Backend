using FraFactu.Application.DTOs;

namespace FraFactu.Application.Interfaces;

public interface IInventoryMigrationService
{
    Task<InventoryExportResponseDto> ExportarInventarioAsync(int hubId, Guid migrationId);
    Task ImportarDesdeSmartInventoryAsync(InventoryImportRequestDto request);
}
