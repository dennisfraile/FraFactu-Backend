using AutoMapper;
using FraFactu.Application.Common.Interfaces;
using FraFactu.Application.Interfaces;
using FraFactu.Application.Interfaces.Hacienda;
using FraFactu.Application.Services;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Helpers;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FraFactu.Tests.UnitTests.Services;

/// <summary>
/// Tests del follow-up del bug de producción (rechazo MH [020] "totalGravada CALCULO INCORRECTO"):
/// - Red de seguridad en el backend que resincroniza el resumen con los ítems recalculados.
/// - Helper RecalcularGravadoSiAplica (origen del descuadre: escala/respeta según precioIncluyeIva).
/// </summary>
public class FacturaResyncResumenTests
{
    private static FacturaService BuildService()
    {
        var ctx = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Resync_{Guid.NewGuid()}").Options);
        return new FacturaService(
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
            Mock.Of<ITelemetryService>());
    }

    private static FacturaElectronica FacturaConItem(decimal totalGravadoResumen, decimal ventaGravadaItem,
        decimal ivaItem)
    {
        return new FacturaElectronica
        {
            NumeroControl = "DTE-01-M001P001-000000000000001",
            // Resumen cargado "crudo del DTO".
            TotalGravado = totalGravadoResumen,
            SubTotalVentas = totalGravadoResumen,
            SubTotal = totalGravadoResumen,
            MontoTotalOperacion = totalGravadoResumen,
            TotalPagar = totalGravadoResumen,
            TotalIva = 0m,
            Detalles = new List<FacturaElectronicaDetalle>
            {
                new() { NumeroItem = 1, Cantidad = 1, VentaGravada = ventaGravadaItem, IvaItem = ivaItem }
            }
        };
    }

    // ─────────────────────────────── Resync (red de seguridad) ───────────────────────────────

    [Fact]
    public void Resync_FE_DescuadreGravado_ResincronizaResumen()
    {
        // Caso real del bug: ítem quedó en 3390 (con IVA) y el resumen llegó en 3000 (base).
        var factura = FacturaConItem(totalGravadoResumen: 3000m, ventaGravadaItem: 3390m, ivaItem: 390m);

        BuildService().ResincronizarResumenConDetalles(factura, "01");

        factura.TotalGravado.Should().Be(3390m);          // cuadra con el ítem
        factura.SubTotalVentas.Should().Be(3390m);
        factura.SubTotal.Should().Be(3390m);
        factura.MontoTotalOperacion.Should().Be(3390m);
        factura.TotalPagar.Should().Be(3390m);
        factura.TotalIva.Should().Be(390m);               // tomado de los ítems
    }

    [Fact]
    public void Resync_FE_YaCuadra_NoModificaNada()
    {
        var factura = FacturaConItem(totalGravadoResumen: 3390m, ventaGravadaItem: 3390m, ivaItem: 390m);

        BuildService().ResincronizarResumenConDetalles(factura, "01");

        factura.TotalGravado.Should().Be(3390m);
        factura.MontoTotalOperacion.Should().Be(3390m);   // sin cambios
    }

    [Fact]
    public void Resync_CCF_Descuadre_NoAutoCorrige()
    {
        // Para CCF no se auto-corrige (la propagación del IVA es distinta): solo alerta.
        var factura = FacturaConItem(totalGravadoResumen: 3000m, ventaGravadaItem: 3390m, ivaItem: 440.70m);

        BuildService().ResincronizarResumenConDetalles(factura, "03");

        factura.TotalGravado.Should().Be(3000m);           // NO se modifica
        factura.MontoTotalOperacion.Should().Be(3000m);
    }

    // ─────────────────────────────── Helper RecalcularGravadoSiAplica (origen del bug) ───────────────────────────────

    [Fact]
    public void Recalc_FE_PrecioSinIvaFlagFalse_EscalaPorIva()
    {
        // El bug: precio base 3000 con precioIncluyeIva=false → el BE escala a 3390 (con IVA).
        var r = FacturaCalculosHelper.RecalcularGravadoSiAplica("01", 3000m, 1m, 0m, 0m, 0m, precioIncluyeIva: false);

        r.Should().NotBeNull();
        r!.Value.precioUni.Should().Be(3390m);
        r.Value.ventaGravada.Should().Be(3390m);
    }

    [Fact]
    public void Recalc_FE_PrecioConIvaFlagTrue_RespetaPrecio()
    {
        // El fix del front: precio 3390 con precioIncluyeIva=true → no se re-escala.
        var r = FacturaCalculosHelper.RecalcularGravadoSiAplica("01", 3390m, 1m, 0m, 0m, 0m, precioIncluyeIva: true);

        r.Should().NotBeNull();
        r!.Value.ventaGravada.Should().Be(3390m);
    }

    [Fact]
    public void Recalc_CCF_PrecioConIva_DevuelveBaseSinIva()
    {
        // CCF: base sin IVA. 3390 con IVA → base 3000.
        var r = FacturaCalculosHelper.RecalcularGravadoSiAplica("03", 3390m, 1m, 0m, 0m, 0m, precioIncluyeIva: true);

        r.Should().NotBeNull();
        r!.Value.ventaGravada.Should().Be(3000m);
        r.Value.ivaItem.Should().Be(390m);
    }

    [Fact]
    public void Recalc_SinFlag_DevuelveNull_RespetandoValoresDelFE()
    {
        // Legacy (sin flag): no recalcula, el caller usa lo que mandó el FE.
        var r = FacturaCalculosHelper.RecalcularGravadoSiAplica("01", 3000m, 1m, 0m, 0m, 0m, precioIncluyeIva: null);

        r.Should().BeNull();
    }
}
