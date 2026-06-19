using FraFactu.Domain.Entities;
using FraFactu.Domain.Entities.Catalogos;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FraFactu.Tests.Services;

/// <summary>
/// Cubre ExportarInventarioAsync (sender Smartix -> SmartInventory).
/// El bug previo derivaba las bodegas a partir de los stocks: una bodega
/// recién creada sin productos quedaba fuera del export. El fix consulta
/// Bodegas directo + union defensiva con las que tienen stock.
/// </summary>
public class InventoryMigrationServiceExportTests
{
    private const int HubId = 99;
    private const int EmisorId = 1;

    private static ApplicationDbContext BuildCtx()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"InvMigrationExport_{Guid.NewGuid()}")
            .Options;
        var db = new ApplicationDbContext(opts);

        db.Set<CatUnidadMedida>().Add(new CatUnidadMedida { Id = 1, Codigo = "59", Valor = "Unidad" });
        db.Set<CatTipoItem>().Add(new CatTipoItem { Id = 1, Codigo = "1", Valor = "Bienes" });

        db.Emisores.Add(new Emisor
        {
            Id = EmisorId,
            HubId = HubId,
            Nit = "06140506141011",
            Nrc = "123456-7",
            NombreRazonSocial = "EMPRESA DE PRUEBAS SA DE CV",
            NombreComercial = "PRUEBAS SA",
            CodigoActividad = "47111",
            DescripcionActividad = "Venta al por menor",
            CatTipoEstablecimientoId = 1,
            CorreoElectronico = "test@empresa.com",
            Telefono = "22223333",
            CatDepartamentoId = 1,
            CatMunicipioId = 1,
            Direccion = "San Salvador"
        });
        db.SaveChanges();
        return db;
    }

    private static Sucursal SeedSucursal(ApplicationDbContext db, int id, int? hubSucursalId)
    {
        var s = new Sucursal
        {
            Id = id,
            EmisorId = EmisorId,
            HubSucursalId = hubSucursalId,
            Codigo = $"S{id:D3}",
            Nombre = $"Sucursal {id}",
            Direccion = "Dirección",
            CatDepartamentoId = 1,
            CatMunicipioId = 1,
            CatTipoEstablecimientoId = 1,
            CodigoEstablecimiento = $"{id:D4}"
        };
        db.Sucursales.Add(s);
        db.SaveChanges();
        return s;
    }

    private static Bodega SeedBodega(ApplicationDbContext db, int id, string nombre, int sucursalId, bool activa = true, bool esPrincipal = false)
    {
        var b = new Bodega
        {
            Id = id,
            Codigo = $"BOD{id:D3}",
            Nombre = nombre,
            SucursalId = sucursalId,
            Activa = activa,
            EsPrincipal = esPrincipal
        };
        db.Bodegas.Add(b);
        db.SaveChanges();
        return b;
    }

    private static ProductoServicio SeedProducto(ApplicationDbContext db, int id, string codigo)
    {
        var p = new ProductoServicio
        {
            Id = id,
            EmisorId = EmisorId,
            Codigo = codigo,
            Nombre = $"Producto {codigo}",
            PrecioVenta = 10m,
            CatTipoItemId = 1,
            CatUnidadMedidaId = 1,
            TipoImpuesto = TipoImpuesto.Gravado,
            PorcentajeIVA = 13m,
            AccesoTodasSucursales = true,
            Activo = true
        };
        db.ProductosServicios.Add(p);
        db.SaveChanges();
        return p;
    }

    private static void SeedStock(ApplicationDbContext db, int productoId, int bodegaId, int cantidad)
    {
        db.StocksBodega.Add(new StockBodega
        {
            ProductoId = productoId,
            BodegaId = bodegaId,
            CantidadDisponible = cantidad,
            CantidadReservada = 0,
            CostoPromedio = 5m,
            UltimaActualizacion = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    private static InventoryMigrationService BuildService(ApplicationDbContext db) =>
        new(db, NullLogger<InventoryMigrationService>.Instance);

    [Fact]
    public async Task Exportar_BodegaActivaSinStock_QuedaIncluidaEnElPayload()
    {
        // Repro Bug 1: dos bodegas activas, solo una tiene stock; la vacía debe aparecer.
        var db = BuildCtx();
        var sucursal = SeedSucursal(db, id: 1, hubSucursalId: 10);
        var bodegaConStock = SeedBodega(db, id: 1, nombre: "Bodega Central", sucursalId: sucursal.Id, esPrincipal: true);
        var bodegaVacia = SeedBodega(db, id: 2, nombre: "Bodega Metrocentro", sucursalId: sucursal.Id);
        var producto = SeedProducto(db, id: 1, codigo: "PROD-001");
        SeedStock(db, producto.Id, bodegaConStock.Id, cantidad: 50);

        var svc = BuildService(db);
        var resp = await svc.ExportarInventarioAsync(HubId, Guid.NewGuid());

        resp.Bodegas.Should().HaveCount(2, "ambas bodegas activas deben exportarse aunque solo una tenga stock");
        resp.Bodegas.Select(b => b.Nombre).Should().BeEquivalentTo(new[] { "Bodega Central", "Bodega Metrocentro" });
        resp.Stock.Should().HaveCount(1);
    }

    [Fact]
    public async Task Exportar_BodegaInactivaConStock_TambienSeIncluye_DefensaEnProfundidad()
    {
        // Si por alguna razón quedó stock en una bodega inactiva, no la perdemos en SI.
        var db = BuildCtx();
        var sucursal = SeedSucursal(db, id: 1, hubSucursalId: 10);
        var bodegaActiva = SeedBodega(db, id: 1, nombre: "Bodega Central", sucursalId: sucursal.Id, esPrincipal: true);
        var bodegaInactiva = SeedBodega(db, id: 2, nombre: "Bodega Vieja", sucursalId: sucursal.Id, activa: false);
        var producto = SeedProducto(db, id: 1, codigo: "PROD-001");
        SeedStock(db, producto.Id, bodegaActiva.Id, cantidad: 10);
        SeedStock(db, producto.Id, bodegaInactiva.Id, cantidad: 3);

        var svc = BuildService(db);
        var resp = await svc.ExportarInventarioAsync(HubId, Guid.NewGuid());

        resp.Bodegas.Should().HaveCount(2);
        resp.Bodegas.Select(b => b.Nombre).Should().Contain("Bodega Vieja");
    }

    [Fact]
    public async Task Exportar_BodegaSinSucursal_QuedaFuera()
    {
        // Bodegas sin SucursalId no se exportan (no podrían mapear HubSucursalId).
        var db = BuildCtx();
        SeedBodega(db, id: 1, nombre: "Bodega Huérfana", sucursalId: 0, activa: true);
        // Forzamos SucursalId = null saltando la convención del helper
        var huerfana = db.Bodegas.First();
        huerfana.SucursalId = null;
        db.SaveChanges();

        var svc = BuildService(db);
        var resp = await svc.ExportarInventarioAsync(HubId, Guid.NewGuid());

        resp.Bodegas.Should().BeEmpty();
    }

    [Fact]
    public async Task Exportar_BodegaDeOtroEmisor_NoSeIncluye()
    {
        // Aislamiento por emisor: una bodega ligada a sucursal de otro emisor no fuga.
        var db = BuildCtx();
        var sucursalMia = SeedSucursal(db, id: 1, hubSucursalId: 10);
        SeedBodega(db, id: 1, nombre: "Mía", sucursalId: sucursalMia.Id);

        // Emisor ajeno + su sucursal + su bodega
        db.Emisores.Add(new Emisor
        {
            Id = 99,
            HubId = 999,
            Nit = "06140506141022",
            Nrc = "999999-9",
            NombreRazonSocial = "OTRA EMPRESA",
            NombreComercial = "OTRA",
            CodigoActividad = "47111",
            DescripcionActividad = "Otra",
            CatTipoEstablecimientoId = 1,
            CorreoElectronico = "otra@empresa.com",
            Telefono = "22220000",
            CatDepartamentoId = 1,
            CatMunicipioId = 1,
            Direccion = "Otra dirección"
        });
        db.SaveChanges();
        var sucursalAjena = SeedSucursal(db, id: 2, hubSucursalId: 20);
        sucursalAjena.EmisorId = 99;
        db.SaveChanges();
        SeedBodega(db, id: 2, nombre: "Ajena", sucursalId: sucursalAjena.Id);

        var svc = BuildService(db);
        var resp = await svc.ExportarInventarioAsync(HubId, Guid.NewGuid());

        resp.Bodegas.Should().ContainSingle();
        resp.Bodegas.Single().Nombre.Should().Be("Mía");
    }

    [Fact]
    public async Task Exportar_MapeaSucursalIdExternoDesdeHubSucursalId()
    {
        // La bodega activa sin stock también debe llevar HubSucursalId correcto.
        var db = BuildCtx();
        var sucursal = SeedSucursal(db, id: 1, hubSucursalId: 42);
        SeedBodega(db, id: 1, nombre: "Vacía", sucursalId: sucursal.Id);

        var svc = BuildService(db);
        var resp = await svc.ExportarInventarioAsync(HubId, Guid.NewGuid());

        resp.Bodegas.Should().ContainSingle();
        resp.Bodegas.Single().SucursalIdExterno.Should().Be(42);
    }
}
