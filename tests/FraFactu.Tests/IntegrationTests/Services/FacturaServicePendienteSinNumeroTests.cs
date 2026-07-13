using AutoMapper;
using FraFactu.Application.Common.Interfaces;
using FraFactu.Application.DTOs.Facturas;
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
/// Task 1 (número de control diferido): una factura guardada como pendiente en el
/// MODELO NORMAL (TipoOperacion=1, TipoModelo=1) NO debe reservar número de control;
/// se queda en PENDIENTE_ENVIO con <c>NumeroControl=""</c> y el correlativo se asigna
/// recién al enviarla. El código de generación SÍ se crea desde el inicio.
///
/// El camino de contingencia/diferido (PENDIENTE_LOTE) NO se cubre aquí porque
/// ObtenerSiguienteCorrelativoAsync usa SQL crudo PostgreSQL (SPLIT_PART) que no
/// corre en el proveedor InMemory.
/// </summary>
public class FacturaServicePendienteSinNumeroTests
{
    private const int EmisorId = 10;
    private const int SucursalId = 100;
    private const int CajaId = 1000;
    private const int ReceptorId = 5000;

    private static ApplicationDbContext BuildContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"PendienteSinNumero_{Guid.NewGuid()}")
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

    private static void Seed(ApplicationDbContext ctx)
    {
        ctx.CatAmbientes.Add(new CatAmbienteDestino { Id = 1, Codigo = "00", Valor = "Pruebas" });
        ctx.CatTiposEstablecimiento.Add(new CatTipoEstablecimiento { Id = 1, Codigo = "01", Valor = "Casa Matriz" });
        ctx.CatDocsIdentidadReceptor.Add(new CatTipoDocumentoIdentificacionReceptor { Id = 1, Codigo = "36", Valor = "NIT" });

        ctx.Emisores.Add(new Emisor
        {
            Id = EmisorId,
            Nit = "06140506141011",
            Nrc = "1234567",
            NombreRazonSocial = "EMPRESA DE PRUEBAS SA DE CV",
            NombreComercial = "PRUEBAS SA",
            CodigoActividad = "47111",
            DescripcionActividad = "Venta al por menor",
            CatTipoEstablecimientoId = 1,
            CatAmbienteDestinoId = 1,
            CorreoElectronico = "test@empresa.com",
            Telefono = "22223333",
            Direccion = "Colonia Escalón"
        });

        ctx.Sucursales.Add(new Sucursal
        {
            Id = SucursalId,
            EmisorId = EmisorId,
            Codigo = "0001",
            Nombre = "Sucursal Principal",
            Direccion = "Colonia Escalón",
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
            NombreRazonSocial = "CLIENTE DE PRUEBA",
            Direccion = "Avenida Norte",
            CorreoElectronico = "cliente@correo.com"
        });

        ctx.SaveChanges();
    }

    private static CreateFacturaElectronicaDto BuildDto() => new()
    {
        Identificacion = new IdentificacionDto
        {
            TipoDte = "01",
            TipoModelo = 1,
            TipoOperacion = 1,
            FechaEmision = DateTime.SpecifyKind(new DateTime(2026, 6, 16), DateTimeKind.Utc),
            HoraEmision = "10:00:00"
        },
        ReceptorId = ReceptorId,
        SucursalId = SucursalId,
        CajaId = CajaId,
        CuerpoDocumento = new List<ItemDocumentoDto>
        {
            new()
            {
                NumItem = 1,
                TipoItem = 1,
                Cantidad = 1,
                UniMedida = 1,
                Descripcion = "Producto de prueba",
                PrecioUni = 100,
                VentaGravada = 100,
                IvaItem = 13
            }
        },
        Resumen = new ResumenDto
        {
            TotalGravada = 100,
            TotalPagar = 113,
            CondicionOperacion = 1,
            TotalLetras = "CIENTO TRECE 00/100",
            Pagos = new List<PagoDto>
            {
                new() { CatFormaPagoId = 1, Monto = 113 }
            }
        }
    };

    [Fact]
    public async Task GuardarComoPendiente_ModeloNormal_NoReservaNumeroControl()
    {
        var ctx = BuildContext();
        Seed(ctx);
        var service = BuildService(ctx);

        await service.GuardarComoPendienteAsync(EmisorId, BuildDto());

        // El mapper es un mock no-op (devuelve null), por eso se valida la entidad persistida.
        var factura = await ctx.Facturas.SingleAsync();

        factura.EstadoHacienda.Should().Be("PENDIENTE_ENVIO");
        factura.NumeroControl.Should().BeEmpty();
        factura.CodigoGeneracion.Should().NotBeNullOrEmpty();
    }
}
