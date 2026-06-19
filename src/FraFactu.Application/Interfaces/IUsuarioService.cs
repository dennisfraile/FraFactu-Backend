using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Usuarios;

namespace FraFactu.Application.Interfaces
{
    public interface IUsuarioService
    {
        Task<PaginatedResponse<UsuarioListDto>> GetAllAsync(PaginatedRequest request, int? emisorId = null, bool? soloActivos = null, List<string>? rolesPermitidos = null, List<int>? sucursalIds = null);
        Task<UsuarioDto?> GetByIdAsync(int id, int? emisorId = null);
        Task<UsuarioDto> CreateAsync(CreateUsuarioDto dto, string? callerRole = null);
        Task<UsuarioDto> UpdateAsync(int id, UpdateUsuarioDto dto, int? emisorId = null, string? callerRole = null);
        Task<bool> DeactivateAsync(int id, int? emisorId = null);
        Task<bool> ToggleActiveAsync(int id, int? emisorId = null);
        Task RegistrarAccesoSucursal(int usuarioId, int sucursalId, string accion);
    }
}
