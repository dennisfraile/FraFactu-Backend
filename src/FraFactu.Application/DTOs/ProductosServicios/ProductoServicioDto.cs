using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Usuarios;

namespace FraFactu.Application.DTOs.ProductosServicios
{
    /// <summary>
    /// DTO completo de Producto (para lectura)
    /// </summary>
    public class ProductoServicioDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal PrecioVenta { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }

        // Catálogos
        public CatalogoDto UnidadMedida { get; set; } = null!;
        public CatalogoDto TipoItem { get; set; } = null!;

        // Emisor
        public int EmisorId { get; set; }
        public string EmisorNombre { get; set; } = string.Empty;

        // ==========================================
        // CAMPOS ADICIONALES PARA PRODUCTOS (Opcionales)
        // ==========================================

        public string? CodigoBarras { get; set; }
        public int? CategoriaId { get; set; }
        public string? CategoriaNombre { get; set; }
        public int? MarcaId { get; set; }
        public string? MarcaNombre { get; set; }
        public decimal? PrecioCosto { get; set; }
        public bool AccesoTodasSucursales { get; set; }
        public List<Usuarios.SucursalAsignadaDto> Sucursales { get; set; } = new();

        /// <summary>
        /// Tipo de impuesto: 1=Gravado, 2=Exento, 3=NoSujeto
        /// </summary>
        public int TipoImpuesto { get; set; }

        public decimal? PorcentajeIVA { get; set; }
        public bool PrecioIncluyeIva { get; set; }
        public bool CostoIncluyeIva { get; set; }
        public decimal? StockMinimo { get; set; }
        public decimal? StockMaximo { get; set; }
        public decimal? PuntoReorden { get; set; }
        public bool? PermiteVentaSinStock { get; set; }

        // ==========================================
        // F2: TIPO DE INVENTARIO (Plan inventario desde DTE)
        // ==========================================

        /// <summary>0=Ventas, 1=MobiliarioEquipo, 2=Insumos.</summary>
        public int TipoInventario { get; set; }

        public DateTime? FechaAdquisicion { get; set; }
        public int? AniosVidaUtil { get; set; }
        public decimal? ValorActual { get; set; }
        public decimal? ValorResidual { get; set; }

        /// <summary>Impuestos adicionales del producto (Sección 1 + Sección 3).</summary>
        public List<ProductoTributoDto> TributosAdicionales { get; set; } = new();
    }
}
