using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using FraFactu.Infrastructure.Persistence;

namespace FraFactu.API.Controllers
{
    /// <summary>
    /// Controlador simple para obtener lista de roles.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RolesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RolesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todos los roles disponibles (Id, Nombre).
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetAll()
        {
            var roles = await _context.Roles
                .AsNoTracking()
                .Select(r => new { r.Id, r.Nombre })
                .ToListAsync();

            return Ok(roles);
        }
    }
}
