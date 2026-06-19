using System.Text.Json;
using FraFactu.Application.Common.Interfaces;
using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace FraFactu.Tests.Services;

/// <summary>
/// Tests para <see cref="FromSmartCarePrefillService"/>. Cubre las tres operaciones
/// clave del refactor "SmartCare deposita un prefill que la UI Smartix hidrata":
/// CrearAsync (con idempotencia), ObtenerAsync, ConsumirAsync.
/// </summary>
public class FromSmartCarePrefillServiceTests
{
    private const int EmisorId = 10;
    private const int SucursalId = 100;

    private static ApplicationDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"FromSmartCarePrefill_{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static IOptions<SmartCareSettings> Settings(string? frontend = null) =>
        Options.Create(new SmartCareSettings
        {
            ApiKey = "x",
            WebhookApiKey = "y",
            SmartixFrontendBaseUrl = frontend ?? "http://localhost:5180"
        });

    private static void SeedSucursal(ApplicationDbContext ctx)
    {
        ctx.Sucursales.Add(new Sucursal
        {
            Id = SucursalId,
            EmisorId = EmisorId,
            Codigo = "SUC01",
            Nombre = "Sucursal Test",
            CatDepartamentoId = 1,
            CatMunicipioId = 1,
            CatTipoEstablecimientoId = 1
        });
        ctx.SaveChanges();
    }

    private static FromSmartCareInvoiceRequestDto BuildValidRequest(string? correlationId = null) => new()
    {
        CorrelationId = correlationId ?? Guid.NewGuid().ToString(),
        WebhookUrl = "https://smartcare.dev/wh",
        ClinicId = "11",
        VisitId = "22",
        SucursalSmartixId = SucursalId,
        TipoDte = "01",
        FormaPagoSugerida = "01",
        Receptor = new FromSmartCareReceptorDto { Nombre = "Consumidor Final" },
        Lineas = new List<FromSmartCareInvoiceLineDto>
        {
            new() { SmartixServicioId = 200, Descripcion = "Consulta", Cantidad = 1, PrecioUnitario = 25m, TipoItem = 2 },
            new() { Descripcion = "Insumo X", Cantidad = 2, PrecioUnitario = 5m, TipoItem = 1 }
        }
    };

    // =========================================================================
    // CrearAsync — persiste prefill con JSONs y devuelve redirect correcto
    // =========================================================================

    [Fact]
    public async Task CrearAsync_PersistePrefillConJsonYResponse()
    {
        var ctx = BuildContext();
        SeedSucursal(ctx);

        var telemetry = new Mock<ITelemetryService>();
        var svc = new FromSmartCarePrefillService(
            ctx,
            NullLogger<FromSmartCarePrefillService>.Instance,
            telemetry.Object,
            Settings("https://smartix.example.com"),
            new Mock<ISmartCareWebhookService>().Object);

        var request = BuildValidRequest();

        var result = await svc.CrearAsync(request);

        result.PrefillId.Should().BeGreaterThan(0);
        result.CorrelationId.Should().Be(request.CorrelationId);
        result.RedirectUrl.Should().Be(
            $"https://smartix.example.com/emision/factura?prefillId={result.PrefillId}");
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        var stored = await ctx.FacturaPrefills.SingleAsync();
        stored.CorrelationId.Should().Be(request.CorrelationId);
        stored.EmisorId.Should().Be(EmisorId);
        stored.SucursalSmartixId.Should().Be(SucursalId);
        stored.TipoDte.Should().Be("01");
        stored.SmartCareWebhookUrl.Should().Be(request.WebhookUrl);
        stored.SmartCareClinicId.Should().Be("11");
        stored.SmartCareVisitId.Should().Be("22");
        stored.FormaPagoSugerida.Should().Be("01");
        stored.ConsumedAt.Should().BeNull();

        // JSONs deben contener los datos serializados.
        var receptorRoundtrip = JsonSerializer.Deserialize<FromSmartCareReceptorDto>(stored.ReceptorJson)!;
        receptorRoundtrip.Nombre.Should().Be("Consumidor Final");

        var lineasRoundtrip = JsonSerializer.Deserialize<List<FromSmartCareInvoiceLineDto>>(stored.LineasJson)!;
        lineasRoundtrip.Should().HaveCount(2);
        lineasRoundtrip[0].SmartixServicioId.Should().Be(200);
        lineasRoundtrip[1].Descripcion.Should().Be("Insumo X");

        telemetry.Verify(t => t.TrackEvent(
            "smartix.prefill.created",
            It.Is<IDictionary<string, string>>(d => d["resultado"] == "creado"),
            It.IsAny<IDictionary<string, double>?>()), Times.Once);
    }

    // =========================================================================
    // CrearAsync — idempotente: misma CorrelationId → mismo PrefillId
    // =========================================================================

    [Fact]
    public async Task CrearAsync_EsIdempotente_PorCorrelationId()
    {
        var ctx = BuildContext();
        SeedSucursal(ctx);

        var telemetry = new Mock<ITelemetryService>();
        var svc = new FromSmartCarePrefillService(
            ctx,
            NullLogger<FromSmartCarePrefillService>.Instance,
            telemetry.Object,
            Settings(),
            new Mock<ISmartCareWebhookService>().Object);

        var correlationId = Guid.NewGuid().ToString();
        var request = BuildValidRequest(correlationId);

        var first = await svc.CrearAsync(request);
        var second = await svc.CrearAsync(request);

        second.PrefillId.Should().Be(first.PrefillId);
        second.CorrelationId.Should().Be(correlationId);

        (await ctx.FacturaPrefills.CountAsync()).Should().Be(1);

        telemetry.Verify(t => t.TrackEvent(
            "smartix.prefill.created",
            It.Is<IDictionary<string, string>>(d => d["resultado"] == "idempotente"),
            It.IsAny<IDictionary<string, double>?>()), Times.Once);
    }

    // =========================================================================
    // CrearAsync — sucursal inexistente lanza KeyNotFoundException
    // =========================================================================

