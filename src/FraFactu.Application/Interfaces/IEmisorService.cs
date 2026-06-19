using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Emisores;

namespace FraFactu.Application.Interfaces
{
    /// <summary>
    /// Servicio para gestión de Emisores (empresas)
    /// </summary>
    public interface IEmisorService
    {
        /// <summary>
        /// Obtener todos los emisores (Solo SuperAdmin)
        /// </summary>
        Task<PaginatedResponse<EmisorDto>> GetAllAsync(PaginatedRequest request);

        /// <summary>
        /// Obtener emisor por ID (Solo SuperAdmin)
        /// </summary>
        Task<EmisorDto?> GetByIdAsync(int id);

        /// <summary>
        /// UsuarioCompartido + Plan B Hub-as-Emisor: devuelve la metadata
        /// minima (id, nombre, nit) de los Emisores que vienen en el claim
        /// <c>emisores_accesibles</c> del JWT. Lo usa el selector de Emisor
        /// del FE para mostrar nombres en lugar de IDs. Cualquier rol puede
        /// llamar este metodo siempre y cuando los IDs salgan de su JWT.
        /// </summary>
        Task<List<EmisorResumenDto>> GetResumenByIdsAsync(IEnumerable<int> ids);

        /// <summary>
        /// Obtener perfil del emisor actual (EmisorAdmin)
        /// </summary>
        Task<EmisorDto?> GetMiPerfilAsync(int emisorId);

        /// <summary>
        /// Crear nuevo emisor - Onboarding (Solo SuperAdmin)
        /// </summary>
        Task<EmisorDto> CreateAsync(CreateEmisorDto dto);

        /// <summary>
        /// Actualizar emisor (Solo SuperAdmin)
        /// </summary>
        Task<EmisorDto?> UpdateAsync(int id, UpdateEmisorDto dto);

        /// <summary>
        /// Actualizar perfil del emisor actual (EmisorAdmin)
        /// </summary>
        Task<EmisorDto?> UpdateMiPerfilAsync(int emisorId, UpdatePerfilEmisorDto dto);

        /// <summary>
        /// Desactivar emisor (Solo SuperAdmin)
        /// </summary>
        Task<bool> DeactivateAsync(int id);

        /// <summary>
        /// Alternar estado activo/inactivo del emisor (Solo SuperAdmin)
        /// </summary>
        Task<bool> ToggleActiveAsync(int id);

        /// <summary>
        /// Actualizar URL del logo del emisor (EmisorAdmin)
        /// </summary>
        Task<bool> UpdateLogoAsync(int emisorId, string logoUrl);

        /// <summary>
        /// Guardar credenciales OAuth2 de Gmail en el emisor
        /// </summary>
        Task UpdateGmailOAuthAsync(int emisorId, string encryptedRefreshToken, string gmailEmail);

        /// <summary>
        /// Desconectar OAuth2 de Gmail del emisor
        /// </summary>
        Task DisconnectGmailOAuthAsync(int emisorId);

        /// <summary>
        /// Sync server-to-server desde SmartHub: fija el flag Activo del Emisor
        /// cuyo HubId coincide con el indicado. Idempotente: si ya estaba en el
        /// estado pedido devuelve Cambio=false. Si no existe Emisor para ese
        /// HubId devuelve Encontrado=false sin error.
        /// </summary>
        Task<DTOs.Sync.SyncEmisorToggleResponseDto> SetActivoByHubIdAsync(int hubId, bool activo);
    }
}
