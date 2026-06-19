using FraFactu.Application.DTOs.Facturas;

namespace FraFactu.Application.Interfaces
{
    /// <summary>
    /// Servicio de control de saldos para DTEs referenciados por Notas de Crédito
    /// </summary>
    public interface ISaldoDteService
    {
        /// <summary>
        /// Obtener el saldo de un DTE por su código de generación
        /// </summary>
        Task<SaldoDteDto?> ObtenerSaldoPorCodigoGeneracion(string codigoGeneracion, int emisorId);

        /// <summary>
        /// Validar si hay saldo disponible suficiente para una NCE
        /// </summary>
        Task<bool> ValidarSaldoDisponible(string codigoGeneracion, decimal montoNCE, int emisorId);

        /// <summary>
        /// Actualizar el saldo tras emisión exitosa de una NCE (transaccional)
        /// </summary>
        Task ActualizarSaldoAsync(string codigoGeneracion, decimal montoNCE, int emisorId);

        /// <summary>
        /// Inicializar registro de saldo cuando un DTE pasa a PROCESADO
        /// </summary>
        Task InicializarSaldoAsync(string codigoGeneracion, string tipoDte, decimal montoTotal, int emisorId);
    }
}
