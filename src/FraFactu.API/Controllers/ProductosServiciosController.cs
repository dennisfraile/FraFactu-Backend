using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;
using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.ProductosServicios;
using FraFactu.Application.Interfaces;
using FraFactu.API.Extensions;

namespace FraFactu.API.Controllers
{
    /// <summary>
    /// Controlador para gestión de productos y servicios del emisor
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductosServiciosController : ControllerBase
    {
        private readonly IProductoServicioService _service;

        public ProductosServiciosController(IProductoServicioService service)
        {
            _service = service;
        }

        /// <summary>
        /// Obtiene un producto/servicio por su ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero,Contador,Auditor")]
        [ProducesResponseType(typeof(ProductoServicioDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductoServicioDto>> GetById(int id)
        {
            var emisorId = User.GetEmisorId();
            var result = await _service.GetByIdAsync(id, emisorId);

            if (result == null)
                return NotFound(new { message = $"Producto/Servicio con ID {id} no encontrado" });

            return Ok(result);
        }

        /// <summary>
        /// Obtiene todos los productos/servicios del emisor (paginado)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero,Contador,Auditor")]
        [ProducesResponseType(typeof(PaginatedResponse<ProductoServicioListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResponse<ProductoServicioListDto>>> GetAll(
            [FromQuery] PaginatedRequest request,
            [FromQuery] int? sucursalId = null,
            [FromQuery] int? categoriaId = null,
            [FromQuery] int? marcaId = null,
            [FromQuery] string? unidadMedida = null,
            [FromQuery] string? tipo = null,
            [FromQuery] bool incluirInactivos = false)
        {
            var emisorId = User.GetEmisorId();
            var result = await _service.GetAllAsync(request, emisorId, sucursalId, categoriaId, marcaId, unidadMedida, tipo, incluirInactivos);
            return Ok(result);
        }

        /// <summary>
        /// Busca productos/servicios por término de búsqueda
        /// </summary>
        [HttpGet("search")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero,Contador,Auditor")]
        [ProducesResponseType(typeof(List<ProductoServicioListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ProductoServicioListDto>>> Search(
            [FromQuery] string searchTerm,
            [FromQuery] int? sucursalId = null)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return BadRequest(new { message = "El término de búsqueda es requerido" });

            var emisorId = User.GetEmisorId();
            var result = await _service.SearchAsync(searchTerm, emisorId, sucursalId);
            return Ok(result);
        }

        /// <summary>
        /// Crea un nuevo producto/servicio
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
        [ProducesResponseType(typeof(ProductoServicioDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ProductoServicioDto>> Create(
            [FromBody] CreateProductoServicioDto dto)
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
        /// Actualiza un producto/servicio existente
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
        [ProducesResponseType(typeof(ProductoServicioDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ProductoServicioDto>> Update(
            int id,
            [FromBody] UpdateProductoServicioDto dto)
        {
            try
            {
                var emisorId = User.GetEmisorId();
                var result = await _service.UpdateAsync(id, dto, emisorId);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Producto/Servicio con ID {id} no encontrado" });
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
        /// Activa o desactiva un producto/servicio
        /// </summary>
        [HttpPatch("{id}/toggle-active")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
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
                return NotFound(new { message = $"Producto/Servicio con ID {id} no encontrado" });
            }
        }

        /// <summary>
        /// Da de baja (soft-delete) un producto/servicio registrando el motivo.
        /// </summary>
        [HttpPost("{id}/desactivar")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Desactivar(int id, [FromBody] DesactivarProductoServicioDto dto)
        {
            try
            {
                var emisorId = User.GetEmisorId();
                var usuarioId = User.GetUserId();
                await _service.DesactivarAsync(id, emisorId, dto, usuarioId);
                return Ok(new { id, activo = false });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Producto/Servicio con ID {id} no encontrado" });
            }
        }
    }
}
