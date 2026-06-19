namespace FraFactu.Application.DTOs.Facturas
{
    /// <summary>
    /// DTO para Ítem/Detalle de la Factura según estándar de Hacienda
    /// </summary>
    public class ItemDocumentoDto
    {
        /// <summary>
        /// Número correlativo del ítem (1-2000)
        /// </summary>
        public int NumItem { get; set; }

        /// <summary>
        /// Tipo de ítem: 1=Bien, 2=Servicio, 3=Ambos, 4=Otros
        /// </summary>
        public int TipoItem { get; set; }

        /// <summary>
        /// Número de identificación del item (opcional)
        /// </summary>
        public string? NumeroDocumento { get; set; }

        /// <summary>
        /// Cantidad del ítem. Debe ser mayor a 0
        /// </summary>
        public decimal Cantidad { get; set; }

        /// <summary>
        /// Código del ítem (SKU, código interno)
        /// Opcional, máximo 25 caracteres
        /// </summary>
        public string? Codigo { get; set; }

        /// <summary>
        /// Código de tributación (opcional)
        /// </summary>
        public string? CodTributo { get; set; }

        /// <summary>
        /// Unidad de Medida según catálogo de Hacienda (1-99)
        /// </summary>
        public int UniMedida { get; set; }

        /// <summary>
        /// Descripción del producto o servicio
        /// Requerido, máximo 1000 caracteres
        /// </summary>
        public string Descripcion { get; set; } = string.Empty;

        /// <summary>
        /// Precio unitario del ítem
        /// </summary>
        public decimal PrecioUni { get; set; }

        /// <summary>
        /// Monto de Descuento aplicado al ítem (opcional)
        /// </summary>
        public decimal? MontoDescuento { get; set; }

        /// <summary>
        /// Venta Gravada (sujeta a IVA)
        /// </summary>
        public decimal VentaGravada { get; set; }

        /// <summary>
        /// Venta Exenta de IVA
        /// </summary>
        public decimal VentaExenta { get; set; }

        /// <summary>
        /// Venta No Sujeta a IVA
        /// </summary>
        public decimal VentaNoSuj { get; set; }

        /// <summary>
        /// Monto de IVA del ítem
        /// Se calcula automáticamente: VentaGravada * 0.13
        /// </summary>
        public decimal IvaItem { get; set; }

        /// <summary>
        /// Tributos adicionales (opcional)
        /// </summary>
        public List<string>? Tributos { get; set; }

        /// <summary>
        /// IEPS (Impuesto Específico) si aplica
        /// </summary>
        public decimal? Ieps { get; set; }

        /// <summary>
        /// Total de compra del ítem (específico de FSE tipo 14)
        /// Fórmula: (precioUni * cantidad) - montoDescu
        /// </summary>
        public decimal? Compra { get; set; }

        /// <summary>
        /// Precio Sugerido de Venta (PSV) - específico de CCF
        /// </summary>
        public decimal? Psv { get; set; }

        /// <summary>
        /// Cargos/Abonos que no afectan la base imponible - específico de CCF
        /// </summary>
        public decimal? NoGravado { get; set; }

        // ========================================
        // CAMPOS PARA INTEGRACIÓN CON INVENTARIO
        // ========================================

        /// <summary>
        /// ID del producto en inventario (opcional)
        /// Si se especifica, permite vincular el detalle con el stock
        /// </summary>
        public int? ProductoId { get; set; }

        /// <summary>
        /// ID de la bodega de donde se descuenta el stock (opcional)
        /// Requerido si ProductoId está especificado y se desea afectar inventario
        /// </summary>
        public int? BodegaId { get; set; }

        /// <summary>
        /// Costo unitario del producto (opcional)
        /// Se obtiene automáticamente del stock si ProductoId/BodegaId están especificados
        /// </summary>
        public decimal? CostoUnitario { get; set; }

        /// <summary>
        /// Indica si <see cref="PrecioUni"/> ya incluye IVA (true) o si es base sin IVA
        /// (false). Cuando el flag viene presente, el backend recalcula
        /// <see cref="VentaGravada"/> a partir de <see cref="PrecioUni"/>+<see cref="Cantidad"/>
        /// para garantizar que CF (Tipo 01) y CCF (Tipo 03) emitidos con el mismo PrecioUni
        /// terminen con el MISMO total (mismo base, mismo IVA).
        ///
        /// Null = comportamiento legacy: el backend confia en el <see cref="VentaGravada"/>
        /// que envia el FE (Smartix Vue calcula segun el tipo de DTE). Asi clientes viejos
        /// no rompen.
        ///
        /// Plan C2 (2026-05-19): el FE de Smartix y los prefills desde SmartCare deberian
        /// pasar a setear este flag siempre. Hasta entonces, el backend solo entra al
        /// recalc cuando lo recibe.
        /// </summary>
        public bool? PrecioIncluyeIva { get; set; }
    }
}
