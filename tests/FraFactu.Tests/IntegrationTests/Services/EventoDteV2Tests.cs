using System.Text.Json;
using FraFactu.Application.DTOs.Hacienda;
using FraFactu.Application.Interfaces;
using FraFactu.Application.Interfaces.Hacienda;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Entities.Catalogos;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FraFactu.Tests.IntegrationTests.Services;

/// <summary>
/// Tests del JSON de eventos para la Normativa DTE V2.0:
/// Invalidación v3 (esquema invalidacion-schema-v3.json) y Contingencia v4
/// (esquema contingencia-schema-v4.json). Verifican los cambios estructurales:
/// Invalidación → fecAnula/horAnula renombrados a fecEmi/horEmi, +fusion, emisor sin
/// tipoEstablecimiento/nomEstablecimiento, documento sin montoIva, version 3.
/// Contingencia → version 4, emisor con codPuntoVentaMH (no codPuntoVenta), nulls explícitos.
/// </summary>
public class EventoDteV2Tests
{
    private const int EmisorId = 20;
    private const int SucursalId = 200;
    private const int CajaId = 2000;
    private const int FacturaId = 8000;
    private const int EventoId = 7000;

    private static ApplicationDbContext BuildContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"EventoV2_{Guid.NewGuid()}")
            .Options);

    private static EventoContingenciaService BuildService(ApplicationDbContext ctx) =>
        new(
            ctx,
            Mock.Of<IHaciendaApiService>(),
            Mock.Of<IServiceProvider>(),
            NullLogger<EventoContingenciaService>.Instance,
            Mock.Of<ISmartCareWebhookService>());

    // ==========================================
    // CONTINGENCIA v4
    // ==========================================

    /// <summary>
    /// El JSON del evento de contingencia debe declarar version 4, emitir el punto de
    /// venta bajo la clave codPuntoVentaMH (con el código asignado por el MH) y NO bajo
    /// codPuntoVenta, y escribir los nulls de forma explícita (motivoContingencia: null).
    /// </summary>
    [Fact]
    public async Task GenerarJsonContingencia_DebeCumplirEsquemaV4()
    {
        // Arrange
        using var ctx = BuildContext();
        SeedContingencia(ctx);
        var service = BuildService(ctx);

        // Act
        var json = await service.GenerarJsonEventoAsync(EventoId, EmisorId);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        root.GetProperty("identificacion").GetProperty("version").GetInt32().Should().Be(4);

        var emisor = root.GetProperty("emisor");
        emisor.TryGetProperty("codPuntoVentaMH", out var codPv).Should().BeTrue("v4 renombra codPuntoVenta → codPuntoVentaMH");
        codPv.GetString().Should().Be("0001", "debe llevar el código de punto de venta asignado por el MH");
        emisor.TryGetProperty("codPuntoVenta", out _).Should().BeFalse("la clave codPuntoVenta ya no existe en v4");

        // Nulls explícitos: la contingencia tipo 2 no lleva motivo y debe emitirse como null
        var motivo = root.GetProperty("motivo");
        motivo.TryGetProperty("motivoContingencia", out var motivoVal).Should().BeTrue("los nulls deben escribirse, no omitirse");
        motivoVal.ValueKind.Should().Be(JsonValueKind.Null);
    }

    /// <summary>El DTO de contingencia debe declarar version 4 por defecto.</summary>
    [Fact]
    public void EventoContingenciaDto_VersionPorDefecto_DebeSer4()
    {
        new IdentificacionContingenciaDto().Version.Should().Be(4);
    }

    // ==========================================
    // INVALIDACIÓN v3
    // ==========================================

    /// <summary>
    /// El JSON del evento de invalidación debe cumplir el esquema v3: version 3, fecEmi/horEmi
    /// (no fecAnula/horAnula), incluir fusion, y NO incluir tipoEstablecimiento/nomEstablecimiento
    /// en el emisor ni montoIva en el documento.
    /// </summary>
    [Fact]
    public void SerializarInvalidacion_DebeCumplirEsquemaV3()
    {
        // Arrange: se construye tal como FacturaService.InvalidarFacturaAsync
        var evento = new EventoInvalidacionDto
        {
            Identificacion = new IdentificacionInvalidacionDto
            {
                Version = 3,
                Ambiente = "00",
                CodigoGeneracion = "A1B2C3D4-E5F6-7890-ABCD-EF1234567890",
                FecEmi = "2026-05-29",
                HorEmi = "10:30:00",
                Fusion = null
            },
            Emisor = new EmisorInvalidacionDto
            {
                Nit = "06140506141011",
                Nombre = "EMPRESA DE PRUEBAS SA DE CV",
                CodEstableMH = "0001",
                CodEstable = "01",
                CodPuntoVentaMH = "0001",
                CodPuntoVenta = "01",
                Telefono = "22223333",
                Correo = "test@empresa.com"
            },
            Documento = new DocumentoInvalidacionDto
            {
                TipoDte = "01",
                CodigoGeneracion = "11111111-2222-3333-4444-555555555555",
                SelloRecibido = new string('A', 40),
                NumeroControl = "DTE-01-M001P001-000000000000001",
                FecEmi = "2026-05-28",
                TipoDocumento = "36",
                NumDocumento = "06140506141011",
                Nombre = "RECEPTOR SA"
            },
            Motivo = new MotivoInvalidacionDto
            {
                TipoAnulacion = 2,
                NombreResponsable = "Juan Pérez López",
                TipDocResponsable = "13",
                NumDocResponsable = "012345678",
                NombreSolicita = "María García",
                TipDocSolicita = "13",
                NumDocSolicita = "087654321"
            }
        };

        // Act: misma serialización por defecto que usa HaciendaApiService.AnularDteAsync
        var json = JsonSerializer.Serialize(evento);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        var ident = root.GetProperty("identificacion");
        ident.GetProperty("version").GetInt32().Should().Be(3);
        ident.TryGetProperty("fecEmi", out _).Should().BeTrue("v3 renombra fecAnula → fecEmi");
        ident.TryGetProperty("horEmi", out _).Should().BeTrue("v3 renombra horAnula → horEmi");
        ident.TryGetProperty("fusion", out var fusion).Should().BeTrue("fusion es nuevo y requerido en v3");
        fusion.ValueKind.Should().Be(JsonValueKind.Null);
        ident.TryGetProperty("fecAnula", out _).Should().BeFalse();
        ident.TryGetProperty("horAnula", out _).Should().BeFalse();

        var emisor = root.GetProperty("emisor");
        emisor.TryGetProperty("tipoEstablecimiento", out _).Should().BeFalse("v3 elimina tipoEstablecimiento del emisor");
        emisor.TryGetProperty("nomEstablecimiento", out _).Should().BeFalse("v3 elimina nomEstablecimiento del emisor");

        var documento = root.GetProperty("documento");
        documento.TryGetProperty("montoIva", out _).Should().BeFalse("v3 elimina montoIva del documento");
    }

    /// <summary>El DTO de invalidación debe declarar version 3 por defecto.</summary>
    [Fact]
    public void EventoInvalidacionDto_VersionPorDefecto_DebeSer3()
    {
        new IdentificacionInvalidacionDto().Version.Should().Be(3);
    }

    // ==========================================
    // SEED
    // ==========================================

    private static void SeedContingencia(ApplicationDbContext ctx)
    {
        ctx.CatTiposEstablecimiento.Add(new CatTipoEstablecimiento { Id = 1, Codigo = "01", Valor = "Casa Matriz" });
        ctx.CatTiposDocumento.Add(new CatTipoDocumento { Id = 1, Codigo = "01", Valor = "Factura" });
        ctx.CatDocsIdentidadReceptor.Add(new CatTipoDocumentoIdentificacionReceptor { Id = 1, Codigo = "36", Valor = "NIT" });

        ctx.Emisores.Add(new Emisor
        {
            Id = EmisorId,
            Nit = "06140506141011",
            NombreRazonSocial = "EMPRESA DE PRUEBAS SA DE CV",
            NombreComercial = "PRUEBAS SA",
            CorreoElectronico = "test@empresa.com",
            Telefono = "22223333"
        });

        ctx.Sucursales.Add(new Sucursal
        {
            Id = SucursalId,
            Codigo = "01",
            CodigoEstablecimiento = "0001",
            Telefono = "22224444",
            CorreoElectronico = "sucursal@empresa.com"
        });

        ctx.Cajas.Add(new Caja
        {
            Id = CajaId,
            SucursalId = SucursalId,
            Codigo = "PV01",
            CodPuntoVenta = "01",
            CodPuntoVentaMH = "0001"
        });

        ctx.Facturas.Add(new FacturaElectronica
        {
            Id = FacturaId,
            EmisorId = EmisorId,
            SucursalId = SucursalId,
            CajaId = CajaId,
            CatTipoDocumentoId = 1,
            CodigoGeneracion = "11111111-2222-3333-4444-555555555555"
        });

        var evento = new EventoContingencia
        {
            Id = EventoId,
            EmisorId = EmisorId,
            Version = 4,
            Ambiente = "00",
            CodigoGeneracion = "A1B2C3D4-E5F6-7890-ABCD-EF1234567890",
            FechaTransmision = new DateTime(2026, 5, 29),
            HoraTransmision = new TimeSpan(10, 30, 0),
            NombreResponsable = "Juan Pérez López",
            CatTipoDocResponsableId = 1,
            NumeroDocResponsable = "012345678",
            CatTipoEstablecimientoId = 1,
            CodigoEstablecimientoMH = "0001",
            FechaInicioContingencia = new DateTime(2026, 5, 29),
            FechaFinContingencia = new DateTime(2026, 5, 29),
            HoraInicioContingencia = new TimeSpan(8, 0, 0),
            HoraFinContingencia = new TimeSpan(9, 0, 0),
            TipoContingencia = 2,
            MotivoContingencia = null,
            EstadoHacienda = "PENDIENTE"
        };
        evento.Detalles.Add(new ContingenciaDetalle
        {
            Id = 1,
            EventoContingenciaId = EventoId,
            FacturaElectronicaId = FacturaId,
            NoItem = 1,
            CodigoGeneracion = "11111111-2222-3333-4444-555555555555",
            CatTipoDocumentoId = 1
        });
        ctx.EventosContingencia.Add(evento);

        ctx.SaveChanges();
    }
}
