using FraFactu.Application.DTOs.Compras;
using FraFactu.Application.Common;

namespace FraFactu.Application.Services;

/// <summary>
/// Servicio para gestión de proveedores
/// </summary>
public interface IProveedorService
{
    /// <summary>
    /// Crear un nuevo proveedor
    /// </summary>
    Task<ProveedorDto> CrearAsync(CrearProveedorDto dto, int emisorId);

    /// <summary>
    /// Actualizar un proveedor existente
    /// </summary>
    Task<ProveedorDto> ActualizarAsync(int id, ActualizarProveedorDto dto);

    /// <summary>
    /// Obtener un proveedor por ID
    /// </summary>
    Task<ProveedorDto> ObtenerPorIdAsync(int id, int emisorId);

    /// <summary>
    /// Obtener un proveedor por NIT
    /// </summary>
    Task<ProveedorDto?> ObtenerPorNITAsync(string nit, int emisorId);

    /// <summary>
    /// Listar proveedores con paginación
    /// </summary>
    Task<PagedResult<ProveedorDto>> ListarAsync(
        int emisorId,
        int pagina = 1,
        int tamanoPagina = 20,
        string? filtro = null,
        bool? soloActivos = true);

    /// <summary>
    /// Activar/Desactivar un proveedor
    /// </summary>
    Task<bool> CambiarEstadoAsync(int id, bool activo);

    /// <summary>
    /// Eliminar un proveedor (soft delete)
    /// </summary>
    Task<bool> EliminarAsync(int id);
}
