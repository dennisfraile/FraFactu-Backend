using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Receptores;

namespace FraFactu.Application.Interfaces
{
    /// <summary>
    /// Servicio para gestión de receptores (clientes) del emisor
    /// </summary>
    public interface IReceptorService
    {
        /// <summary>
        /// Obtiene un receptor por su ID
        /// </summary>
        Task<ReceptorDto?> GetByIdAsync(int id, int emisorId);

        /// <summary>
        /// Obtiene todos los receptores del emisor (paginado)
        /// </summary>
        Task<PaginatedResponse<ReceptorListDto>> GetAllAsync(
            PaginatedRequest request, int emisorId, bool? soloActivos = null,
            string? search = null, string? orderBy = null, string? orderDirection = null,
            DateTime? fechaDesde = null, DateTime? fechaHasta = null,
            bool soloSujetosExcluidos = false);

        /// <summary>
        /// Crea un nuevo receptor
        /// </summary>
        Task<ReceptorDto> CreateAsync(CreateReceptorDto dto, int emisorId);

        /// <summary>
        /// Actualiza un receptor existente
        /// </summary>
        Task<ReceptorDto> UpdateAsync(int id, UpdateReceptorDto dto, int emisorId);

        /// <summary>
        /// Busca receptores por documento o nombre
        /// </summary>
        Task<List<ReceptorListDto>> SearchAsync(string searchTerm, int emisorId, bool soloSujetosExcluidos = false);
    }
}
