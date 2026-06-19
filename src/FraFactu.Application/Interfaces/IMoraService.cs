namespace FraFactu.Application.Interfaces
{
    public interface IMoraService
    {
        /// <summary>
        /// Calcula el interés por mora (monto total con IVA incluido) de una cuota.
        /// Devuelve 0 si no aplica.
        /// </summary>
        /// <param name="montoCuota">Monto de la cuota (con IVA).</param>
        /// <param name="fechaPactada">Vencimiento de la cuota.</param>
        /// <param name="fechaPago">Fecha en que se paga.</param>
        /// <param name="tasaMoraMensual">Tasa mensual (ej. 0.03).</param>
        /// <param name="diasGracia">Días de gracia.</param>
        /// <param name="moraHabilitada">Si la mora está activa.</param>
        decimal CalcularMora(decimal montoCuota, DateTime fechaPactada, DateTime fechaPago,
            decimal tasaMoraMensual, int diasGracia, bool moraHabilitada);
    }
}
