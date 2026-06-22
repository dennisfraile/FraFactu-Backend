namespace FraFactu.Application.Interfaces
{
    /// <summary>
    /// F3 (G4). Infraestructura reutilizable de idempotencia para movimientos de
    /// inventario de origen externo reintentable (integraciones, jobs). Garantiza
    /// que una misma clave externa por emisor solo se procese una vez.
    /// </summary>
    public interface IIdempotenciaMovimientosService
    {
        /// <summary>
        /// Intenta registrar la clave externa. Devuelve <c>true</c> si quedó registrada
        /// por primera vez (el caller debe procesar el movimiento) o <c>false</c> si ya
        /// existía (el caller debe omitirlo por idempotencia).
        /// </summary>
        Task<bool> IntentarRegistrarAsync(int emisorId, string movimientoIdExterno, string? tipoDocumento, int itemsProcesados);

        /// <summary>Indica si la clave externa ya fue registrada para el emisor.</summary>
        Task<bool> YaRegistradoAsync(int emisorId, string movimientoIdExterno);
    }
}
