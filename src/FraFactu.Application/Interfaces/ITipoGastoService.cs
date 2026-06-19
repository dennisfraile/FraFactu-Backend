using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Inventario;

namespace FraFactu.Application.Interfaces;

public interface ITipoGastoService
{
    Task<PaginatedResponse<TipoGastoDto>> GetAllAsync(PaginatedRequest request, int emisorId, bool? soloActivos = null);
}
