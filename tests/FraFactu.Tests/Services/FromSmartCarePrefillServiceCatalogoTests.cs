using FraFactu.Application.Common.Interfaces;
using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs.FromSmartCare;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Entities.Catalogos;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace FraFactu.Tests.Services;

/// <summary>
/// Tests para <see cref="FromSmartCarePrefillService.BuscarCatalogoAsync"/>
/// y <see cref="FromSmartCarePrefillService.ResolverEmisorPorSucursalAsync"/>.
/// </summary>
public class FromSmartCarePrefillServiceCatalogoTests
{
    private static ApplicationDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"catalogo-{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static IOptions<SmartCareSettings> Settings() =>
        Options.Create(new SmartCareSettings
        {
            ApiKey = "x",
            WebhookApiKey = "y",
            SmartixFrontendBaseUrl = "http://localhost:5180"
        });

    private static FromSmartCarePrefillService BuildService(ApplicationDbContext ctx) =>
        new FromSmartCarePrefillService(
            ctx,
            NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object,
            Settings(),
            new Mock<ISmartCareWebhookService>().Object);

    // =========================================================================
    // BuscarCatalogoAsync — filtros de sucursal + activo + search
    // =========================================================================

    [Fact]
    public async Task BuscarCatalogoAsync_ReturnsActiveProductsForEmisor_FilteredByTerm()
    {
        using var ctx = BuildContext();

        // Se necesita CatUnidadMedida para que la FK de ProductoServicio no falle
        ctx.CatUnidadesMedida.Add(new CatUnidadMedida { Id = 1, Codigo = "UN", Valor = "Unidad" });
        ctx.CatTiposItem.AddRange(
            new CatTipoItem { Id = 1, Codigo = "1", Valor = "Bien" },
            new CatTipoItem { Id = 2, Codigo = "2", Valor = "Servicio" }
        );
        ctx.Sucursales.Add(new Sucursal
        {
            Id = 1, EmisorId = 1, Activo = true,
            Codigo = "SUC01", Nombre = "Sucursal Test",
            CatDepartamentoId = 1, CatMunicipioId = 1, CatTipoEstablecimientoId = 1
        });
        ctx.ProductosServicios.AddRange(
            new ProductoServicio
            {
                Id = 10, EmisorId = 1, Codigo = "GASA-001",
                Nombre = "Gasa estéril 5x5", PrecioVenta = 0.50m,
                Activo = true, CatTipoItemId = 1, CatUnidadMedidaId = 1,
                AccesoTodasSucursales = true
            },
            new ProductoServicio
            {
                Id = 11, EmisorId = 1, Codigo = "CONS-001",
                Nombre = "Consulta general", PrecioVenta = 35m,
                Activo = true, CatTipoItemId = 2, CatUnidadMedidaId = 1,
                AccesoTodasSucursales = true
            },
            // Inactivo — no debe aparecer
            new ProductoServicio
            {
                Id = 12, EmisorId = 1, Codigo = "GASA-002",
                Nombre = "Gasa 10x10", PrecioVenta = 0.80m,
                Activo = false, CatTipoItemId = 1, CatUnidadMedidaId = 1,
                AccesoTodasSucursales = true
            },
            // Otro emisor — no debe aparecer
            new ProductoServicio
            {
                Id = 20, EmisorId = 2, Codigo = "GASA-001",
                Nombre = "Otra clínica", PrecioVenta = 1m,
                Activo = true, CatTipoItemId = 1, CatUnidadMedidaId = 1,
                AccesoTodasSucursales = true
            }
        );
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);
        var result = await svc.BuscarCatalogoAsync(sucursalSmartixId: 1, search: "gasa", limit: 10);

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Codigo.Should().Be("GASA-001");
        result.Items[0].Descripcion.Should().Be("Gasa estéril 5x5");
        result.Items[0].EsServicio.Should().BeFalse();
        result.Total.Should().Be(1);
    }

    [Fact]
    public async Task BuscarCatalogoAsync_SinSearch_ReturnsTodosLosActivosDelEmisor()
    {
        using var ctx = BuildContext();

        ctx.CatUnidadesMedida.Add(new CatUnidadMedida { Id = 1, Codigo = "UN", Valor = "Unidad" });
        ctx.CatTiposItem.Add(new CatTipoItem { Id = 2, Codigo = "2", Valor = "Servicio" });
        ctx.Sucursales.Add(new Sucursal
        {
            Id = 50, EmisorId = 5, Activo = true,
            Codigo = "SUC50", Nombre = "Sucursal 50",
            CatDepartamentoId = 1, CatMunicipioId = 1, CatTipoEstablecimientoId = 1
        });
        ctx.ProductosServicios.AddRange(
            new ProductoServicio { Id = 1, EmisorId = 5, Codigo = "A", Nombre = "Alpha", PrecioVenta = 10m, Activo = true, CatTipoItemId = 2, CatUnidadMedidaId = 1, AccesoTodasSucursales = true },
            new ProductoServicio { Id = 2, EmisorId = 5, Codigo = "B", Nombre = "Beta",  PrecioVenta = 20m, Activo = true, CatTipoItemId = 2, CatUnidadMedidaId = 1, AccesoTodasSucursales = true },
            new ProductoServicio { Id = 3, EmisorId = 5, Codigo = "C", Nombre = "Gamma", PrecioVenta = 30m, Activo = false, CatTipoItemId = 2, CatUnidadMedidaId = 1, AccesoTodasSucursales = true }
        );
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);
        var result = await svc.BuscarCatalogoAsync(sucursalSmartixId: 50, search: null, limit: 50);

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Total.Should().Be(2);
    }

    [Fact]
    public async Task BuscarCatalogoAsync_LimitRespetado()
    {
        using var ctx = BuildContext();

        ctx.CatUnidadesMedida.Add(new CatUnidadMedida { Id = 1, Codigo = "UN", Valor = "Unidad" });
        ctx.CatTiposItem.Add(new CatTipoItem { Id = 1, Codigo = "1", Valor = "Bien" });
        ctx.Sucursales.Add(new Sucursal
        {
            Id = 30, EmisorId = 3, Activo = true,
            Codigo = "SUC30", Nombre = "Sucursal 30",
            CatDepartamentoId = 1, CatMunicipioId = 1, CatTipoEstablecimientoId = 1
        });
        for (var i = 1; i <= 10; i++)
        {
            ctx.ProductosServicios.Add(new ProductoServicio
            {
                Id = i, EmisorId = 3, Codigo = $"P-{i:D3}", Nombre = $"Producto {i}",
                PrecioVenta = i, Activo = true, CatTipoItemId = 1, CatUnidadMedidaId = 1,
                AccesoTodasSucursales = true
            });
        }
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);
        var result = await svc.BuscarCatalogoAsync(sucursalSmartixId: 30, search: null, limit: 3);

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.Total.Should().Be(10); // total sin límite
    }

    [Fact]
    public async Task BuscarCatalogoAsync_EsServicio_TrueParaCatTipoItemId2()
    {
        using var ctx = BuildContext();

        ctx.CatUnidadesMedida.Add(new CatUnidadMedida { Id = 1, Codigo = "UN", Valor = "Unidad" });
        ctx.CatTiposItem.AddRange(
            new CatTipoItem { Id = 1, Codigo = "1", Valor = "Bien" },
            new CatTipoItem { Id = 2, Codigo = "2", Valor = "Servicio" }
        );
        ctx.Sucursales.Add(new Sucursal
        {
            Id = 70, EmisorId = 7, Activo = true,
            Codigo = "SUC70", Nombre = "Sucursal 70",
            CatDepartamentoId = 1, CatMunicipioId = 1, CatTipoEstablecimientoId = 1
        });
        ctx.ProductosServicios.AddRange(
            new ProductoServicio { Id = 1, EmisorId = 7, Codigo = "P1", Nombre = "Producto", PrecioVenta = 5m, Activo = true, CatTipoItemId = 1, CatUnidadMedidaId = 1, AccesoTodasSucursales = true },
            new ProductoServicio { Id = 2, EmisorId = 7, Codigo = "S1", Nombre = "Servicio", PrecioVenta = 50m, Activo = true, CatTipoItemId = 2, CatUnidadMedidaId = 1, AccesoTodasSucursales = true }
        );
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);
        var result = await svc.BuscarCatalogoAsync(sucursalSmartixId: 70, search: null, limit: 50);

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        var producto = result.Items.Single(x => x.Codigo == "P1");
        var servicio = result.Items.Single(x => x.Codigo == "S1");
        producto.EsServicio.Should().BeFalse();
        servicio.EsServicio.Should().BeTrue();
    }

    // =========================================================================
    // BuscarCatalogoAsync — filtro de acceso por sucursal (AccesoTodasSucursales + puente)
    // =========================================================================

    [Fact]
    public async Task BuscarCatalogoAsync_FiltroSucursal_AccesoTodasYPuente()
    {
        // 3 productos en emisor:
        // - GASA-001: AccesoTodasSucursales=true  → incluido para sucursal 42
        // - LENTE-001: AccesoTodasSucursales=false, fila puente para suc 42 → incluido
        // - INSUMO-X: AccesoTodasSucursales=false, sin puente → excluido
        using var ctx = BuildContext();

        ctx.CatUnidadesMedida.Add(new CatUnidadMedida { Id = 1, Codigo = "UN", Valor = "Unidad" });
        ctx.CatTiposItem.Add(new CatTipoItem { Id = 1, Codigo = "1", Valor = "Bien" });
        ctx.Sucursales.Add(new Sucursal
        {
            Id = 42, EmisorId = 10, Activo = true,
            Codigo = "SUC42", Nombre = "Sucursal 42",
            CatDepartamentoId = 1, CatMunicipioId = 1, CatTipoEstablecimientoId = 1
        });
        ctx.ProductosServicios.AddRange(
            new ProductoServicio
            {
                Id = 101, EmisorId = 10, Codigo = "GASA-001", Nombre = "Gasa estéril",
                PrecioVenta = 5m, Activo = true, CatTipoItemId = 1, CatUnidadMedidaId = 1,
                AccesoTodasSucursales = true
            },
            new ProductoServicio
            {
                Id = 102, EmisorId = 10, Codigo = "LENTE-001", Nombre = "Lente contacto",
                PrecioVenta = 20m, Activo = true, CatTipoItemId = 1, CatUnidadMedidaId = 1,
                AccesoTodasSucursales = false
            },
            new ProductoServicio
            {
                Id = 103, EmisorId = 10, Codigo = "INSUMO-X", Nombre = "Insumo extra",
                PrecioVenta = 8m, Activo = true, CatTipoItemId = 1, CatUnidadMedidaId = 1,
                AccesoTodasSucursales = false
            }
        );
        // Tabla puente: LENTE-001 asignado a sucursal 42; INSUMO-X no tiene fila
        ctx.ProductosServiciosSucursales.Add(new ProductoServicioSucursal
        {
            ProductoServicioId = 102,
            SucursalId = 42
        });
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);
        var result = await svc.BuscarCatalogoAsync(sucursalSmartixId: 42, search: null, limit: 50);

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items.Select(i => i.Codigo).Should().Contain("GASA-001");
        result.Items.Select(i => i.Codigo).Should().Contain("LENTE-001");
        result.Items.Select(i => i.Codigo).Should().NotContain("INSUMO-X");
    }

    // =========================================================================
    // BuscarCatalogoAsync — IVA aplicado para Gravado, no para Exento
    // =========================================================================

    [Fact]
    public async Task BuscarCatalogoAsync_IVAAplicadoParaGravado_ExentoSinCambio()
    {
        // GASA-G: Gravado 13%, PrecioVenta=50.00 → PrecioUnitario debe ser 56.50
        // GASA-E: Exento, PrecioVenta=10.00 → PrecioUnitario debe ser 10.00
        // Ambos deben tener PrecioIncluyeIva=true
        using var ctx = BuildContext();

        ctx.CatUnidadesMedida.Add(new CatUnidadMedida { Id = 1, Codigo = "UN", Valor = "Unidad" });
        ctx.CatTiposItem.Add(new CatTipoItem { Id = 1, Codigo = "1", Valor = "Bien" });
        ctx.Sucursales.Add(new Sucursal
        {
            Id = 55, EmisorId = 20, Activo = true,
            Codigo = "SUC55", Nombre = "Sucursal 55",
            CatDepartamentoId = 1, CatMunicipioId = 1, CatTipoEstablecimientoId = 1
        });
        ctx.ProductosServicios.AddRange(
            new ProductoServicio
            {
                Id = 201, EmisorId = 20, Codigo = "GASA-G", Nombre = "Gasa Gravada",
                PrecioVenta = 50.00m, TipoImpuesto = TipoImpuesto.Gravado, PorcentajeIVA = 13m,
                Activo = true, CatTipoItemId = 1, CatUnidadMedidaId = 1,
                AccesoTodasSucursales = true
            },
            new ProductoServicio
            {
                Id = 202, EmisorId = 20, Codigo = "GASA-E", Nombre = "Gasa Exenta",
                PrecioVenta = 10.00m, TipoImpuesto = TipoImpuesto.Exento, PorcentajeIVA = null,
                Activo = true, CatTipoItemId = 1, CatUnidadMedidaId = 1,
                AccesoTodasSucursales = true
            }
        );
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);
        var result = await svc.BuscarCatalogoAsync(sucursalSmartixId: 55, search: null, limit: 50);

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);

        var gravado = result.Items.Single(x => x.Codigo == "GASA-G");
        gravado.PrecioUnitario.Should().Be(56.50m); // 50 * 1.13
        gravado.PrecioIncluyeIva.Should().BeTrue();

        var exento = result.Items.Single(x => x.Codigo == "GASA-E");
        exento.PrecioUnitario.Should().Be(10.00m);
        exento.PrecioIncluyeIva.Should().BeTrue();
    }

    // =========================================================================
    // BuscarCatalogoAsync — sucursal inexistente o inactiva → null
    // =========================================================================

    [Fact]
    public async Task BuscarCatalogoAsync_SucursalNoExiste_RetornaNull()
    {
        using var ctx = BuildContext();
        var svc = BuildService(ctx);

        var result = await svc.BuscarCatalogoAsync(sucursalSmartixId: 99999, search: null, limit: 50);

        result.Should().BeNull();
    }

    [Fact]
    public async Task BuscarCatalogoAsync_SucursalInactiva_RetornaNull()
    {
        using var ctx = BuildContext();
        ctx.Sucursales.Add(new Sucursal
        {
            Id = 200, EmisorId = 99, Activo = false,
            Codigo = "SUC-INACT", Nombre = "Sucursal Inactiva",
            CatDepartamentoId = 1, CatMunicipioId = 1, CatTipoEstablecimientoId = 1
        });
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);
        var result = await svc.BuscarCatalogoAsync(sucursalSmartixId: 200, search: null, limit: 50);

        result.Should().BeNull();
    }

    // =========================================================================
    // BuscarCatalogoAsync — filtro por tipo (producto / servicio)
    // =========================================================================

    [Fact]
    public async Task BuscarCatalogoAsync_TipoProducto_ReturnsSoloBienes()
    {
        using var ctx = BuildContext();

        ctx.CatUnidadesMedida.Add(new CatUnidadMedida { Id = 1, Codigo = "UN", Valor = "Unidad" });
        ctx.CatTiposItem.AddRange(
            new CatTipoItem { Id = 1, Codigo = "1", Valor = "Bien" },
            new CatTipoItem { Id = 2, Codigo = "2", Valor = "Servicio" }
        );
        ctx.Sucursales.Add(new Sucursal
        {
            Id = 80, EmisorId = 8, Activo = true,
            Codigo = "SUC80", Nombre = "Sucursal 80",
            CatDepartamentoId = 1, CatMunicipioId = 1, CatTipoEstablecimientoId = 1
        });
        ctx.ProductosServicios.AddRange(
            new ProductoServicio { Id = 1, EmisorId = 8, Codigo = "PROD-001", Nombre = "Gasa", PrecioVenta = 1m, Activo = true, CatTipoItemId = 1, CatUnidadMedidaId = 1, AccesoTodasSucursales = true },
            new ProductoServicio { Id = 2, EmisorId = 8, Codigo = "SERV-001", Nombre = "Consulta", PrecioVenta = 30m, Activo = true, CatTipoItemId = 2, CatUnidadMedidaId = 1, AccesoTodasSucursales = true }
        );
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);
        var result = await svc.BuscarCatalogoAsync(sucursalSmartixId: 80, search: null, limit: 50, tipo: "producto");

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Codigo.Should().Be("PROD-001");
        result.Items[0].EsServicio.Should().BeFalse();
    }

    [Fact]
    public async Task BuscarCatalogoAsync_TipoServicio_ReturnsSoloServicios()
    {
        using var ctx = BuildContext();

        ctx.CatUnidadesMedida.Add(new CatUnidadMedida { Id = 1, Codigo = "UN", Valor = "Unidad" });
        ctx.CatTiposItem.AddRange(
            new CatTipoItem { Id = 1, Codigo = "1", Valor = "Bien" },
            new CatTipoItem { Id = 2, Codigo = "2", Valor = "Servicio" }
        );
        ctx.Sucursales.Add(new Sucursal
        {
            Id = 81, EmisorId = 9, Activo = true,
            Codigo = "SUC81", Nombre = "Sucursal 81",
            CatDepartamentoId = 1, CatMunicipioId = 1, CatTipoEstablecimientoId = 1
        });
        ctx.ProductosServicios.AddRange(
            new ProductoServicio { Id = 1, EmisorId = 9, Codigo = "PROD-002", Nombre = "Aguja", PrecioVenta = 0.5m, Activo = true, CatTipoItemId = 1, CatUnidadMedidaId = 1, AccesoTodasSucursales = true },
            new ProductoServicio { Id = 2, EmisorId = 9, Codigo = "SERV-002", Nombre = "Cirugía", PrecioVenta = 200m, Activo = true, CatTipoItemId = 2, CatUnidadMedidaId = 1, AccesoTodasSucursales = true },
            new ProductoServicio { Id = 3, EmisorId = 9, Codigo = "SERV-003", Nombre = "Revisión", PrecioVenta = 50m, Activo = true, CatTipoItemId = 2, CatUnidadMedidaId = 1, AccesoTodasSucursales = true }
        );
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);
        var result = await svc.BuscarCatalogoAsync(sucursalSmartixId: 81, search: null, limit: 50, tipo: "servicio");

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items.All(x => x.EsServicio).Should().BeTrue();
        result.Items.Select(x => x.Codigo).Should().Contain("SERV-002").And.Contain("SERV-003");
    }

    // =========================================================================
    // BuscarCatalogoAsync — StockDisponible
    // (sumado desde StockBodega.CantidadDisponible filtrado por Bodega.SucursalId)
    // =========================================================================

    [Fact]
    public async Task BuscarCatalogoAsync_IncluyeStockDisponibleParaProducto()
    {
        // Multibodega: producto con stock 10 + 5 en dos bodegas de la sucursal → total 15.
        using var ctx = BuildContext();

        ctx.CatUnidadesMedida.Add(new CatUnidadMedida { Id = 1, Codigo = "UN", Valor = "Unidad" });
        ctx.CatTiposItem.Add(new CatTipoItem { Id = 1, Codigo = "1", Valor = "Bien" });
        ctx.Sucursales.Add(new Sucursal
        {
            Id = 300, EmisorId = 30, Activo = true,
            Codigo = "SUC300", Nombre = "Sucursal 300",
            CatDepartamentoId = 1, CatMunicipioId = 1, CatTipoEstablecimientoId = 1
        });
        ctx.ProductosServicios.Add(new ProductoServicio
        {
            Id = 1001, EmisorId = 30, Codigo = "PROD-STK", Nombre = "Producto con stock",
            PrecioVenta = 10m, Activo = true, CatTipoItemId = 1, CatUnidadMedidaId = 1,
            AccesoTodasSucursales = true
        });
        ctx.Bodegas.AddRange(
            new Bodega { Id = 11, Codigo = "B1", Nombre = "Bodega 1", SucursalId = 300, Activa = true },
            new Bodega { Id = 12, Codigo = "B2", Nombre = "Bodega 2", SucursalId = 300, Activa = true }
        );
        ctx.StocksBodega.AddRange(
            new StockBodega { Id = 1, ProductoId = 1001, BodegaId = 11, CantidadDisponible = 10m },
            new StockBodega { Id = 2, ProductoId = 1001, BodegaId = 12, CantidadDisponible = 5m }
        );
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);
        var result = await svc.BuscarCatalogoAsync(sucursalSmartixId: 300, search: "STK", limit: 50, tipo: "producto");

        result.Should().NotBeNull();
        var item = result!.Items.Single();
        item.StockDisponible.Should().Be(15m);
    }

    [Fact]
    public async Task BuscarCatalogoAsync_ProductoSinStock_RetornaCero()
    {
        // Producto sin filas en StockBodega para esta sucursal → StockDisponible = 0,
        // NO null. Null se reserva para servicios.
        using var ctx = BuildContext();

        ctx.CatUnidadesMedida.Add(new CatUnidadMedida { Id = 1, Codigo = "UN", Valor = "Unidad" });
        ctx.CatTiposItem.Add(new CatTipoItem { Id = 1, Codigo = "1", Valor = "Bien" });
        ctx.Sucursales.Add(new Sucursal
        {
            Id = 301, EmisorId = 31, Activo = true,
            Codigo = "SUC301", Nombre = "Sucursal 301",
            CatDepartamentoId = 1, CatMunicipioId = 1, CatTipoEstablecimientoId = 1
        });
        // Bodega de OTRA sucursal con stock — NO debe contar para la 301.
        ctx.Sucursales.Add(new Sucursal
        {
            Id = 302, EmisorId = 31, Activo = true,
            Codigo = "SUC302", Nombre = "Sucursal 302",
            CatDepartamentoId = 1, CatMunicipioId = 1, CatTipoEstablecimientoId = 1
        });
        ctx.ProductosServicios.Add(new ProductoServicio
        {
            Id = 1101, EmisorId = 31, Codigo = "PROD-ZERO", Nombre = "Producto sin stock",
            PrecioVenta = 10m, Activo = true, CatTipoItemId = 1, CatUnidadMedidaId = 1,
            AccesoTodasSucursales = true
        });
        ctx.Bodegas.Add(new Bodega { Id = 21, Codigo = "B-OTRA", Nombre = "Bodega otra suc", SucursalId = 302, Activa = true });
        ctx.StocksBodega.Add(new StockBodega { Id = 11, ProductoId = 1101, BodegaId = 21, CantidadDisponible = 99m });
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);
        var result = await svc.BuscarCatalogoAsync(sucursalSmartixId: 301, search: null, limit: 50, tipo: "producto");

        result.Should().NotBeNull();
        var item = result!.Items.Single();
        item.StockDisponible.Should().Be(0m);
    }

    [Fact]
    public async Task BuscarCatalogoAsync_Servicio_StockDisponibleEsNull()
    {
        // Para servicios (CatTipoItemId=2) no aplica stock → debe devolver null.
        using var ctx = BuildContext();

        ctx.CatUnidadesMedida.Add(new CatUnidadMedida { Id = 1, Codigo = "UN", Valor = "Unidad" });
        ctx.CatTiposItem.Add(new CatTipoItem { Id = 2, Codigo = "2", Valor = "Servicio" });
        ctx.Sucursales.Add(new Sucursal
        {
            Id = 310, EmisorId = 32, Activo = true,
            Codigo = "SUC310", Nombre = "Sucursal 310",
            CatDepartamentoId = 1, CatMunicipioId = 1, CatTipoEstablecimientoId = 1
        });
        ctx.ProductosServicios.Add(new ProductoServicio
        {
            Id = 1201, EmisorId = 32, Codigo = "SVC-001", Nombre = "Consulta",
            PrecioVenta = 30m, Activo = true, CatTipoItemId = 2, CatUnidadMedidaId = 1,
            AccesoTodasSucursales = true
        });
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);
        var result = await svc.BuscarCatalogoAsync(sucursalSmartixId: 310, search: null, limit: 50, tipo: "servicio");

        result.Should().NotBeNull();
        var item = result!.Items.Single();
        item.EsServicio.Should().BeTrue();
        item.StockDisponible.Should().BeNull();
    }

    // =========================================================================
    // BuscarCatalogoAsync — TipoImpuesto + PorcentajeIVA expuestos en la respuesta
    // (feature TipoImpuesto+IVA SI→Smartix 2026-06-09 — cierre flujo SC→DTE)
    // =========================================================================

    [Fact]
    public async Task BuscarCatalogoAsync_ProductoExento_ExponeTipoImpuesto2YPorcentajeIVANull()
    {
        // Producto Exento: TipoImpuesto debe ser 2, PorcentajeIVA null, y el
        // precio no sufre ajuste de IVA (10.00 → 10.00).
        using var ctx = BuildContext();

        ctx.CatUnidadesMedida.Add(new CatUnidadMedida { Id = 1, Codigo = "UN", Valor = "Unidad" });
        ctx.CatTiposItem.Add(new CatTipoItem { Id = 2, Codigo = "2", Valor = "Servicio" });
        ctx.Sucursales.Add(new Sucursal
        {
            Id = 400, EmisorId = 40, Activo = true,
            Codigo = "SUC400", Nombre = "Sucursal 400",
            CatDepartamentoId = 1, CatMunicipioId = 1, CatTipoEstablecimientoId = 1
        });
        ctx.ProductosServicios.Add(new ProductoServicio
        {
            Id = 2001, EmisorId = 40, Codigo = "P-EX-01", Nombre = "Producto Exento",
            PrecioVenta = 100m, TipoImpuesto = TipoImpuesto.Exento, PorcentajeIVA = null,
            Activo = true, CatTipoItemId = 2, CatUnidadMedidaId = 1,
            AccesoTodasSucursales = true
        });
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);
        var result = await svc.BuscarCatalogoAsync(sucursalSmartixId: 400, search: "P-EX-01", limit: 10);

        result.Should().NotBeNull();
        var item = result!.Items.Single();
        item.TipoImpuesto.Should().Be(2);
        item.PorcentajeIVA.Should().BeNull();
        item.PrecioUnitario.Should().Be(100m);
    }

    [Fact]
    public async Task BuscarCatalogoAsync_ProductoGravado_ExponeTipoImpuesto1YPorcentajeIVA13()
    {
        // Producto Gravado 13%: TipoImpuesto debe ser 1, PorcentajeIVA=13,
        // y el precio ya incluye IVA (100 × 1.13 = 113.00).
        using var ctx = BuildContext();

        ctx.CatUnidadesMedida.Add(new CatUnidadMedida { Id = 1, Codigo = "UN", Valor = "Unidad" });
        ctx.CatTiposItem.Add(new CatTipoItem { Id = 1, Codigo = "1", Valor = "Bien" });
        ctx.Sucursales.Add(new Sucursal
        {
            Id = 401, EmisorId = 41, Activo = true,
            Codigo = "SUC401", Nombre = "Sucursal 401",
            CatDepartamentoId = 1, CatMunicipioId = 1, CatTipoEstablecimientoId = 1
        });
        ctx.ProductosServicios.Add(new ProductoServicio
        {
            Id = 2002, EmisorId = 41, Codigo = "P-GR-01", Nombre = "Producto Gravado",
            PrecioVenta = 100m, TipoImpuesto = TipoImpuesto.Gravado, PorcentajeIVA = 13m,
            Activo = true, CatTipoItemId = 1, CatUnidadMedidaId = 1,
            AccesoTodasSucursales = true
        });
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);
        var result = await svc.BuscarCatalogoAsync(sucursalSmartixId: 401, search: "P-GR-01", limit: 10);

        result.Should().NotBeNull();
        var item = result!.Items.Single();
        item.TipoImpuesto.Should().Be(1);
        item.PorcentajeIVA.Should().Be(13m);
        item.PrecioUnitario.Should().Be(113m); // 100 * 1.13
    }

    // =========================================================================
    // ResolverEmisorPorSucursalAsync
    // =========================================================================

    [Fact]
    public async Task ResolverEmisorPorSucursalAsync_RetornaEmisorId_CuandoSucursalExiste()
    {
        using var ctx = BuildContext();
        ctx.Sucursales.Add(new Sucursal
        {
            Id = 100, EmisorId = 42,
            Codigo = "SUC01", Nombre = "Sucursal Test",
            CatDepartamentoId = 1, CatMunicipioId = 1, CatTipoEstablecimientoId = 1
        });
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);
        var result = await svc.ResolverEmisorPorSucursalAsync(100);

        result.Should().Be(42);
    }

    [Fact]
    public async Task ResolverEmisorPorSucursalAsync_RetornaNull_CuandoSucursalNoExiste()
    {
        using var ctx = BuildContext();
        var svc = BuildService(ctx);

        var result = await svc.ResolverEmisorPorSucursalAsync(9999);

        result.Should().BeNull();
    }
}
