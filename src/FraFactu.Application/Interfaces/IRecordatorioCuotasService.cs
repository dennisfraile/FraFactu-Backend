namespace FraFactu.Application.Interfaces
{
    public interface IRecordatorioCuotasService
    {
        /// <summary>
        /// Marca cuotas/planes vencidos y envía recordatorios (por vencer / vencida)
        /// por email al cliente + notificación in-app al emisor. Idempotente por flags.
        /// </summary>
        Task ProcesarAsync();
    }
}
