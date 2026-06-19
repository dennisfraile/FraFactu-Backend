using FraFactu.Application.DTOs.Cuotas;

namespace FraFactu.Application.Interfaces;

public interface IPlanCuotasService
{
    /// <summary>Crea la venta a crédito/mixto, emite el DTE de la cuota 1 y devuelve el plan.</summary>
    Task<PlanCuotasDto> CrearVentaCreditoAsync(CrearVentaCreditoDto dto, int emisorId);

    /// <summary>Lista los planes con saldo del emisor (filtro opcional por solo-pendientes).</summary>
    Task<List<PlanCuotasDto>> GetAllAsync(int emisorId, bool soloConSaldo);

    /// <summary>Detalle de un plan.</summary>
    Task<PlanCuotasDto?> GetByIdAsync(int planId, int emisorId);

    /// <summary>Registra el pago de la siguiente cuota pendiente: emite su DTE y recalcula saldo/estado.</summary>
    Task<PlanCuotasDto> PagarCuotaAsync(int planId, int numeroCuota, RegistrarPagoCuotaDto dto, int emisorId);

    /// <summary>
    /// Refinancia las cuotas pendientes de un plan: desactiva las pendientes actuales,
    /// crea nuevas con numeración continua y registra un historial auditable.
    /// Las nuevas cuotas deben sumar exactamente el saldo pendiente.
    /// </summary>
    Task<PlanCuotasDto> RefinanciarPlanAsync(int planId, RefinanciarPlanDto dto, int emisorId, int? usuarioId);

    /// <summary>
    /// Calcula (sin emitir) la mora estimada de una cuota a la fecha actual.
    /// Solo lectura: no modifica el plan ni emite DTE.
    /// </summary>
    Task<MoraEstimadaDto> EstimarMoraCuotaAsync(int planId, int numero, int emisorId);
}
