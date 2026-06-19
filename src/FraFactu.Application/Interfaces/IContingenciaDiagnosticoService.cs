using FraFactu.Application.DTOs.Common;

namespace FraFactu.Application.Interfaces
{
    public interface IContingenciaDiagnosticoService
    {
        Task<DiagnosticoConectividad> DiagnosticarFalloAsync(Exception excepcion);
    }
}
