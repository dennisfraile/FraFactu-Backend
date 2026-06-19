using FraFactu.Application.DTOs.Integraciones;

namespace FraFactu.Application.Services;

/// <summary>
/// F4 (Plan inventario desde DTE): consulta las divergencias detectadas por
/// el job de reconciliacion. Scope tenant: el caller pasa el emisorId del
/// JWT; el servicio nunca cruza tenants.
/// </summary>
public interface IDivergenciaInventarioService
{
    Task<DivergenciasListadoDto> ListarAsync(int emisorId, DivergenciasFiltroDto filtro);
}
