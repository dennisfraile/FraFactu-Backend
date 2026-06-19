using FraFactu.Application.DTOs.Common;

namespace FraFactu.Application.DTOs.Facturas
{
    /// <summary>
    /// DTO de detalle/item de factura (para lectura)
    /// </summary>
    public class FacturaDetalleDto
    {
        public int Id { get; set; }
        public int NumeroItem { get; set; }

        // Tipo de Item
        public int CatTipoItemId { get; set; }
        public CatalogoDto TipoItem { get; set; } = new();

        // Cantidad y Unidad de Medida
        public decimal Cantidad { get; set; }
        public int CatUnidadMedidaId { get; set; }
        public CatalogoDto UnidadMedida { get; set; } = new();

        // Producto/Servicio
        public string CodigoProducto { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;

        // Precios y Descuentos
        public decimal PrecioUnitario { get; set; }
        public decimal MontoDescuento { get; set; }

        // Clasificación de Venta (solo uno debe tener valor)
        public decimal VentaNoSujeta { get; set; }
        public decimal VentaExenta { get; set; }
        public decimal VentaGravada { get; set; }

        // Tributos y Cargos
        public string? TributosAplicados { get; set; }
        public decimal NoGravado { get; set; }
        public decimal IvaItem { get; set; }

        // Totales calculados
        public decimal SubTotal => (Cantidad * PrecioUnitario) - MontoDescuento;
        public decimal Total => SubTotal + IvaItem + NoGravado;
    }
}
