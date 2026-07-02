using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FraFactu.Infrastructure.Persistence;
using FraFactu.API.Extensions;
using FraFactu.API.Helpers;
using FraFactu.Application.Services;
using FraFactu.Infrastructure.Services.Helpers;

namespace FraFactu.API.Controllers
{
    /// <summary>
    /// Controlador para gestión de bodegas/almacenes
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BodegasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventarioReporteService _reporteService;

        public BodegasController(ApplicationDbContext context, IInventarioReporteService reporteService)
        {
            _context = context;
            _reporteService = reporteService;
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
        /// Obtiene una bodega por su ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "SuperAdmin,EmisorAdmin,GerenteSucursal,Cajero,Auditor,Contador")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetById(int id)
        {
            var emisorId = GetEmisorId();

            var bodega = await _context.Bodegas
                .Include(b => b.Sucursal)
                .FirstOrDefaultAsync(b => b.Id == id && b.Sucursal!.EmisorId == emisorId);

            if (bodega == null)
                return NotFound(new { message = $"Bodega con ID {id} no encontrada" });

            return Ok(new
            {
                bodega.Id,
                bodega.Codigo,
                bodega.Nombre,
                bodega.Direccion,
                bodega.SucursalId,
                SucursalNombre = bodega.Sucursal?.Nombre,
                bodega.EsPrincipal,
                bodega.Activa,
                bodega.FechaCreacion
            });
        }

        /// <summary>
        /// Obtiene todas las bodegas (con filtro opcional soloActivas)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,EmisorAdmin,GerenteSucursal,Cajero,Auditor,Contador")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetAll([FromQuery] bool? soloActivas = null)
        {
            var emisorId = GetEmisorId();

            var query = _context.Bodegas
                .Include(b => b.Sucursal)
                .Where(b => b.Sucursal!.EmisorId == emisorId)
                .AsQueryable();

            // Filtrar por sucursales del usuario para roles restringidos
            var rol = ScopeHelper.GetRolFromClaims(User);
            if (ScopeHelper.RequiereRestriccionSucursal(rol) && !ScopeHelper.GetAccesoTodasSucursales(User))
            {
                var userSucursalIds = ScopeHelper.GetSucursalIdsFromClaims(User);
                if (userSucursalIds.Count > 0)
                {
                    query = query.Where(b => b.SucursalId.HasValue && userSucursalIds.Contains(b.SucursalId.Value));
                }
            }

            if (soloActivas == true)
            {
                query = query.Where(b => b.Activa);
            }

            var bodegas = await query
                .OrderBy(b => b.Nombre)
                .Select(b => new
                {
                    b.Id,
                    b.Codigo,
                    b.Nombre,
                    b.Direccion,
                    b.SucursalId,
                    SucursalNombre = b.Sucursal != null ? b.Sucursal.Nombre : null,
                    b.EsPrincipal,
                    b.Activa,
                    b.FechaCreacion
                })
                .ToListAsync();

            return Ok(bodegas);
        }

        /// <summary>
        /// Obtiene todas las bodegas activas del emisor sin restricción de rol
        /// </summary>
        [HttpGet("todas-activas")]
        [Authorize(Roles = "SuperAdmin,EmisorAdmin,GerenteSucursal,Cajero,Auditor,Contador")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetAllActivas()
        {
            var emisorId = GetEmisorId();

            var bodegas = await _context.Bodegas
                .Include(b => b.Sucursal)
                .Where(b => b.Sucursal!.EmisorId == emisorId && b.Activa)
                .OrderBy(b => b.Nombre)
                .Select(b => new
                {
                    b.Id,
                    b.Codigo,
                    b.Nombre,
                    b.Direccion,
                    b.SucursalId,
                    SucursalNombre = b.Sucursal != null ? b.Sucursal.Nombre : null,
                    b.EsPrincipal,
                    b.Activa,
                    b.FechaCreacion
                })
                .ToListAsync();

            return Ok(bodegas);
        }

        /// <summary>
        /// Obtiene las bodegas de una sucursal específica
        /// </summary>
        [HttpGet("sucursal/{sucursalId}")]
        [Authorize(Roles = "SuperAdmin,EmisorAdmin,GerenteSucursal,Cajero,Auditor,Contador")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetBySucursal(int sucursalId, [FromQuery] bool? soloActivas = null)
        {
            var emisorId = GetEmisorId();

            // Validar que la sucursal pertenezca al emisor
            var sucursalValida = await _context.Sucursales
                .AnyAsync(s => s.Id == sucursalId && s.EmisorId == emisorId);

            if (!sucursalValida)
                return NotFound(new { message = "La sucursal no pertenece al emisor" });

            var query = _context.Bodegas
                .Where(b => b.SucursalId == sucursalId)
                .AsQueryable();

            if (soloActivas == true)
            {
                query = query.Where(b => b.Activa);
            }

            var bodegas = await query
                .OrderBy(b => b.Nombre)
                .Select(b => new
                {
                    b.Id,
                    b.Codigo,
                    b.Nombre,
                    b.Direccion,
                    b.SucursalId,
                    b.EsPrincipal,
                    b.Activa,
                    b.FechaCreacion
                })
                .ToListAsync();

            return Ok(bodegas);
        }

        /// <summary>
        /// Obtiene el stock de una bodega específica
        /// </summary>
        [HttpGet("{id}/stock")]
        [Authorize(Roles = "SuperAdmin,EmisorAdmin,GerenteSucursal,Cajero,Auditor,Contador")]
        public async Task<ActionResult> GetStock(int id)
        {
            var emisorId = GetEmisorId();
            // Usamos el servicio de reportes filtrando por bodegaId
            var stock = await _reporteService.ObtenerStockPorBodegaAsync(emisorId, bodegaId: id);
            return Ok(stock);
        }

        /// <summary>
        /// Crea una nueva bodega
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "EmisorAdmin")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Create([FromBody] CreateBodegaRequest request)
        {
            var emisorId = GetEmisorId();

            if (request.SucursalId.HasValue)
            {
                // Validar sucursal
                var sucursalValida = await _context.Sucursales
                    .AnyAsync(s => s.Id == request.SucursalId && s.EmisorId == emisorId);

                if (!sucursalValida)
                    return BadRequest(new { message = "La sucursal no pertenece al emisor" });
            }
            else
            {
                // No permitir bodegas globales por API si no se controla
                return BadRequest(new { message = "Se requiere SucursalId." });
            }

            var codigoToUse = request.Codigo;

            // Autogeneración
            if (string.IsNullOrWhiteSpace(codigoToUse))
            {
                if (request.SucursalId.HasValue)
                {
                    var lastEntity = await _context.Bodegas
                        .Where(x => x.SucursalId == request.SucursalId)
                        .OrderByDescending(x => x.Id)
                        .FirstOrDefaultAsync();

                    var totalCount = await _context.Bodegas
                         .CountAsync(x => x.SucursalId == request.SucursalId && x.Activa);

                    codigoToUse = CodeGeneratorHelper.GenerateNextCode("BOD", lastEntity?.Codigo, totalCount);
                }
            }

            // Validar código único (Scoped por Sucursal)
            if (request.SucursalId.HasValue)
            {
                if (await _context.Bodegas.AnyAsync(b => b.SucursalId == request.SucursalId && b.Codigo == codigoToUse && b.Activa))
                    return BadRequest(new { message = "Ya existe una bodega con ese código en la sucursal seleccionada" });
            }

            var bodega = new Domain.Entities.Bodega
            {
                Codigo = codigoToUse ?? throw new InvalidOperationException("No se pudo generar código de bodega"),
                Nombre = request.Nombre,
                Direccion = request.Direccion,
                SucursalId = request.SucursalId,
                EsPrincipal = request.EsPrincipal,
                Activa = true,
                FechaCreacion = DateTime.UtcNow
            };

            _context.Bodegas.Add(bodega);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = bodega.Id }, new
            {
                bodega.Id,
                bodega.Codigo,
                bodega.Nombre,
                bodega.Direccion,
                bodega.SucursalId,
                bodega.EsPrincipal,
                bodega.Activa,
                bodega.FechaCreacion
            });
        }

