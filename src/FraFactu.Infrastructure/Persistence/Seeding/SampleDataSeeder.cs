using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Persistence.Seeding;

/// <summary>
/// Siembra un tenant de ejemplo (emisor + sucursal + caja + EmisorAdmin + Cajero)
/// para Development. Idempotente por el Nit del emisor demo. Las FKs de catálogo
/// se resuelven por lookup (primera fila disponible), sin hardcodear IDs. Asume
/// que los roles EmisorAdmin/Cajero ya existen (los siembra BootstrapSeeder).
/// </summary>
public static class SampleDataSeeder
{
    public const string DemoNit = "00000000000000";
    public const string DemoTempPassword = "Demo#2026";

    public static async Task SeedAsync(ApplicationDbContext db, IAuthService authService, ILogger logger)
    {
        if (await db.Emisores.AnyAsync(e => e.Nit == DemoNit))
            return;

        var departamentoId = (await db.CatDepartamentos.OrderBy(c => c.Id).FirstAsync()).Id;
        var municipioId = (await db.CatMunicipios.OrderBy(c => c.Id).FirstAsync()).Id;
        var tipoEstablecimientoId = (await db.CatTiposEstablecimiento.OrderBy(c => c.Id).FirstAsync()).Id;

        var rolEmisorAdmin = await db.Roles.FirstAsync(r => r.Nombre == "EmisorAdmin");
        var rolCajero = await db.Roles.FirstAsync(r => r.Nombre == "Cajero");

        var emisor = new Emisor
        {
            Nit = DemoNit,
            Nrc = "0000000",
            NombreRazonSocial = "Empresa Demo S.A. de C.V.",
            NombreComercial = "Demo",
            CodigoActividad = "00000",
            DescripcionActividad = "Actividad demo",
            CorreoElectronico = "demo@frafactu.local",
            Telefono = "00000000",
            CatDepartamentoId = departamentoId,
            CatMunicipioId = municipioId,
            Direccion = "Dirección demo"
        };
        db.Emisores.Add(emisor);
        await db.SaveChangesAsync();

        var sucursal = new Sucursal
        {
            Codigo = "S001",
            Nombre = "Casa Matriz",
            CatDepartamentoId = departamentoId,
            CatMunicipioId = municipioId,
            CatTipoEstablecimientoId = tipoEstablecimientoId,
            EmisorId = emisor.Id
        };
        db.Sucursales.Add(sucursal);
        await db.SaveChangesAsync();

        db.Cajas.Add(new Caja
        {
            Codigo = "C001",
            Nombre = "Caja 1",
            CodPuntoVenta = string.Empty,
            CodPuntoVentaMH = string.Empty,
            SucursalId = sucursal.Id
        });

        var admin = new Usuario
        {
            NombreCompleto = "Admin Demo",
            Email = "admin.demo@frafactu.local",
            PasswordHash = authService.HashPassword(DemoTempPassword),
            ProveedorAuth = ProveedorAutenticacion.Local,
            Estado = EstadoUsuario.Activo,
            RequiereCambioPwd = true,
            AccesoTodasSucursales = true,
            EmisorId = emisor.Id,
            RolId = rolEmisorAdmin.Id
        };
        var cajero = new Usuario
        {
            NombreCompleto = "Cajero Demo",
            Email = "cajero.demo@frafactu.local",
            PasswordHash = authService.HashPassword(DemoTempPassword),
            ProveedorAuth = ProveedorAutenticacion.Local,
            Estado = EstadoUsuario.Activo,
            RequiereCambioPwd = true,
            AccesoTodasSucursales = false,
            EmisorId = emisor.Id,
            RolId = rolCajero.Id
        };
        db.Usuarios.Add(admin);
        db.Usuarios.Add(cajero);
        await db.SaveChangesAsync();

        db.UsuariosSucursales.Add(new UsuarioSucursal { UsuarioId = cajero.Id, SucursalId = sucursal.Id });
        await db.SaveChangesAsync();

        logger.LogInformation(
            "SampleData: sembrado tenant demo (emisor {Nit}). Usuarios demo: admin.demo@frafactu.local / cajero.demo@frafactu.local, clave temporal '{Pwd}' (RequiereCambioPwd).",
            DemoNit, DemoTempPassword);
    }
}
