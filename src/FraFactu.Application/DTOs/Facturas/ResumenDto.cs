namespace FraFactu.Application.DTOs.Facturas
{
    /// <summary>
    /// DTO para el Resumen/Totales de la Factura según estándar de Hacienda
    /// </summary>
    public class ResumenDto
    {
        /// <summary>
        /// Total de Ventas No Sujetas a IVA
        /// </summary>
        public decimal TotalNoSuj { get; set; }

        /// <summary>
        /// Total de Ventas Exentas de IVA
        /// </summary>
        public decimal TotalExenta { get; set; }

        /// <summary>
        /// Total de Ventas Gravadas (sujetas a IVA)
        /// </summary>
        public decimal TotalGravada { get; set; }

        /// <summary>
        /// Sub-Total de Ventas (TotalNoSuj + TotalExenta + TotalGravada)  
        /// </summary>
        public decimal? SubTotalVentas { get; set; }

        /// <summary>
        /// Descuento aplicado a operaciones no sujetas
        /// </summary>
        public decimal? DescuNoSuj { get; set; }

        /// <summary>
        /// Descuento aplicado a operaciones exentas
        /// </summary>
        public decimal? DescuExenta { get; set; }

        /// <summary>
        /// Descuento aplicado a operaciones gravadas
        /// </summary>
        public decimal? DescuGravada { get; set; }

        /// <summary>
        /// Porcentaje de descuento aplicado
        /// </summary>
        public decimal? PorcentajeDescuento { get; set; }

        /// <summary>
        /// Suma de descuentos aplicados (total)
        /// </summary>
        public decimal? TotalDescu { get; set; }

        /// <summary>
        /// Total de IVA de la factura
        /// Se calcula: TotalGravada * 0.13
        /// </summary>
        public decimal TotalIva { get; set; }

        /// <summary>
        /// Sub-Total antes de impuestos
        /// TotalNoSuj + TotalExenta + TotalGravada
        /// </summary>
        public decimal SubTotal { get; set; }

        /// <summary>
        /// IEPS Total (opcional)
        /// </summary>
        public decimal? IvaRete1 { get; set; }

        /// <summary>
        /// Retención Renta (opcional)
        /// </summary>
        public decimal? ReteRenta { get; set; }

        /// <summary>
        /// Monto Total del Documento a Pagar
        /// SubTotal + TotalIva - Descuentos + Otros
        /// </summary>
        public decimal TotalPagar { get; set; }

        /// <summary>
        /// Total a Pagar en Letras
        /// Ejemplo: "CIEN DÓLARES CON 00/100"
        /// </summary>
        public string TotalLetras { get; set; } = string.Empty;

        /// <summary>
        /// Saldo a favor del comprador (opcional)
        /// </summary>
        public decimal? SaldoFavor { get; set; }

        /// <summary>
        /// Monto total en operaciones sujetas a percepción (opcional)
        /// </summary>
        public decimal? TotalIvaPerc { get; set; }

        /// <summary>
        /// Monto total de IVA percibido (opcional)
        /// </summary>
        public decimal? MontoTotalOperacion { get; set; }

        /// <summary>
        /// Total de compras brutas (específico de FSE tipo 14)
        /// Fórmula: SUM(compra + montoDescu) por ítem
        /// </summary>
        public decimal? TotalCompras { get; set; }

        /// <summary>
        /// Descuento global no sujeto a retención (específico de FSE tipo 14)
        /// </summary>
        public decimal? Descu { get; set; }

        /// <summary>
        /// Condición de la Operación
        /// 1 = Contado, 2 = Crédito, 3 = Otro
        /// </summary>
        public int CondicionOperacion { get; set; }

        /// <summary>
        /// Formas de Pago
        /// Debe haber al menos una y la suma debe igualar TotalPagar
        /// </summary>
        public List<PagoDto> Pagos { get; set; } = new List<PagoDto>();

        /// <summary>
        /// Resumen de Tributos (específico de CCF)
        /// </summary>
        public List<TributoResumenDto>? Tributos { get; set; }

        /// <summary>
        /// Total Cargos/Abonos que no afectan la base imponible (específico de CCF)
        /// </summary>
        public decimal? TotalNoGravado { get; set; }

        /// <summary>
        /// IVA Percibido (específico de CCF)
        /// </summary>
        public decimal? IvaPerci1 { get; set; }

        /// <summary>
        /// Número de control de pago electrónico (opcional)
        /// </summary>
        public string? NumPagoElectronico { get; set; }
    }
}
