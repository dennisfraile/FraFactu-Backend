using System.Collections.Generic;

namespace FraFactu.Application.DTOs.Cuotas
{
    public class RefinanciarPlanDto
    {
        public string Motivo { get; set; } = string.Empty;
        /// <summary>Nuevas cuotas para el saldo pendiente (deben sumar el saldo).</summary>
        public List<DefinicionCuotaDto> NuevasCuotas { get; set; } = new();
    }
}
