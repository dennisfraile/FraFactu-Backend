using FraFactu.Application.DTOs.Usuarios;

namespace FraFactu.Application.DTOs.ProductosServicios
{
    /// <summary>
    /// DTO ligero de Producto para listados
    /// </summary>
    public class ProductoServicioListDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal PrecioVenta { get; set; }
        public decimal? PrecioCosto { get; set; }
        public bool Activo { get; set; }

        public string UnidadMedida { get; set; } = string.Empty;
        public string TipoItem { get; set; } = string.Empty;

        // IDs opcionales para filtrado/relaciones
        public int? CategoriaId { get; set; }
        public string? CategoriaNombre { get; set; } // Opcional: para evitar lookup en frontend

        public int? MarcaId { get; set; }
        public string? MarcaNombre { get; set; } // Opcional: para evitar lookup en frontend

        public int TipoImpuesto { get; set; } // 1=Gravado, 2=Exento, 3=NoSujeto
        public decimal? PorcentajeIVA { get; set; }
        public bool PrecioIncluyeIva { get; set; }
        public bool CostoIncluyeIva { get; set; }

        public string? CodigoBarras { get; set; }

        public int? StockMinimo { get; set; }
        public int? StockMaximo { get; set; }
        public int? PuntoReorden { get; set; }
        public bool? PermiteVentaSinStock { get; set; }

        public DateTime FechaCreacion { get; set; }

        public bool AccesoTodasSucursales { get; set; }
        public List<Usuarios.SucursalAsignadaDto> Sucursales { get; set; } = new();

        // ==========================================
        // F2: TIPO DE INVENTARIO (Plan inventario desde DTE)
        // ==========================================

        /// <summary>0=Ventas, 1=MobiliarioEquipo, 2=Insumos. Permite filtrar el listado.</summary>
        public int TipoInventario { get; set; }

        public DateTime? FechaAdquisicion { get; set; }
        public int? AniosVidaUtil { get; set; }
        public decimal? ValorActual { get; set; }
        public decimal? ValorResidual { get; set; }

        /// <summary>Impuestos adicionales del producto (los consume el autocomplete de la factura).</summary>
        public List<ProductoTributoDto> TributosAdicionales { get; set; } = new();
    }
}
