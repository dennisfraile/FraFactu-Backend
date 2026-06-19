namespace FraFactu.Application.Interfaces
{
    /// <summary>
    /// Consulta el máximo correlativo de NumeroControl en todas las BDs
    /// (producción, test, demo) para evitar colisiones con MH.
    /// </summary>
    public interface ICrossDbCorrelativoService
    {
        /// <summary>
        /// Obtiene el máximo correlativo encontrado en todas las BDs
        /// para un emisor y tipo de DTE, de forma global (sin importar sucursal/caja).
        /// </summary>
        /// <param name="emisorId">ID del emisor</param>
        /// <param name="tipoDte">Tipo de DTE (ej: "01", "03")</param>
        /// <param name="anio">Año de emisión para el correlativo</param>
        /// <param name="ambiente">Código de ambiente MH ("00" pruebas, "01" producción)</param>
        /// <returns>El máximo correlativo encontrado, o 0 si no hay facturas</returns>
        Task<int> ObtenerMaxCorrelativoAsync(int emisorId, string tipoDte, int anio, string ambiente);
    }
}
