namespace FraFactu.Application.DTOs.ProductosServicios
{
    /// <summary>
    /// Impuesto adicional asociado a un producto/servicio.
    /// Sección 1: TipoCalculo + Valor poblados. Sección 3: ambos null (solo Codigo).
    /// </summary>
    public class ProductoTributoDto
    {
        /// <summary>Código del CatTributo (ej. "D1", "59"). El IVA "20" no se almacena aquí.</summary>
        public string Codigo { get; set; } = string.Empty;

        /// <summary>"monto_fijo" | "porcentaje" | null (Sección 3).</summary>
        public string? TipoCalculo { get; set; }

        /// <summary>Monto o porcentaje | null (Sección 3).</summary>
        public decimal? Valor { get; set; }
    }
}
