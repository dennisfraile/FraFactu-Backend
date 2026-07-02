using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;
using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Receptores;
using FraFactu.Application.Interfaces;
using FraFactu.API.Extensions;

namespace FraFactu.API.Controllers
{
    /// <summary>
    /// Controlador para gestión de receptores (clientes) del emisor
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReceptoresController : ControllerBase
    {
        private readonly IReceptorService _service;

        public ReceptoresController(IReceptorService service)
        {
            _service = service;
        }

        /// <summary>
        /// Obtiene un receptor por su ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "SuperAdmin,EmisorAdmin,GerenteSucursal,Cajero,Auditor")]
        [ProducesResponseType(typeof(ReceptorDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ReceptorDto>> GetById(int id)
        {
            var emisorId = User.GetEmisorId();
            var result = await _service.GetByIdAsync(id, emisorId);

            if (result == null)
                return NotFound(new { message = $"Receptor con ID {id} no encontrado" });

            return Ok(result);
        }

        /// <summary>
        /// Obtiene todos los receptores del emisor (paginado)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,EmisorAdmin,GerenteSucursal,Cajero,Auditor")]
        [ProducesResponseType(typeof(PaginatedResponse<ReceptorListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResponse<ReceptorListDto>>> GetAll(
            [FromQuery] PaginatedRequest request,
            [FromQuery] bool? soloActivos,
            [FromQuery] string? search = null,
            [FromQuery] string? orderBy = null,
            [FromQuery] string? orderDirection = null,
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null,
            [FromQuery] bool soloSujetosExcluidos = false)
        {
            var emisorId = User.GetEmisorId();
            var result = await _service.GetAllAsync(request, emisorId, soloActivos, search, orderBy, orderDirection, fechaDesde, fechaHasta, soloSujetosExcluidos);
            return Ok(result);
        }

        /// <summary>
        /// Busca receptores por término de búsqueda (nombre o documento)
        /// </summary>
        [HttpGet("search")]
        [Authorize(Roles = "SuperAdmin,EmisorAdmin,GerenteSucursal,Cajero,Auditor")]
        [ProducesResponseType(typeof(List<ReceptorListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ReceptorListDto>>> Search(
            [FromQuery] string searchTerm,
            [FromQuery] bool soloSujetosExcluidos = false)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return BadRequest(new { message = "El término de búsqueda es requerido" });

            var emisorId = User.GetEmisorId();
            var result = await _service.SearchAsync(searchTerm, emisorId, soloSujetosExcluidos);
            return Ok(result);
        }

        /// <summary>
        /// Crea un nuevo receptor
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero")]
        [ProducesResponseType(typeof(ReceptorDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ReceptorDto>> Create(
            [FromBody] CreateReceptorDto dto)
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
        }

        /// <summary>
        /// Actualiza un receptor existente
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero")]
        [ProducesResponseType(typeof(ReceptorDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ReceptorDto>> Update(
            int id,
            [FromBody] UpdateReceptorDto dto)
        {
            try
            {
                var emisorId = User.GetEmisorId();
                var result = await _service.UpdateAsync(id, dto, emisorId);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Receptor con ID {id} no encontrado" });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
            }
        }
    }
}
