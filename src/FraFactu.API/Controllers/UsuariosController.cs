using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Usuarios;
using FraFactu.Application.Interfaces;
using FraFactu.API.Helpers;
using FraFactu.API.Extensions;

namespace FraFactu.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;

        public UsuariosController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        /// <summary>
        /// Obtiene todos los usuarios del emisor (paginado)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,EmisorAdmin,GerenteSucursal,Auditor")]
        [ProducesResponseType(typeof(PaginatedResponse<UsuarioListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResponse<UsuarioListDto>>> GetAll(
            [FromQuery] PaginatedRequest request, [FromQuery] bool? soloActivos)
        {
            // Determinar emisorId basado en el rol
            int? emisorId = null;
            if (!User.IsInRole("SuperAdmin"))
            {
                emisorId = User.GetEmisorId();
            }

            // GerenteSucursal solo puede ver Cajero de sus sucursales (y a sí mismo)
            List<string>? rolesPermitidos = null;
            List<int>? sucursalIds = null;
            if (User.IsInRole("GerenteSucursal"))
            {
                rolesPermitidos = new List<string> { "GerenteSucursal", "Cajero" };
                sucursalIds = Helpers.ScopeHelper.GetSucursalIdsFromClaims(User);
            }

            var result = await _usuarioService.GetAllAsync(request, emisorId, soloActivos, rolesPermitidos, sucursalIds);
            return Ok(result);
        }

        /// <summary>
        /// Obtiene un usuario por su ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "SuperAdmin,EmisorAdmin,GerenteSucursal,Cajero,Auditor")]
        [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UsuarioDto>> GetById(int id)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (roleClaim != "SuperAdmin" && roleClaim != "EmisorAdmin" && roleClaim != "GerenteSucursal" && roleClaim != "Auditor")
            {
                // Cajero/Contador solo pueden ver su propio perfil
                if (userIdClaim != id.ToString())
                {
                    return Forbid();
                }
            }

            int? emisorId = null;
            if (!User.IsInRole("SuperAdmin"))
            {
                emisorId = User.GetEmisorId();
            }

            var result = await _usuarioService.GetByIdAsync(id, emisorId);

            // GerenteSucursal: validar que el usuario consultado sea de sus sucursales y tenga rol permitido
            if (roleClaim == "GerenteSucursal" && result != null && userIdClaim != id.ToString())
            {
                var rolesPermitidos = new[] { "GerenteSucursal", "Cajero" };
                if (!rolesPermitidos.Contains(result.RolNombre))
                    return Forbid();

                var misSucursalIds = Helpers.ScopeHelper.GetSucursalIdsFromClaims(User);
                var usuarioEnMisSucursales = result.Sucursales.Any(s => misSucursalIds.Contains(s.Id));
                if (!usuarioEnMisSucursales)
                    return Forbid();
            }

            if (result == null)
                return NotFound(new { message = $"Usuario con ID {id} no encontrado" });

            return Ok(result);
        }

        /// <summary>
        /// Crea un nuevo usuario.
        /// </summary>
        /// <remarks>
        /// Plan centralizacion F4: usar SmartHub para crear usuarios. Este endpoint
        /// sera removido en F7 cuando el frontend ya no lo invoque.
        /// </remarks>
        [Obsolete("Usar SmartHub para gestion de usuarios. Sera removido en F7 del plan de centralizacion.")]
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,EmisorAdmin,GerenteSucursal")]
        [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UsuarioDto>> Create([FromBody] CreateUsuarioDto dto)
        {
            try
            {
                // Si no es SuperAdmin, forzar el EmisorId del token
                if (!User.IsInRole("SuperAdmin"))
                {
                    dto.EmisorId = User.GetEmisorId();
                }

                var callerRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                var result = await _usuarioService.CreateAsync(dto, callerRole);
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
        /// Actualiza un usuario existente.
        /// </summary>
        /// <remarks>
        /// Plan centralizacion F4: la edicion de usuarios de otros vive en SmartHub.
        /// "Editar mi perfil" (self-service) sigue usando este endpoint hasta F6.
        /// </remarks>
        [Obsolete("Usar SmartHub para edicion de otros usuarios. Sera removido en F7 (mantener para self-edit hasta F6).")]
        [HttpPut("{id}")]
        [Authorize(Roles = "SuperAdmin,EmisorAdmin,GerenteSucursal")]
        [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UsuarioDto>> Update(int id, [FromBody] UpdateUsuarioDto dto)
        {
            try
            {
                // GerenteSucursal: validar que solo edite usuarios permitidos de sus sucursales
                if (User.IsInRole("GerenteSucursal"))
                {
                    var misSucursalIds = Helpers.ScopeHelper.GetSucursalIdsFromClaims(User);
                    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (userIdClaim != id.ToString())
                    {
                        var usuarioActual = await _usuarioService.GetByIdAsync(id, User.GetEmisorId());
                        if (usuarioActual == null)
                            return NotFound(new { message = $"Usuario con ID {id} no encontrado" });

                        var rolesPermitidos = new[] { "Cajero" };
                        if (!rolesPermitidos.Contains(usuarioActual.RolNombre))
                            return Forbid();

                        if (!usuarioActual.Sucursales.Any(s => misSucursalIds.Contains(s.Id)))
                            return Forbid();
                    }

                    // GerenteSucursal no puede asignar acceso a todas las sucursales
                    dto.AccesoTodasSucursales = false;

                    // Solo puede asignar sucursales que él mismo tiene
                    dto.SucursalIds = dto.SucursalIds
                        .Where(sid => misSucursalIds.Contains(sid))
                        .ToList();
                }

                int? emisorId = null;
                if (!User.IsInRole("SuperAdmin"))
                {
                    emisorId = User.GetEmisorId();
                }

                var callerRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                var result = await _usuarioService.UpdateAsync(id, dto, emisorId, callerRole);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Usuario con ID {id} no encontrado" });
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
        /// Desactiva (elimina lógicamente) un usuario.
        /// </summary>
        /// <remarks>
        /// Plan centralizacion F4: desactivar usuarios vive en SmartHub.
        /// </remarks>
        [Obsolete("Usar SmartHub para desactivar usuarios. Sera removido en F7 del plan de centralizacion.")]
        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin,EmisorAdmin,GerenteSucursal")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                int? emisorId = null;
                if (!User.IsInRole("SuperAdmin"))
                {
                    emisorId = User.GetEmisorId();
                }

                var result = await _usuarioService.DeactivateAsync(id, emisorId);

                if (!result)
                    return NotFound(new { message = $"Usuario con ID {id} no encontrado" });

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Alterna el estado activo/inactivo de un usuario.
        /// </summary>
        /// <remarks>
        /// Plan centralizacion F4: cambiar estado de usuarios vive en SmartHub.
        /// </remarks>
        [Obsolete("Usar SmartHub para cambiar estado de usuarios. Sera removido en F7 del plan de centralizacion.")]
        [HttpPatch("{id}/toggle-active")]
        [Authorize(Roles = "SuperAdmin,EmisorAdmin,GerenteSucursal")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<bool>> ToggleActive(int id)
        {
            try
            {
                int? emisorId = null;
                if (!User.IsInRole("SuperAdmin"))
                {
                    emisorId = User.GetEmisorId();
                }

                var result = await _usuarioService.ToggleActiveAsync(id, emisorId);
                return Ok(new { activo = result });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Usuario con ID {id} no encontrado" });
            }
        }
    }
}
