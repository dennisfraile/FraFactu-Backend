using System.Text.Json.Serialization;

namespace FraFactu.Application.DTOs.Hacienda
{
    /// <summary>
    /// Detalle de un ítem/línea del documento - máximo 2000 items por DTE
    /// </summary>
    public class CuerpoDocumentoDto
    {
        /// <summary>
        /// Número correlativo del ítem (1-2000)
        /// </summary>
        [JsonPropertyName("numItem")]
        public int NumItem { get; set; }

        /// <summary>
        /// Clasificación del ítem: 1=Bien, 2=Servicio, 3=Ambos, 4=Impuesto - FK a CatTipoItem
        /// </summary>
        [JsonPropertyName("tipoItem")]
        public int TipoItem { get; set; }

        /// <summary>
        /// Número de documento relacionado por ítem (opcional)
        /// Según MH JSON schema: type ["string", "null"] - código de generación del doc relacionado
        /// </summary>
        [JsonPropertyName("numeroDocumento")]
        public string? NumeroDocumento { get; set; }

        /// <summary>
        /// Cantidad del ítem
        /// </summary>
        [JsonPropertyName("cantidad")]
        public double Cantidad { get; set; }

        /// <summary>
        /// Código interno del producto/servicio (maxLength: 25)
        /// </summary>
        [JsonPropertyName("codigo")]
        public string Codigo { get; set; } = string.Empty;

        /// <summary>
        /// Código de tributo cuando TipoItem=4 (A8, 57, 90, D4, D5, 25, A6)
        /// </summary>
        [JsonPropertyName("codTributo")]
        public string? CodTributo { get; set; }

        /// <summary>
        /// Unidad de medida - FK a CatUnidadMedida (1-99)
        /// </summary>
        [JsonPropertyName("uniMedida")]
        public int UniMedida { get; set; }

        /// <summary>
        /// Descripción del ítem (maxLength: 1000)
        /// </summary>
        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        /// <summary>
        /// Precio unitario
        /// </summary>
        [JsonPropertyName("precioUni")]
        public double PrecioUni { get; set; }

        /// <summary>
        /// Monto de descuento, bonificación o rebaja por ítem (campo MH: montoDescu)
        /// </summary>
        [JsonPropertyName("montoDescu")]
        public double MontoDescu { get; set; }

        /// <summary>
        /// Monto de ventas no sujetas a impuestos
        /// </summary>
        [JsonPropertyName("ventaNoSuj")]
        public double VentaNoSuj { get; set; }

        /// <summary>
        /// Monto de ventas exentas de impuestos
        /// </summary>
        [JsonPropertyName("ventaExenta")]
        public double VentaExenta { get; set; }

        /// <summary>
        /// Monto de ventas gravadas con impuestos
        /// </summary>
        [JsonPropertyName("ventaGravada")]
        public double VentaGravada { get; set; }

        /// <summary>
        /// Lista de códigos de tributos aplicables (opcional)
        /// </summary>
        [JsonPropertyName("tributos")]
        public List<string>? Tributos { get; set; }

        /// <summary>
        /// Precio sugerido de venta (opcional)
        /// </summary>
        [JsonPropertyName("psv")]
        public double Psv { get; set; }

        /// <summary>
        /// Cargos/abonos que no afectan la base imponible
        /// </summary>
        [JsonPropertyName("noGravado")]
        public double NoGravado { get; set; }

        /// <summary>
        /// IVA calculado del ítem (campo interno del sistema, NO enviado a MH)
        /// </summary>
        [JsonPropertyName("ivaItem")]
        public double IvaItem { get; set; }
    }

    /// <summary>
    /// Resumen del documento - totales, impuestos, pagos y condiciones
    /// </summary>
    public class ResumenDto
    {
        /// <summary>
        /// Total de operaciones no sujetas a impuestos
        /// </summary>
        [JsonPropertyName("totalNoSuj")]
        public double TotalNoSuj { get; set; }

        /// <summary>
        /// Total de operaciones exentas de impuestos
        /// </summary>
        [JsonPropertyName("totalExenta")]
        public double TotalExenta { get; set; }

        /// <summary>
        /// Total de operaciones gravadas (sujetas a IVA)
        /// </summary>
        [JsonPropertyName("totalGravada")]
        public double TotalGravada { get; set; }

        /// <summary>
        /// Suma de operaciones sin impuestos aún
        /// </summary>
        [JsonPropertyName("subTotalVentas")]
        public double SubTotalVentas { get; set; }

        /// <summary>
        /// Descuentos aplicados a ventas no sujetas
        /// </summary>
        [JsonPropertyName("descuNoSuj")]
        public double DescuNoSuj { get; set; }

        /// <summary>
        /// Descuentos aplicados a ventas exentas
        /// </summary>
        [JsonPropertyName("descuExenta")]
        public double DescuExenta { get; set; }

