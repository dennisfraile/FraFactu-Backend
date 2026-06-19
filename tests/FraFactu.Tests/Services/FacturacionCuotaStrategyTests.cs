using FraFactu.Application.DTOs.Facturas;
using FraFactu.Application.Services;
using FluentAssertions;
using Xunit;

namespace FraFactu.Tests.Services;

public class FacturacionCuotaStrategyTests
{
    private static CreateFacturaElectronicaDto VentaBase(decimal totalConIva)
    {
        var baseSinIva = decimal.Round(totalConIva / 1.13m, 2);
        return new CreateFacturaElectronicaDto
        {
            SucursalId = 1,
            CuerpoDocumento = new List<ItemDocumentoDto>
            {
                new ItemDocumentoDto
                {
                    NumItem = 1, TipoItem = 1, Cantidad = 1, UniMedida = 59,
                    Descripcion = "Producto X", PrecioUni = baseSinIva,
                    VentaGravada = baseSinIva, IvaItem = totalConIva - baseSinIva
                }
            },
            Resumen = new ResumenDto
            {
                CondicionOperacion = 2,
                TotalGravada = baseSinIva,
                TotalIva = totalConIva - baseSinIva,
                SubTotal = baseSinIva,
                TotalPagar = totalConIva,
                Pagos = new List<PagoDto>()
            }
        };
    }

    private readonly FacturacionCuotaStrategy _sut = new();

    [Fact]
    public void CuotaIntermedia_EmiteUnaLineaPorElMontoDeLaCuota()
    {
        var venta = VentaBase(113m); // total $113 (base 100 + IVA 13)

        var dte = _sut.ConstruirDteCuota(
            ventaOriginal: venta, montoCuota: 56.50m, numeroCuota: 1, totalCuotas: 2,
            montoNetoFacturadoPrevio: 0m, catFormaPagoId: 1, referenciaPago: null,
            esCuotaFinal: false);

        dte.CuerpoDocumento.Should().HaveCount(1);
        var linea = dte.CuerpoDocumento[0];
        linea.Descripcion.Should().Contain("Cuota 1/2");
        linea.VentaGravada.Should().Be(50.00m);          // 56.50 / 1.13
        linea.IvaItem.Should().Be(6.50m);                // 56.50 - 50.00
        dte.Resumen.TotalPagar.Should().Be(56.50m);
        dte.Resumen.CondicionOperacion.Should().Be(1);   // se emite como Contado
        dte.Resumen.Pagos.Should().ContainSingle(p => p.Monto == 56.50m && p.CatFormaPagoId == 1);
    }

    [Fact]
    public void CuotaFinal_EmitePorTotalConDescuentoDeLoYaFacturado()
    {
        var venta = VentaBase(113m);

        // Previa: cuota 1 facturó neto 50.00 (sin IVA)
        var dte = _sut.ConstruirDteCuota(
            ventaOriginal: venta, montoCuota: 56.50m, numeroCuota: 2, totalCuotas: 2,
            montoNetoFacturadoPrevio: 50.00m, catFormaPagoId: 1, referenciaPago: null,
            esCuotaFinal: true);

        // Mantiene el detalle completo de ítems (total)
        dte.CuerpoDocumento.Should().HaveCount(1);
        dte.CuerpoDocumento[0].VentaGravada.Should().Be(100.00m);
        // Descuento global gravado por lo ya facturado neto
        dte.Resumen.DescuGravada.Should().Be(50.00m);
        dte.Resumen.TotalDescu.Should().Be(50.00m);
        // Neto del documento = última cuota
        dte.Resumen.TotalGravada.Should().Be(50.00m);
        dte.Resumen.TotalIva.Should().Be(6.50m);
        dte.Resumen.TotalPagar.Should().Be(56.50m);
    }

    [Fact]
    public void CuotaFinal_TresCuotas_DescuentaSumaDeLasDosPrevias()
    {
        var venta = VentaBase(113m); // base 100

        // 3 cuotas; previas facturaron neto 30 + 30 = 60
        var dte = _sut.ConstruirDteCuota(
            ventaOriginal: venta, montoCuota: 45.20m, numeroCuota: 3, totalCuotas: 3,
            montoNetoFacturadoPrevio: 60.00m, catFormaPagoId: 1, referenciaPago: null,
            esCuotaFinal: true);

        dte.Resumen.DescuGravada.Should().Be(60.00m);
        dte.Resumen.TotalGravada.Should().Be(40.00m);   // 100 - 60
        dte.Resumen.TotalIva.Should().Be(5.20m);        // 40 * 0.13
        dte.Resumen.TotalPagar.Should().Be(45.20m);
    }

