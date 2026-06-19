using FraFactu.Application.DTOs;

namespace FraFactu.Application.Interfaces
{
    public interface ICorrelativoInicialService
    {
        /// <summary>Devuelve el UltimoCorrelativo configurado, o 0 si no hay registro.</summary>
        Task<int> ObtenerUltimoCorrelativoAsync(int emisorId, string tipoDte, int anio, string ambiente);

        /// <summary>Lista los correlativos iniciales del emisor para un año/ambiente, con flag de bloqueo.</summary>
        Task<List<CorrelativoInicialDto>> ListarAsync(int emisorId, int anio, string ambiente);

        /// <summary>Crea o actualiza el correlativo inicial. Lanza InvalidOperationException si ya hay DTE emitidos.</summary>
        Task<CorrelativoInicialDto> UpsertAsync(int emisorId, CorrelativoInicialUpsertDto dto);

        /// <summary>True si existe al menos una factura para (emisor, tipoDte, anio, ambiente).</summary>
        Task<bool> ExistenFacturasAsync(int emisorId, string tipoDte, int anio, string ambiente);
    }
}
