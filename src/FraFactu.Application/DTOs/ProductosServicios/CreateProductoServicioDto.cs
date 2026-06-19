namespace FraFactu.Application.DTOs.ProductosServicios
{
    /// <summary>
    /// DTO para crear un nuevo Producto
    /// </summary>
    public class CreateProductoServicioDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal PrecioVenta { get; set; }

        public int CatUnidadMedidaId { get; set; }
        public int CatTipoItemId { get; set; }

        // ==========================================
        // CAMPOS ADICIONALES PARA PRODUCTOS (Opcionales)
        // ==========================================

        /// <summary>
        /// Código de barras / SKU
        /// </summary>
        public string? CodigoBarras { get; set; }

        /// <summary>
        /// Clasificación del producto
        /// </summary>
        public int? CategoriaId { get; set; }

        /// <summary>
        /// Marca del producto
        /// </summary>
        public int? MarcaId { get; set; }

        /// <summary>
        /// Costo de adquisición
        /// </summary>
        public decimal? PrecioCosto { get; set; }

        /// <summary>
        /// Tipo de impuesto: 1=Gravado (IVA 13%), 2=Exento, 3=NoSujeto
        /// </summary>
        public int TipoImpuesto { get; set; } = 1; // Por defecto Gravado

        /// <summary>
        /// Porcentaje de IVA (ej: 13) - Solo aplica si TipoImpuesto = Gravado
        /// </summary>
        public decimal? PorcentajeIVA { get; set; } = 13m;

        public bool PrecioIncluyeIva { get; set; } = false;

        public bool CostoIncluyeIva { get; set; } = false;

        /// <summary>
        /// Alerta de stock bajo
        /// </summary>
        public decimal? StockMinimo { get; set; }

        /// <summary>
        /// Límite de inventario
        /// </summary>
        public decimal? StockMaximo { get; set; }

        /// <summary>
        /// Cuándo reabastecer
        /// </summary>
        public decimal? PuntoReorden { get; set; }

        /// <summary>
        /// Vender sin existencia
        /// </summary>
        public bool? PermiteVentaSinStock { get; set; }

        // ==========================================
        // F2: TIPO DE INVENTARIO (Plan inventario desde DTE)
        // ==========================================

        /// <summary>
        /// Clasificacion contable del producto: 0=Ventas (default), 1=MobiliarioEquipo,
        /// 2=Insumos. MobiliarioEquipo (activo fijo) NO afecta StockBodega.
        /// </summary>
        public int TipoInventario { get; set; } = 0;

        /// <summary>Fecha de adquisicion. Solo aplica si TipoInventario=MobiliarioEquipo.</summary>
        public DateTime? FechaAdquisicion { get; set; }

        /// <summary>Anios de vida util del activo.</summary>
        public int? AniosVidaUtil { get; set; }

        /// <summary>Valor neto en libros (arranca = PrecioCosto al adquirir).</summary>
        public decimal? ValorActual { get; set; }

        /// <summary>Piso de devaluacion (0 si no se especifica).</summary>
        public decimal? ValorResidual { get; set; }

        /// <summary>
        /// Si es true, disponible en todas las sucursales del emisor
        /// </summary>
        public bool AccesoTodasSucursales { get; set; } = false;

        /// <summary>
        /// IDs de sucursales asignadas (si AccesoTodasSucursales es false)
        /// </summary>
        public List<int>? SucursalIds { get; set; }

        /// <summary>
        /// Impuestos adicionales del producto (Sección 1 + Sección 3). El IVA ("20")
        /// no va aquí; se deriva de TipoImpuesto/PorcentajeIVA al emitir.
        /// </summary>
        public List<ProductoTributoDto>? TributosAdicionales { get; set; }

        // EmisorId se infiere del usuario autenticado
    }
}
