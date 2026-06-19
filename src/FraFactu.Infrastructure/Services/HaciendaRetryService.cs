using FraFactu.Application.DTOs.Common;
using FraFactu.Application.Interfaces;
using FraFactu.Application.Interfaces.Hacienda;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Services
{
    public class HaciendaRetryService : IHaciendaRetryService
    {
        private readonly IHaciendaApiService _haciendaApiService;
        private readonly IContingenciaDiagnosticoService _diagnosticoService;
        private readonly ILogger<HaciendaRetryService> _logger;

        // MH Manual: máximo 2 reintentos (3 intentos total)
        private const int MAX_REINTENTOS = 2;
        // MH Manual: timeout de 8 segundos por intento
        private const int TIMEOUT_SEGUNDOS = 8;

        public HaciendaRetryService(
            IHaciendaApiService haciendaApiService,
            IContingenciaDiagnosticoService diagnosticoService,
            ILogger<HaciendaRetryService> logger)
        {
            _haciendaApiService = haciendaApiService;
            _diagnosticoService = diagnosticoService;
            _logger = logger;
        }

        public async Task<ResultadoEnvio> EnviarConReintentosAsync(
            int facturaId,
            int emisorId,
            string jsonDte,
            int version,
            string tipoDte,
            string codigoGeneracion,
            string ambiente)
        {
            Exception? ultimaExcepcion = null;
            string? ultimoDocumentoFirmado = null;

            // MH Manual 3.3: 1 intento original + 2 reintentos = 3 intentos total
            for (int intento = 0; intento <= MAX_REINTENTOS; intento++)
            {
                try
                {
                    _logger.LogInformation("Intento de envío {Intento}/{Max} para factura {Id}",
                        intento + 1, MAX_REINTENTOS + 1, facturaId);

                    // MH Manual: timeout de 8 segundos por intento
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TIMEOUT_SEGUNDOS));
                    var tareaEnvio = _haciendaApiService.TransmitirDteJsonAsync(
                        emisorId, jsonDte, version, tipoDte, codigoGeneracion);

                    var respuesta = await tareaEnvio.WaitAsync(cts.Token);
                    ultimoDocumentoFirmado = respuesta.DocumentoFirmado;

                    if (respuesta.Estado == "PROCESADO")
                    {
                        return new ResultadoEnvio
                        {
                            Exitoso = true,
                            SelloRecibido = respuesta.SelloRecibido,
                            DocumentoFirmado = respuesta.DocumentoFirmado
                        };
                    }
                    else if (respuesta.Estado == "RECHAZADO")
                    {
                        // Código 004 = "YA EXISTE UN REGISTRO CON ESE VALOR"
                        if (respuesta.CodigoMsg == "004")
                        {
                            _logger.LogWarning("DTE {CodigoGeneracion} ya existe en MH (código 004). Consultando estado real...", codigoGeneracion);
                            var resultadoConsulta = await ConsultarEstadoRealAsync(emisorId, codigoGeneracion, tipoDte, respuesta.DocumentoFirmado);
                            if (resultadoConsulta != null) return resultadoConsulta;
                        }

                        // MH respondió pero rechazó -> No es contingencia, es error de datos
                        return new ResultadoEnvio
                        {
                            Exitoso = false,
                            EsRechazoMH = true,
                            CodigoMsg = respuesta.CodigoMsg,
                            DescripcionMsg = respuesta.DescripcionMsg,
                            MensajeError = respuesta.DescripcionMsg ?? "Rechazado por MH",
                            Observaciones = respuesta.Observaciones
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Fallo en intento {Intento} para factura {Id}", intento + 1, facturaId);
                    ultimaExcepcion = ex;

                    // MH Manual 3.3: Consultar estado ANTES de reintentar
                    var resultadoConsulta = await ConsultarEstadoRealAsync(emisorId, codigoGeneracion, tipoDte, ultimoDocumentoFirmado);
                    if (resultadoConsulta != null) return resultadoConsulta;

                    // Si la consulta dice que no fue recibido y quedan reintentos, continuar
                    if (intento < MAX_REINTENTOS)
                    {
                        _logger.LogInformation("DTE no encontrado en MH. Reintentando envío para factura {Id}...", facturaId);
                    }
                }
            }

            // Todos los intentos fallaron y las consultas de estado no encontraron el DTE
            // -> Iniciar proceso de contingencia
            _logger.LogWarning("Todos los intentos agotados para factura {Id}. Iniciando contingencia.", facturaId);
            var diagnostico = await _diagnosticoService.DiagnosticarFalloAsync(
                ultimaExcepcion ?? new Exception("Fallo de conexión persistente"));

            return new ResultadoEnvio
            {
                Exitoso = false,
                EsContingencia = true,
                MensajeError = ultimaExcepcion?.Message ?? "Error de conexión",
                Diagnostico = diagnostico
            };
        }

        /// <summary>
        /// Consulta el estado real del DTE en MH.
        /// Retorna ResultadoEnvio si se pudo determinar el estado (PROCESADO o RECHAZADO).
        /// Retorna null si la consulta falló o el DTE no existe en MH (para continuar reintentando).
        /// </summary>
        private async Task<ResultadoEnvio?> ConsultarEstadoRealAsync(
            int emisorId, string codigoGeneracion, string tipoDte, string? documentoFirmado)
        {
            try
            {
                _logger.LogInformation("Consultando estado del DTE {CodigoGeneracion} en MH...", codigoGeneracion);
                var estado = await _haciendaApiService.ConsultarEstadoDteAsync(emisorId, codigoGeneracion, tipoDte);

                if (estado.Estado == "PROCESADO")
                {
                    _logger.LogInformation("DTE {CodigoGeneracion} confirmado como PROCESADO en MH. Sello: {Sello}",
                        codigoGeneracion, estado.SelloRecibido);
                    return new ResultadoEnvio
                    {
                        Exitoso = true,
                        SelloRecibido = estado.SelloRecibido,
                        DocumentoFirmado = documentoFirmado
                    };
                }
                else if (estado.Estado == "RECHAZADO")
                {
                    return new ResultadoEnvio
                    {
                        Exitoso = false,
                        EsRechazoMH = true,
                        MensajeError = "Rechazado por MH (Consultado)",
                        Observaciones = estado.Observaciones
                    };
                }

                // Estado desconocido o no encontrado -> retornar null para continuar reintentando
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo consultar estado del DTE {CodigoGeneracion}.", codigoGeneracion);
                return null;
            }
        }
    }
}
