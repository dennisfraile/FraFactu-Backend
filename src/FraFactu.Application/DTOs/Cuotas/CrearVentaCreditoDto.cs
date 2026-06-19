using FraFactu.Application.DTOs.Facturas;

namespace FraFactu.Application.DTOs.Cuotas
{
    /// <summary>
    /// Crear una venta a crédito/mixto con plan de cuotas.
    /// La condición de operación va dentro de Venta.Resumen.CondicionOperacion (2 ó 3).
    /// </summary>
    public class CrearVentaCreditoDto
    {
        /// <summary>La venta completa (ítems, receptor, resumen). Igual que una factura normal.</summary>
        public CreateFacturaElectronicaDto Venta { get; set; } = new();

        /// <summary>Cuotas del plan (al menos 2).</summary>
        public List<DefinicionCuotaDto> Cuotas { get; set; } = new();

        public string? Observaciones { get; set; }
    }
}
