using FraFactu.Application.DTOs;
using FraFactu.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FraFactu.API.Controllers
{
    [ApiController]
    [Route("api/correlativos-iniciales")]
    [Authorize]
    public class CorrelativosInicialesController : ControllerBase
    {
        private readonly ICorrelativoInicialService _service;
        private readonly ILogger<CorrelativosInicialesController> _logger;

        public CorrelativosInicialesController(
            ICorrelativoInicialService service,
            ILogger<CorrelativosInicialesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        private int EmisorId => int.Parse(User.FindFirst("EmisorId")?.Value
            ?? throw new UnauthorizedAccessException("EmisorId no encontrado en el token"));

        /// <summary>Lista los correlativos iniciales configurados del emisor para un año/ambiente.</summary>
        [HttpGet]
        [Authorize(Roles = "EmisorAdmin")]
        public async Task<IActionResult> Listar([FromQuery] int? anio, [FromQuery] string ambiente = "01")
        {
            var a = anio ?? DateTime.UtcNow.Year;
            return Ok(await _service.ListarAsync(EmisorId, a, ambiente));
        }

        /// <summary>Crea o actualiza el correlativo inicial de un tipo de DTE (bloqueado si ya hay emisiones).</summary>
        [HttpPut]
        [Authorize(Roles = "EmisorAdmin")]
        public async Task<IActionResult> Upsert([FromBody] CorrelativoInicialUpsertDto dto)
        {
            try
            {
                return Ok(await _service.UpsertAsync(EmisorId, dto));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Upsert correlativo inicial rechazado: EmisorId={EmisorId}, {Reason}", EmisorId, ex.Message);
                return Conflict(new { error = ex.Message });
            }
        }
    }
}
