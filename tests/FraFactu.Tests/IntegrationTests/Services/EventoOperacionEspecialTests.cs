using System.Text.Json;
using FraFactu.Application.DTOs.Hacienda;
using FraFactu.Application.DTOs.OperacionesEspeciales;
using FraFactu.Application.Interfaces.Hacienda;
using FraFactu.Application.Validators.OperacionesEspeciales;
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
/// Tests del Evento de Operaciones Especiales (tipoEvento "17", esquema fe-eop-v1.json):
/// estructura del JSON, cálculo del resumen y validador de entrada.
/// </summary>
public class EventoOperacionEspecialTests
{
    private const int EmisorId = 30;

    private static ApplicationDbContext BuildContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"EopV1_{Guid.NewGuid()}")
            .Options);

    private static EventoOperacionEspecialService BuildService(ApplicationDbContext ctx, IHaciendaApiService? hacienda = null) =>
        new(
            ctx,
            hacienda ?? Mock.Of<IHaciendaApiService>(),
            NullLogger<EventoOperacionEspecialService>.Instance);

    private static void SeedEmisor(ApplicationDbContext ctx)
    {
        ctx.CatAmbientes.Add(new CatAmbienteDestino { Id = 1, Codigo = "00", Valor = "Pruebas" });
        ctx.Emisores.Add(new Emisor
        {
            Id = EmisorId,
            Nit = "0614-050614-101-1",
            NombreRazonSocial = "EMPRESA DE PRUEBAS SA DE CV",
            NombreComercial = "PRUEBAS SA",
            CatAmbienteDestinoId = 1,
            CorreoElectronico = "test@empresa.com",
            Telefono = "22223333"
        });
        ctx.SaveChanges();
    }

    private static CrearEventoOperacionEspecialDto DtoValido() => new()
    {
        Items = new List<ItemOperacionEspecialDto>
        {
            new()
            {
                TipoDocumento = "97",
                NumDocumento = "CCI-001",
                FechaEmision = new DateTime(2026, 5, 20),
                Cantidad = 1,
                Descripcion = "Comprobante de crédito por internación (CCI)",
                DocDel = "1",
                DocAl = "100",
                PrecioUni = 100m,
                VentaNoSuj = 0m,
                VentaExenta = 0m,
                VentaGravada = 100m,
                Tributos = new List<string> { "20" }
            },
            new()
            {
                TipoDocumento = "02",
                Cantidad = 1,
                Descripcion = "Factura de venta simplificada",
                PrecioUni = 50m,
                VentaNoSuj = 0m,
                VentaExenta = 50m,
                VentaGravada = 0m
            }
        },
        Tributos = new List<TributoResumenInputDto>
        {
            new() { Codigo = "20", Descripcion = "Impuesto al Valor Agregado 13%", Valor = 13m }
        },
        Apendice = new List<ApendiceInputDto>
        {
            new() { Campo = "origen", Etiqueta = "Origen del reporte", Valor = "Carga manual" }
        }
    };

    [Fact]
    public async Task CrearEvento_CalculaResumenYTransmite()
    {
        // Arrange
        using var ctx = BuildContext();
        SeedEmisor(ctx);

        var hacienda = new Mock<IHaciendaApiService>();
        hacienda
            .Setup(h => h.EnviarEventoOperacionesEspecialesAsync(EmisorId, It.IsAny<EventoOperacionesEspecialesDto>()))
            .ReturnsAsync(new OperacionesEspecialesResponseDto { Estado = "RECIBIDO", SelloRecibido = "SELLO123" });

        var service = BuildService(ctx, hacienda.Object);

        // Act
        var resultado = await service.CrearEventoAsync(DtoValido(), EmisorId);

        // Assert: totales calculados en el backend
        resultado.TotalGravada.Should().Be(100m);
        resultado.TotalExenta.Should().Be(50m);
        resultado.TotalNoSuj.Should().Be(0m);
        resultado.SubTotal.Should().Be(150m);
        resultado.Total.Should().Be(163m); // subTotal 150 + tributo 13
        resultado.TotalLetras.Should().NotBeNullOrEmpty();
        resultado.TotalItems.Should().Be(2);
        resultado.EstadoHacienda.Should().Be("RECIBIDO");
        resultado.SelloRecibido.Should().Be("SELLO123");
        resultado.CodigoGeneracion.Should().MatchRegex("^[A-F0-9]{8}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{12}$");
    }

    [Fact]
    public async Task GenerarJson_DebeCumplirEsquemaEopV1()
    {
        // Arrange
        using var ctx = BuildContext();
        SeedEmisor(ctx);
        var service = BuildService(ctx);
        var resultado = await service.CrearEventoAsync(DtoValido(), EmisorId);

        // Act
        var json = await service.GenerarJsonEventoAsync(resultado.Id, EmisorId);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert identificación
        var ident = root.GetProperty("identificacion");
        ident.GetProperty("version").GetInt32().Should().Be(1);
        ident.GetProperty("tipoModelo").GetInt32().Should().Be(1);
        ident.GetProperty("tipoOperacion").GetInt32().Should().Be(1);
        ident.GetProperty("tipoEvento").GetString().Should().Be("17");
        ident.GetProperty("tipoMoneda").GetString().Should().Be("USD");

        // Emisor: solo nit + nombre, nit sin guiones
        var emisor = root.GetProperty("emisor");
        emisor.GetProperty("nit").GetString().Should().Be("06140506141011");
        emisor.TryGetProperty("nombre", out _).Should().BeTrue();
        emisor.EnumerateObject().Count().Should().Be(2, "el emisor del evento 17 solo lleva nit y nombre");

        // Cuerpo del documento
        var cuerpo = root.GetProperty("cuerpoDocumento");
        cuerpo.GetArrayLength().Should().Be(2);
        cuerpo[0].GetProperty("numItem").GetInt32().Should().Be(1);
        cuerpo[0].GetProperty("tributos").EnumerateArray().Select(t => t.GetString()).Should().Contain("20");
        // null explícito en el segundo ítem (sin tributos)
        cuerpo[1].GetProperty("tributos").ValueKind.Should().Be(JsonValueKind.Null);

        // Resumen
        var resumen = root.GetProperty("resumen");
        resumen.GetProperty("subTotal").GetDecimal().Should().Be(150m);
        resumen.GetProperty("total").GetDecimal().Should().Be(163m);
        resumen.GetProperty("totalLetras").GetString().Should().NotBeNullOrEmpty();
        resumen.GetProperty("tributos").GetArrayLength().Should().Be(1);

        // Apéndice
        root.GetProperty("apendice").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public void Validador_DtoValido_DebeSerValido()
    {
        var validator = new CrearEventoOperacionEspecialDtoValidator();
        validator.Validate(DtoValido()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validador_SinItems_DebeFallar()
    {
        var validator = new CrearEventoOperacionEspecialDtoValidator();
        var dto = DtoValido();
        dto.Items.Clear();
        validator.Validate(dto).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validador_ApendiceConMasDe10_DebeFallar()
    {
        var validator = new CrearEventoOperacionEspecialDtoValidator();
        var dto = DtoValido();
        dto.Apendice = Enumerable.Range(1, 11)
            .Select(i => new ApendiceInputDto { Campo = $"c{i}", Etiqueta = $"e{i}", Valor = $"v{i}" })
            .ToList();
        validator.Validate(dto).IsValid.Should().BeFalse();
    }

    [Fact]
    public void EventoOperacionesEspecialesDto_DefaultsDeIdentificacion()
    {
        var ident = new IdentificacionOperacionEspecialDto();
        ident.Version.Should().Be(1);
        ident.TipoEvento.Should().Be("17");
        ident.TipoMoneda.Should().Be("USD");
    }
}