        /// <summary>
        /// Descuentos aplicados a ventas gravadas
        /// </summary>
        [JsonPropertyName("descuGravada")]
        public double DescuGravada { get; set; }

        /// <summary>
        /// Porcentaje de descuento global (0-100)
        /// </summary>
        [JsonPropertyName("porcentajeDescuento")]
        public double PorcentajeDescuento { get; set; }

        /// <summary>
        /// Total de descuentos aplicados
        /// </summary>
        [JsonPropertyName("totalDescu")]
        public double TotalDescu { get; set; }

        /// <summary>
        /// Resumen de tributos aplicados al documento
        /// </summary>
        [JsonPropertyName("tributos")]
        public List<TributoResumenDto> Tributos { get; set; } = new();

        /// <summary>
        /// Subtotal del documento (antes de percepciones y retenciones)
        /// </summary>
        [JsonPropertyName("subTotal")]
        public double SubTotal { get; set; }

        /// <summary>
        /// IVA percibido (1% según regulación MH)
        /// </summary>
        [JsonPropertyName("ivaPerci1")]
        public double IvaPerci1 { get; set; }

        /// <summary>
        /// IVA retenido (1% según regulación MH)
        /// </summary>
        [JsonPropertyName("ivaRete1")]
        public double IvaRete1 { get; set; }

        /// <summary>
        /// Retención de renta aplicada
        /// </summary>
        [JsonPropertyName("reteRenta")]
        public double ReteRenta { get; set; }

        /// <summary>
        /// Monto total de la operación
        /// </summary>
        [JsonPropertyName("montoTotalOperacion")]
        public double MontoTotalOperacion { get; set; }

        /// <summary>
        /// Total de cargos/abonos que no afectan base imponible
        /// </summary>
        [JsonPropertyName("totalNoGravado")]
        public double TotalNoGravado { get; set; }

        /// <summary>
        /// Total a pagar por el cliente
        /// </summary>
        [JsonPropertyName("totalPagar")]
        public double TotalPagar { get; set; }

        /// <summary>
        /// Valor del total en letras (maxLength: 200)
        /// </summary>
        [JsonPropertyName("totalLetras")]
        public string TotalLetras { get; set; } = string.Empty;

        /// <summary>
        /// Saldo a favor del cliente (si aplica)
        /// </summary>
        [JsonPropertyName("saldoFavor")]
        public double SaldoFavor { get; set; }

        /// <summary>
        /// Condición de operación - FK a CatCondicionOperacion: 1=Contado, 2=Crédito, 3=Otro
        /// </summary>
        [JsonPropertyName("condicionOperacion")]
        public int CondicionOperacion { get; set; } = 1;

        /// <summary>
        /// Formas de pago utilizadas
        /// </summary>
        [JsonPropertyName("pagos")]
        public List<PagoDto>? Pagos { get; set; }

        /// <summary>
        /// Número de pago electrónico (campo interno del sistema, NO enviado a MH)
        /// </summary>
        [JsonPropertyName("numPagoElectronico")]
        public string? NumPagoElectronico { get; set; }
    }

    /// <summary>
    /// Tributo aplicado en el resumen del documento
    /// </summary>
    public class TributoResumenDto
    {
        /// <summary>
        /// Código del tributo (2 caracteres, ej: "20" para IVA)
        /// </summary>
        [JsonPropertyName("codigo")]
        public string Codigo { get; set; } = string.Empty;

        /// <summary>
        /// Descripción del tributo (maxLength: 150)
        /// </summary>
        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        /// <summary>
        /// Valor/monto del tributo calculado
        /// </summary>
        [JsonPropertyName("valor")]
        public double Valor { get; set; }
    }

    /// <summary>
    /// Forma de pago utilizada en la transacción
    /// </summary>
    public class PagoDto
    {
        /// <summary>
        /// Código de forma de pago en formato string "01"-"14", "99" (se mapea a CatFormaPago)
        /// </summary>
        [JsonPropertyName("codigo")]
        public string Codigo { get; set; } = string.Empty;

        /// <summary>
        /// Monto pagado con esta forma de pago
        /// </summary>
        [JsonPropertyName("montoPago")]
        public double MontoPago { get; set; }

        /// <summary>
        /// Referencia de la transacción (número de cheque, autorización, etc.) - maxLength: 50
        /// </summary>
        [JsonPropertyName("referencia")]
        public string? Referencia { get; set; }

        /// <summary>
        /// Plazo del pago - FK a CatPlazo: 01=Días, 02=Meses, 03=Años (nullable)
        /// </summary>
        [JsonPropertyName("plazo")]
        public int? Plazo { get; set; }

        /// <summary>
        /// Período/cantidad de tiempo del plazo (ej: 30 días, 6 meses)
        /// </summary>
        [JsonPropertyName("periodo")]
        public decimal? Periodo { get; set; }
    }
}