    [Fact]
    public async Task CrearAsync_SucursalNoExiste_LanzaKeyNotFoundException()
    {
        var ctx = BuildContext();
        // No seed de sucursal

        var svc = new FromSmartCarePrefillService(
            ctx,
            NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object,
            Settings(),
            new Mock<ISmartCareWebhookService>().Object);

        var request = BuildValidRequest();
        request.SucursalSmartixId = 9999;

        var act = async () => await svc.CrearAsync(request);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // =========================================================================
    // ObtenerAsync — round-trip de JSONs y devuelve null si no existe
    // =========================================================================

    [Fact]
    public async Task ObtenerAsync_RetornaPrefillConDatosDeserializados()
    {
        var ctx = BuildContext();
        SeedSucursal(ctx);

        var svc = new FromSmartCarePrefillService(
            ctx,
            NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object,
            Settings(),
            new Mock<ISmartCareWebhookService>().Object);

        var creado = await svc.CrearAsync(BuildValidRequest());

        var read = await svc.ObtenerAsync(creado.PrefillId);

        read.Should().NotBeNull();
        read!.Id.Should().Be(creado.PrefillId);
        read.CorrelationId.Should().Be(creado.CorrelationId);
        read.EmisorId.Should().Be(EmisorId); // Plan-C2 facturar-respeta-prefill
        read.SucursalSmartixId.Should().Be(SucursalId);
        read.TipoDte.Should().Be("01");
        read.Receptor.Nombre.Should().Be("Consumidor Final");
        read.Lineas.Should().HaveCount(2);
        read.Lineas[0].SmartixServicioId.Should().Be(200);
        read.ClinicId.Should().Be("11");
        read.VisitId.Should().Be("22");
        read.Consumed.Should().BeFalse();
    }

    [Fact]
    public async Task ObtenerAsync_PrefillInexistente_RetornaNull()
    {
        var ctx = BuildContext();
        var svc = new FromSmartCarePrefillService(
            ctx,
            NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object,
            Settings(),
            new Mock<ISmartCareWebhookService>().Object);

        var read = await svc.ObtenerAsync(9999);
        read.Should().BeNull();
    }

    // =========================================================================
    // ConsumirAsync — marca consumed y copia metadata a la factura
    // =========================================================================

    [Fact]
    public async Task ConsumirAsync_MarcaConsumedYCopiaMetadataAFactura()
    {
        var ctx = BuildContext();
        SeedSucursal(ctx);

        var telemetry = new Mock<ITelemetryService>();
        var svc = new FromSmartCarePrefillService(
            ctx,
            NullLogger<FromSmartCarePrefillService>.Instance,
            telemetry.Object,
            Settings(),
            new Mock<ISmartCareWebhookService>().Object);

        var request = BuildValidRequest();
        var creado = await svc.CrearAsync(request);

        // Seed factura recién emitida (simula que pasó por FacturaService.CreateAsync).
        var factura = new FacturaElectronica
        {
            Id = 555,
            EmisorId = EmisorId,
            CatTipoDocumentoId = 1,
            CodigoGeneracion = "GEN-555",
            NumeroControl = "DTE-01-0001-00000001",
            EstadoHacienda = "PROCESADO",
            FechaEmision = DateTime.UtcNow
        };
        ctx.Facturas.Add(factura);
        await ctx.SaveChangesAsync();

        await svc.ConsumirAsync(creado.PrefillId, 555);

        // Prefill marcado como consumido.
        var prefill = await ctx.FacturaPrefills.SingleAsync();
        prefill.ConsumedAt.Should().NotBeNull();
        prefill.ConsumedFacturaId.Should().Be(555);

        // Metadata copiada a la factura.
        var f = await ctx.Facturas.FindAsync(555);
        f!.SmartCareCorrelationId.Should().Be(request.CorrelationId);
        f.SmartCareWebhookUrl.Should().Be(request.WebhookUrl);
        f.SmartCareClinicId.Should().Be("11");
        f.SmartCareVisitId.Should().Be("22");

        telemetry.Verify(t => t.TrackEvent(
            "smartix.prefill.consumed",
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<IDictionary<string, double>?>()), Times.Once);
    }

    // =========================================================================
    // ConsumirAsync — prefill expirado NO copia metadata pero tampoco lanza
    // =========================================================================

    [Fact]
    public async Task ConsumirAsync_PrefillExpirado_NoCopiaMetadataPeroNoLanza()
    {
        var ctx = BuildContext();
        SeedSucursal(ctx);

        // Prefill expirado a mano (ya vencido).
        var prefill = new FacturaPrefill
        {
            Id = 1,
            CorrelationId = Guid.NewGuid().ToString(),
            EmisorId = EmisorId,
            SucursalSmartixId = SucursalId,
            TipoDte = "01",
            ReceptorJson = "{}",
            LineasJson = "[]",
            SmartCareWebhookUrl = "https://x",
            ExpiresAt = DateTime.UtcNow.AddHours(-1)
        };
        ctx.FacturaPrefills.Add(prefill);

        var factura = new FacturaElectronica
        {
            Id = 777,
            EmisorId = EmisorId,
            CatTipoDocumentoId = 1,
            CodigoGeneracion = "GEN-777",
            NumeroControl = "DTE-01-0001-00000002",
            EstadoHacienda = "PROCESADO",
            FechaEmision = DateTime.UtcNow
        };
        ctx.Facturas.Add(factura);
        await ctx.SaveChangesAsync();

        var svc = new FromSmartCarePrefillService(
            ctx,
            NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object,
            Settings(),
            new Mock<ISmartCareWebhookService>().Object);

        var act = async () => await svc.ConsumirAsync(1, 777);
        await act.Should().NotThrowAsync();

        var p = await ctx.FacturaPrefills.SingleAsync();
        p.ConsumedAt.Should().BeNull();

        var f = await ctx.Facturas.FindAsync(777);
        f!.SmartCareCorrelationId.Should().BeNull();
    }

    // =========================================================================
    // CrearAsync — normalizacion de DUI del receptor (Bug 2 UAT 2026-05-14)
    // SmartCare ya formatea con formatDui, pero esto es defensa en profundidad
    // por si otro cliente (o regresion) manda el DUI sin formato.
    // =========================================================================

    [Fact]
    public async Task CrearAsync_NormalizaDuiSinGuion_AFormatoMh()
    {
        var ctx = BuildContext();
        SeedSucursal(ctx);
        // Necesitamos el catalogo CatDocsIdentidadReceptor para que el upsert
        // del receptor encuentre el tipoDoc=13.
        ctx.CatDocsIdentidadReceptor.Add(new FraFactu.Domain.Entities.Catalogos.CatTipoDocumentoIdentificacionReceptor
        {
            Id = 1,
            Codigo = "13",
            Valor = "DUI"
        });
        await ctx.SaveChangesAsync();

        var telemetry = new Mock<ITelemetryService>();
        var svc = new FromSmartCarePrefillService(
            ctx, NullLogger<FromSmartCarePrefillService>.Instance, telemetry.Object,
            Settings(), new Mock<ISmartCareWebhookService>().Object);

        var request = BuildValidRequest();
        request.Receptor = new FromSmartCareReceptorDto
        {
            Nombre = "Paciente Prueba 3",
            TipoDocumento = "13",
            NumeroDocumento = "093462176" // sin guion — caso real UAT
        };

        var result = await svc.CrearAsync(request);

        result.PrefillId.Should().BeGreaterThan(0);

        // El receptor persistido en BD debe tener el DUI con guion.
        var receptorPersistido = await ctx.Receptores.SingleAsync();
        receptorPersistido.NumeroDocumento.Should().Be("09346217-6");

        // El receptor serializado en el prefill JSON tambien.
        var prefill = await ctx.FacturaPrefills.SingleAsync();
        var receptorRoundtrip = JsonSerializer
            .Deserialize<FromSmartCareReceptorDto>(prefill.ReceptorJson)!;
        receptorRoundtrip.NumeroDocumento.Should().Be("09346217-6");
    }

    [Fact]
    public async Task CrearAsync_DuiYaFormateado_QuedaIgual()
    {
        var ctx = BuildContext();
        SeedSucursal(ctx);
        ctx.CatDocsIdentidadReceptor.Add(new FraFactu.Domain.Entities.Catalogos.CatTipoDocumentoIdentificacionReceptor
        {
            Id = 1,
            Codigo = "13",
            Valor = "DUI"
        });
        await ctx.SaveChangesAsync();

        var svc = new FromSmartCarePrefillService(
            ctx, NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object,
            Settings(), new Mock<ISmartCareWebhookService>().Object);

        var request = BuildValidRequest();
        request.Receptor = new FromSmartCareReceptorDto
        {
            Nombre = "Test",
            TipoDocumento = "13",
            NumeroDocumento = "09346217-6"
        };

        var result = await svc.CrearAsync(request);
        result.PrefillId.Should().BeGreaterThan(0);

        var receptor = await ctx.Receptores.SingleAsync();
        receptor.NumeroDocumento.Should().Be("09346217-6");
    }

    [Fact]
    public async Task CrearAsync_DuiInvalido_LanzaValidationException()
    {
        var ctx = BuildContext();
        SeedSucursal(ctx);

        var svc = new FromSmartCarePrefillService(
            ctx, NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object,
            Settings(), new Mock<ISmartCareWebhookService>().Object);

        var request = BuildValidRequest();
        request.Receptor = new FromSmartCareReceptorDto
        {
            Nombre = "Test",
            TipoDocumento = "13",
            NumeroDocumento = "12345" // solo 5 digitos
        };

        var act = async () => await svc.CrearAsync(request);
        // ValidationException mapea a 400 con errorCode VALIDATION_ERROR en
        // GlobalExceptionMiddleware (no a 500 generico como seria con
        // InvalidOperationException). Cliente ve el problema claro.
        var thrown = await act.Should().ThrowAsync<FraFactu.Domain.Exceptions.ValidationException>();
        thrown.Which.Message.Should().Contain("no es un DUI valido");
        thrown.Which.StatusCode.Should().Be(400);
        thrown.Which.ErrorCode.Should().Be("VALIDATION_ERROR");
        thrown.Which.Errors.Should().ContainKey("receptor.numeroDocumento");
    }

    [Fact]
    public async Task CrearAsync_CorrelationIdYaConsumido_NoValidaDuiDelRequestNuevo()
    {
        // Finding 2 del review: el check de idempotencia debe ir antes que
        // cualquier validacion del body, asi un retry con misma correlation
        // siempre devuelve el prefill original sin re-validar.
        var ctx = BuildContext();
        SeedSucursal(ctx);

        var svc = new FromSmartCarePrefillService(
            ctx, NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object,
            Settings(), new Mock<ISmartCareWebhookService>().Object);

        var correlationId = Guid.NewGuid().ToString();
        var first = BuildValidRequest(correlationId);

        // Primer request con receptor sin documento (valido para CF anonimo)
        var firstResult = await svc.CrearAsync(first);
        firstResult.PrefillId.Should().BeGreaterThan(0);

        // Segundo request con misma correlation pero DUI invalido — no debe
        // lanzar, debe devolver el prefill original (idempotencia gana).
        var second = BuildValidRequest(correlationId);
        second.Receptor = new FromSmartCareReceptorDto
        {
            Nombre = "Test",
            TipoDocumento = "13",
            NumeroDocumento = "INVALIDO"
        };

        var secondResult = await svc.CrearAsync(second);
        secondResult.PrefillId.Should().Be(firstResult.PrefillId);

        // Solo se persistio un prefill.
        (await ctx.FacturaPrefills.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CrearAsync_NitNoSeTocaAunqueElValidador13EsteCorriendo()
    {
        var ctx = BuildContext();
        SeedSucursal(ctx);
        ctx.CatDocsIdentidadReceptor.Add(new FraFactu.Domain.Entities.Catalogos.CatTipoDocumentoIdentificacionReceptor
        {
            Id = 36,
            Codigo = "36",
            Valor = "NIT"
        });
        await ctx.SaveChangesAsync();

        var svc = new FromSmartCarePrefillService(
            ctx, NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object,
            Settings(), new Mock<ISmartCareWebhookService>().Object);

        // CCF requiere mas campos; los completamos minimos para que pase el flujo.
        var request = BuildValidRequest();
        request.TipoDte = "03";
        request.Receptor = new FromSmartCareReceptorDto
        {
            Nombre = "Empresa Test",
            TipoDocumento = "36",
            NumeroDocumento = "06140101230012",
            Nrc = "12345",
            CodigoActividad = "86202",
            DepartamentoId = 1,
            MunicipioId = 1,
            Direccion = "Col. Escalon"
        };

        var result = await svc.CrearAsync(request);
        result.PrefillId.Should().BeGreaterThan(0);

        var receptor = await ctx.Receptores.SingleAsync();
        receptor.NumeroDocumento.Should().Be("06140101230012"); // sin tocar
    }

    // =========================================================================
    // Plan C1: PrecioIncluyeIva por linea viaja en el JSON del prefill
    // =========================================================================

    [Fact]
    public async Task CrearAsync_PrecioIncluyeIva_porLinea_se_persiste_en_JSON_y_se_devuelve_en_ObtenerAsync()
    {
        var ctx = BuildContext();
        SeedSucursal(ctx);

        var telemetry = new Mock<ITelemetryService>();
        var svc = new FromSmartCarePrefillService(
            ctx,
            NullLogger<FromSmartCarePrefillService>.Instance,
            telemetry.Object,
            Settings(),
            Mock.Of<ISmartCareWebhookService>());

        var request = BuildValidRequest();
        request.Lineas = new List<FromSmartCareInvoiceLineDto>
        {
            new() { Descripcion = "Insumo con IVA",  Cantidad = 1, PrecioUnitario = 22.60m, PrecioIncluyeIva = true,  TipoItem = 1 },
            new() { Descripcion = "Insumo sin IVA",  Cantidad = 1, PrecioUnitario = 20.00m, PrecioIncluyeIva = false, TipoItem = 1 },
            new() { Descripcion = "Insumo legacy",   Cantidad = 1, PrecioUnitario = 10.00m, PrecioIncluyeIva = null,  TipoItem = 1 },
        };

        var response = await svc.CrearAsync(request);

        var read = await svc.ObtenerAsync(response.PrefillId);
        read.Should().NotBeNull();
        read!.Lineas.Should().HaveCount(3);
        read.Lineas[0].PrecioIncluyeIva.Should().BeTrue();
        read.Lineas[1].PrecioIncluyeIva.Should().BeFalse();
        read.Lineas[2].PrecioIncluyeIva.Should().BeNull();
    }

    // =========================================================================
    // Linkeo catálogo (2026-05-29): ObtenerAsync hidrata el Codigo desde el
    // catálogo cuando la línea tiene SmartixServicioId. Si no existe el servicio
    // (o pertenece a otro emisor), Codigo queda null y el FE cae al item ad-hoc.
    // =========================================================================

    [Fact]
    public async Task ObtenerAsync_HidrataCodigo_desde_catalogo_cuando_SmartixServicioId_existe()
    {
        var ctx = BuildContext();
        SeedSucursal(ctx);
        // Producto en el emisor del prefill.
        ctx.ProductosServicios.Add(new ProductoServicio
        {
            Id = 200, EmisorId = EmisorId, Codigo = "SVC-CONS-01", Nombre = "Consulta",
            PrecioVenta = 10m, Activo = true, AccesoTodasSucursales = true,
            CatTipoItemId = 2,
        });
        // Producto en OTRO emisor — NO debe matchear (filtro EmisorId).
        ctx.ProductosServicios.Add(new ProductoServicio
        {
            Id = 201, EmisorId = 999, Codigo = "OTRO-EMISOR", Nombre = "Servicio ajeno",
            PrecioVenta = 5m, Activo = true, AccesoTodasSucursales = true,
            CatTipoItemId = 2,
        });
        ctx.SaveChanges();

        var telemetry = new Mock<ITelemetryService>();
        var svc = new FromSmartCarePrefillService(
            ctx,
            NullLogger<FromSmartCarePrefillService>.Instance,
            telemetry.Object,
            Settings(),
            Mock.Of<ISmartCareWebhookService>());

        var request = BuildValidRequest();
        request.Lineas = new List<FromSmartCareInvoiceLineDto>
        {
            new() { SmartixServicioId = 200, Descripcion = "Consulta", Cantidad = 1, PrecioUnitario = 25m, TipoItem = 2 },
            new() { SmartixServicioId = 201, Descripcion = "Servicio ajeno", Cantidad = 1, PrecioUnitario = 5m, TipoItem = 2 },
            new() { SmartixServicioId = 9999, Descripcion = "ID inexistente", Cantidad = 1, PrecioUnitario = 3m, TipoItem = 2 },
            new() { Descripcion = "Línea ad-hoc sin SmartixServicioId", Cantidad = 1, PrecioUnitario = 2m, TipoItem = 1 },
        };

        var response = await svc.CrearAsync(request);
        var read = await svc.ObtenerAsync(response.PrefillId);

        read.Should().NotBeNull();
        read!.Lineas.Should().HaveCount(4);
        // Línea con SmartixServicioId que existe en el emisor del prefill → Codigo hidratado.
        read.Lineas[0].Codigo.Should().Be("SVC-CONS-01");
        // Línea con SmartixServicioId que existe pero en OTRO emisor → Codigo null (no matchea).
        read.Lineas[1].Codigo.Should().BeNull();
        // Línea con SmartixServicioId inexistente → Codigo null.
        read.Lineas[2].Codigo.Should().BeNull();
        // Línea ad-hoc sin SmartixServicioId → Codigo null.
        read.Lineas[3].Codigo.Should().BeNull();
    }

    // =========================================================================
    // Plan B Hub-as-Emisor — Fase 2 B.1: payload fiscal + sync + auto-create
    // Sucursal por HubSucursalId + snapshot en FacturaPrefill.
    // =========================================================================

    private const int HubId = 444;
    private const int HubSucursalId = 555;

    private static void SeedCatalogosFiscales(ApplicationDbContext ctx)
    {
        ctx.CatDepartamentos.Add(new FraFactu.Domain.Entities.Catalogos.CatDepartamento
        { Id = 6, Codigo = "06", Valor = "San Salvador" });
        ctx.CatMunicipios.Add(new FraFactu.Domain.Entities.Catalogos.CatMunicipio
        { Id = 23, Codigo = "23", Valor = "San Salvador (capital)", CodigoDepartamento = "06" });
        ctx.CatTiposEstablecimiento.Add(new FraFactu.Domain.Entities.Catalogos.CatTipoEstablecimiento
        { Id = 1, Codigo = "01", Valor = "Casa Matriz" });
        ctx.CatTiposEstablecimiento.Add(new FraFactu.Domain.Entities.Catalogos.CatTipoEstablecimiento
        { Id = 2, Codigo = "02", Valor = "Sucursal" });
        ctx.SaveChanges();
    }

    private static void SeedEmisor(ApplicationDbContext ctx)
    {
        ctx.Emisores.Add(new Emisor
        {
            Id = EmisorId,
            HubId = HubId,
            Nit = "06140101231234",
            Nrc = "1111111",
            NombreRazonSocial = "Razon Vieja",
            CodigoActividad = "10001",
            DescripcionActividad = "Empleados",
            CatDepartamentoId = 6,
            CatMunicipioId = 23,
            Direccion = "Direccion vieja",
            CatTipoEstablecimientoId = 1,
            CorreoElectronico = "viejo@test.com",
            Telefono = "11112222",
            CatAmbienteDestinoId = 1,
            MhUsuario = "mh-user-secret",  // Smartix-only, NO debe pisarse
            SmtpHost = "smtp.gmail.com",   // Smartix-only
            LogoUrl = "https://logo.png"   // Smartix-only
        });
        ctx.SaveChanges();
    }

    private static EmisorFiscalDto BuildEmisorPayload() => new()
    {
        HubId = HubId,
        Nit = "06141502331567",
        Nrc = "2222222",
        NombreRazonSocial = "Razon NUEVA SA de CV",
        NombreComercial = "Comercial NUEVO",
        CodActividadEconomica = "10001",
        DescActividadEconomica = "Empleados",
        CodTipoEstablecimiento = "01",
        CodDepartamento = "06",
        CodMunicipio = "23",
        DireccionComplemento = "Direccion NUEVA",
        TelefonoFiscal = "99998888",
        CorreoFiscal = "nuevo@test.com"
    };

    private static SucursalFiscalDto BuildSucursalPayload(int hubSucursalId = HubSucursalId) => new()
    {
        HubSucursalId = hubSucursalId,
        Nombre = "Casa Matriz Hub",
        CodigoEstablecimientoMH = "M001P001",
        CodTipoEstablecimiento = "01",
        CodDepartamento = "06",
        CodMunicipio = "23",
        DireccionComplemento = "Sucursal addr",
        TelefonoSucursal = "77770000",
        CorreoSucursal = "suc@test.com",
        Contingencia = new ContingenciaDto
        {
            NombreResponsable = "Responsable Test",
            TipoDocResponsable = "36",
            NumeroDocResponsable = "1317-300685-101-1"
        }
    };

    [Fact]
    public async Task CrearAsync_ConPayload_SucursalExistente_HaceLookupYSincroniza()
    {
        var ctx = BuildContext();
        SeedCatalogosFiscales(ctx);
        SeedEmisor(ctx);
        // Sucursal preexistente con HubSucursalId match — debe encontrarse y sincronizarse.
        ctx.Sucursales.Add(new Sucursal
        {
            Id = SucursalId,
            EmisorId = EmisorId,
            HubSucursalId = HubSucursalId,
            Codigo = "OLD01",
            Nombre = "Nombre Viejo",
            CodigoEstablecimiento = "OLD",
            CatDepartamentoId = 6,
            CatMunicipioId = 23,
            CatTipoEstablecimientoId = 1
        });
        ctx.SaveChanges();

        var svc = new FromSmartCarePrefillService(ctx, NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object, Settings(), new Mock<ISmartCareWebhookService>().Object);

        var request = BuildValidRequest();
        request.SucursalSmartixId = 99; // ignorado cuando viene payload
        request.EmisorFiscal = BuildEmisorPayload();
        request.SucursalFiscal = BuildSucursalPayload();

        var response = await svc.CrearAsync(request);

        // No se debio crear una sucursal nueva — sigue habiendo solo 1.
        (await ctx.Sucursales.CountAsync()).Should().Be(1);

        // Y se sincronizo con datos del payload.
        var sucReloaded = await ctx.Sucursales.FirstAsync();
        sucReloaded.Nombre.Should().Be("Casa Matriz Hub");
        sucReloaded.CodigoEstablecimiento.Should().Be("M001P001");
        sucReloaded.ContingenciaTipoDocResponsable.Should().Be("36");

        // El prefill apunta a la sucursal real, no al SucursalSmartixId stale.
        var prefill = await ctx.FacturaPrefills.SingleAsync();
        prefill.SucursalSmartixId.Should().Be(SucursalId);
    }

    [Fact]
    public async Task CrearAsync_ConPayload_SucursalInexistente_AutoCreaPorHubSucursalId()
    {
        var ctx = BuildContext();
        SeedCatalogosFiscales(ctx);
        SeedEmisor(ctx);
        // NO seed de Sucursal — debe auto-crearse desde el payload.

        var svc = new FromSmartCarePrefillService(ctx, NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object, Settings(), new Mock<ISmartCareWebhookService>().Object);

        var request = BuildValidRequest();
        request.EmisorFiscal = BuildEmisorPayload();
        request.SucursalFiscal = BuildSucursalPayload();

        await svc.CrearAsync(request);

        var creada = await ctx.Sucursales.SingleAsync();
        creada.HubSucursalId.Should().Be(HubSucursalId);
        creada.EmisorId.Should().Be(EmisorId);
        creada.Nombre.Should().Be("Casa Matriz Hub");
        creada.CodigoEstablecimiento.Should().Be("M001P001");
        creada.CatDepartamentoId.Should().Be(6);
        creada.CatMunicipioId.Should().Be(23);
        creada.CatTipoEstablecimientoId.Should().Be(1);
        creada.ContingenciaNombreResponsable.Should().Be("Responsable Test");
    }

    [Fact]
    public async Task CrearAsync_ConPayload_SincronizaEmisorPreservandoCamposSmartixOnly()
    {
        var ctx = BuildContext();
        SeedCatalogosFiscales(ctx);
        SeedEmisor(ctx);
        ctx.Sucursales.Add(new Sucursal
        {
            Id = SucursalId, EmisorId = EmisorId, HubSucursalId = HubSucursalId,
            Codigo = "M001", Nombre = "X", CodigoEstablecimiento = "M001",
            CatDepartamentoId = 6, CatMunicipioId = 23, CatTipoEstablecimientoId = 1
        });
        ctx.SaveChanges();

        var svc = new FromSmartCarePrefillService(ctx, NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object, Settings(), new Mock<ISmartCareWebhookService>().Object);

        var request = BuildValidRequest();
        request.EmisorFiscal = BuildEmisorPayload();
        request.SucursalFiscal = BuildSucursalPayload();

        await svc.CrearAsync(request);

        var emisor = await ctx.Emisores.SingleAsync();
        // Campos fiscales identitarios sincronizados desde el payload.
        emisor.Nit.Should().Be("06141502331567");
        emisor.Nrc.Should().Be("2222222");
        emisor.NombreRazonSocial.Should().Be("Razon NUEVA SA de CV");
        emisor.NombreComercial.Should().Be("Comercial NUEVO");
        emisor.Direccion.Should().Be("Direccion NUEVA");
        emisor.Telefono.Should().Be("99998888");
        emisor.CorreoElectronico.Should().Be("nuevo@test.com");

        // Campos Smartix-only NO se tocaron.
        emisor.MhUsuario.Should().Be("mh-user-secret");
        emisor.SmtpHost.Should().Be("smtp.gmail.com");
        emisor.LogoUrl.Should().Be("https://logo.png");
    }

    [Fact]
    public async Task CrearAsync_ConPayload_PersisteSnapshotFiscalJsonEnPrefill()
    {
        var ctx = BuildContext();
        SeedCatalogosFiscales(ctx);
        SeedEmisor(ctx);
        ctx.Sucursales.Add(new Sucursal
        {
            Id = SucursalId, EmisorId = EmisorId, HubSucursalId = HubSucursalId,
            Codigo = "M001", Nombre = "X", CodigoEstablecimiento = "M001",
            CatDepartamentoId = 6, CatMunicipioId = 23, CatTipoEstablecimientoId = 1
        });
        ctx.SaveChanges();

        var svc = new FromSmartCarePrefillService(ctx, NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object, Settings(), new Mock<ISmartCareWebhookService>().Object);

        var request = BuildValidRequest();
        request.EmisorFiscal = BuildEmisorPayload();
        request.SucursalFiscal = BuildSucursalPayload();

        await svc.CrearAsync(request);

        var prefill = await ctx.FacturaPrefills.SingleAsync();
        prefill.SnapshotFiscalJson.Should().NotBeNullOrEmpty();
        using var doc = JsonDocument.Parse(prefill.SnapshotFiscalJson!);
        doc.RootElement.GetProperty("emisor").GetProperty("nit").GetString().Should().Be("06141502331567");
        doc.RootElement.GetProperty("sucursal").GetProperty("codigoEstablecimientoMH").GetString().Should().Be("M001P001");
    }

    [Fact]
    public async Task CrearAsync_SinPayloadFiscal_FlujoLegacyOk_NoSnapshot()
    {
        var ctx = BuildContext();
        SeedSucursal(ctx); // flujo legacy puro

        var svc = new FromSmartCarePrefillService(ctx, NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object, Settings(), new Mock<ISmartCareWebhookService>().Object);

        var request = BuildValidRequest();
        // EmisorFiscal = null, SucursalFiscal = null → fallback al lookup por SucursalSmartixId

        await svc.CrearAsync(request);

        var prefill = await ctx.FacturaPrefills.SingleAsync();
        prefill.SnapshotFiscalJson.Should().BeNull();
        prefill.SucursalSmartixId.Should().Be(SucursalId);
    }

    [Fact]
    public async Task CrearAsync_ConPayload_PeroSinEmisorEnBD_LanzaValidationException()
    {
        var ctx = BuildContext();
        SeedCatalogosFiscales(ctx);
        // NO seed de Emisor — el payload tiene HubId=444 pero ningun Emisor lo referencia.

        var svc = new FromSmartCarePrefillService(ctx, NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object, Settings(), new Mock<ISmartCareWebhookService>().Object);

        var request = BuildValidRequest();
        request.EmisorFiscal = BuildEmisorPayload();
        request.SucursalFiscal = BuildSucursalPayload();

        var act = async () => await svc.CrearAsync(request);
        var ex = await act.Should().ThrowAsync<FraFactu.Domain.Exceptions.ValidationException>();
        ex.Which.Message.Should().Contain("Hub");
        ex.Which.Message.Should().Contain("Emisor");
    }

    [Fact]
    public async Task CrearAsync_ConSucursalFiscalPeroSinEmisorFiscal_LanzaValidationException()
    {
        // Caso descubierto en review: si SmartCare manda sucursalFiscal sin emisorFiscal
        // y la sucursal no existe aun, no podemos identificar el Emisor padre. Debe
        // devolver 400 con field claro, no 500.
        var ctx = BuildContext();
        SeedCatalogosFiscales(ctx);

        var svc = new FromSmartCarePrefillService(ctx, NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object, Settings(), new Mock<ISmartCareWebhookService>().Object);

        var request = BuildValidRequest();
        request.EmisorFiscal = null;
        request.SucursalFiscal = BuildSucursalPayload();

        var act = async () => await svc.CrearAsync(request);
        var ex = await act.Should().ThrowAsync<FraFactu.Domain.Exceptions.ValidationException>();
        ex.Which.Message.Should().Contain("emisorFiscal");
    }

    [Fact]
    public async Task CrearAsync_ConCodigoMHInvalido_LanzaValidationException400()
    {
        // Codigo de departamento que no existe en catalogo → 400, no 500.
        var ctx = BuildContext();
        SeedCatalogosFiscales(ctx);
        SeedEmisor(ctx);

        var svc = new FromSmartCarePrefillService(ctx, NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object, Settings(), new Mock<ISmartCareWebhookService>().Object);

        var request = BuildValidRequest();
        var emisor = BuildEmisorPayload();
        emisor.CodDepartamento = "99"; // inexistente
        request.EmisorFiscal = emisor;
        request.SucursalFiscal = BuildSucursalPayload();

        var act = async () => await svc.CrearAsync(request);
        var ex = await act.Should().ThrowAsync<FraFactu.Domain.Exceptions.ValidationException>();
        ex.Which.Message.Should().Contain("departamento");
    }

    [Fact]
    public async Task CrearAsync_AutoCreaSucursal_UsaCodigoHubAsSentinel()
    {
        // Verifica que el Codigo interno Smartix de la sucursal auto-creada es
        // unique-by-HubSucursalId, no copia ciega del CodigoEstablecimientoMH.
        var ctx = BuildContext();
        SeedCatalogosFiscales(ctx);
        SeedEmisor(ctx);

        var svc = new FromSmartCarePrefillService(ctx, NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object, Settings(), new Mock<ISmartCareWebhookService>().Object);

        var request = BuildValidRequest();
        request.EmisorFiscal = BuildEmisorPayload();
        request.SucursalFiscal = BuildSucursalPayload();

        await svc.CrearAsync(request);

        var creada = await ctx.Sucursales.SingleAsync();
        creada.Codigo.Should().Be($"HUB-{HubSucursalId}");
        creada.CodigoEstablecimiento.Should().Be("M001P001"); // MH code intacto en su propio campo
    }

    [Fact]
    public async Task ConsumirAsync_ConPayloadFiscal_CopiaSnapshotJsonALaFactura()
    {
        // Plan B Hub-as-Emisor — Fase 2 B.2.
        // Verifica que al consumir un prefill con SnapshotFiscalJson, ese mismo
        // JSON se copia al row de FacturaElectronica para auditoria histórica.
        var ctx = BuildContext();
        SeedCatalogosFiscales(ctx);
        SeedEmisor(ctx);
        ctx.Sucursales.Add(new Sucursal
        {
            Id = SucursalId, EmisorId = EmisorId, HubSucursalId = HubSucursalId,
            Codigo = $"HUB-{HubSucursalId}", Nombre = "X", CodigoEstablecimiento = "M001",
            CatDepartamentoId = 6, CatMunicipioId = 23, CatTipoEstablecimientoId = 1
        });
        ctx.SaveChanges();

        var svc = new FromSmartCarePrefillService(ctx, NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object, Settings(), new Mock<ISmartCareWebhookService>().Object);

        var request = BuildValidRequest();
        request.EmisorFiscal = BuildEmisorPayload();
        request.SucursalFiscal = BuildSucursalPayload();
        var creado = await svc.CrearAsync(request);

        var factura = new FacturaElectronica
        {
            Id = 777,
            EmisorId = EmisorId,
            CatTipoDocumentoId = 1,
            CodigoGeneracion = "GEN-777",
            NumeroControl = "DTE-01-HUB-555-00000777",
            EstadoHacienda = "PROCESADO",
            FechaEmision = DateTime.UtcNow
        };
        ctx.Facturas.Add(factura);
        await ctx.SaveChangesAsync();

        await svc.ConsumirAsync(creado.PrefillId, 777);

        var consumida = await ctx.Facturas.FindAsync(777);
        consumida!.SnapshotFiscalJson.Should().NotBeNullOrEmpty();
        using var doc = JsonDocument.Parse(consumida.SnapshotFiscalJson!);
        doc.RootElement.GetProperty("emisor").GetProperty("nit").GetString().Should().Be("06141502331567");
        doc.RootElement.GetProperty("sucursal").GetProperty("hubSucursalId").GetInt32().Should().Be(HubSucursalId);
    }

    [Fact]
    public async Task ConsumirAsync_SinPayloadFiscal_NoSeteaSnapshotEnFactura()
    {
        // Flujo legacy: prefill sin payload fiscal → factura queda con SnapshotFiscalJson=null.
        var ctx = BuildContext();
        SeedSucursal(ctx);

        var svc = new FromSmartCarePrefillService(ctx, NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object, Settings(), new Mock<ISmartCareWebhookService>().Object);

        var creado = await svc.CrearAsync(BuildValidRequest());
        var factura = new FacturaElectronica
        {
            Id = 888,
            EmisorId = EmisorId,
            CatTipoDocumentoId = 1,
            CodigoGeneracion = "GEN-888",
            NumeroControl = "DTE-01-LEG-00000888",
            EstadoHacienda = "PROCESADO",
            FechaEmision = DateTime.UtcNow
        };
        ctx.Facturas.Add(factura);
        await ctx.SaveChangesAsync();

        await svc.ConsumirAsync(creado.PrefillId, 888);

        var consumida = await ctx.Facturas.FindAsync(888);
        consumida!.SnapshotFiscalJson.Should().BeNull();
        // Metadata SmartCare normal sigue funcionando.
        consumida.SmartCareCorrelationId.Should().NotBeNull();
    }

    [Fact]
    public async Task CrearAsync_SyncSucursal_SinTelefonoNiCorreoEnPayload_ConservaCache()
    {
        // Si payload omite TelefonoSucursal/CorreoSucursal, no debe pisar la data
        // que ya tenia el cache local (alineado con SyncEmisor).
        var ctx = BuildContext();
        SeedCatalogosFiscales(ctx);
        SeedEmisor(ctx);
        ctx.Sucursales.Add(new Sucursal
        {
            Id = SucursalId, EmisorId = EmisorId, HubSucursalId = HubSucursalId,
            Codigo = "OLD", Nombre = "Vieja", CodigoEstablecimiento = "M001",
            CatDepartamentoId = 6, CatMunicipioId = 23, CatTipoEstablecimientoId = 1,
            Telefono = "1111-1111",
            CorreoElectronico = "viejo@suc.com"
        });
        ctx.SaveChanges();

        var svc = new FromSmartCarePrefillService(ctx, NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object, Settings(), new Mock<ISmartCareWebhookService>().Object);

        var request = BuildValidRequest();
        request.EmisorFiscal = BuildEmisorPayload();
        var suc = BuildSucursalPayload();
        suc.TelefonoSucursal = null;
        suc.CorreoSucursal = null;
        suc.Contingencia = null;
        request.SucursalFiscal = suc;

        await svc.CrearAsync(request);

        var sincronizada = await ctx.Sucursales.SingleAsync();
        sincronizada.Telefono.Should().Be("1111-1111"); // preservado
        sincronizada.CorreoElectronico.Should().Be("viejo@suc.com"); // preservado
        sincronizada.Nombre.Should().Be("Casa Matriz Hub"); // si se actualizo (campo no opcional)
    }

    [Fact]
    public async Task CrearAsync_ConEmisorFiscalSinCodTipoEstablecimiento_NoFallaYLoDejaNull()
    {
        // Regresion bug 2026-05-18: SmartHub PR #14 hizo CodTipoEstablecimiento opcional
        // en el Hub (el DTE lo toma de la Sucursal). SmartCare lo propaga como null.
        // El sync del Emisor debe tolerar null sin fallar (la entidad Emisor.CatTipo
        // EstablecimientoId ya es nullable). Antes del fix lanzaba ModelState
        // "The CodTipoEstablecimiento field is required" porque el DTO era string
        // no-nullable con default empty.
        var ctx = BuildContext();
        SeedCatalogosFiscales(ctx);
        SeedEmisor(ctx);
        ctx.Sucursales.Add(new Sucursal
        {
            Id = SucursalId, EmisorId = EmisorId, HubSucursalId = HubSucursalId,
            Codigo = "M001", Nombre = "X", CodigoEstablecimiento = "M001",
            CatDepartamentoId = 6, CatMunicipioId = 23, CatTipoEstablecimientoId = 1
        });
        ctx.SaveChanges();

        var svc = new FromSmartCarePrefillService(ctx, NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object, Settings(), new Mock<ISmartCareWebhookService>().Object);

        var request = BuildValidRequest();
        var emisor = BuildEmisorPayload();
        emisor.CodTipoEstablecimiento = null; // Hub sin este campo
        request.EmisorFiscal = emisor;
        request.SucursalFiscal = BuildSucursalPayload();

        await svc.CrearAsync(request);

        var emisorReloaded = await ctx.Emisores.SingleAsync();
        emisorReloaded.CatTipoEstablecimientoId.Should().BeNull();
        // El resto si se sincronizo (no hubo abort).
        emisorReloaded.Nit.Should().Be("06141502331567");
    }

    [Fact]
    public async Task CrearAsync_ConSucursalFiscalSinCodTipoEstablecimiento_LanzaValidationException()
    {
        // La Sucursal SI requiere codTipoEstablecimiento (lo usa el DTE), distinto
        // del Emisor. Si SmartCare manda null/empty, devolvemos 400 con field claro.
        var ctx = BuildContext();
        SeedCatalogosFiscales(ctx);
        SeedEmisor(ctx);

        var svc = new FromSmartCarePrefillService(ctx, NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object, Settings(), new Mock<ISmartCareWebhookService>().Object);

        var request = BuildValidRequest();
        request.EmisorFiscal = BuildEmisorPayload();
        var suc = BuildSucursalPayload();
        suc.CodTipoEstablecimiento = string.Empty;
        request.SucursalFiscal = suc;

        var act = async () => await svc.CrearAsync(request);
        var ex = await act.Should().ThrowAsync<FraFactu.Domain.Exceptions.ValidationException>();
        ex.Which.Message.Should().Contain("Sucursal");
        ex.Which.Message.Should().Contain("codTipoEstablecimiento");
    }

    // =========================================================================
    // Task 2 TDD — CatDistritoId persiste en UpsertReceptorAsync (spec 5.3)
    // =========================================================================

    [Fact]
    public async Task CrearAsync_CCF_persiste_CatDistritoId_en_receptor_nuevo()
    {
        // Arrange
        var ctx = BuildContext();
        SeedSucursal(ctx);
        ctx.CatDocsIdentidadReceptor.Add(new FraFactu.Domain.Entities.Catalogos.CatTipoDocumentoIdentificacionReceptor
        {
            Id = 36,
            Codigo = "36",
            Valor = "NIT"
        });
        await ctx.SaveChangesAsync();

        var svc = new FromSmartCarePrefillService(
            ctx, NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object,
            Settings(), new Mock<ISmartCareWebhookService>().Object);

        var request = BuildValidRequest();
        request.TipoDte = "03";
        request.Receptor = new FromSmartCareReceptorDto
        {
            Nombre = "Empresa Distrito Test",
            TipoDocumento = "36",
            NumeroDocumento = "06140101230099",
            Nrc = "12345",
            CodigoActividad = "86202",
            DepartamentoId = 7,
            MunicipioId = 24,
            Direccion = "x",
            DistritoId = 111
        };

        // Act
        var result = await svc.CrearAsync(request);
        result.PrefillId.Should().BeGreaterThan(0);

        // Assert: el receptor nuevo fue persistido con CatDistritoId=111
        var receptor = await ctx.Receptores.SingleAsync();
        receptor.CatDistritoId.Should().Be(111);
    }

    [Fact]
    public async Task CrearAsync_CCF_actualiza_CatDistritoId_en_receptor_existente()
    {
        // Arrange: receptor legado con CatDistritoId null
        var ctx = BuildContext();
        SeedSucursal(ctx);
        ctx.CatDocsIdentidadReceptor.Add(new FraFactu.Domain.Entities.Catalogos.CatTipoDocumentoIdentificacionReceptor
        {
            Id = 36,
            Codigo = "36",
            Valor = "NIT"
        });
        ctx.Receptores.Add(new Receptor
        {
            Id = 900,
            EmisorId = EmisorId,
            CatTipoDocumentoIdentificacionReceptorId = 36,
            NumeroDocumento = "06140101230099",
            NombreRazonSocial = "Empresa Vieja",
            Nrc = "99999",
            CodigoActividad = "86202",
            CatDepartamentoId = 1,
            CatMunicipioId = 1,
            Direccion = "Direccion vieja",
            CatDistritoId = null,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();

        var svc = new FromSmartCarePrefillService(
            ctx, NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object,
            Settings(), new Mock<ISmartCareWebhookService>().Object);

        var request = BuildValidRequest();
        request.TipoDte = "03";
        request.Receptor = new FromSmartCareReceptorDto
        {
            Nombre = "Empresa Actualizada",
            TipoDocumento = "36",
            NumeroDocumento = "06140101230099", // mismo doc → update
            Nrc = "12345",
            CodigoActividad = "86202",
            DepartamentoId = 7,
            MunicipioId = 24,
            Direccion = "x",
            DistritoId = 111
        };

        // Act
        var result = await svc.CrearAsync(request);
        result.PrefillId.Should().BeGreaterThan(0);

        // Assert: el receptor existente ahora tiene CatDistritoId=111
        var receptor = await ctx.Receptores.SingleAsync();
        receptor.CatDistritoId.Should().Be(111);
    }

    [Fact]
    public async Task CrearAsync_CCF_sin_DistritoId_no_pisa_existente_con_null()
    {
        // Arrange: receptor ya tiene CatDistritoId=111 (backfill previo)
        var ctx = BuildContext();
        SeedSucursal(ctx);
        ctx.CatDocsIdentidadReceptor.Add(new FraFactu.Domain.Entities.Catalogos.CatTipoDocumentoIdentificacionReceptor
        {
            Id = 36,
            Codigo = "36",
            Valor = "NIT"
        });
        ctx.Receptores.Add(new Receptor
        {
            Id = 901,
            EmisorId = EmisorId,
            CatTipoDocumentoIdentificacionReceptorId = 36,
            NumeroDocumento = "06140101230099",
            NombreRazonSocial = "Empresa Backfilled",
            Nrc = "99999",
            CodigoActividad = "86202",
            CatDepartamentoId = 1,
            CatMunicipioId = 1,
            Direccion = "Direccion vieja",
            CatDistritoId = 111, // ya backfilled
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();

        var svc = new FromSmartCarePrefillService(
            ctx, NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object,
            Settings(), new Mock<ISmartCareWebhookService>().Object);

        // Cliente SC viejo que aún no manda DistritoId (deploy progresivo)
        var request = BuildValidRequest();
        request.TipoDte = "03";
        request.Receptor = new FromSmartCareReceptorDto
        {
            Nombre = "Empresa Backfilled",
            TipoDocumento = "36",
            NumeroDocumento = "06140101230099", // mismo doc → update
            Nrc = "12345",
            CodigoActividad = "86202",
            DepartamentoId = 7,
            MunicipioId = 24,
            Direccion = "x",
            DistritoId = null // cliente viejo no manda distrito
        };

        // Act
        var result = await svc.CrearAsync(request);
        result.PrefillId.Should().BeGreaterThan(0);

        // Assert: el backfill NO fue pisado — sigue siendo 111
        var receptor = await ctx.Receptores.SingleAsync();
        receptor.CatDistritoId.Should().Be(111);
    }

    // =========================================================================
    // Tetrada territorial todos-o-ninguno en UpsertReceptorAsync (2026-06-10)
    // MH rechaza [096] cuando receptor.direccion viaja parcial. SC-BE valida
    // tetrada completa en patientsController, pero Smartix-BE tiene defensa
    // simétrica: solo actualiza los 4 si vienen completos, ignora con log si
    // llegan parciales, preserva backfill si vienen vacíos.
    // =========================================================================

    [Fact]
    public async Task Upsert_receptor_existente_con_tetrada_completa_actualiza_los_4()
    {
        var ctx = BuildContext();
        SeedSucursal(ctx);
        ctx.CatDocsIdentidadReceptor.Add(new FraFactu.Domain.Entities.Catalogos.CatTipoDocumentoIdentificacionReceptor
        {
            Id = 36, Codigo = "36", Valor = "NIT"
        });
        ctx.Receptores.Add(new Receptor
        {
            Id = 950, EmisorId = EmisorId, CatTipoDocumentoIdentificacionReceptorId = 36,
            NumeroDocumento = "06140101230099", NombreRazonSocial = "Empresa Vieja",
            Nrc = "00000", CodigoActividad = "86202",
            CatDepartamentoId = 1, CatMunicipioId = 2, CatDistritoId = 3,
            Direccion = "Dir vieja", Activo = true, FechaCreacion = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();

        var svc = new FromSmartCarePrefillService(
            ctx, NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object,
            Settings(), new Mock<ISmartCareWebhookService>().Object);

        var request = BuildValidRequest();
        request.TipoDte = "03";
        request.Receptor = new FromSmartCareReceptorDto
        {
            Nombre = "Empresa Actualizada", TipoDocumento = "36",
            NumeroDocumento = "06140101230099", Nrc = "12345", CodigoActividad = "86202",
            DepartamentoId = 7, MunicipioId = 24, DistritoId = 111,
            Direccion = "Dir nueva completa"
        };

        var result = await svc.CrearAsync(request);
        result.PrefillId.Should().BeGreaterThan(0);

        var receptor = await ctx.Receptores.SingleAsync();
        receptor.CatDepartamentoId.Should().Be(7);
        receptor.CatMunicipioId.Should().Be(24);
        receptor.CatDistritoId.Should().Be(111);
        receptor.Direccion.Should().Be("Dir nueva completa");
    }

    [Fact]
    public async Task Upsert_receptor_existente_con_tetrada_vacia_preserva_los_4_existentes()
    {
        var ctx = BuildContext();
        SeedSucursal(ctx);
        ctx.CatDocsIdentidadReceptor.Add(new FraFactu.Domain.Entities.Catalogos.CatTipoDocumentoIdentificacionReceptor
        {
            Id = 13, Codigo = "13", Valor = "DUI"
        });
        ctx.Receptores.Add(new Receptor
        {
            Id = 951, EmisorId = EmisorId, CatTipoDocumentoIdentificacionReceptorId = 13,
            NumeroDocumento = "01234567-8", NombreRazonSocial = "CF Backfilled",
            CatDepartamentoId = 7, CatMunicipioId = 24, CatDistritoId = 111,
            Direccion = "Direccion preservada", Activo = true, FechaCreacion = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();

        var svc = new FromSmartCarePrefillService(
            ctx, NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object,
            Settings(), new Mock<ISmartCareWebhookService>().Object);

        // SC manda CF sin datos fiscales (tetrada 0 de 4)
        var request = BuildValidRequest();
        request.TipoDte = "01";
        request.Receptor = new FromSmartCareReceptorDto
        {
            Nombre = "CF Backfilled", TipoDocumento = "13",
            NumeroDocumento = "012345678",
            DepartamentoId = null, MunicipioId = null, DistritoId = null,
            Direccion = null
        };

        var result = await svc.CrearAsync(request);
        result.PrefillId.Should().BeGreaterThan(0);

        var receptor = await ctx.Receptores.SingleAsync();
        receptor.CatDepartamentoId.Should().Be(7);
        receptor.CatMunicipioId.Should().Be(24);
        receptor.CatDistritoId.Should().Be(111);
        receptor.Direccion.Should().Be("Direccion preservada");
    }

    [Fact]
    public async Task Upsert_receptor_existente_con_tetrada_parcial_no_toca_nada()
    {
        var ctx = BuildContext();
        SeedSucursal(ctx);
        ctx.CatDocsIdentidadReceptor.Add(new FraFactu.Domain.Entities.Catalogos.CatTipoDocumentoIdentificacionReceptor
        {
            Id = 13, Codigo = "13", Valor = "DUI"
        });
        ctx.Receptores.Add(new Receptor
        {
            Id = 952, EmisorId = EmisorId, CatTipoDocumentoIdentificacionReceptorId = 13,
            NumeroDocumento = "01234567-8", NombreRazonSocial = "CF Mixto",
            CatDepartamentoId = 1, CatMunicipioId = 2, CatDistritoId = 3,
            Direccion = "Dir antigua", Activo = true, FechaCreacion = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();

        var svc = new FromSmartCarePrefillService(
            ctx, NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object,
            Settings(), new Mock<ISmartCareWebhookService>().Object);

        // SC manda solo depto+muni (tetrada 2 de 4) — caso defectuoso
        var request = BuildValidRequest();
        request.TipoDte = "01";
        request.Receptor = new FromSmartCareReceptorDto
        {
            Nombre = "CF Mixto", TipoDocumento = "13",
            NumeroDocumento = "012345678",
            DepartamentoId = 7, MunicipioId = 24,
            DistritoId = null, Direccion = null
        };

        var result = await svc.CrearAsync(request);
        result.PrefillId.Should().BeGreaterThan(0);

        // Tetrada parcial entrante ignorada → los 4 quedan como estaban antes
        var receptor = await ctx.Receptores.SingleAsync();
        receptor.CatDepartamentoId.Should().Be(1);
        receptor.CatMunicipioId.Should().Be(2);
        receptor.CatDistritoId.Should().Be(3);
        receptor.Direccion.Should().Be("Dir antigua");
    }

    [Fact]
    public async Task Upsert_receptor_nuevo_con_tetrada_completa_persiste_los_4()
    {
        var ctx = BuildContext();
        SeedSucursal(ctx);
        ctx.CatDocsIdentidadReceptor.Add(new FraFactu.Domain.Entities.Catalogos.CatTipoDocumentoIdentificacionReceptor
        {
            Id = 13, Codigo = "13", Valor = "DUI"
        });
        await ctx.SaveChangesAsync();

        var svc = new FromSmartCarePrefillService(
            ctx, NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object,
            Settings(), new Mock<ISmartCareWebhookService>().Object);

        var request = BuildValidRequest();
        request.TipoDte = "01";
        request.Receptor = new FromSmartCareReceptorDto
        {
            Nombre = "CF Nuevo", TipoDocumento = "13",
            NumeroDocumento = "012345678",
            DepartamentoId = 7, MunicipioId = 24, DistritoId = 111,
            Direccion = "Col Atlacatl"
        };

        var result = await svc.CrearAsync(request);
        result.PrefillId.Should().BeGreaterThan(0);

        var receptor = await ctx.Receptores.SingleAsync();
        receptor.CatDepartamentoId.Should().Be(7);
        receptor.CatMunicipioId.Should().Be(24);
        receptor.CatDistritoId.Should().Be(111);
        receptor.Direccion.Should().Be("Col Atlacatl");
    }

    [Fact]
    public async Task Upsert_receptor_nuevo_con_tetrada_vacia_persiste_los_4_null()
    {
        var ctx = BuildContext();
        SeedSucursal(ctx);
        ctx.CatDocsIdentidadReceptor.Add(new FraFactu.Domain.Entities.Catalogos.CatTipoDocumentoIdentificacionReceptor
        {
            Id = 13, Codigo = "13", Valor = "DUI"
        });
        await ctx.SaveChangesAsync();

        var svc = new FromSmartCarePrefillService(
            ctx, NullLogger<FromSmartCarePrefillService>.Instance,
            new Mock<ITelemetryService>().Object,
            Settings(), new Mock<ISmartCareWebhookService>().Object);

        // CF puro sin dirección fiscal
        var request = BuildValidRequest();
        request.TipoDte = "01";
        request.Receptor = new FromSmartCareReceptorDto
        {
            Nombre = "CF Puro", TipoDocumento = "13",
            NumeroDocumento = "012345678",
            DepartamentoId = null, MunicipioId = null, DistritoId = null,
            Direccion = null
        };

        var result = await svc.CrearAsync(request);
        result.PrefillId.Should().BeGreaterThan(0);

        var receptor = await ctx.Receptores.SingleAsync();
        receptor.CatDepartamentoId.Should().BeNull();
        receptor.CatMunicipioId.Should().BeNull();
        receptor.CatDistritoId.Should().BeNull();
        receptor.Direccion.Should().BeEmpty();
    }

    // =========================================================================
    // Task A2 TDD — TipoImpuesto+PorcentajeIVA por línea sobreviven round-trip
    // JSON (request → LineasJson → deserializado). El wizard Smartix-FE los
    // lee al hidratar el prefill para inicializar item.tipoVenta correctamente.
    // Feature TipoImpuesto+IVA 2026-06-12.
    // =========================================================================

    [Fact]
    public async Task CrearAsync_LineaConTipoImpuestoYPorcentajeIVA_PersisteEnLineasJsonYDeserializaIgual()
    {
        // Arrange
        var ctx = BuildContext();
        SeedSucursal(ctx);

        var telemetry = new Mock<ITelemetryService>();
        var svc = new FromSmartCarePrefillService(
            ctx,
            NullLogger<FromSmartCarePrefillService>.Instance,
            telemetry.Object,
            Settings(),
            new Mock<ISmartCareWebhookService>().Object);

        var request = new FromSmartCareInvoiceRequestDto
        {
            CorrelationId = Guid.NewGuid().ToString(),
            WebhookUrl = "https://smartcare.dev/wh",
            SucursalSmartixId = SucursalId,
            TipoDte = "01",
            Receptor = new FromSmartCareReceptorDto { Nombre = "Cliente Prueba" },
            Lineas = new List<FromSmartCareInvoiceLineDto>
            {
                new()
                {
                    Descripcion = "Servicio Exento",
                    Cantidad = 1m,
                    PrecioUnitario = 50m,
                    TipoItem = 2,
                    PrecioIncluyeIva = false,
                    TipoImpuesto = 2,
                    PorcentajeIVA = null,
                },
                new()
                {
                    Descripcion = "Producto Gravado Custom",
                    Cantidad = 1m,
                    PrecioUnitario = 100m,
                    TipoItem = 1,
                    PrecioIncluyeIva = false,
                    TipoImpuesto = 1,
                    PorcentajeIVA = 13m,
                },
            },
        };

        // Act
        var resp = await svc.CrearAsync(request);

        // Assert: row LineasJson deserializa preservando los nuevos campos
        var prefill = await ctx.FacturaPrefills.AsNoTracking()
            .FirstAsync(p => p.Id == resp.PrefillId);

        var lineas = System.Text.Json.JsonSerializer.Deserialize<List<FromSmartCareInvoiceLineDto>>(
            prefill.LineasJson)!;

        lineas.Should().HaveCount(2);
        lineas[0].TipoImpuesto.Should().Be(2);
        lineas[0].PorcentajeIVA.Should().BeNull();
        lineas[1].TipoImpuesto.Should().Be(1);
        lineas[1].PorcentajeIVA.Should().Be(13m);
    }
}
