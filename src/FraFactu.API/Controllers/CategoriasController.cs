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
    /// Controlador para gestión de categorías de productos
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CategoriasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICategoriaService _categoriaService;

        public CategoriasController(ApplicationDbContext context, ICurrentUserService currentUserService, ICategoriaService categoriaService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _categoriaService = categoriaService;
        }

        /// <summary>
        /// Obtiene una categoría por su ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "SuperAdmin,EmisorAdmin,GerenteSucursal,Cajero,Auditor")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetById(int id)
        {
            var emisorId = _currentUserService.GetEmisorId();
            var categoria = await _context.Categorias
                .Include(c => c.CategoriaPadre)
                .Include(c => c.Subcategorias)
                .FirstOrDefaultAsync(c => c.Id == id && c.EmisorId == emisorId);

            if (categoria == null)
                return NotFound(new { message = $"Categoría con ID {id} no encontrada" });

            return Ok(new
            {
                categoria.Id,
                categoria.Codigo,
                categoria.Nombre,
                categoria.Descripcion,
                categoria.CategoriaPadreId,
                CategoriaPadreNombre = categoria.CategoriaPadre?.Nombre,
                Subcategorias = categoria.Subcategorias.Select(s => new { s.Id, s.Nombre }).ToList(),
                categoria.FechaCreacion
            });
        }

        /// <summary>
        /// Obtiene todas las categorías con paginación, búsqueda y ordenamiento
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,EmisorAdmin,GerenteSucursal,Cajero,Auditor")]
        [ProducesResponseType(typeof(PaginatedResponse<CategoriaDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetAll(
            [FromQuery] PaginatedRequest request,
            [FromQuery] bool soloRaices = false,
            [FromQuery] bool? soloActivos = null)
        {
            var emisorId = _currentUserService.GetEmisorId();
            var resultado = await _categoriaService.GetAllAsync(request, emisorId, soloActivos, soloRaices);
            return Ok(resultado);
        }

        /// <summary>
        /// Crea una nueva categoría
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Create([FromBody] CreateCategoriaRequest request)
        {
            var emisorId = _currentUserService.GetEmisorId();

            // Validar código único
            if (await _context.Categorias.AnyAsync(c => c.Codigo == request.Codigo && c.EmisorId == emisorId && c.Activo))
                return BadRequest(new { message = "Ya existe una categoría con ese código" });

            // Validar padre si existe
            if (request.CategoriaPadreId.HasValue)
            {
                if (!await _context.Categorias.AnyAsync(c => c.Id == request.CategoriaPadreId && c.EmisorId == emisorId))
                    return BadRequest(new { message = "La categoría padre especificada no existe" });
            }

            var categoria = new Categoria
            {
                EmisorId = emisorId,
                Codigo = request.Codigo,
                Nombre = request.Nombre,
                Descripcion = request.Descripcion,
                CategoriaPadreId = request.CategoriaPadreId,
                FechaCreacion = DateTime.UtcNow
            };

            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = categoria.Id }, new
            {
                categoria.Id,
                categoria.Codigo,
                categoria.Nombre,
                categoria.Descripcion,
                categoria.CategoriaPadreId,
                categoria.FechaCreacion
            });
        }

        /// <summary>
        /// Actualiza una categoría existente
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Update(int id, [FromBody] UpdateCategoriaRequest request)
        {
            var emisorId = _currentUserService.GetEmisorId();
            var categoria = await _context.Categorias.FirstOrDefaultAsync(c => c.Id == id && c.EmisorId == emisorId);
            if (categoria == null)
                return NotFound(new { message = $"Categoría con ID {id} no encontrada" });

            // Validar código único
            if (request.Codigo != categoria.Codigo &&
                await _context.Categorias.AnyAsync(c => c.Codigo == request.Codigo && c.EmisorId == emisorId && c.Activo))
                return BadRequest(new { message = "Ya existe otra categoría con ese código" });

            // Validar padre (no puede ser ella misma ni generar ciclos simples - aquí simplificado)
            if (request.CategoriaPadreId.HasValue)
            {
                if (request.CategoriaPadreId == id)
                    return BadRequest(new { message = "Una categoría no puede ser su propio padre" });

                if (!await _context.Categorias.AnyAsync(c => c.Id == request.CategoriaPadreId && c.EmisorId == emisorId))
                    return BadRequest(new { message = "La categoría padre especificada no existe" });
            }

            categoria.Codigo = request.Codigo;
            categoria.Nombre = request.Nombre;
            categoria.Descripcion = request.Descripcion;
            categoria.CategoriaPadreId = request.CategoriaPadreId;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                categoria.Id,
                categoria.Codigo,
                categoria.Nombre,
                categoria.Descripcion,
                categoria.CategoriaPadreId,
                categoria.FechaCreacion
            });
        }

        /// <summary>
        /// Elimina una categoría
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "EmisorAdmin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Delete(int id)
        {
            var emisorId = _currentUserService.GetEmisorId();
            var categoria = await _context.Categorias
                .Include(c => c.Subcategorias)
                .Include(c => c.Productos)
                .FirstOrDefaultAsync(c => c.Id == id && c.EmisorId == emisorId);

            if (categoria == null)
                return NotFound(new { message = $"Categoría con ID {id} no encontrada" });

            // Validaciones de integridad
            if (categoria.Subcategorias.Any())
                return BadRequest(new { message = "No se puede eliminar la categoría porque tiene subcategorías" });

            if (categoria.Productos.Any())
                return BadRequest(new { message = "No se puede eliminar la categoría porque tiene productos asociados" });

            _context.Categorias.Remove(categoria);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Activa o desactiva una categoría
        /// </summary>
        [HttpPatch("{id}/toggle-active")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
        public async Task<ActionResult> ToggleActive(int id)
        {
            var emisorId = _currentUserService.GetEmisorId();
            var categoria = await _context.Categorias.FirstOrDefaultAsync(c => c.Id == id && c.EmisorId == emisorId);
            if (categoria == null)
                return NotFound(new { message = $"Categoría con ID {id} no encontrada" });

            categoria.Activo = !categoria.Activo;
            await _context.SaveChangesAsync();

            return Ok(new { id, activo = categoria.Activo });
        }
    }

    public record CreateCategoriaRequest(
        string Codigo,
        string Nombre,
        string? Descripcion,
        int? CategoriaPadreId
    );

    public record UpdateCategoriaRequest(
        string Codigo,
        string Nombre,
        string? Descripcion,
        int? CategoriaPadreId
    );
}
