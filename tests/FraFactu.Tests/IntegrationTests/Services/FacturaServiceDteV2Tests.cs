using System.Text.Json;
using AutoMapper;
using FraFactu.Application.Common.Interfaces;
using FraFactu.Application.Interfaces;
using FraFactu.Application.Interfaces.Hacienda;
using FraFactu.Application.Services;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Entities.Catalogos;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FraFactu.Tests.IntegrationTests.Services;

/// <summary>
/// Tests del JSON DTE generado por <see cref="FacturaService.GenerateJsonDteAsync"/>
/// para la Normativa DTE V2.0: Factura (01) v2 y CCF (03) v4. Verifican los cambios
/// estructurales contra los esquemas fe-f-v2.json / fe-ccf-v4.json:
/// distrito en dirección, emisor adelgazado, rename ivaRete/ivaPerci, eliminación de
/// reteRenta y de la sección extension, y aparición de observaciones.
/// </summary>
public class FacturaServiceDteV2Tests
{
    private const int EmisorId = 10;
    private const int SucursalId = 100;
    private const int CajaId = 1000;
    private const int ReceptorId = 5000;
    private const int FacturaId = 9000;
    private const string DistritoCodigo = "13";

    private static ApplicationDbContext BuildContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"DteV2_{Guid.NewGuid()}")
            .Options);

    private static FacturaService BuildService(ApplicationDbContext ctx) =>
        new(
            ctx,
            Mock.Of<IMapper>(),
            Mock.Of<IInventarioIntegrationService>(),
            Mock.Of<IHaciendaApiService>(),
            Mock.Of<IHaciendaRetryService>(),
            Mock.Of<IEventoContingenciaService>(),
            Mock.Of<ICurrentUserService>(),
            Mock.Of<IHttpContextAccessor>(),
            NullLogger<FacturaService>.Instance,
            Mock.Of<IEmailService>(),
            Mock.Of<ICrossDbCorrelativoService>(),
            Mock.Of<ICorrelativoInicialService>(),
            Mock.Of<ISaldoDteService>(),
            Mock.Of<ITelemetryService>(),
            Mock.Of<IFacturaQueryService>(),
            new DteJsonBuilder(ctx, NullLogger<DteJsonBuilder>.Instance),
            new FacturaLoteSync(ctx, NullLogger<FacturaLoteSync>.Instance),
            Mock.Of<IFacturaInvalidacionService>());

    /// <summary>
    /// Construye un grafo completo de Factura listo para generar el JSON DTE.
    /// </summary>
    private static void SeedFactura(ApplicationDbContext ctx, string tipoDte, int version, bool receptorConDistrito,
        string? descripcion = null, string? observaciones = null, bool altaPrecision = false)
    {
        ctx.CatAmbientes.Add(new CatAmbienteDestino { Id = 1, Codigo = "00", Valor = "Pruebas" });
        ctx.CatDepartamentos.Add(new CatDepartamento { Id = 1, Codigo = "06", Valor = "San Salvador" });
        ctx.CatMunicipios.Add(new CatMunicipio { Id = 1, Codigo = "0614", Valor = "San Salvador Centro" });
        ctx.CatDistritos.Add(new CatDistrito { Id = 1, Codigo = DistritoCodigo, Valor = "San Salvador", CodigoDepartamento = "06", CodigoMunicipio = "0614" });
        ctx.CatTiposEstablecimiento.Add(new CatTipoEstablecimiento { Id = 1, Codigo = "01", Valor = "Casa Matriz" });
        ctx.CatTiposDocumento.Add(new CatTipoDocumento { Id = 1, Codigo = tipoDte, Valor = "DTE" });
        ctx.CatUnidadesMedida.Add(new CatUnidadMedida { Id = 1, Codigo = "59", Valor = "Unidad" });
        ctx.CatTiposItem.Add(new CatTipoItem { Id = 1, Codigo = "1", Valor = "Bienes" });
        ctx.CatFormasPago.Add(new CatFormaPago { Id = 1, Codigo = "01", Valor = "Billetes y monedas" });
        ctx.CatCondicionesOperacion.Add(new CatCondicionOperacion { Id = 1, Codigo = "1", Valor = "Contado" });
        ctx.CatMonedas.Add(new CatMoneda { Id = 1, Codigo = "USD", Valor = "Dólar" });
        ctx.CatDocsIdentidadReceptor.Add(new CatTipoDocumentoIdentificacionReceptor { Id = 1, Codigo = "36", Valor = "NIT" });

        ctx.Emisores.Add(new Emisor
        {
            Id = EmisorId,
            Nit = "06140506141011",
            Nrc = "1234567",
            NombreRazonSocial = "EMPRESA DE PRUEBAS SA DE CV",
            NombreComercial = "PRUEBAS SA",
            CodigoActividad = "47111",
            DescripcionActividad = "Venta al por menor en comercios no especializados",
            CatTipoEstablecimientoId = 1,
            CatAmbienteDestinoId = 1,
            CorreoElectronico = "test@empresa.com",
            Telefono = "22223333",
            CatDepartamentoId = 1,
            CatMunicipioId = 1,
            CatDistritoId = 1,
            Direccion = "Colonia Escalón"
        });

        ctx.Sucursales.Add(new Sucursal
        {
            Id = SucursalId,
            EmisorId = EmisorId,
            Codigo = "0001",
            Nombre = "Sucursal Principal",
            Direccion = "Colonia Escalón",
            CatDepartamentoId = 1,
            CatMunicipioId = 1,
            CatDistritoId = 1,
            CatTipoEstablecimientoId = 1,
            CodigoEstablecimiento = "0001"
        });

        ctx.Cajas.Add(new Caja
        {
            Id = CajaId,
            Codigo = "CAJA01",
            Nombre = "Caja 1",
            CodPuntoVenta = "P001",
            CodPuntoVentaMH = "PV01",
            SucursalId = SucursalId
        });

        ctx.Receptores.Add(new Receptor
        {
            Id = ReceptorId,
            EmisorId = EmisorId,
            CatTipoDocumentoIdentificacionReceptorId = 1,
            NumeroDocumento = "06141804941020",
            Nrc = "9876543",
            NombreRazonSocial = "CLIENTE EMPRESARIAL SA DE CV",
            CodigoActividad = "47190",
            DescripcionActividad = "Venta al por menor en otros comercios",
            CatDepartamentoId = 1,
            CatMunicipioId = 1,
            CatDistritoId = receptorConDistrito ? 1 : null,
            Direccion = "Avenida Norte",
            CorreoElectronico = "cliente@correo.com",
            Telefono = "22224444"
        });

        ctx.Facturas.Add(new FacturaElectronica
        {
            Id = FacturaId,
            EmisorId = EmisorId,
            SucursalId = SucursalId,
            CajaId = CajaId,
            ReceptorId = ReceptorId,
            CatTipoDocumentoId = 1,
            CatMonedaId = 1,
            CatCondicionOperacionId = 1,
            CatModeloFacturacionId = 1,
            CatTipoTransmisionId = 1,
            Version = version,
            NumeroControl = $"DTE-{tipoDte}-M001P001-000000000000001",
            CodigoGeneracion = "12345678-1234-1234-1234-123456789012",
            FechaEmision = new DateTime(2026, 5, 28),
            HoraEmision = new TimeSpan(10, 30, 0),
            // En modo altaPrecision los totales del resumen llevan 3+ decimales para
            // verificar que la regla 7.2 los recorta a 2.
            TotalNoSujeto = altaPrecision ? 0.111m : 0m,
            TotalExento = altaPrecision ? 0.222m : 0m,
            TotalGravado = altaPrecision ? 100.123456m : 100m,
            SubTotalVentas = altaPrecision ? 100.456789m : 100m,
            DescuentoNoSujeto = altaPrecision ? 0.011m : 0m,
            DescuentoExento = altaPrecision ? 0.022m : 0m,
            DescuentoGravado = altaPrecision ? 0.033m : 0m,
            PorcentajeDescuento = altaPrecision ? 1.555m : 0m,
            TotalDescuento = altaPrecision ? 0.066m : 0m,
            SubTotal = altaPrecision ? 100.789m : 100m,
            TotalIva = altaPrecision ? 13.123456m : 13m,
            IvaPercibido = altaPrecision ? 1.234m : 0m,
            IvaRetenido = altaPrecision ? 0.567m : 0m,
            RetencionRenta = 0m,
            MontoTotalOperacion = altaPrecision ? 113.987654m : 113m,
            TotalNoGravado = altaPrecision ? 0.333m : 0m,
            TotalPagar = altaPrecision ? 113.999m : 113m,
            SaldoFavor = altaPrecision ? 0.001m : 0m,
            TotalLetras = "CIENTO TRECE 00/100",
            Observaciones = observaciones ?? "OBSERVACION DE PRUEBA",
            Tributos = altaPrecision
                ? new List<FacturaTributo>
                {
                    new() { FacturaId = FacturaId, CatTributoId = 1, CodigoAttribute = "20", Descripcion = "IVA 13%", Valor = 13.123456789m }
                }
                : new List<FacturaTributo>(),
            Detalles = new List<FacturaElectronicaDetalle>
            {
                new()
                {
                    FacturaId = FacturaId,
                    NumeroItem = 1,
                    CatTipoItemId = 1,
                    CatUnidadMedidaId = 1,
                    // En modo altaPrecision el cuerpo lleva 9 decimales para verificar
                    // que la regla 7.2 los recorta a 8.
                    Cantidad = altaPrecision ? 1.123456789m : 1m,
                    CodigoProducto = "PROD001",
                    Descripcion = descripcion ?? "Producto de prueba",
                    PrecioUnitario = altaPrecision ? 100.123456789m : 100m,
                    MontoDescuento = altaPrecision ? 0.123456789m : 0m,
                    VentaNoSujeta = altaPrecision ? 0.111111111m : 0m,
                    VentaExenta = altaPrecision ? 0.222222222m : 0m,
                    VentaGravada = altaPrecision ? 100.123456789m : 100m,
                    NoGravado = altaPrecision ? 0.333333333m : 0m,
                    PrecioSugeridoVenta = altaPrecision ? 99.987654321m : (decimal?)null,
                    TributosAplicados = "20",
                    IvaItem = altaPrecision ? 13.123456789m : 13m
                }
            },
            Pagos = new List<Pago>
            {
                new() { FacturaId = FacturaId, CatFormaPagoId = 1, Monto = altaPrecision ? 113.12345m : 113m }
            }
        });

        ctx.SaveChanges();
    }

    private static async Task<JsonElement> GenerarRoot(string tipoDte, int version, bool receptorConDistrito,
        string? descripcion = null, string? observaciones = null, bool altaPrecision = false)
    {
        var ctx = BuildContext();
        SeedFactura(ctx, tipoDte, version, receptorConDistrito, descripcion, observaciones, altaPrecision);
        var json = await BuildService(ctx).GenerateJsonDteAsync(FacturaId, EmisorId);
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    /// <summary>
    /// Cuenta los decimales presentes en el texto crudo de un número JSON.
    /// </summary>
    private static int ContarDecimales(JsonElement numero)
    {
        var raw = numero.GetRawText();
        var punto = raw.IndexOf('.');
        return punto < 0 ? 0 : raw.Length - punto - 1;
    }

    /// <summary>
    /// Recorre todo el subárbol JSON y exige que ningún número supere <paramref name="maxDecimales"/> decimales.
    /// </summary>
    private static void AssertDecimalesMaximos(JsonElement el, int maxDecimales, string ruta)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Number:
                var decimales = ContarDecimales(el);
                (decimales <= maxDecimales).Should().BeTrue(
                    $"el campo '{ruta}' = {el.GetRawText()} no debe exceder {maxDecimales} decimales (regla 7.2 MH), pero tiene {decimales}");
                break;
            case JsonValueKind.Object:
                foreach (var prop in el.EnumerateObject())
                    AssertDecimalesMaximos(prop.Value, maxDecimales, $"{ruta}.{prop.Name}");
                break;
            case JsonValueKind.Array:
                var i = 0;
                foreach (var item in el.EnumerateArray())
                    AssertDecimalesMaximos(item, maxDecimales, $"{ruta}[{i++}]");
                break;
        }
    }

    // ─────────────────────────────────── Factura (01) v2 ───────────────────────────────────

    [Fact]
    public async Task Factura01_GeneraVersion2()
    {
        var root = await GenerarRoot("01", 2, receptorConDistrito: true);
        root.GetProperty("identificacion").GetProperty("version").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Factura01_EmisorAdelgazadoConDistrito()
    {
        var root = await GenerarRoot("01", 2, receptorConDistrito: true);
        var emisor = root.GetProperty("emisor");

        emisor.TryGetProperty("tipoEstablecimiento", out _).Should().BeFalse();
        emisor.TryGetProperty("codEstableMH", out _).Should().BeFalse();
        emisor.TryGetProperty("codPuntoVentaMH", out _).Should().BeFalse();
        emisor.TryGetProperty("codEstable", out _).Should().BeTrue();
        emisor.TryGetProperty("codPuntoVenta", out _).Should().BeTrue();

        emisor.GetProperty("direccion").GetProperty("distrito").GetString().Should().Be(DistritoCodigo);
    }

    [Fact]
    public async Task Factura01_ResumenRenombraIvaReteYConservaTotalIva()
    {
        var root = await GenerarRoot("01", 2, receptorConDistrito: true);
        var resumen = root.GetProperty("resumen");

        resumen.TryGetProperty("ivaRete", out _).Should().BeTrue();
        resumen.TryGetProperty("totalIva", out _).Should().BeTrue();
        resumen.TryGetProperty("observaciones", out _).Should().BeTrue();
        resumen.TryGetProperty("ivaRete1", out _).Should().BeFalse();
        resumen.TryGetProperty("reteRenta", out _).Should().BeFalse();
        resumen.TryGetProperty("ivaPerci", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Factura01_NoIncluyeExtension()
    {
        var root = await GenerarRoot("01", 2, receptorConDistrito: true);
        root.TryGetProperty("extension", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Factura01_ReceptorConDistrito_EmiteDireccionConDistrito()
    {
        var root = await GenerarRoot("01", 2, receptorConDistrito: true);
        var direccion = root.GetProperty("receptor").GetProperty("direccion");
        direccion.ValueKind.Should().Be(JsonValueKind.Object);
        direccion.GetProperty("distrito").GetString().Should().Be(DistritoCodigo);
    }

    [Fact]
    public async Task Factura01_ReceptorSinDistrito_EmiteDireccionNull()
    {
        var root = await GenerarRoot("01", 2, receptorConDistrito: false);
        root.GetProperty("receptor").GetProperty("direccion").ValueKind.Should().Be(JsonValueKind.Null);
    }

    // ─────────────────────────────────── CCF (03) v4 ───────────────────────────────────

    [Fact]
    public async Task Ccf03_GeneraVersion4()
    {
        var root = await GenerarRoot("03", 4, receptorConDistrito: true);
        root.GetProperty("identificacion").GetProperty("version").GetInt32().Should().Be(4);
    }

    [Fact]
    public async Task Ccf03_ResumenUsaIvaPerciEIvaReteSinTotalIvaNiReteRenta()
    {
        var root = await GenerarRoot("03", 4, receptorConDistrito: true);
        var resumen = root.GetProperty("resumen");

        resumen.TryGetProperty("ivaPerci", out _).Should().BeTrue();
        resumen.TryGetProperty("ivaRete", out _).Should().BeTrue();
        resumen.TryGetProperty("observaciones", out _).Should().BeTrue();
        resumen.TryGetProperty("totalIva", out _).Should().BeFalse();
        resumen.TryGetProperty("reteRenta", out _).Should().BeFalse();
        resumen.TryGetProperty("ivaRete1", out _).Should().BeFalse();
        resumen.TryGetProperty("ivaPerci1", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Ccf03_EmisorAdelgazadoConDistrito_YSinExtension()
    {
        var root = await GenerarRoot("03", 4, receptorConDistrito: true);
        var emisor = root.GetProperty("emisor");

        emisor.TryGetProperty("tipoEstablecimiento", out _).Should().BeFalse();
        emisor.TryGetProperty("codEstableMH", out _).Should().BeFalse();
        emisor.TryGetProperty("codPuntoVentaMH", out _).Should().BeFalse();
        emisor.GetProperty("direccion").GetProperty("distrito").GetString().Should().Be(DistritoCodigo);

        root.TryGetProperty("extension", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Ccf03_ReceptorEmiteDistrito()
    {
        var root = await GenerarRoot("03", 4, receptorConDistrito: true);
        root.GetProperty("receptor").GetProperty("direccion").GetProperty("distrito").GetString().Should().Be(DistritoCodigo);
    }

    // ─────────────────────────────────── Nota de Crédito (05) v4 ───────────────────────────────────

    [Fact]
    public async Task Nc05_GeneraVersion4ConFusion()
    {
        var root = await GenerarRoot("05", 4, receptorConDistrito: true);
        var ident = root.GetProperty("identificacion");

        ident.GetProperty("version").GetInt32().Should().Be(4);
        ident.TryGetProperty("fusion", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Nc05_EmisorSinTipoEstablecimientoNiCodEstable_ConDistrito()
    {
        var root = await GenerarRoot("05", 4, receptorConDistrito: true);
        var emisor = root.GetProperty("emisor");

        emisor.TryGetProperty("tipoEstablecimiento", out _).Should().BeFalse();
        emisor.TryGetProperty("codEstable", out _).Should().BeFalse();
        emisor.TryGetProperty("codPuntoVenta", out _).Should().BeFalse();
        emisor.GetProperty("direccion").GetProperty("distrito").GetString().Should().Be(DistritoCodigo);
    }

    [Fact]
    public async Task Nc05_ReceptorUsaTipoDocumentoYNumDocumentoConDistrito()
    {
        var root = await GenerarRoot("05", 4, receptorConDistrito: true);
        var receptor = root.GetProperty("receptor");

        receptor.TryGetProperty("tipoDocumento", out _).Should().BeTrue();
        receptor.TryGetProperty("numDocumento", out _).Should().BeTrue();
        receptor.TryGetProperty("nit", out _).Should().BeFalse();
        receptor.GetProperty("direccion").GetProperty("distrito").GetString().Should().Be(DistritoCodigo);
    }

    [Fact]
    public async Task Nc05_CuerpoDocumentoConCamposIvaV4()
    {
        var root = await GenerarRoot("05", 4, receptorConDistrito: true);
        var item = root.GetProperty("cuerpoDocumento")[0];

        item.TryGetProperty("noGravado", out _).Should().BeTrue();
        item.TryGetProperty("ivaPerci", out _).Should().BeTrue();
        item.TryGetProperty("totalIva", out _).Should().BeTrue();
        item.TryGetProperty("ivaRete", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Nc05_ResumenV4SinCamposObsoletos()
    {
        var root = await GenerarRoot("05", 4, receptorConDistrito: true);
        var resumen = root.GetProperty("resumen");

        resumen.TryGetProperty("ivaPerci", out _).Should().BeTrue();
        resumen.TryGetProperty("totalIva", out _).Should().BeTrue();
        resumen.TryGetProperty("ivaRete", out _).Should().BeTrue();
        resumen.TryGetProperty("totalNoGravado", out _).Should().BeTrue();
        resumen.TryGetProperty("totalPagar", out _).Should().BeTrue();
        resumen.TryGetProperty("observaciones", out _).Should().BeTrue();
        resumen.TryGetProperty("codigoRetencionMH", out _).Should().BeTrue();

        resumen.TryGetProperty("subTotal", out _).Should().BeFalse();
        resumen.TryGetProperty("reteRenta", out _).Should().BeFalse();
        resumen.TryGetProperty("descuNoSuj", out _).Should().BeFalse();
        resumen.TryGetProperty("ivaRete1", out _).Should().BeFalse();
        resumen.TryGetProperty("ivaPerci1", out _).Should().BeFalse();
        resumen.TryGetProperty("saldoFavor", out _).Should().BeFalse();
        resumen.TryGetProperty("pagos", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Nc05_NoIncluyeExtension()
    {
        var root = await GenerarRoot("05", 4, receptorConDistrito: true);
        root.TryGetProperty("extension", out _).Should().BeFalse();
    }

    // ─────────────────────────────────── Nota de Débito (06) v4 ───────────────────────────────────

    [Fact]
    public async Task Nd06_GeneraVersion4ConFusion()
    {
        var root = await GenerarRoot("06", 4, receptorConDistrito: true);
        var ident = root.GetProperty("identificacion");

        ident.GetProperty("version").GetInt32().Should().Be(4);
        ident.TryGetProperty("fusion", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Nd06_EmisorSinTipoEstablecimientoNiCodEstable_ConDistrito()
    {
        var root = await GenerarRoot("06", 4, receptorConDistrito: true);
        var emisor = root.GetProperty("emisor");

        emisor.TryGetProperty("tipoEstablecimiento", out _).Should().BeFalse();
        emisor.TryGetProperty("codEstable", out _).Should().BeFalse();
        emisor.TryGetProperty("codPuntoVenta", out _).Should().BeFalse();
        emisor.GetProperty("direccion").GetProperty("distrito").GetString().Should().Be(DistritoCodigo);
    }

    [Fact]
    public async Task Nd06_ReceptorUsaTipoDocumentoYNumDocumentoConDistrito()
    {
        var root = await GenerarRoot("06", 4, receptorConDistrito: true);
        var receptor = root.GetProperty("receptor");

        receptor.TryGetProperty("tipoDocumento", out _).Should().BeTrue();
        receptor.TryGetProperty("numDocumento", out _).Should().BeTrue();
        receptor.TryGetProperty("nit", out _).Should().BeFalse();
        receptor.GetProperty("direccion").GetProperty("distrito").GetString().Should().Be(DistritoCodigo);
    }

    [Fact]
    public async Task Nd06_CuerpoDocumentoConCamposIvaV4()
    {
        var root = await GenerarRoot("06", 4, receptorConDistrito: true);
        var item = root.GetProperty("cuerpoDocumento")[0];

        item.TryGetProperty("noGravado", out _).Should().BeTrue();
        item.TryGetProperty("ivaPerci", out _).Should().BeTrue();
        item.TryGetProperty("totalIva", out _).Should().BeTrue();
        item.TryGetProperty("ivaRete", out _).Should().BeTrue();
        item.TryGetProperty("tributos", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Nd06_ResumenV4ConNumPagoElectronicoSinCamposObsoletos()
    {
        var root = await GenerarRoot("06", 4, receptorConDistrito: true);
        var resumen = root.GetProperty("resumen");

        resumen.TryGetProperty("ivaPerci", out _).Should().BeTrue();
        resumen.TryGetProperty("totalIva", out _).Should().BeTrue();
        resumen.TryGetProperty("ivaRete", out _).Should().BeTrue();
        resumen.TryGetProperty("totalNoGravado", out _).Should().BeTrue();
        resumen.TryGetProperty("totalPagar", out _).Should().BeTrue();
        resumen.TryGetProperty("numPagoElectronico", out _).Should().BeTrue();
        resumen.TryGetProperty("observaciones", out _).Should().BeTrue();
        resumen.TryGetProperty("codigoRetencionMH", out _).Should().BeTrue();

        resumen.TryGetProperty("subTotal", out _).Should().BeFalse();
        resumen.TryGetProperty("reteRenta", out _).Should().BeFalse();
        resumen.TryGetProperty("descuNoSuj", out _).Should().BeFalse();
        resumen.TryGetProperty("ivaRete1", out _).Should().BeFalse();
        resumen.TryGetProperty("ivaPerci1", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Nd06_NoIncluyeExtension()
    {
        var root = await GenerarRoot("06", 4, receptorConDistrito: true);
        root.TryGetProperty("extension", out _).Should().BeFalse();
    }

    // ─────────────────────────────────── Factura de Sujeto Excluido (14) v2 ───────────────────────────────────

    [Fact]
    public async Task Fse14_GeneraVersion2()
    {
        var root = await GenerarRoot("14", 2, receptorConDistrito: true);
        root.GetProperty("identificacion").GetProperty("version").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Fse14_EmisorSinCodMHConCodEstableYDistrito()
    {
        var root = await GenerarRoot("14", 2, receptorConDistrito: true);
        var emisor = root.GetProperty("emisor");

        emisor.TryGetProperty("codEstableMH", out _).Should().BeFalse();
        emisor.TryGetProperty("codPuntoVentaMH", out _).Should().BeFalse();
        emisor.TryGetProperty("codEstable", out _).Should().BeTrue();
        emisor.TryGetProperty("codPuntoVenta", out _).Should().BeTrue();
        emisor.GetProperty("direccion").GetProperty("distrito").GetString().Should().Be(DistritoCodigo);
    }

    [Fact]
    public async Task Fse14_UsaNodoReceptorNoSujetoExcluido_ConDistrito()
    {
        var root = await GenerarRoot("14", 2, receptorConDistrito: true);

        root.TryGetProperty("receptor", out var receptor).Should().BeTrue();
        root.TryGetProperty("sujetoExcluido", out _).Should().BeFalse();
        receptor.GetProperty("direccion").GetProperty("distrito").GetString().Should().Be(DistritoCodigo);
    }

    [Fact]
    public async Task Fse14_CuerpoUsaCompra()
    {
        var root = await GenerarRoot("14", 2, receptorConDistrito: true);
        var item = root.GetProperty("cuerpoDocumento")[0];

        item.TryGetProperty("compra", out _).Should().BeTrue();
        item.TryGetProperty("ventaGravada", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Fse14_ResumenSinIvaRete1ConReteRenta()
    {
        var root = await GenerarRoot("14", 2, receptorConDistrito: true);
        var resumen = root.GetProperty("resumen");

        resumen.TryGetProperty("totalCompra", out _).Should().BeTrue();
        resumen.TryGetProperty("reteRenta", out _).Should().BeTrue();
        resumen.TryGetProperty("ivaRete1", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Fse14_NoIncluyeExtension()
    {
        var root = await GenerarRoot("14", 2, receptorConDistrito: true);
        root.TryGetProperty("extension", out _).Should().BeFalse();
    }

    // ─────────────────────────────────── Sanitización de saltos de línea (F5) ───────────────────────────────────

    [Fact]
    public async Task Factura01_SanitizaSaltosDeLineaEnTextos()
    {
        var root = await GenerarRoot("01", 2, receptorConDistrito: true,
            descripcion: "Línea1\r\nLínea2", observaciones: "Obs1\nObs2\rObs3");

        var descripcion = root.GetProperty("cuerpoDocumento")[0].GetProperty("descripcion").GetString();
        var observaciones = root.GetProperty("resumen").GetProperty("observaciones").GetString();

        descripcion.Should().NotContainAny("\r", "\n").And.Be("Línea1 Línea2");
        observaciones.Should().NotContainAny("\r", "\n").And.Be("Obs1 Obs2 Obs3");
    }

    [Fact]
    public async Task Nc05_SanitizaSaltosDeLineaEnDescripcion()
    {
        var root = await GenerarRoot("05", 4, receptorConDistrito: true,
            descripcion: "Detalle\r\ncon salto");

        var descripcion = root.GetProperty("cuerpoDocumento")[0].GetProperty("descripcion").GetString();

        descripcion.Should().NotContainAny("\r", "\n").And.Be("Detalle con salto");
    }

    // ─────────────────────────────────── Redondeo regla 7.2 MH (P0-A) ───────────────────────────────────
    // Cuerpo del documento: máximo 8 decimales. Resumen: máximo 2 decimales.
    // Se siembran valores con 9 decimales (cuerpo) y 3+ decimales (resumen) para que,
    // si el redondeo no se aplica, el test falle.

    [Theory]
    [InlineData("01", 2)] // Factura
    [InlineData("03", 4)] // CCF
    [InlineData("05", 4)] // Nota de Crédito
    [InlineData("06", 4)] // Nota de Débito
    public async Task Redondeo72_CuerpoNoSupera8Decimales(string tipoDte, int version)
    {
        var root = await GenerarRoot(tipoDte, version, receptorConDistrito: true, altaPrecision: true);

        AssertDecimalesMaximos(root.GetProperty("cuerpoDocumento"), 8, "cuerpoDocumento");
    }

    [Theory]
    [InlineData("01", 2)] // Factura
    [InlineData("03", 4)] // CCF
    [InlineData("05", 4)] // Nota de Crédito
    [InlineData("06", 4)] // Nota de Débito
    public async Task Redondeo72_ResumenNoSupera2Decimales(string tipoDte, int version)
    {
        var root = await GenerarRoot(tipoDte, version, receptorConDistrito: true, altaPrecision: true);

        AssertDecimalesMaximos(root.GetProperty("resumen"), 2, "resumen");
    }

    [Theory]
    [InlineData("01", 2)]
    [InlineData("03", 4)]
    [InlineData("05", 4)]
    [InlineData("06", 4)]
    public async Task Redondeo72_AplicaAwayFromZeroEnPrecioUni(string tipoDte, int version)
    {
        // 100.123456789 redondeado a 8 decimales (AwayFromZero) = 100.12345679
        var root = await GenerarRoot(tipoDte, version, receptorConDistrito: true, altaPrecision: true);
        var precioUni = root.GetProperty("cuerpoDocumento")[0].GetProperty("precioUni").GetDecimal();

        precioUni.Should().Be(100.12345679m);
    }

    [Theory]
    [InlineData("01", 2)]
    [InlineData("03", 4)]
    [InlineData("05", 4)]
    [InlineData("06", 4)]
    public async Task Redondeo72_AplicaAwayFromZeroEnTotalGravadaResumen(string tipoDte, int version)
    {
        // 100.123456 redondeado a 2 decimales (AwayFromZero) = 100.12
        var root = await GenerarRoot(tipoDte, version, receptorConDistrito: true, altaPrecision: true);
        var totalGravada = root.GetProperty("resumen").GetProperty("totalGravada").GetDecimal();

        totalGravada.Should().Be(100.12m);
    }

    // ─────────────────────────────────── Golden master (Fase 2 refactor FacturaService) ───────────────────────────────────

    // Normaliza el GUID de codigoGeneracion para que el snapshot sea estable.
    private static string NormalizarCodigoGeneracion(string json)
    {
        var root = JsonDocument.Parse(json).RootElement;
        // Reemplaza el valor de "codigoGeneracion" (aparece en identificacion) por un placeholder fijo.
        var cg = root.GetProperty("identificacion").GetProperty("codigoGeneracion").GetString();
        return cg == null ? json : json.Replace(cg, "00000000-0000-0000-0000-000000000000");
    }

    [Fact]
    public async Task GoldenMaster_FE_JsonByteIdentico()
    {
        var ctx = BuildContext();
        SeedFactura(ctx, "01", version: 1, receptorConDistrito: true);
        var json = await BuildService(ctx).GenerateJsonDteAsync(FacturaId, EmisorId);
        var normalizado = NormalizarCodigoGeneracion(json);

        // EXPECTED capturado ejecutando el test contra el código actual (baseline Fase 2).
        // Único campo no determinista normalizado: identificacion.codigoGeneracion (ver
        // NormalizarCodigoGeneracion). El resto del JSON es estable para esta semilla fija.
        const string EXPECTED = """
        {"identificacion":{"version":1,"ambiente":"00","tipoDte":"01","numeroControl":"DTE-01-M001P001-000000000000001","codigoGeneracion":"00000000-0000-0000-0000-000000000000","tipoModelo":1,"tipoOperacion":1,"tipoContingencia":null,"motivoContin":null,"fecEmi":"2026-05-28","horEmi":"10:30:00","tipoMoneda":"USD"},"documentoRelacionado":null,"emisor":{"nit":"06140506141011","nrc":"1234567","nombre":"EMPRESA DE PRUEBAS SA DE CV","codActividad":"47111","descActividad":"Venta al por menor en comercios no especializados","nombreComercial":"PRUEBAS SA","direccion":{"departamento":"06","municipio":"0614","distrito":"13","complemento":"Colonia Escal\u00F3n"},"telefono":"22223333","correo":"test@empresa.com","codEstable":"0001","codPuntoVenta":"P001"},"receptor":{"tipoDocumento":"36","numDocumento":"06141804941020","nrc":"9876543","nombre":"CLIENTE EMPRESARIAL SA DE CV","codActividad":"47190","descActividad":"Venta al por menor en otros comercios","direccion":{"departamento":"06","municipio":"0614","distrito":"13","complemento":"Avenida Norte"},"telefono":"22224444","correo":"cliente@correo.com"},"otrosDocumentos":null,"ventaTercero":null,"cuerpoDocumento":[{"numItem":1,"tipoItem":1,"numeroDocumento":null,"cantidad":1,"codigo":"PROD001","codTributo":null,"uniMedida":59,"descripcion":"Producto de prueba","precioUni":100,"montoDescu":0,"ventaNoSuj":0,"ventaExenta":0,"ventaGravada":100,"tributos":["20"],"psv":0.00,"noGravado":0,"ivaItem":13}],"resumen":{"totalNoSuj":0,"totalExenta":0,"totalGravada":100,"subTotalVentas":100,"descuNoSuj":0,"descuExenta":0,"descuGravada":0,"porcentajeDescuento":0,"totalDescu":0,"tributos":null,"subTotal":100,"ivaRete":0,"montoTotalOperacion":113,"totalNoGravado":0,"totalPagar":113,"totalLetras":"CIENTO TRECE 00/100","totalIva":13,"saldoFavor":0,"condicionOperacion":1,"pagos":[{"codigo":"01","montoPago":113,"referencia":null,"plazo":null,"periodo":null}],"numPagoElectronico":null,"observaciones":"OBSERVACION DE PRUEBA"},"apendice":null}
        """;
        normalizado.Should().Be(EXPECTED);
    }
}
