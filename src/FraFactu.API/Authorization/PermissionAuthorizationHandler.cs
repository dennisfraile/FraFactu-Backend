using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FraFactu.Infrastructure.Persistence;

namespace FraFactu.API.Authorization
{
    /// <summary>
    /// Handler que verifica si un usuario tiene un permiso específico
    /// </summary>
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public PermissionAuthorizationHandler(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            // Obtener RolId del usuario del JWT
            var rolIdClaim = context.User.FindFirst("RolId")?.Value;
            if (string.IsNullOrEmpty(rolIdClaim))
            {
                return; // No tiene rol, no puede acceder
            }

            var rolId = int.Parse(rolIdClaim);

            // Crear scope para acceder al DbContext
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Verificar si el rol tiene el permiso requerido
            var tienePermiso = await dbContext.RolPermisos
                .Include(rp => rp.Permiso)
                .AnyAsync(rp =>
                    rp.RolId == rolId &&
                    rp.Permiso.Codigo == requirement.PermissionCode &&
                    rp.Permiso.Activo);

            if (tienePermiso)
            {
                context.Succeed(requirement);
            }
        }
    }
}
