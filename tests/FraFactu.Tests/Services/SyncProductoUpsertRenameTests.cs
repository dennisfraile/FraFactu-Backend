using FraFactu.Application.DTOs.ProductosServicios;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Entities.Catalogos;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FraFactu.Tests.Services;

/// <summary>
/// Cubre el renombrado de codigo via UpsertByCodigoAsync (sync desde SmartInventory).
/// Cuando el upsert trae CodigoAnterior != Codigo, Smartix debe encontrar el producto
/// por el codigo VIEJO y cambiarle el Codigo (en vez de crear un duplicado). El codigo
/// es la clave de match cross-app, asi que esta es la unica forma limpia de renombrarlo.
/// </summary>
public class SyncProductoUpsertRenameTests
{
    private const int Emisor = 1;

    private static ApplicationDbContext BuildCtx()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"SyncUpsertRename_{Guid.NewGuid()}")
            .Options;
        var db = new ApplicationDbContext(opts);
        db.Set<CatUnidadMedida>().Add(new CatUnidadMedida { Id = 1, Codigo = "59", Valor = "Unidad" });
        db.SaveChanges();
        return db;
    }

    // UpsertByCodigoAsync solo usa el DbContext; mapper/validators no se tocan.
    private static ProductoServicioService BuildService(ApplicationDbContext db) =>
        new ProductoServicioService(db, null!, null!, null!);

    private static ProductoServicio SeedProducto(ApplicationDbContext db, string codigo, bool activo = true)
    {
        var p = new ProductoServicio
        {
            EmisorId = Emisor, Codigo = codigo, Nombre = "Producto " + codigo,
            PrecioVenta = 10m, CatTipoItemId = 1, CatUnidadMedidaId = 1,
            TipoImpuesto = Domain.Enums.TipoImpuesto.Gravado, PorcentajeIVA = 13m,
            AccesoTodasSucursales = true, Activo = activo,
        };
        db.ProductosServicios.Add(p);
        db.SaveChanges();
        return p;
    }

    private static SyncProductoUpsertRequestDto Req(string codigo, string? codigoAnterior = null) => new()
    {
        EmisorId = Emisor, Codigo = codigo, CodigoAnterior = codigoAnterior,
        Nombre = "Producto " + codigo, Tipo = "Producto", PrecioVenta = 10m,
        UnidadMedida = "Unidad", StockMinimo = 0, Activo = true,
    };

    [Fact]
    public async Task ConCodigoAnterior_RenombraElExistente_SinDuplicar()
    {
        var db = BuildCtx();
        var original = SeedProducto(db, "PROD-001");
        var svc = BuildService(db);

        var resp = await svc.UpsertByCodigoAsync(Req("PROD-999", codigoAnterior: "PROD-001"));

        resp.Creado.Should().BeFalse("debe actualizar el existente, no crear");
        resp.Id.Should().Be(original.Id);
        resp.Codigo.Should().Be("PROD-999");

        // Un solo producto en BD, con el codigo nuevo.
        var todos = await db.ProductosServicios.Where(p => p.EmisorId == Emisor).ToListAsync();
        todos.Should().ContainSingle();
        todos[0].Codigo.Should().Be("PROD-999");
        todos[0].Id.Should().Be(original.Id);
    }

    [Fact]
    public async Task SinCodigoAnterior_CambioDeCodigo_CreaNuevo_NoRenombra()
    {
        // Sin CodigoAnterior, un codigo "nuevo" se trata como upsert normal: no encuentra
        // el viejo y crea uno nuevo (comportamiento legacy, queda el duplicado). Documenta
        // por que el rename necesita explicitamente CodigoAnterior.
        var db = BuildCtx();
        SeedProducto(db, "PROD-001");
        var svc = BuildService(db);

        var resp = await svc.UpsertByCodigoAsync(Req("PROD-999")); // sin codigoAnterior

        resp.Creado.Should().BeTrue();
        var todos = await db.ProductosServicios.Where(p => p.EmisorId == Emisor).ToListAsync();
        todos.Should().HaveCount(2, "sin CodigoAnterior se crea un duplicado");
    }

    [Fact]
    public async Task RenameConCodigoNuevoYaEnUsoPorOtroActivo_Lanza()
    {
        var db = BuildCtx();
        SeedProducto(db, "PROD-001");
        SeedProducto(db, "PROD-999"); // ya existe activo con el codigo destino
        var svc = BuildService(db);

        var act = async () => await svc.UpsertByCodigoAsync(Req("PROD-999", codigoAnterior: "PROD-001"));

        (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage("*PROD-999*");
        // No se renombro: PROD-001 sigue existiendo.
        (await db.ProductosServicios.AnyAsync(p => p.Codigo == "PROD-001")).Should().BeTrue();
    }

    [Fact]
    public async Task RenameRetry_YaRenombrado_EsIdempotente_NoDuplicaNiFalla()
    {
        // El producto ya quedo con el codigo nuevo (un reintento del rename). Buscar por
        // el viejo no encuentra nada -> fallback al nuevo -> no duplica, no cambia el codigo.
        var db = BuildCtx();
        var p = SeedProducto(db, "PROD-999");
        var svc = BuildService(db);

        var resp = await svc.UpsertByCodigoAsync(Req("PROD-999", codigoAnterior: "PROD-001"));

        resp.Creado.Should().BeFalse();
        resp.Id.Should().Be(p.Id);
        (await db.ProductosServicios.CountAsync(x => x.EmisorId == Emisor)).Should().Be(1);
    }
}
