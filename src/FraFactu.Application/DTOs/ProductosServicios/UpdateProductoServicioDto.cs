namespace FraFactu.Application.DTOs.ProductosServicios
{
    /// <summary>
    /// DTO para actualizar un Producto existente
    /// </summary>
    public class UpdateProductoServicioDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal PrecioVenta { get; set; }
        public bool Activo { get; set; }

        public int CatUnidadMedidaId { get; set; }
        public int CatTipoItemId { get; set; }

        // ==========================================
        // CAMPOS ADICIONALES PARA PRODUCTOS (Opcionales)
        // ==========================================

        public string? CodigoBarras { get; set; }
        public int? CategoriaId { get; set; }
        public int? MarcaId { get; set; }
        public decimal? PrecioCosto { get; set; }

        /// <summary>
        /// Tipo de impuesto: 1=Gravado, 2=Exento, 3=NoSujeto
        /// </summary>
        public int TipoImpuesto { get; set; }

        public decimal? PorcentajeIVA { get; set; }
        public bool PrecioIncluyeIva { get; set; } = false;
        public bool CostoIncluyeIva { get; set; } = false;
        public decimal? StockMinimo { get; set; }
        public decimal? StockMaximo { get; set; }
        public decimal? PuntoReorden { get; set; }
        public bool? PermiteVentaSinStock { get; set; }
        public bool AccesoTodasSucursales { get; set; } = false;
        public List<int>? SucursalIds { get; set; }

        // ==========================================
        // F2: TIPO DE INVENTARIO (Plan inventario desde DTE)
        // ==========================================

        /// <summary>0=Ventas, 1=MobiliarioEquipo, 2=Insumos.</summary>
        public int TipoInventario { get; set; } = 0;

        /// <summary>Solo aplica si TipoInventario=MobiliarioEquipo.</summary>
        public DateTime? FechaAdquisicion { get; set; }

        public int? AniosVidaUtil { get; set; }
        public decimal? ValorActual { get; set; }
        public decimal? ValorResidual { get; set; }

        /// <summary>Impuestos adicionales (sincronización completa: reemplaza los previos).</summary>
        public List<ProductoTributoDto>? TributosAdicionales { get; set; }
    }
}
