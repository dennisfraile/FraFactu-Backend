using FraFactu.Application.DTOs.Common;

namespace FraFactu.Application.Interfaces
{
    public interface IHaciendaRetryService
    {
        Task<ResultadoEnvio> EnviarConReintentosAsync(
            int facturaId,
            int emisorId,
            string jsonDte,
            int version,
            string tipoDte,
            string codigoGeneracion,
            string ambiente);
    }
}
