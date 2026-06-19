using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Inventario;

namespace FraFactu.Application.Interfaces;

public interface IMarcaService
{
    Task<PaginatedResponse<MarcaDto>> GetAllAsync(PaginatedRequest request, int emisorId, bool? soloActivas = null);
}
