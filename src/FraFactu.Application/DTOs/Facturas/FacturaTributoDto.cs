using FraFactu.Application.DTOs.Common;

namespace FraFactu.Application.DTOs.Facturas
{
    /// <summary>
    /// DTO de tributo aplicado a la factura (resumen)
    /// </summary>
    public class FacturaTributoDto
    {
        public int Id { get; set; }

        // Catálogo de Tributo
        public int CatTributoId { get; set; }
        public CatalogoDto Tributo { get; set; } = new();

        // Snapshot del tributo (para historial)
        public string CodigoAttribute { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;

        // Monto calculado
        public decimal Valor { get; set; }
    }
}
