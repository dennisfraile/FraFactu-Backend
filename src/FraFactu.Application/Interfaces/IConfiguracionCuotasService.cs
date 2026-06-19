using FraFactu.Application.DTOs.Cuotas;

namespace FraFactu.Application.Interfaces
{
    public interface IConfiguracionCuotasService
    {
        Task<ConfiguracionCuotasDto> ObtenerOCrearAsync(int emisorId);
        Task<ConfiguracionCuotasDto> ActualizarAsync(int emisorId, ActualizarConfiguracionCuotasDto dto);
    }
}
