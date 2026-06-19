using FraFactu.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FraFactu.API.Controllers
{
    [ApiController]
    [Route("api/notificaciones")]
    [Authorize]
    public class NotificacionesController : ControllerBase
    {
        private readonly INotificacionService _service;
        public NotificacionesController(INotificacionService service) { _service = service; }

        private int EmisorId => int.Parse(User.FindFirst("EmisorId")?.Value
            ?? throw new UnauthorizedAccessException("EmisorId no encontrado en el token"));

        private int UsuarioId => int.Parse(
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("UsuarioId no encontrado en el token"));

        /// <summary>Notificaciones recientes del emisor, con flag de leída para el usuario actual.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _service.ListarAsync(EmisorId, UsuarioId));

        /// <summary>Conteo de no-leídas del usuario actual.</summary>
        [HttpGet("no-leidas/count")]
        public async Task<IActionResult> ContarNoLeidas()
            => Ok(new { count = await _service.ContarNoLeidasAsync(EmisorId, UsuarioId) });

        /// <summary>Marca una notificación como leída para el usuario actual.</summary>
        [HttpPost("{id:int}/leer")]
        public async Task<IActionResult> MarcarLeida(int id)
        {
            try
            {
                await _service.MarcarLeidaAsync(id, UsuarioId);
                return NoContent();
            }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>Marca todas las notificaciones del emisor como leídas para el usuario actual.</summary>
        [HttpPost("leer-todas")]
        public async Task<IActionResult> MarcarTodasLeidas()
        {
            await _service.MarcarTodasLeidasAsync(EmisorId, UsuarioId);
            return NoContent();
        }
    }
}
