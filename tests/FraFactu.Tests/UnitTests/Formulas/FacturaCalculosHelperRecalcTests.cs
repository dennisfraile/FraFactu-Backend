using FraFactu.Infrastructure.Helpers;
using Xunit;

namespace FraFactu.Tests.UnitTests.Formulas
{
    /// <summary>
    /// Plan C2 (2026-05-19, hotfix 2026-05-19) — Recalculo defensivo DTE-aware.
    /// Garantiza que se cumpla el invariante MH
    /// <c>VentaGravada == PrecioUni × Cantidad - Descuento</c>:
    ///   - CF (Tipo 01): PrecioUni y VentaGravada en GROSS. IvaItem inverso.
    ///   - CCF (Tipo 03) y otros: PrecioUni y VentaGravada en BASE. IvaItem = base * 0.13.
    /// </summary>
    public class FacturaCalculosHelperRecalcTests
    {
        [Fact]
        public void FlagAusente_DevuelveNull_PreservaComportamientoLegacy()
        {
            var result = FacturaCalculosHelper.RecalcularGravadoSiAplica(
                tipoDte: "01",
                precioUni: 100m, cantidad: 1m, montoDescuento: 0m,
                ventaNoSujeta: 0m, ventaExenta: 0m, precioIncluyeIva: null);
            Assert.Null(result);
        }

        [Fact]
        public void CF_FlagTrue_MantienePrecioUniGrossYCalculaVentaGravadaGross()
        {
            // PrecioUni 100 ya es gross (flag=true). CF requiere PrecioUni y
            // VentaGravada en gross. PrecioUni queda 100, VentaGravada = 100.
            // IVA embebido = (100/1.13)*0.13 = 11.50.
            var result = FacturaCalculosHelper.RecalcularGravadoSiAplica(
                tipoDte: "01",
                precioUni: 100m, cantidad: 1m, montoDescuento: 0m,
                ventaNoSujeta: 0m, ventaExenta: 0m, precioIncluyeIva: true);

            Assert.NotNull(result);
            Assert.Equal(100m, result!.Value.precioUni);
            Assert.Equal(100m, result.Value.ventaGravada);
            Assert.Equal(11.50m, result.Value.ivaItem);
        }

        [Fact]
        public void CF_FlagFalse_GrossUpPrecioUniYVentaGravada()
        {
            // PrecioUni 100 es base (flag=false). CF necesita gross.
            // PrecioUni final = 100 * 1.13 = 113. VentaGravada = 113.
            var result = FacturaCalculosHelper.RecalcularGravadoSiAplica(
                tipoDte: "01",
                precioUni: 100m, cantidad: 1m, montoDescuento: 0m,
                ventaNoSujeta: 0m, ventaExenta: 0m, precioIncluyeIva: false);

            Assert.NotNull(result);
            Assert.Equal(113m, result!.Value.precioUni);
            Assert.Equal(113m, result.Value.ventaGravada);
            Assert.Equal(13m, result.Value.ivaItem);
        }

        [Fact]
        public void CCF_FlagTrue_ConviertePrecioUniABaseYCalculaVentaGravadaBase()
        {
            // PrecioUni 113 con IVA. CCF necesita base.
            // PrecioUni final = 113/1.13 = 100. VentaGravada = 100. IVA = 13.
            var result = FacturaCalculosHelper.RecalcularGravadoSiAplica(
                tipoDte: "03",
                precioUni: 113m, cantidad: 1m, montoDescuento: 0m,
                ventaNoSujeta: 0m, ventaExenta: 0m, precioIncluyeIva: true);

            Assert.NotNull(result);
            Assert.Equal(100m, result!.Value.precioUni);
            Assert.Equal(100m, result.Value.ventaGravada);
            Assert.Equal(13m, result.Value.ivaItem);
        }

        [Fact]
        public void CCF_FlagFalse_MantienePrecioUniBaseYCalculaIVA()
        {
            // PrecioUni 100 ya es base (flag=false). CCF lo deja igual.
            var result = FacturaCalculosHelper.RecalcularGravadoSiAplica(
                tipoDte: "03",
                precioUni: 100m, cantidad: 1m, montoDescuento: 0m,
                ventaNoSujeta: 0m, ventaExenta: 0m, precioIncluyeIva: false);

            Assert.NotNull(result);
            Assert.Equal(100m, result!.Value.precioUni);
            Assert.Equal(100m, result.Value.ventaGravada);
            Assert.Equal(13m, result.Value.ivaItem);
        }