        /// <summary>
        /// Actualiza una bodega existente
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "EmisorAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Update(int id, [FromBody] UpdateBodegaRequest request)
        {
            var emisorId = GetEmisorId();

            var bodega = await _context.Bodegas
                .Include(b => b.Sucursal)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bodega == null)
                return NotFound(new { message = $"Bodega con ID {id} no encontrada" });

            if (bodega.SucursalId.HasValue && bodega.Sucursal != null && bodega.Sucursal.EmisorId != emisorId)
                return Unauthorized(new { message = "No tiene permiso para editar esta bodega" });

            // Si cambia de sucursal, validar la nueva
            if (request.SucursalId.HasValue && request.SucursalId != bodega.SucursalId)
            {
                var nuevaSucursalValida = await _context.Sucursales
                   .AnyAsync(s => s.Id == request.SucursalId && s.EmisorId == emisorId);

                if (!nuevaSucursalValida)
                    return BadRequest(new { message = "La nueva sucursal no pertenece al emisor" });
            }

            // Validar código único (Scoped por Sucursal)
            int? targetSucursalId = request.SucursalId ?? bodega.SucursalId;

            if (request.Codigo != bodega.Codigo || request.SucursalId != bodega.SucursalId)
            {
                if (await _context.Bodegas.AnyAsync(b =>
                        b.Id != id &&
                        b.SucursalId == targetSucursalId &&
                        b.Codigo == request.Codigo &&
                        b.Activa))
                {
                    return BadRequest(new { message = "Ya existe otra bodega con ese código en la sucursal destino" });
                }
            }

