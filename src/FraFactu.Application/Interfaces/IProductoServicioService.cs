using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.ProductosServicios;

namespace FraFactu.Application.Interfaces
{
    /// <summary>
    /// Servicio para gestión de productos del emisor
    /// </summary>
    public interface IProductoServicioService
    {
        /// <summary>
        /// Obtiene un producto por su ID
        /// </summary>
        Task<ProductoServicioDto?> GetByIdAsync(int id, int emisorId);

        /// <summary>
        /// Obtiene todos los productos del emisor (paginado)
        /// </summary>
        Task<PaginatedResponse<ProductoServicioListDto>> GetAllAsync(PaginatedRequest request, int emisorId, int? sucursalId = null, int? categoriaId = null, int? marcaId = null, string? unidadMedida = null, string? tipo = null, bool incluirInactivos = false);

        /// <summary>
        /// Crea un nuevo producto
        /// </summary>
        Task<ProductoServicioDto> CreateAsync(CreateProductoServicioDto dto, int emisorId);

        /// <summary>
        /// Actualiza un producto existente
        /// </summary>
        Task<ProductoServicioDto> UpdateAsync(int id, UpdateProductoServicioDto dto, int emisorId);

        /// <summary>
        /// Activa/Desactiva un producto. La reactivación limpia la auditoría de baja.
        /// </summary>
        Task<bool> ToggleActiveAsync(int id, int emisorId);

        /// <summary>
        /// Da de baja (soft-delete) un producto registrando motivo, quién y cuándo.
        /// </summary>
        Task DesactivarAsync(int id, int emisorId, DesactivarProductoServicioDto dto, int? usuarioId);

        /// <summary>
        /// Busca productos por código o nombre
        /// </summary>
        Task<List<ProductoServicioListDto>> SearchAsync(string searchTerm, int emisorId, int? sucursalId = null);
    }
}