    [Fact]
    public void CuotaFinal_MontosNoDivisiblesExactos_CuadraExacto()
    {
        // Venta total $113.00 (base 100 + IVA 13). Cuota final con monto no divisible exacto.
        // montoCuota = 37.67 → gravadaNeta = Round(37.67 / 1.13, 2) = 33.34
        //                     → iva = 37.67 - 33.34 = 4.33
        //                     → descuento = Round(100 - 33.34, 2) = 66.66
        var venta = VentaBase(113m);

        var dte = _sut.ConstruirDteCuota(
            ventaOriginal: venta, montoCuota: 37.67m, numeroCuota: 3, totalCuotas: 3,
            montoNetoFacturadoPrevio: 66.66m, catFormaPagoId: 2, referenciaPago: "REF-001",
            esCuotaFinal: true);

        dte.Resumen.TotalPagar.Should().Be(37.67m);
        dte.Resumen.TotalGravada.Should().Be(33.34m);
        dte.Resumen.TotalIva.Should().Be(4.33m);
        dte.Resumen.DescuGravada.Should().Be(66.66m);
        // Cuadre exacto: TotalGravada + TotalIva == TotalPagar
        (dte.Resumen.TotalGravada + dte.Resumen.TotalIva).Should().Be(dte.Resumen.TotalPagar);
    }

    [Fact]
    public void CuotaConMora_AgregaLineaGravadaDeInteres()
    {
        var venta = VentaBase(113m);
        // cuota intermedia 56.50 + mora 1.13 (con IVA)
        var dte = _sut.ConstruirDteCuota(
            ventaOriginal: venta, montoCuota: 56.50m, numeroCuota: 1, totalCuotas: 2,
            montoNetoFacturadoPrevio: 0m, catFormaPagoId: 1, referenciaPago: null,
            esCuotaFinal: false, interesMora: 1.13m);

        dte.CuerpoDocumento.Should().HaveCount(2); // cuota + mora
        var lineaMora = dte.CuerpoDocumento.Last();
        lineaMora.Descripcion.Should().Contain("mora");
        lineaMora.VentaGravada.Should().Be(1.00m);   // 1.13 / 1.13
        lineaMora.IvaItem.Should().Be(0.13m);
        // total = cuota (56.50) + mora (1.13)
        dte.Resumen.TotalPagar.Should().Be(57.63m);
        dte.Resumen.TotalGravada.Should().Be(51.00m); // 50.00 cuota + 1.00 mora
        dte.Resumen.TotalIva.Should().Be(6.63m);      // 6.50 + 0.13
        dte.Resumen.Pagos.Single().Monto.Should().Be(57.63m);
    }

    [Fact]
    public void ConstruirDteCuota_VentaConItemExento_LanzaNotSupportedException()
    {
        // Una venta con un ítem exento no puede procesarse por cuotas en esta versión.
        var venta = new CreateFacturaElectronicaDto
        {
            SucursalId = 1,
            CuerpoDocumento = new List<ItemDocumentoDto>
            {
                new ItemDocumentoDto
                {
                    NumItem = 1, TipoItem = 1, Cantidad = 1, UniMedida = 59,
                    Descripcion = "Producto exento", PrecioUni = 50.00m,
                    VentaGravada = 0m, VentaExenta = 50.00m, IvaItem = 0m
                }
            },
            Resumen = new ResumenDto
            {
                CondicionOperacion = 2,
                TotalGravada = 0m,
                TotalIva = 0m,
                SubTotal = 50.00m,
                TotalPagar = 50.00m,
                Pagos = new List<PagoDto>()
            }
        };

        var act = () => _sut.ConstruirDteCuota(
            ventaOriginal: venta, montoCuota: 25.00m, numeroCuota: 1, totalCuotas: 2,
            montoNetoFacturadoPrevio: 0m, catFormaPagoId: 1, referenciaPago: null,
            esCuotaFinal: false);

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*100% gravadas*");
    }
}
