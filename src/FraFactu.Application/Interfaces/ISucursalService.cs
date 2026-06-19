using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Sucursales;

namespace FraFactu.Application.Interfaces
{
    /// <summary>
    /// Servicio para gestión de sucursales del emisor
    /// </summary>
    public interface ISucursalService
    {
        /// <summary>
        /// Obtiene una sucursal por su ID
        /// </summary>
        Task<SucursalDto?> GetByIdAsync(int id, int emisorId);

        /// <summary>
        /// Obtiene todas las sucursales del emisor
        /// </summary>
        Task<List<SucursalListDto>> GetAllAsync(int emisorId);

        /// <summary>
        /// Crea una nueva sucursal
        /// </summary>
        Task<SucursalDto> CreateAsync(CreateSucursalDto dto, int emisorId);

        /// <summary>
        /// Actualiza una sucursal existente
        /// </summary>
        Task<SucursalDto> UpdateAsync(int id, UpdateSucursalDto dto, int emisorId);

        /// <summary>
        /// Activa/Desactiva una sucursal
        /// </summary>
        Task<bool> ToggleActiveAsync(int id, int emisorId);
    }
}
