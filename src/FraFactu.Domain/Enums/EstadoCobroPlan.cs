namespace FraFactu.Domain.Enums
{
    /// <summary>
    /// Estado de cobro de un plan de cuotas (venta a crédito/mixto).
    /// </summary>
    public enum EstadoCobroPlan
    {
        Pendiente = 0,
        Parcial = 1,
        Pagada = 2,
        Vencida = 3
    }
}
