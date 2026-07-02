using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeder de arranque idempotente: siembra los roles canónicos y (si hay
/// credenciales por configuración) un usuario SuperAdmin inicial. Corre solo
/// contra una BD relacional (ver Program.cs); la lógica se cubre con unit tests.
/// </summary>
public static class BootstrapSeeder
{
    private static readonly string[] RolesCanonicos =
    {
        "SuperAdmin", "EmisorAdmin", "GerenteSucursal", "Cajero", "Auditor", "Contador", "EncargadoInventario"
    };

    public static async Task SeedAsync(
        ApplicationDbContext db, BootstrapOptions options, IAuthService authService, ILogger logger)
    {
        await SeedRolesAsync(db, logger);
        await SeedSuperAdminAsync(db, options, authService, logger);
    }

    private static async Task SeedRolesAsync(ApplicationDbContext db, ILogger logger)
    {
        var existentes = await db.Roles.Select(r => r.Nombre).ToListAsync();
        var faltantes = RolesCanonicos.Where(n => !existentes.Contains(n)).ToList();
        if (faltantes.Count == 0) return;

        foreach (var nombre in faltantes)
            db.Roles.Add(new Rol { Nombre = nombre });

        await db.SaveChangesAsync();
        logger.LogInformation("Bootstrap: sembrados {Count} roles ({Roles}).", faltantes.Count, string.Join(", ", faltantes));
    }

    private static async Task SeedSuperAdminAsync(
        ApplicationDbContext db, BootstrapOptions options, IAuthService authService, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(options.AdminEmail) || string.IsNullOrWhiteSpace(options.AdminPassword))
        {
            logger.LogWarning("Bootstrap: sin Bootstrap:AdminEmail/AdminPassword; no se crea el usuario SuperAdmin inicial.");
            return;
        }

        var email = options.AdminEmail.Trim();
        if (await db.Usuarios.AnyAsync(u => u.Email == email))
            return;

        var rolSuperAdmin = await db.Roles.FirstAsync(r => r.Nombre == "SuperAdmin");

        db.Usuarios.Add(new Usuario
        {
            NombreCompleto = string.IsNullOrWhiteSpace(options.AdminNombre) ? "Administrador" : options.AdminNombre!.Trim(),
            Email = email,
            PasswordHash = authService.HashPassword(options.AdminPassword!),
            ProveedorAuth = ProveedorAutenticacion.Local,
            Estado = EstadoUsuario.Activo,
            RequiereCambioPwd = true,
            AccesoTodasSucursales = true,
            EmisorId = null,
            RolId = rolSuperAdmin.Id
        });

        await db.SaveChangesAsync();
        logger.LogInformation("Bootstrap: creado usuario SuperAdmin inicial {Email} (RequiereCambioPwd=true).", email);
    }
}
