using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Services;

public class RecordatorioCuotasService : IRecordatorioCuotasService
{
    private readonly ApplicationDbContext _ctx;
    private readonly IEmailService _emailService;
    private readonly INotificacionService _notificacionService;
    private readonly ILogger<RecordatorioCuotasService> _logger;

    public RecordatorioCuotasService(
        ApplicationDbContext ctx,
        IEmailService emailService,
        INotificacionService notificacionService,
        ILogger<RecordatorioCuotasService> logger)
    {
        _ctx = ctx;
        _emailService = emailService;
        _notificacionService = notificacionService;
        _logger = logger;
    }

    public async Task ProcesarAsync()
    {
        var hoy = DateTime.UtcNow.Date;

        var configs = await _ctx.Set<ConfiguracionCuotas>().ToDictionaryAsync(c => c.EmisorId);

        var planes = await _ctx.Set<PlanCuotas>()
            .Include(p => p.Cuotas)
            .Include(p => p.Receptor)
            .Where(p => p.Activo && p.SaldoAdeudado > 0)
            .ToListAsync();

        int marcadas = 0, avisos = 0;

        foreach (var plan in planes)
        {
            configs.TryGetValue(plan.EmisorId, out var cfg);
            bool habilitados = cfg?.RecordatoriosHabilitados ?? true;
            int diasAntes = cfg?.DiasAntesRecordatorio ?? 3;
            bool tieneVencida = false;

            foreach (var cuota in plan.Cuotas.Where(c => c.Activo && c.Estado != EstadoCuota.Pagada))
            {
                var fecha = cuota.FechaPactada.Date;
                bool estaVencida = hoy > fecha;

                if (estaVencida && cuota.Estado != EstadoCuota.Vencida)
                {
                    cuota.Estado = EstadoCuota.Vencida;
                    marcadas++;
                }
                if (estaVencida) tieneVencida = true;

                if (!habilitados) continue;

                int diasParaVencer = (fecha - hoy).Days;
                if (!estaVencida && diasParaVencer >= 0 && diasParaVencer <= diasAntes
                    && !cuota.RecordatorioPorVencerEnviado)
                {
                    await NotificarAsync(plan, cuota, esVencida: false);
                    cuota.RecordatorioPorVencerEnviado = true;
                    avisos++;
                }

                if (estaVencida && !cuota.RecordatorioVencidaEnviado)
                {
                    await NotificarAsync(plan, cuota, esVencida: true);
                    cuota.RecordatorioVencidaEnviado = true;
                    avisos++;
                }
            }

            if (tieneVencida && plan.SaldoAdeudado > 0)
                plan.EstadoCobro = EstadoCobroPlan.Vencida;
        }

        await _ctx.SaveChangesAsync();
        _logger.LogInformation(
            "[RECORDATORIO-CUOTAS] {Marcadas} cuotas marcadas vencidas, {Avisos} avisos enviados", marcadas, avisos);
    }

    private async Task NotificarAsync(PlanCuotas plan, Cuota cuota, bool esVencida)
    {
        string monto = cuota.Monto.ToString("0.00");
        string fecha = cuota.FechaPactada.ToString("dd/MM/yyyy");
        string cliente = plan.Receptor?.NombreRazonSocial ?? "Cliente";

        string asunto = esVencida ? "Cuota vencida" : "Recordatorio: cuota próxima a vencer";
        string cuerpo = esVencida
            ? $"<p>Estimado/a {cliente},</p><p>La cuota {cuota.Numero} de su plan venció el {fecha} por un monto de ${monto}. Por favor regularice su pago.</p>"
            : $"<p>Estimado/a {cliente},</p><p>Le recordamos que la cuota {cuota.Numero} de su plan vence el {fecha} por un monto de ${monto}.</p>";

        var correo = plan.Receptor?.CorreoElectronico;
        if (!string.IsNullOrWhiteSpace(correo))
        {
            try
            {
                await _emailService.EnviarNotificacionGenericaAsync(asunto, cuerpo, correo, plan.EmisorId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[RECORDATORIO-CUOTAS] Falló email a {Correo} (plan {Plan}, cuota {Cuota})",
                    correo, plan.Id, cuota.Numero);
            }
        }

        string tipo = esVencida ? "CuotaVencida" : "CuotaPorVencer";
        string titulo = esVencida ? "Cuota vencida" : "Cuota por vencer";
        string nivel = esVencida ? "warning" : "info";
        string mensaje = esVencida
            ? $"{cliente}: la cuota {cuota.Numero} venció el {fecha} (${monto})."
            : $"{cliente}: la cuota {cuota.Numero} vence el {fecha} (${monto}).";

        await _notificacionService.CrearAsync(plan.EmisorId, tipo, titulo, mensaje,
            ruta: "/cuentas-por-cobrar", referenciaId: plan.Id, nivel: nivel);
    }
}
