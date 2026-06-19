using FraFactu.Application.DTOs.Hacienda;
using FraFactu.Application.Interfaces.Hacienda;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FraFactu.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class HaciendaController : ControllerBase
    {
        private readonly IHaciendaApiService _haciendaService;
        private readonly IHaciendaAuthService _haciendaAuthService;
        private readonly ILogger<HaciendaController> _logger;

        public HaciendaController(
            IHaciendaApiService haciendaService,
            IHaciendaAuthService haciendaAuthService,
            ILogger<HaciendaController> logger)
        {
            _haciendaService = haciendaService;
            _haciendaAuthService = haciendaAuthService;
            _logger = logger;
        }

        /// <summary>
        /// Prueba de autenticación con Hacienda (temporal para debugging)
        /// </summary>
        [HttpGet("test-auth/{emisorId}")]
        [AllowAnonymous] // Temporal para prueba rápida
        public async Task<IActionResult> TestAuth(int emisorId)
        {
            try
            {
                _logger.LogInformation("[TEST-AUTH] Iniciando prueba de autenticación para emisorId={EmisorId}", emisorId);

                var token = await _haciendaAuthService.ObtenerTokenAsync(emisorId);

                _logger.LogInformation("[TEST-AUTH] Token obtenido exitosamente. Longitud={Length}", token?.Length ?? 0);

                return Ok(new
                {
                    success = true,
                    message = "Autenticación exitosa con Ministerio de Hacienda",
                    emisorId,
                    tokenLength = token?.Length ?? 0,
                    tokenPreview = token?.Length > 50 ? $"{token.Substring(0, 50)}..." : token,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TEST-AUTH] Error en prueba de autenticación para emisorId={EmisorId}", emisorId);
                return BadRequest(new
                {
                    success = false,
                    message = "Error al autenticar con Ministerio de Hacienda",
                    error = ex.Message,
                    emisorId,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Transmite un DTE individual al Ministerio de Hacienda
        /// </summary>
        [HttpPost("transmitir")]
        [ApiExplorerSettings(IgnoreApi = true)] // Temporal: DteBaseDto tiene esquemas complejos
        public async Task<IActionResult> TransmitirDte([FromBody] TransmitirDteRequest request)
        {
            try
            {
                var resultado = await _haciendaService.TransmitirDteAsync(request.EmisorId, request.Dte);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transmitiendo DTE");
                return StatusCode(500, new { error = "Error transmitiendo DTE", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Transmite múltiples DTEs en lote al Ministerio de Hacienda
        /// </summary>
        [HttpPost("transmitir-lote")]
        [ApiExplorerSettings(IgnoreApi = true)] // Temporal: DteBaseDto tiene esquemas complejos
        public async Task<IActionResult> TransmitirLote([FromBody] TransmitirLoteRequest request)
        {
            try
            {
                var resultado = await _haciendaService.TransmitirDteLoteAsync(request.EmisorId, request.Dtes);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transmitiendo lote de DTEs");
                return StatusCode(500, new { error = "Error transmitiendo lote", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Envía un evento de contingencia al Ministerio de Hacienda
        /// </summary>
        [HttpPost("contingencia")]
        public async Task<IActionResult> EnviarContingencia([FromBody] EventoContingenciaRequest request)
        {
            try
            {
                var resultado = await _haciendaService.EnviarEventoContingenciaAsync(request.EmisorId, request.Evento);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando evento de contingencia");
                return StatusCode(500, new { error = "Error enviando contingencia", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Anula/Invalida un DTE previamente enviado
        /// </summary>
        [HttpPost("anular")]
        [ApiExplorerSettings(IgnoreApi = true)] // TODO: Fix Swagger schema generation - Falla a pesar de usar DTOs concretos y fix nullable/required
        public async Task<IActionResult> AnularDte([FromBody] AnularDteRequest request)
        {
            try
            {
                var resultado = await _haciendaService.AnularDteAsync(request.EmisorId, request.EventoInvalidacion);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error anulando DTE");
                return StatusCode(500, new { error = "Error anulando DTE", detalle = ex.Message });
            }
        }


        /// <summary>
        /// Consulta el estado de un DTE individual en Hacienda
        /// </summary>
        [HttpPost("consultar-dte")]
        public async Task<IActionResult> ConsultarDte([FromBody] ConsultaDteRequest request)
        {
            try
            {
                var resultado = await _haciendaService.ConsultarEstadoDteAsync(request.EmisorId, request.CodigoGeneracion, request.TipoDte);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando estado DTE");
                return StatusCode(500, new { error = "Error consultando DTE", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Consulta el estado de un lote de DTEs en Hacienda
        /// </summary>
        [HttpGet("consultar-lote/{codigoLote}")]
        public async Task<IActionResult> ConsultarLote(string codigoLote, [FromQuery] int emisorId)
        {
            try
            {
                var resultado = await _haciendaService.ConsultarEstadoLoteAsync(emisorId, codigoLote);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando estado Lote");
                return StatusCode(500, new { error = "Error consultando Lote", detalle = ex.Message });
            }
        }
    }

}