            bodega.Codigo = request.Codigo;
            bodega.Nombre = request.Nombre;
            bodega.Direccion = request.Direccion;
            bodega.SucursalId = request.SucursalId;
            bodega.EsPrincipal = request.EsPrincipal;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                bodega.Id,
                bodega.Codigo,
                bodega.Nombre,
                bodega.Direccion,
                bodega.SucursalId,
                bodega.EsPrincipal,
                bodega.Activa,
                bodega.FechaCreacion
            });
        }

        /// <summary>
        /// Activa o desactiva una bodega
        /// </summary>
        [HttpPatch("{id}/toggle-active")]
        [Authorize(Roles = "EmisorAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ToggleActive(int id)
        {
            var emisorId = GetEmisorId();

            var bodega = await _context.Bodegas
                .Include(b => b.Sucursal)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bodega == null)
                return NotFound(new { message = $"Bodega con ID {id} no encontrada" });

            if (bodega.SucursalId.HasValue && bodega.Sucursal != null && bodega.Sucursal.EmisorId != emisorId)
                return Unauthorized(new { message = "No tiene permiso" });

            // Si se va a desactivar, verificar que no tenga stock
            if (bodega.Activa)
            {
                var tieneStock = await _context.StocksBodega
                    .AnyAsync(s => s.BodegaId == id && (s.CantidadDisponible + s.CantidadReservada) > 0);

                if (tieneStock)
                    return BadRequest(new { message = "No se puede desactivar la bodega porque tiene productos con stock. Debe transferir o ajustar el inventario antes de desactivarla." });
            }

            bodega.Activa = !bodega.Activa;
            await _context.SaveChangesAsync();

            return Ok(new { id, activa = bodega.Activa });
        }
    }

    // Request DTOs inline (simple)
    public record CreateBodegaRequest(
        string? Codigo,
        string Nombre,
        string? Direccion,
        int? SucursalId,
        bool EsPrincipal
    );

    public record UpdateBodegaRequest(
        string Codigo,
        string Nombre,
        string? Direccion,
        int? SucursalId,
        bool EsPrincipal
    );
}
