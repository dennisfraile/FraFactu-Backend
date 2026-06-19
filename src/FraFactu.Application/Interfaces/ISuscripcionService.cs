using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Suscripciones;

namespace FraFactu.Application.Interfaces
{
    public interface ISuscripcionService
    {
        // CRUD Suscripciones (SuperAdmin)
        Task<PaginatedResponse<SuscripcionResponseDto>> GetAllAsync(PaginatedRequest request);
        Task<SuscripcionResponseDto> GetByIdAsync(int id);
        Task<SuscripcionResponseDto> CreateAsync(CreateSuscripcionDto dto);
        Task<SuscripcionResponseDto> UpdateAsync(int id, UpdateSuscripcionDto dto);
        Task DeleteAsync(int id);

        // Mi suscripción (EmisorAdmin)
        Task<MiSuscripcionResponseDto> GetMiSuscripcionAsync(int emisorId);

        // Configuración Proveedor (SuperAdmin)
        Task<ConfiguracionProveedorDto> GetConfiguracionProveedorAsync();
        Task<ConfiguracionProveedorDto> UpdateConfiguracionProveedorAsync(ConfiguracionProveedorDto dto);

        // Envío manual de recordatorio (SuperAdmin)
        Task EnviarRecordatorioManualAsync(int suscripcionId);

        // Marcar como pagado (SuperAdmin)
        Task<SuscripcionResponseDto> MarcarComoPagadoAsync(int suscripcionId);

        // Reactivar suscripción (SuperAdmin)
        Task<SuscripcionResponseDto> ReactivarAsync(int suscripcionId);

        // Vista previa del PDF de factura (SuperAdmin)
        Task<byte[]> GenerarPdfPreviewAsync(int suscripcionId);

        // Descargar PDF del mes actual (EmisorAdmin)
        Task<byte[]> GenerarPdfMiSuscripcionAsync(int emisorId);

        // Historial de facturas de suscripción
        Task<List<FacturaSuscripcionResponseDto>> GetHistorialFacturasAsync(int suscripcionId);

        // Métodos para el Background Job
        Task ProcesarRecordatoriosMensualesAsync();
        Task ProcesarRecordatoriosProximidadAsync();
    }
}
