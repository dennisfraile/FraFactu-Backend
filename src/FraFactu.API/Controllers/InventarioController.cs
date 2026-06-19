using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FraFactu.Application.Services;
using FraFactu.Application.DTOs.Inventario;

namespace FraFactu.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,EncargadoInventario")]
    public class InventarioController : ControllerBase
    {
        private readonly IInventarioIntegrationService _inventarioService;
        private readonly Application.Interfaces.ICurrentUserService _currentUserService;

        public InventarioController(IInventarioIntegrationService inventarioService, Application.Interfaces.ICurrentUserService currentUserService)
        {
            _inventarioService = inventarioService;
            _currentUserService = currentUserService;
        }

        private int GetEmisorId()
        {
            var emisorIdClaim = User.FindFirst("EmisorId")?.Value;
            if (string.IsNullOrEmpty(emisorIdClaim) || !int.TryParse(emisorIdClaim, out var emisorId))
            {
                throw new UnauthorizedAccessException("EmisorId no encontrado en el token");
            }
            return emisorId;
        }



        /// <summary>
        /// Registrar un ajuste de inventario (Entrada/Salida manual)
        /// </summary>
        [HttpPost("ajustes")]
        public async Task<IActionResult> RegistrarAjuste([FromBody] AjusteInventarioDto ajusteDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var emisorId = GetEmisorId();

                var usuarioId = _currentUserService.GetUsuarioId();

                var movimiento = await _inventarioService.RegistrarAjusteAsync(ajusteDto, emisorId, usuarioId);

                return CreatedAtAction(nameof(RegistrarAjuste), new { id = movimiento.Id }, movimiento);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Error interno al registrar el ajuste" });
            }
        }
        /// <summary>
        /// Realizar un traslado de productos entre bodegas
        /// </summary>
        [HttpPost("traslado")]
        public async Task<IActionResult> Trasladar([FromBody] TrasladoInventarioDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var usuarioId = _currentUserService.GetUsuarioId();
                if (!usuarioId.HasValue)
                    return Unauthorized(new { error = "Usuario no identificado" });

                // TODO: Validar que el usuario tenga permiso de mover inventario de la bodega origen
                // Por ahora el Authorize del controlador cubre roles generales

                await _inventarioService.TrasladarAsync(dto, usuarioId.Value);

                return Ok(new { message = "Traslado realizado exitosamente" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Error interno al procesar el traslado" });
            }
        }
    }
}