        [Fact]
        public void InvarianteMH_CF_VentaGravadaIgualPrecioUniPorCantidadMenosDescuento()
        {
            // El invariante critico de MH: VentaGravada == PrecioUni × Cantidad - Descuento.
            // Sin este invariante, MH rechaza con "[003] El calculo de total por item es erroneo".
            foreach (var (precioInput, flag, cantidad, descuento) in new[] {
                (100m, true,  1m, 0m),
                (50m,  true,  2m, 10m),
                (100m, false, 1m, 0m),
                (50m,  false, 3m, 5m),
            })
            {
                var result = FacturaCalculosHelper.RecalcularGravadoSiAplica(
                    tipoDte: "01",
                    precioUni: precioInput, cantidad: cantidad, montoDescuento: descuento,
                    ventaNoSujeta: 0m, ventaExenta: 0m, precioIncluyeIva: flag);

                Assert.NotNull(result);
                var expectedVenta = decimal.Round(result!.Value.precioUni * cantidad, 2) - descuento;
                if (expectedVenta < 0) expectedVenta = 0;
                Assert.Equal(expectedVenta, result.Value.ventaGravada);
            }
        }

        [Fact]
        public void InvarianteMH_CCF_VentaGravadaIgualPrecioUniPorCantidadMenosDescuento()
        {
            foreach (var (precioInput, flag, cantidad, descuento) in new[] {
                (113m, true,  1m, 0m),
                (113m, true,  2m, 10m),
                (100m, false, 1m, 0m),
                (50m,  false, 3m, 5m),
            })
            {
                var result = FacturaCalculosHelper.RecalcularGravadoSiAplica(
                    tipoDte: "03",
                    precioUni: precioInput, cantidad: cantidad, montoDescuento: descuento,
                    ventaNoSujeta: 0m, ventaExenta: 0m, precioIncluyeIva: flag);

                Assert.NotNull(result);
                var expectedVenta = decimal.Round(result!.Value.precioUni * cantidad, 2) - descuento;
                if (expectedVenta < 0) expectedVenta = 0;
                Assert.Equal(expectedVenta, result.Value.ventaGravada);
            }
        }

        [Fact]
        public void CFyCCF_MismoBaseSemantico_ProducenMismoTotalFiscal()
        {
            // Invariante Plan C2: el mismo producto semantico (mismo base $100)
            // emitido como CF (gross input $113 + flag=true) y como CCF (base $100
            // + flag=false) cierran con el MISMO total fiscal = $113.
            var cf = FacturaCalculosHelper.RecalcularGravadoSiAplica(
                tipoDte: "01",
                precioUni: 113m, cantidad: 1m, montoDescuento: 0m,
                ventaNoSujeta: 0m, ventaExenta: 0m, precioIncluyeIva: true);
            var ccf = FacturaCalculosHelper.RecalcularGravadoSiAplica(
                tipoDte: "03",
                precioUni: 100m, cantidad: 1m, montoDescuento: 0m,
                ventaNoSujeta: 0m, ventaExenta: 0m, precioIncluyeIva: false);

            Assert.NotNull(cf);
            Assert.NotNull(ccf);
            // CF total = VentaGravada (ya incluye IVA). CCF total = VentaGravada + IvaItem.
            decimal cfTotal = cf!.Value.ventaGravada;
            decimal ccfTotal = ccf!.Value.ventaGravada + ccf.Value.ivaItem;
            Assert.Equal(cfTotal, ccfTotal);
        }

        [Fact]
        public void VentaNoSujetaOExenta_NoActivaRecalc()
        {
            var conNoSujeta = FacturaCalculosHelper.RecalcularGravadoSiAplica(
                tipoDte: "01",
                precioUni: 113m, cantidad: 1m, montoDescuento: 0m,
                ventaNoSujeta: 113m, ventaExenta: 0m, precioIncluyeIva: true);
            var conExenta = FacturaCalculosHelper.RecalcularGravadoSiAplica(
                tipoDte: "03",
                precioUni: 113m, cantidad: 1m, montoDescuento: 0m,
                ventaNoSujeta: 0m, ventaExenta: 113m, precioIncluyeIva: true);
            Assert.Null(conNoSujeta);
            Assert.Null(conExenta);
        }

        [Fact]
        public void DescuentoMayorAlSubtotal_DevuelveCeroNoNegativo()
        {
            var result = FacturaCalculosHelper.RecalcularGravadoSiAplica(
                tipoDte: "03",
                precioUni: 100m, cantidad: 1m, montoDescuento: 999m,
                ventaNoSujeta: 0m, ventaExenta: 0m, precioIncluyeIva: false);
            Assert.NotNull(result);
            Assert.Equal(0m, result!.Value.ventaGravada);
            Assert.Equal(0m, result.Value.ivaItem);
        }
    }
}
