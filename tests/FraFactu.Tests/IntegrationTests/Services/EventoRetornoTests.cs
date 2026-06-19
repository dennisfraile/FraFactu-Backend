using System.Text.Json;
using FraFactu.Application.DTOs.Hacienda;
using FraFactu.Application.DTOs.Retorno;
using FraFactu.Application.Interfaces.Hacienda;
using FraFactu.Application.Validators.Retorno;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Entities.Catalogos;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FraFactu.Tests.IntegrationTests.Services;

/// <summary>
/// Tests del Evento de Retorno (tipoEvento "18", esquema fe-eret-v1.json):
/// estructura del JSON, cálculo del resumen, exclusión ventaTercero/compraTercero y validador.
/// </summary>
public class EventoRetornoTests
{
    private const int EmisorId = 40;

    private static ApplicationDbContext BuildContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"EretV1_{Guid.NewGuid()}")
            .Options);

    private static EventoRetornoService BuildService(ApplicationDbContext ctx, IHaciendaApiService? hacienda = null) =>
        new(ctx, hacienda ?? Mock.Of<IHaciendaApiService>(), NullLogger<EventoRetornoService>.Instance);

    private static void SeedEmisor(ApplicationDbContext ctx)
    {
        ctx.CatAmbientes.Add(new CatAmbienteDestino { Id = 1, Codigo = "00", Valor = "Pruebas" });
        ctx.Emisores.Add(new Emisor
        {
            Id = EmisorId,
            Nit = "0614-050614-101-1",
            NombreRazonSocial = "EXPORTADORA DE PRUEBAS SA DE CV",
            NombreComercial = "EXPORTA SA",
            CatAmbienteDestinoId = 1,
            CorreoElectronico = "test@empresa.com",
            Telefono = "22223333"
        });
        ctx.SaveChanges();
    }

    private static CrearEventoRetornoDto DtoValido() => new()
    {
        TipoModelo = 1,
        TipoOperacion = 1,
        RecintoFiscal = "01",
        TipoRegimen = "EX1",
        Regimen = "DEFINITIVO",
        TipoItemExpor = 1,
        DocumentoRelacionado = new List<DocumentoRelacionadoInputDto>
        {
            new() { TipoDocumento = "11", CodigoGeneracion = "11111111-2222-3333-4444-555555555555", FechaEmision = new DateTime(2026, 5, 1) }
        },
        Documento = new DocumentoReceptorInputDto
        {
            Nombre = "CLIENTE EXTERIOR INC",
            CodPais = "9300",
            NombrePais = "Estados Unidos"
        },
        VentaTercero = new VentaTerceroInputDto { Nit = "06141234567890", Nombre = "TERCERO SA", CodDomiciliado = 1 },
        Items = new List<ItemRetornoDto>
        {
            new()
            {
                TipoItem = 1,
                CodigoGeneracion = "11111111-2222-3333-4444-555555555555",
                Cantidad = 2m,
                PrecioUni = 100m,
                Descripcion = "Bien exportado retornado",
                UniMedida = 59,
                MontoDescu = 0m,
                VentaNoSuj = 0m,
                VentaExenta = 0m,
                VentaGravada = 200m,
                Compra = 0m,
                IvaItem = 26m,
                NoGravado = 0m,
                Seguro = 0m,
                Flete = 0m,
                IvaRete = 0m,
                ReteRenta = 0m
            }
        },
        Tributos = new List<TributoResumenInputDto>
        {
            new() { Codigo = "20", Descripcion = "Impuesto al Valor Agregado 13%", Valor = 26m }
        },
        Apendice = new List<ApendiceInputDto>
        {
            new() { Campo = "origen", Etiqueta = "Origen", Valor = "Carga manual" }
        }
    };

    [Fact]
    public async Task CrearEvento_CalculaResumenYTransmite()
    {
        using var ctx = BuildContext();
        SeedEmisor(ctx);

        var hacienda = new Mock<IHaciendaApiService>();
        hacienda
            .Setup(h => h.EnviarEventoRetornoAsync(EmisorId, It.IsAny<EventoRetornoDto>()))
            .ReturnsAsync(new RetornoResponseDto { Estado = "PROCESADO", SelloRecibido = "SELLO999" });

        var service = BuildService(ctx, hacienda.Object);

        var resultado = await service.CrearEventoAsync(DtoValido(), EmisorId);

        resultado.SubTotalVentas.Should().Be(200m);
        resultado.TotalIva.Should().Be(26m);
        resultado.MontoTotalOperacion.Should().Be(226m); // subTotal 200 + iva 26
        resultado.TotalPagar.Should().Be(226m);          // sin retenciones
        resultado.TotalLetras.Should().NotBeNullOrEmpty();
        resultado.TotalItems.Should().Be(1);
        resultado.TotalDocumentosRelacionados.Should().Be(1);
        resultado.EstadoHacienda.Should().Be("PROCESADO");
        resultado.SelloRecibido.Should().Be("SELLO999");
    }

    [Fact]
    public async Task GenerarJson_DebeCumplirEsquemaEretV1()
    {
        using var ctx = BuildContext();
        SeedEmisor(ctx);
        var service = BuildService(ctx);
        var resultado = await service.CrearEventoAsync(DtoValido(), EmisorId);

        var json = await service.GenerarJsonEventoAsync(resultado.Id, EmisorId);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ident = root.GetProperty("identificacion");
        ident.GetProperty("version").GetInt32().Should().Be(1);
        ident.GetProperty("tipoEvento").GetString().Should().Be("18");
        ident.GetProperty("tipoMoneda").GetString().Should().Be("USD");
        ident.GetProperty("tipoContingencia").ValueKind.Should().Be(JsonValueKind.Null);
        ident.GetProperty("fusion").ValueKind.Should().Be(JsonValueKind.Null);

        root.GetProperty("documentoRelacionado").GetArrayLength().Should().Be(1);

        var emisor = root.GetProperty("emisor");
        emisor.GetProperty("nit").GetString().Should().Be("06140506141011");
        emisor.GetProperty("recintoFiscal").GetString().Should().Be("01");

        // oneOf: ventaTercero presente, compraTercero null
        root.GetProperty("ventaTercero").ValueKind.Should().Be(JsonValueKind.Object);
        root.GetProperty("compraTercero").ValueKind.Should().Be(JsonValueKind.Null);

        root.GetProperty("cuerpoDocumento").GetArrayLength().Should().Be(1);
        var resumen = root.GetProperty("resumen");
        resumen.GetProperty("subTotalVentas").GetDecimal().Should().Be(200m);
        resumen.GetProperty("totalPagar").GetDecimal().Should().Be(226m);
        resumen.GetProperty("tributos").GetArrayLength().Should().Be(1);

        root.GetProperty("apendice").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task CrearEvento_ConAmbosTerceros_DebeFallar()
    {
        using var ctx = BuildContext();
        SeedEmisor(ctx);
        var service = BuildService(ctx);

        var dto = DtoValido();
        dto.CompraTercero = new CompraTerceroInputDto { NumDocumento = "X", Nombre = "Y" };

        var act = async () => await service.CrearEventoAsync(dto, EmisorId);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void Validador_DtoValido_DebeSerValido()
    {
        new CrearEventoRetornoDtoValidator().Validate(DtoValido()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validador_AmbosTerceros_DebeFallar()
    {
        var dto = DtoValido();
        dto.CompraTercero = new CompraTerceroInputDto { NumDocumento = "X", Nombre = "Y" };
        new CrearEventoRetornoDtoValidator().Validate(dto).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validador_SinDocumentoRelacionado_DebeFallar()
    {
        var dto = DtoValido();
        dto.DocumentoRelacionado.Clear();
        new CrearEventoRetornoDtoValidator().Validate(dto).IsValid.Should().BeFalse();
    }

    [Fact]
    public void EventoRetornoDto_DefaultsDeIdentificacion()
    {
        var ident = new IdentificacionRetornoDto();
        ident.Version.Should().Be(1);
        ident.TipoEvento.Should().Be("18");
        ident.TipoMoneda.Should().Be("USD");
    }
}
