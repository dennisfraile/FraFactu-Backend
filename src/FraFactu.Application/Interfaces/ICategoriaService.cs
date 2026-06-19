using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Inventario;

namespace FraFactu.Application.Interfaces;

public interface ICategoriaService
{
    Task<PaginatedResponse<CategoriaDto>> GetAllAsync(PaginatedRequest request, int emisorId, bool? soloActivos = null, bool soloRaices = false);
}
