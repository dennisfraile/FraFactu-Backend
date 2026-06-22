using FraFactu.Domain.Common;
using FraFactu.Domain.Entities.Catalogos;

namespace FraFactu.Domain.Entities
{
    /// <summary>
    /// Catálogo de productos y servicios del emisor
    /// </summary>
    public class ProductoServicio : BaseEntity
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal PrecioVenta { get; set; }

        // Relación con Unidad de Medida
        public int CatUnidadMedidaId { get; set; }
        public CatUnidadMedida UnidadMedida { get; set; } = null!;

        // Relación con Tipo de Item (1=Bien/Producto, 2=Servicio)
        public int CatTipoItemId { get; set; }
        public CatTipoItem TipoItem { get; set; } = null!;

        // Relación con Emisor (Multi-tenant)
        public int EmisorId { get; set; }
        public Emisor Emisor { get; set; } = null!;

        // ==========================================
        // CAMPOS MERGEADOS DE PRODUCTO (Opcionales)
        // ==========================================

        public string? CodigoBarras { get; set; }

        // CLASIFICACIÓN
        public int? CategoriaId { get; set; }
        public Categoria? Categoria { get; set; }

        public int? MarcaId { get; set; }
        public Marca? Marca { get; set; }

        // INVENTARIO
        public decimal? StockMinimo { get; set; }
        public decimal? StockMaximo { get; set; }
        public decimal? PuntoReorden { get; set; }
        public bool? PermiteVentaSinStock { get; set; }

        // COSTOS E IMPUESTOS
        public decimal? PrecioCosto { get; set; }

        /// <summary>
        /// Tipo de impuesto: Gravado (IVA 13%), Exento, No Sujeto
        /// </summary>
        public Enums.TipoImpuesto TipoImpuesto { get; set; } = Enums.TipoImpuesto.Gravado;

        /// <summary>
        /// Porcentaje de IVA aplicable (solo si TipoImpuesto = Gravado)
        /// Por defecto 13% en El Salvador
        /// </summary>
        public decimal? PorcentajeIVA { get; set; } = 13m;

        /// <summary>Si true, el usuario ingresó el PrecioVenta con IVA incluido (PrecioVenta se almacena neto).</summary>
        public bool PrecioIncluyeIva { get; set; } = false;

        /// <summary>Si true, el usuario ingresó el PrecioCosto con IVA incluido (PrecioCosto se almacena neto).</summary>
        public bool CostoIncluyeIva { get; set; } = false;


        // ==========================================
        // F2: TIPO DE INVENTARIO (PLAN INVENTARIO DESDE DTE)
        // ==========================================

        /// <summary>
        /// Clasificacion contable del producto. Ventas y Insumos manejan stock
        /// circulante; MobiliarioEquipo se trata como activo fijo (no toca
        /// StockBodega al confirmar compra, pero si genera MovimientoInventario
        /// para trazabilidad). Default <c>Ventas</c> para no romper el catalogo
        /// existente.
        /// </summary>
        public Enums.TipoInventario TipoInventario { get; set; } = Enums.TipoInventario.Ventas;

        /// <summary>
        /// Fecha de adquisicion del activo. Solo aplica a MobiliarioEquipo.
        /// Smartix no devalua automaticamente; este campo es informativo y
        /// se envia a SmartInventory cuando el sync este activo.
        /// </summary>
        public DateTime? FechaAdquisicion { get; set; }

        /// <summary>
        /// Anios totales de vida util desde <see cref="FechaAdquisicion"/>.
        /// El job de devaluacion deja de tocar el item al cumplirse.
        /// </summary>
        public int? AniosVidaUtil { get; set; }

        /// <summary>
        /// Valor neto en libros actual del activo. Arranca igual a
        /// <c>PrecioCosto</c> al adquirirlo. F3 (G1): el job anual de
        /// devaluacion lo actualiza in-process.
        /// </summary>
        public decimal? ValorActual { get; set; }

        /// <summary>
        /// Piso de devaluacion. El job nunca baja del residual (default 0).
        /// </summary>
        public decimal? ValorResidual { get; set; }

        /// <summary>
        /// Porcentaje anual de devaluacion sobre <see cref="ValorActual"/>
        /// (decreciente). Solo aplica a MobiliarioEquipo. F3 (G1).
        /// </summary>
        public decimal? PorcentajeDevaluacionAnual { get; set; }

        /// <summary>
        /// Fecha (UTC) de la ultima devaluacion aplicada. Hace el job
        /// idempotente por anio calendario. F3 (G1).
        /// </summary>
        public DateTime? FechaUltimaDevaluacion { get; set; }

        // ==========================================
        // F3 (G2): SOFT-DELETE AUDITADO
        // ==========================================

        /// <summary>
        /// Motivo de la baja. Obligatorio al desactivar; se limpia al reactivar.
        /// </summary>
        public string? MotivoDesactivacion { get; set; }

        /// <summary>
        /// Usuario que ejecutó la baja. Se limpia al reactivar.
        /// </summary>
        public int? DesactivadoPorUsuarioId { get; set; }

        /// <summary>
        /// Fecha (UTC) de la baja. Se limpia al reactivar.
        /// </summary>
        public DateTime? DesactivadoEn { get; set; }

        /// <summary>
        /// Si es true, el producto/servicio está disponible en todas las sucursales del emisor.
        /// Si es false, solo en las sucursales asignadas en ProductoServicioSucursales.
        /// </summary>
        public bool AccesoTodasSucursales { get; set; } = false;

        /// <summary>
        /// Sucursales asignadas al producto/servicio (many-to-many)
        /// </summary>
        public ICollection<ProductoServicioSucursal> ProductoServicioSucursales { get; set; } = new List<ProductoServicioSucursal>();

        /// <summary>
        /// Impuestos adicionales (tributos) asignados al producto/servicio (many-to-many)
        /// </summary>
        public ICollection<ProductoServicioTributo> TributosAdicionales { get; set; } = new List<ProductoServicioTributo>();

        // NAVEGACIÓN
        public ICollection<StockBodega> Stocks { get; set; } = new List<StockBodega>();
        public ICollection<MovimientoInventario> Movimientos { get; set; } = new List<MovimientoInventario>();
    }
}
