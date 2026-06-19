using FraFactu.Application.DTOs.ProductosServicios;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Entities.Catalogos;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FraFactu.Tests.Services;

/// <summary>
/// Cobertura de TipoImpuesto + PorcentajeIVA en UpsertByCodigoAsync.
/// CREATE: usa valores del request (con validación defensiva). UPDATE: pisa
/// si difieren — SI es fuente de verdad cuando está activo.
/// </summary>
public class SyncProductoUpsertTipoImpuestoTests
{
    private const int Emisor = 1;

    private static ApplicationDbContext BuildCtx()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"UpsertTipoImpuesto_{Guid.NewGuid()}")
            .Options;
        var db = new ApplicationDbContext(opts);
        db.Set<CatUnidadMedida>().Add(new CatUnidadMedida { Id = 1, Codigo = "59", Valor = "Unidad" });
        db.SaveChanges();
        return db;
    }

    private static ProductoServicioService BuildService(ApplicationDbContext db) =>
        new ProductoServicioService(db, null!, null!, null!);

    private static SyncProductoUpsertRequestDto Req(
        string codigo = "P-01",
        int tipoImpuesto = 1,
        decimal? porcentajeIVA = 13m) => new()
    {
        EmisorId = Emisor, Codigo = codigo,
        Nombre = "Producto " + codigo, Tipo = "Producto", PrecioVenta = 10m,
        UnidadMedida = "Unidad", StockMinimo = 0, Activo = true,
        TipoImpuesto = tipoImpuesto, PorcentajeIVA = porcentajeIVA,
    };

    [Fact]
    public async Task Create_ConTipoImpuestoExento_PersisteExentoYIVANull()
    {
        await using var db = BuildCtx();
        var svc = BuildService(db);

        var res = await svc.UpsertByCodigoAsync(Req("P-EX", tipoImpuesto: 2, porcentajeIVA: 13m));

        res.Creado.Should().BeTrue();
        var p = await db.ProductosServicios.SingleAsync(x => x.Codigo == "P-EX");
        p.TipoImpuesto.Should().Be(TipoImpuesto.Exento);
        p.PorcentajeIVA.Should().BeNull();
    }

    [Fact]
    public async Task Create_ConGravadoIvaCustom_PersisteAmbos()
    {
        await using var db = BuildCtx();
        var svc = BuildService(db);

        var res = await svc.UpsertByCodigoAsync(Req("P-G18", tipoImpuesto: 1, porcentajeIVA: 18m));

        res.Creado.Should().BeTrue();
        var p = await db.ProductosServicios.SingleAsync(x => x.Codigo == "P-G18");
        p.TipoImpuesto.Should().Be(TipoImpuesto.Gravado);
        p.PorcentajeIVA.Should().Be(18m);
    }

    [Fact]
    public async Task Create_TipoImpuestoFueraDeRango_CaeAGravado13()
    {
        await using var db = BuildCtx();
        var svc = BuildService(db);

        var res = await svc.UpsertByCodigoAsync(Req("P-OOR", tipoImpuesto: 99, porcentajeIVA: null));

        res.Creado.Should().BeTrue();
        var p = await db.ProductosServicios.SingleAsync(x => x.Codigo == "P-OOR");
        p.TipoImpuesto.Should().Be(TipoImpuesto.Gravado);
        p.PorcentajeIVA.Should().Be(13m);
    }

    [Fact]
    public async Task Create_GravadoSinIvaValido_CaeA13()
    {
        await using var db = BuildCtx();
        var svc = BuildService(db);

        var res = await svc.UpsertByCodigoAsync(Req("P-G0", tipoImpuesto: 1, porcentajeIVA: 0m));

        res.Creado.Should().BeTrue();
        var p = await db.ProductosServicios.SingleAsync(x => x.Codigo == "P-G0");
        p.PorcentajeIVA.Should().Be(13m);
    }

    [Fact]
    public async Task Update_CambiaGravadoAExento_PisaYNullea_IVA()
    {
        await using var db = BuildCtx();
        // Seed: producto existente Gravado 13
        db.ProductosServicios.Add(new ProductoServicio
        {
            EmisorId = Emisor, Codigo = "P-CHG", Nombre = "Original",
            PrecioVenta = 10m, CatTipoItemId = 1, CatUnidadMedidaId = 1,
            TipoImpuesto = TipoImpuesto.Gravado, PorcentajeIVA = 13m,
            AccesoTodasSucursales = true, Activo = true,
        });
        await db.SaveChangesAsync();
        var svc = BuildService(db);

        var res = await svc.UpsertByCodigoAsync(Req("P-CHG", tipoImpuesto: 2, porcentajeIVA: 13m));

        res.Creado.Should().BeFalse();
        res.Cambio.Should().BeTrue();
        var p = await db.ProductosServicios.SingleAsync(x => x.Codigo == "P-CHG");
        p.TipoImpuesto.Should().Be(TipoImpuesto.Exento);
        p.PorcentajeIVA.Should().BeNull();
    }

    [Fact]
    public async Task Update_SinCambioEnTipoImpuesto_CambioFlagFalse()
    {
        await using var db = BuildCtx();
        // Todos los campos deben coincidir con lo que envía Req() para evitar otros diffs
        db.ProductosServicios.Add(new ProductoServicio
        {
            EmisorId = Emisor, Codigo = "P-EQ", Nombre = "Producto P-EQ",
            PrecioVenta = 10m, CatTipoItemId = 1, CatUnidadMedidaId = 1,
            StockMinimo = 0m,
            TipoImpuesto = TipoImpuesto.Gravado, PorcentajeIVA = 13m,
            AccesoTodasSucursales = true, Activo = true,
        });
        await db.SaveChangesAsync();
        var svc = BuildService(db);

        // Mismo TipoImpuesto y PorcentajeIVA que el existente — no deben provocar cambio
        var res = await svc.UpsertByCodigoAsync(Req("P-EQ", tipoImpuesto: 1, porcentajeIVA: 13m));

        res.Cambio.Should().BeFalse();
    }
}
