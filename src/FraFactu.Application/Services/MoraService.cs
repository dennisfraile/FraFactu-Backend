using FraFactu.Application.Interfaces;

namespace FraFactu.Application.Services;

public class MoraService : IMoraService
{
    public decimal CalcularMora(decimal montoCuota, DateTime fechaPactada, DateTime fechaPago,
        decimal tasaMoraMensual, int diasGracia, bool moraHabilitada)
    {
        if (!moraHabilitada) return 0m;
        int diasAtraso = (fechaPago.Date - fechaPactada.Date).Days - diasGracia;
        if (diasAtraso <= 0) return 0m;
        decimal tasaDiaria = tasaMoraMensual / 30m;
        return decimal.Round(montoCuota * tasaDiaria * diasAtraso, 2);
    }
}
