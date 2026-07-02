using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Domain.Entities;
using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Inventario;
using FraFactu.Application.Interfaces;

namespace FraFactu.API.Controllers
{
    /// <summary>
    /// Controlador para gestión de marcas de productos
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MarcasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMarcaService _marcaService;

        public MarcasController(ApplicationDbContext context, ICurrentUserService currentUserService, IMarcaService marcaService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _marcaService = marcaService;
        }

        /// <summary>
        /// Obtiene una marca por su ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "SuperAdmin,EmisorAdmin,GerenteSucursal,Cajero,Auditor")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetById(int id)
        {
            var emisorId = _currentUserService.GetEmisorId();
            var marca = await _context.Marcas
                .FirstOrDefaultAsync(m => m.Id == id && m.EmisorId == emisorId);

            if (marca == null)
                return NotFound(new { message = $"Marca con ID {id} no encontrada" });

            return Ok(new
            {
                marca.Id,
                marca.Nombre,
                marca.Descripcion,
                marca.Activa,
                marca.FechaCreacion
            });
        }

        /// <summary>
        /// Obtiene todas las marcas con paginación, búsqueda y ordenamiento
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,EmisorAdmin,GerenteSucursal,Cajero,Auditor")]
        [ProducesResponseType(typeof(PaginatedResponse<MarcaDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetAll(
            [FromQuery] PaginatedRequest request,
            [FromQuery] bool? soloActivas = null)
        {
            var emisorId = _currentUserService.GetEmisorId();
            var resultado = await _marcaService.GetAllAsync(request, emisorId, soloActivas);
            return Ok(resultado);
        }

        /// <summary>
        /// Crea una nueva marca
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Create([FromBody] CreateMarcaRequest request)
        {
            var emisorId = _currentUserService.GetEmisorId();

            // Validar nombre único (solo entre activas del mismo emisor)
            if (await _context.Marcas.AnyAsync(m => m.Nombre == request.Nombre && m.EmisorId == emisorId && m.Activa))
                return BadRequest(new { message = "Ya existe una marca con ese nombre" });

            var marca = new Marca
            {
                EmisorId = emisorId,
                Nombre = request.Nombre,
                Descripcion = request.Descripcion,
                Activa = true,
                FechaCreacion = DateTime.UtcNow
            };

            _context.Marcas.Add(marca);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = marca.Id }, new
            {
                marca.Id,
                marca.Nombre,
                marca.Descripcion,
                marca.Activa,
                marca.FechaCreacion
            });
        }

        /// <summary>
        /// Actualiza una marca existente
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Update(int id, [FromBody] UpdateMarcaRequest request)
        {
            var emisorId = _currentUserService.GetEmisorId();
            var marca = await _context.Marcas.FirstOrDefaultAsync(m => m.Id == id && m.EmisorId == emisorId);
            if (marca == null)
                return NotFound(new { message = $"Marca con ID {id} no encontrada" });

            // Validar nombre único
            if (request.Nombre != marca.Nombre &&
                await _context.Marcas.AnyAsync(m => m.Nombre == request.Nombre && m.EmisorId == emisorId && m.Activa))
                return BadRequest(new { message = "Ya existe otra marca con ese nombre" });

            marca.Nombre = request.Nombre;
            marca.Descripcion = request.Descripcion;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                marca.Id,
                marca.Nombre,
                marca.Descripcion,
                marca.Activa,
                marca.FechaCreacion
            });
        }

        /// <summary>
        /// Elimina una marca (o la desactiva si tiene productos)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "EmisorAdmin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Delete(int id)
        {
            var emisorId = _currentUserService.GetEmisorId();
            var marca = await _context.Marcas
                .Include(m => m.Productos)
                .FirstOrDefaultAsync(m => m.Id == id && m.EmisorId == emisorId);

            if (marca == null)
                return NotFound(new { message = $"Marca con ID {id} no encontrada" });

            // Si tiene productos, no se elimina físicamente
            if (marca.Productos.Any())
                return BadRequest(new { message = "No se puede eliminar la marca porque tiene productos asociados" });

            _context.Marcas.Remove(marca);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Activa o desactiva una marca
        /// </summary>
        [HttpPatch("{id}/toggle-active")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ToggleActive(int id)
        {
            var emisorId = _currentUserService.GetEmisorId();
            var marca = await _context.Marcas.FirstOrDefaultAsync(m => m.Id == id && m.EmisorId == emisorId);
            if (marca == null)
                return NotFound(new { message = $"Marca con ID {id} no encontrada" });

            marca.Activa = !marca.Activa;
            await _context.SaveChangesAsync();

            return Ok(new { id, activa = marca.Activa });
        }
    }

    public record CreateMarcaRequest(
        string Nombre,
        string? Descripcion
    );

    public record UpdateMarcaRequest(
        string Nombre,
        string? Descripcion
    );
}
