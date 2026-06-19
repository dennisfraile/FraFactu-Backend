using FraFactu.Domain.Common;
using FraFactu.Domain.Entities.Catalogos;

namespace FraFactu.Domain.Entities
{
    public class FacturaElectronicaDetalle : BaseEntity
    {
        public int FacturaId { get; set; }
        public FacturaElectronica Factura { get; set; } = null!;

        public int NumeroItem { get; set; }

        /// <summary>
        /// Clasificación del Ítem según MH
        /// 1=Bien, 2=Servicio, 3=Ambos, 4=Impuesto
        /// </summary>
        public int CatTipoItemId { get; set; }
        public CatTipoItem TipoItem { get; set; } = null!;

        public decimal Cantidad { get; set; }
        public int CatUnidadMedidaId { get; set; }
        public CatUnidadMedida UnidadMedida { get; set; } = null!;



        public string CodigoProducto { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;

        public decimal PrecioUnitario { get; set; }
        public decimal MontoDescuento { get; set; }

        // DTE exige separar estos montos (solo uno se llena, los otros van en 0.00)
        public decimal VentaNoSujeta { get; set; }
        public decimal VentaExenta { get; set; }
        public decimal VentaGravada { get; set; }

        // Tributos específicos de este ítem (ej: 20 para IVA)
        // Se guardan como array ["20", "59"] en JSON, aquí podemos usar una relación o string simple si son pocos.
        // Para simplificar y rendimiento, a veces se guarda el JSON de tributos del item:
        public string? TributosAplicados { get; set; } // Ej: "['20']"

        public decimal NoGravado { get; set; } // Cargos que no afectan impuestos
        public decimal IvaItem { get; set; } // Calculado para referencia

        // ==========================================
        // CAMPOS ADICIONALES SEGÚN ESQUEMA MH
        // ==========================================

        /// <summary>
        /// Número de documento relacionado por ítem (opcional)
        /// Para vincular el ítem con un documento específico
        /// </summary>
        public string? NumeroDocumentoRelacionado { get; set; }

        /// <summary>
        /// Código de tributo (requerido solo cuando CatTipoItemId=4 Impuesto)
        /// Valores permitidos: A8, 57, 90, D4, D5, 25, A6
        /// </summary>
        public string? CodTributo { get; set; }

        /// <summary>
        /// Precio Sugerido de Venta (PSV) - opcional
        /// </summary>
        public decimal? PrecioSugeridoVenta { get; set; }

        // ==========================================
        // INTEGRACIÓN CON INVENTARIO
        // ==========================================

        /// <summary>
        /// FK al producto del inventario (opcional - puede ser ítem manual)
        /// </summary>
        public int? ProductoId { get; set; }
        public ProductoServicio? Producto { get; set; }

        /// <summary>
        /// FK a la bodega desde donde se despacha
        /// </summary>
        public int? BodegaId { get; set; }
        public Bodega? Bodega { get; set; }

        /// <summary>
        /// Costo unitario del producto al momento de la venta
        /// </summary>
        public decimal CostoUnitario { get; set; }

        /// <summary>
        /// Costo total de este ítem
        /// </summary>
        public decimal CostoTotal => Cantidad * CostoUnitario;

        /// <summary>
        /// Utilidad bruta del ítem (solo para ventas gravadas)
        /// </summary>
        public decimal Utilidad => VentaGravada - CostoTotal;

        /// <summary>
        /// Porcentaje de margen de utilidad
        /// </summary>
        public decimal PorcentajeUtilidad => CostoTotal > 0
            ? (Utilidad / CostoTotal) * 100
            : 0;
    }
}