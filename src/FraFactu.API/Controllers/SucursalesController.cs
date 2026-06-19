using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;
using FraFactu.Application.DTOs.Sucursales;
using FraFactu.Application.Interfaces;
using FraFactu.API.Extensions;
using FraFactu.API.Helpers;

namespace FraFactu.API.Controllers
{
    /// <summary>
    /// Controlador para gestión de sucursales del emisor
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SucursalesController : ControllerBase
    {
        private readonly ISucursalService _service;

        public SucursalesController(ISucursalService service)
        {
            _service = service;
        }

        /// <summary>
        /// Obtiene una sucursal por su ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero,Auditor,Contador")]
        [ProducesResponseType(typeof(SucursalDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SucursalDto>> GetById(int id)
        {
            var emisorId = User.GetEmisorId();

            // Validar acceso a la sucursal para roles restringidos
            if (!ScopeHelper.ValidarAccesoSucursal(User, id))
                return StatusCode(403, new { message = "No tiene permiso para acceder a esta sucursal" });

            var result = await _service.GetByIdAsync(id, emisorId);

            if (result == null)
                return NotFound(new { message = $"Sucursal con ID {id} no encontrada" });

            return Ok(result);
        }

        /// <summary>
        /// Obtiene todas las sucursales del emisor
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero,Auditor,Contador")]
        [ProducesResponseType(typeof(List<SucursalListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<SucursalListDto>>> GetAll(
            [FromQuery] bool? soloActivos,
            [FromQuery] bool? soloMiSucursal)
        {
            var emisorId = User.GetEmisorId();
            var result = await _service.GetAllAsync(emisorId);

            // Filtrar por sucursales del usuario para roles restringidos
            var rol = ScopeHelper.GetRolFromClaims(User);
            if (ScopeHelper.RequiereRestriccionSucursal(rol) && !ScopeHelper.GetAccesoTodasSucursales(User))
            {
                var userSucursalIds = ScopeHelper.GetSucursalIdsFromClaims(User);
                if (userSucursalIds.Count > 0)
                {
                    result = result.Where(s => userSucursalIds.Contains(s.Id)).ToList();
                }
            }

            if (soloActivos.HasValue && soloActivos.Value)
            {
                result = result.Where(s => s.Activo).ToList();
            }

            return Ok(result);
        }

        /// <summary>
        /// Obtiene todas las sucursales activas del emisor sin restricción de rol.
        /// Usado por vistas como stock donde todos los roles necesitan ver todas las sucursales.
        /// </summary>
        [HttpGet("todas-activas")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero,Auditor,Contador")]
        [ProducesResponseType(typeof(List<SucursalListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<SucursalListDto>>> GetAllActivas()
        {
            var emisorId = User.GetEmisorId();
            var result = await _service.GetAllAsync(emisorId);
            result = result.Where(s => s.Activo).ToList();
            return Ok(result);
        }

        /// <summary>
        /// Crea una nueva sucursal
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "EmisorAdmin")]
        [ProducesResponseType(typeof(SucursalDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SucursalDto>> Create(
            [FromBody] CreateSucursalDto dto)
        {
            try
            {
                var emisorId = User.GetEmisorId();
                var result = await _service.CreateAsync(dto, emisorId);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Actualiza una sucursal existente
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "EmisorAdmin")]
        [ProducesResponseType(typeof(SucursalDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SucursalDto>> Update(int id, [FromBody] UpdateSucursalDto dto)
        {
            try
            {
                var emisorId = User.GetEmisorId();
                var result = await _service.UpdateAsync(id, dto, emisorId);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Sucursal con ID {id} no encontrada" });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Activa o desactiva una sucursal
        /// </summary>
        [HttpPatch("{id}/toggle-active")]
        [Authorize(Roles = "EmisorAdmin")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ToggleActive(int id)
        {
            try
            {
                var emisorId = User.GetEmisorId();
                var isActive = await _service.ToggleActiveAsync(id, emisorId);
                return Ok(new { id, activo = isActive });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Sucursal con ID {id} no encontrada" });
            }
        }
    }
}
