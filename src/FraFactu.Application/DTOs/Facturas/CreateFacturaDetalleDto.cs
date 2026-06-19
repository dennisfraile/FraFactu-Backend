namespace FraFactu.Application.DTOs.Facturas
{
    /// <summary>
    /// DTO para creación de detalle de factura
    /// </summary>
    public class CreateFacturaDetalleDto
    {
        public int NumeroItem { get; set; }
        public int CatTipoItemId { get; set; }
        public decimal Cantidad { get; set; }
        public int CatUnidadMedidaId { get; set; }
        public string CodigoProducto { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal PrecioUnitario { get; set; }
        public decimal MontoDescuento { get; set; } = 0;

        // Solo uno de estos debe tener valor, los demás en 0
        public decimal VentaNoSujeta { get; set; } = 0;
        public decimal VentaExenta { get; set; } = 0;
        public decimal VentaGravada { get; set; } = 0;

        public string? TributosAplicados { get; set; }
        public decimal NoGravado { get; set; } = 0;
    }
}
