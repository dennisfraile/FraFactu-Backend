using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Inventario;
using FraFactu.Application.Interfaces;

namespace FraFactu.API.Controllers
{
    /// <summary>
    /// Controlador para catálogo de tipos de gasto
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TiposGastoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ITipoGastoService _tipoGastoService;

        public TiposGastoController(ApplicationDbContext context, ICurrentUserService currentUserService, ITipoGastoService tipoGastoService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _tipoGastoService = tipoGastoService;
        }

        /// <summary>
        /// Obtiene todos los tipos de gasto con paginación, búsqueda y ordenamiento
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero,Auditor")]
        [ProducesResponseType(typeof(PaginatedResponse<TipoGastoDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetAll(
            [FromQuery] PaginatedRequest request,
            [FromQuery] bool? soloActivos = null)
        {
            var emisorId = _currentUserService.GetEmisorId();
            var resultado = await _tipoGastoService.GetAllAsync(request, emisorId, soloActivos);
            return Ok(resultado);
        }

        /// <summary>
        /// Obtiene un tipo de gasto por su ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero,Auditor")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetById(int id)
        {
            var emisorId = _currentUserService.GetEmisorId();
            var tipo = await _context.CatTiposGasto.FirstOrDefaultAsync(t => t.Id == id && t.EmisorId == emisorId);

            if (tipo == null)
                return NotFound(new { message = $"Tipo de gasto con ID {id} no encontrado" });

            return Ok(new
            {
                tipo.Id,
                tipo.Codigo,
                tipo.Nombre,
                tipo.Descripcion,
                tipo.Activo
            });
        }

        /// <summary>
        /// Crea un nuevo tipo de gasto
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Create([FromBody] CreateTipoGastoDto dto)
        {
            var emisorId = _currentUserService.GetEmisorId();

            // Validar código único (solo entre activos del mismo emisor)
            var existeCodigo = await _context.CatTiposGasto
                .AnyAsync(t => t.Codigo == dto.Codigo && t.EmisorId == emisorId && t.Activo);

            if (existeCodigo)
                return BadRequest(new { message = $"Ya existe un tipo de gasto con el código {dto.Codigo}" });

            var tipoGasto = new Domain.Entities.Catalogos.CatTipoGasto
            {
                EmisorId = emisorId,
                Codigo = dto.Codigo,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                Activo = true
            };

            _context.CatTiposGasto.Add(tipoGasto);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = tipoGasto.Id }, new
            {
                tipoGasto.Id,
                tipoGasto.Codigo,
                tipoGasto.Nombre,
                tipoGasto.Descripcion,
                tipoGasto.Activo
            });
        }

        /// <summary>
        /// Actualiza un tipo de gasto existente
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Update(int id, [FromBody] UpdateTipoGastoDto dto)
        {
            var emisorId = _currentUserService.GetEmisorId();
            var tipoGasto = await _context.CatTiposGasto.FirstOrDefaultAsync(t => t.Id == id && t.EmisorId == emisorId);

            if (tipoGasto == null)
                return NotFound(new { message = $"Tipo de gasto con ID {id} no encontrado" });

            // Validar código único (solo entre activos del mismo emisor, excepto el actual)
            var existeCodigo = await _context.CatTiposGasto
                .AnyAsync(t => t.Codigo == dto.Codigo && t.Id != id && t.EmisorId == emisorId && t.Activo);

            if (existeCodigo)
                return BadRequest(new { message = $"Ya existe otro tipo de gasto con el código {dto.Codigo}" });

            tipoGasto.Codigo = dto.Codigo;
            tipoGasto.Nombre = dto.Nombre;
            tipoGasto.Descripcion = dto.Descripcion;
            tipoGasto.Activo = dto.Activo;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                tipoGasto.Id,
                tipoGasto.Codigo,
                tipoGasto.Nombre,
                tipoGasto.Descripcion,
                tipoGasto.Activo
            });
        }

        /// <summary>
        /// Elimina un tipo de gasto (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "EmisorAdmin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(int id)
        {
            var emisorId = _currentUserService.GetEmisorId();
            var tipoGasto = await _context.CatTiposGasto.FirstOrDefaultAsync(t => t.Id == id && t.EmisorId == emisorId);

            if (tipoGasto == null)
                return NotFound(new { message = $"Tipo de gasto con ID {id} no encontrado" });

            tipoGasto.Activo = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    /// <summary>
    /// DTO para crear un tipo de gasto
    /// </summary>
    public record CreateTipoGastoDto(
        string Codigo,
        string Nombre,
        string? Descripcion
    );

    /// <summary>
    /// DTO para actualizar un tipo de gasto
    /// </summary>
    public record UpdateTipoGastoDto(
        string Codigo,
        string Nombre,
        string? Descripcion,
        bool Activo
    );
}
