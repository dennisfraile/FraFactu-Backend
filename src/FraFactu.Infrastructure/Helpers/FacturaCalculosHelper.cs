using FraFactu.Application.DTOs.Facturas;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Helpers
{
    /// <summary>
    /// Ayudante para cálculos de facturación según estándares de Hacienda
    /// </summary>
    public static class FacturaCalculosHelper
    {
        /// <summary>
        /// Calcula el IVA para un monto gravado (13%)
        /// </summary>
        public static decimal CalcularIVA(decimal montoGravado, decimal tasaIVA = 0.13m)
        {
            return decimal.Round(montoGravado * tasaIVA, 2);
        }

        /// <summary>
        /// Plan C2 (2026-05-19, hotfix 2026-05-19) — Recalcula <c>PrecioUni</c>,
        /// <c>VentaGravada</c> e <c>IvaItem</c> a partir del precio recibido y el flag
        /// <paramref name="precioIncluyeIva"/>. Garantiza que CF (Tipo 01) y CCF
        /// (Tipo 03) emitidos con el mismo precio semantico terminen con el MISMO
        /// total fiscal, RESPETANDO el invariante de Hacienda
        /// <c>VentaGravada == PrecioUni × Cantidad - MontoDescuento</c>:
        ///   - CF: PrecioUni y VentaGravada son GROSS (con IVA). IvaItem es el IVA
        ///     embebido (inverso).
        ///   - CCF y otros: PrecioUni y VentaGravada son BASE (sin IVA). IvaItem es
        ///     base * 0.13.
        ///
        /// Devuelve null si el item no es gravado o si <paramref name="precioIncluyeIva"/>
        /// no fue especificado (legacy). El caller debe seguir usando los valores que
        /// envio el FE en esos casos para no romper clientes antiguos.
        /// </summary>
        public static (decimal precioUni, decimal ventaGravada, decimal ivaItem)? RecalcularGravadoSiAplica(
            string tipoDte,
            decimal precioUni,
            decimal cantidad,
            decimal montoDescuento,
            decimal ventaNoSujeta,
            decimal ventaExenta,
            bool? precioIncluyeIva)
        {
            // Solo activamos el recalc si el FE mando el flag y el item es gravado.
            if (!precioIncluyeIva.HasValue) return null;
            if (ventaNoSujeta != 0 || ventaExenta != 0) return null;
            if (cantidad <= 0 || precioUni <= 0) return null;

            if (tipoDte == "01")
            {
                // CF: PrecioUni y VentaGravada deben ser GROSS para que MH valide
                // VentaGravada == PrecioUni × Cantidad - Descuento.
                decimal grossUnit = precioIncluyeIva.Value
                    ? precioUni
                    : decimal.Round(precioUni * 1.13m, 2);
                decimal ventaGravada = decimal.Round(grossUnit * cantidad, 2) - montoDescuento;
                if (ventaGravada < 0) ventaGravada = 0;
                // IVA embebido: base = gross / 1.13; iva = base * 0.13.
                decimal iva = decimal.Round((ventaGravada / 1.13m) * 0.13m, 2);
                return (grossUnit, ventaGravada, iva);
            }
            else
            {
                // CCF (03) y otros: PrecioUni y VentaGravada deben ser BASE.
                decimal baseUnit = precioIncluyeIva.Value
                    ? decimal.Round(precioUni / 1.13m, 8)
                    : precioUni;
                decimal ventaGravada = decimal.Round(baseUnit * cantidad, 2) - montoDescuento;
                if (ventaGravada < 0) ventaGravada = 0;
                decimal iva = decimal.Round(ventaGravada * 0.13m, 2);
                return (baseUnit, ventaGravada, iva);
            }
        }

        /// <summary>
        /// Calcula el total de un ítem con descuento
        /// </summary>
        public static decimal CalcularTotalItem(decimal cantidad, decimal precioUnitario, decimal descuento = 0)
        {
            decimal subtotal = cantidad * precioUnitario;
            decimal conDescuento = subtotal - descuento;
            return conDescuento;
        }

        /// <summary>
        /// Calcula automáticamente el resumen de una factura basado en sus ítems
        /// </summary>
        public static ResumenDto CalcularResumen(
            List<ItemDocumentoDto> items,
            int condicionOperacion = 1,
            List<PagoDto>? pagos = null,
            decimal porcentajeDescuentoGlobal = 0.00m)
        {
            if (items == null || items.Count == 0)
                throw new ArgumentException("La factura debe tener al menos un ítem");

            // Calcular totales por tipo de venta
            decimal totalNoSuj = items.Sum(i => i.VentaNoSuj);
            decimal totalExenta = items.Sum(i => i.VentaExenta);
            decimal totalGravada = items.Sum(i => i.VentaGravada);
            decimal totalDescuentos = items.Sum(i => i.MontoDescuento ?? 0);
            decimal totalIva = items.Sum(i => i.IvaItem);

            decimal subTotalVentas = totalNoSuj + totalExenta + totalGravada;
            decimal subTotal = subTotalVentas - totalDescuentos;
            decimal montoTotalOperacion = subTotal + totalIva;

            // Generar lista de pagos por defecto si no se proporcionó
            var pagosList = pagos ?? new List<PagoDto>
            {
                new PagoDto
                {
                    CatFormaPagoId = 1, // Efectivo por defecto
                    Monto = montoTotalOperacion
                }
            };

            // Crear resumen
            var resumen = new ResumenDto
            {
                TotalNoSuj = totalNoSuj,
                TotalExenta = totalExenta,
                TotalGravada = totalGravada,
                TotalDescu = totalDescuentos,
                SubTotal = subTotal,
                TotalIva = totalIva,
                IvaRete1 = 0.00m,
                ReteRenta = 0.00m,
                MontoTotalOperacion = montoTotalOperacion,
                TotalPagar = montoTotalOperacion,
                TotalLetras = ConvertirMontoALetras(montoTotalOperacion),
                TotalIvaPerc = null,
                CondicionOperacion = condicionOperacion,
                Pagos = pagosList,
                NumPagoElectronico = null
            };

            return resumen;
        }

        /// <summary>
        /// Valida que el IVA de la factura sea correcto
        /// </summary>
        public static bool ValidarIVA(List<ItemDocumentoDto> items, decimal totalIvaResumen, decimal tolerancia = 0.01m)
        {
            if (items == null || items.Count == 0)
                return false;

            decimal ivaCalculado = items.Sum(i => i.IvaItem);
            return Math.Abs(ivaCalculado - totalIvaResumen) <= tolerancia;
        }

        /// <summary>
        /// Valida que el total gravado sea correcto
        /// </summary>
        public static bool ValidarTotalGravado(List<ItemDocumentoDto> items, decimal totalGravadoResumen, decimal tolerancia = 0.01m)
        {
            if (items == null || items.Count == 0)
                return false;

            decimal totalGravadoCalculado = items.Sum(i => i.VentaGravada);
            return Math.Abs(totalGravadoCalculado - totalGravadoResumen) <= tolerancia;
        }

        /// <summary>
        /// Valida que los descuentos no excedan el total
        /// </summary>
        public static bool ValidarDescuentos(List<ItemDocumentoDto> items, decimal totalDescuentos)
        {
            if (items == null || items.Count == 0)
                return false;

            decimal totalVentas = items.Sum(i => i.VentaGravada + i.VentaExenta + i.VentaNoSuj);
            return totalDescuentos <= totalVentas;
        }

        /// <summary>
        /// Calcula el porcentaje de descuento
        /// </summary>
        public static decimal CalcularPorcentajeDescuento(decimal totalSinDescuento, decimal totalDescuento)
        {
            if (totalSinDescuento == 0)
                return 0;

            return decimal.Round((totalDescuento / totalSinDescuento) * 100, 2);
        }

        /// <summary>
        /// Convierte un monto a letras (formato de Hacienda)
        /// </summary>
        public static string ConvertirMontoALetras(decimal monto)
        {
            // Por ahora retornamos formato simple, se puede mejorar con librería especializada
            int dolares = (int)Math.Floor(monto);
            int centavos = (int)Math.Round((monto - dolares) * 100);

            string resultado = $"{NumeroALetras(dolares)} DÓLARES";

            if (centavos > 0)
            {
                resultado += $" CON {centavos:D2}/100";
            }
            else
            {
                resultado += " CON 00/100";
            }

            return resultado.ToUpper();
        }

        /// <summary>
        /// Convierte un número a letras (simplificado)
        /// </summary>
        private static string NumeroALetras(int numero)
        {
            if (numero == 0) return "CERO";
            if (numero == 1) return "UN";
            if (numero < 0) return "MENOS " + NumeroALetras(Math.Abs(numero));

            string letras = "";

            if (numero >= 1000000)
            {
                int millones = numero / 1000000;
                letras += (millones == 1 ? "UN MILLÓN" : NumeroALetras(millones) + " MILLONES");
                numero %= 1000000;
                if (numero > 0) letras += " ";
            }

            if (numero >= 1000)
            {
                int miles = numero / 1000;
                letras += (miles == 1 ? "MIL" : NumeroALetras(miles) + " MIL");
                numero %= 1000;
                if (numero > 0) letras += " ";
            }

            if (numero >= 100)
            {
                int centenas = numero / 100;
                letras += Centenas(centenas);
                numero %= 100;
                if (numero > 0) letras += " ";
            }

            if (numero >= 10)
            {
                int decenas = numero / 10;
                letras += Decenas(decenas, numero % 10);
            }
            else if (numero > 0)
            {
                letras += Unidades(numero);
            }

            return letras.Trim();
        }

        private static string Unidades(int numero)
        {
            string[] unidades = { "", "UNO", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE" };
            return unidades[numero];
        }

        private static string Decenas(int decena, int unidad)
        {
            if (decena == 1)
            {
                string[] especiales = { "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE", "DIECISÉIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE" };
                return especiales[unidad];
            }

            string[] decenas = { "", "", "VEINTE", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA" };

            if (unidad == 0)
                return decenas[decena];

            if (decena == 2)
                return "VEINTI" + Unidades(unidad);

            return decenas[decena] + " Y " + Unidades(unidad);
        }

        private static string Centenas(int centena)
        {
            if (centena == 1) return "CIEN";
            if (centena == 5) return "QUINIENTOS";
            if (centena == 7) return "SETECIENTOS";
            if (centena == 9) return "NOVECIENTOS";

            string[] centenas = { "", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS",
                                  "QUINIENTOS", "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS" };
            return centenas[centena];
        }

        /// <summary>
        /// Obtiene el nombre de la forma de pago según catálogo de Hacienda
        /// </summary>
        public static string ObtenerNombreFormaPago(int codigo)
        {
            return codigo switch
            {
                1 => "Efectivo",
                2 => "Tarjeta de Débito",
                3 => "Tarjeta de Crédito",
                4 => "Cheque",
                5 => "Transferencia Bancaria",
                6 => "Depósito Bancario",
                99 => "Otro",
                _ => "Desconocido"
            };
        }
    }
}
