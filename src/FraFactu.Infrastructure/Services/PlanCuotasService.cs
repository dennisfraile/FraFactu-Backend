using System.Text.Json;
using FraFactu.Application.DTOs.Cuotas;
using FraFactu.Application.DTOs.Facturas;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Services;

public class PlanCuotasService : IPlanCuotasService
{
    private const decimal TasaIva = 0.13m;
    private readonly ApplicationDbContext _ctx;
    private readonly IFacturaService _facturaService;
    private readonly IFacturacionCuotaStrategy _strategy;
    private readonly IMoraService _moraService;
    private readonly ILogger<PlanCuotasService> _logger;

    public PlanCuotasService(
        ApplicationDbContext ctx,
        IFacturaService facturaService,
        IFacturacionCuotaStrategy strategy,
        IMoraService moraService,
        ILogger<PlanCuotasService> logger)
    {
        _ctx = ctx;
        _facturaService = facturaService;
        _strategy = strategy;
        _moraService = moraService;
        _logger = logger;
    }

    public async Task<PlanCuotasDto> CrearVentaCreditoAsync(CrearVentaCreditoDto dto, int emisorId)
    {
        if (dto.Cuotas.Count < 2)
            throw new InvalidOperationException("Un plan de cuotas requiere al menos 2 cuotas.");

        var condicion = dto.Venta.Resumen.CondicionOperacion;
        if (condicion != 2 && condicion != 3)
            throw new InvalidOperationException("La condición de operación debe ser Crédito (2) o Mixto (3).");

        decimal total = dto.Venta.Resumen.TotalPagar;
        decimal sumaCuotas = dto.Cuotas.Sum(c => MontoDeCuota(c, total));
        if (decimal.Round(sumaCuotas, 2) != decimal.Round(total, 2))
            throw new InvalidOperationException(
                $"La suma de las cuotas ({sumaCuotas:0.00}) no coincide con el total ({total:0.00}).");

        var cuotasOrdenadas = dto.Cuotas.OrderBy(c => c.Numero).ToList();
        int n = cuotasOrdenadas.Count;

        var plan = new PlanCuotas
        {
            EmisorId = emisorId,
            ReceptorId = dto.Venta.ReceptorId,
            CondicionOperacion = condicion,
            MontoTotal = total,
            MontoPagado = 0m,
            SaldoAdeudado = total,
            EstadoCobro = EstadoCobroPlan.Pendiente,
            VentaSnapshotJson = JsonSerializer.Serialize(dto.Venta),
            Observaciones = dto.Observaciones,
            FechaCreacion = DateTime.UtcNow
        };

        for (int i = 0; i < n; i++)
        {
            var def = cuotasOrdenadas[i];
            plan.Cuotas.Add(new Cuota
            {
                Numero = def.Numero,
                Monto = MontoDeCuota(def, total),
                Porcentaje = def.Porcentaje,
                FechaPactada = DateTime.SpecifyKind(def.FechaPactada, DateTimeKind.Utc),
                Estado = EstadoCuota.Pendiente,
                EsCuotaFinal = (i == n - 1)
            });
        }

        _ctx.Set<PlanCuotas>().Add(plan);
        await _ctx.SaveChangesAsync();

        // Emitir el DTE de la cuota 1 inmediatamente.
        await EmitirCuotaAsync(plan, plan.Cuotas.OrderBy(c => c.Numero).First(),
            dto.Venta, catFormaPagoId: PrimerFormaPago(dto.Venta), referencia: null);

        RecalcularPlan(plan);
        await _ctx.SaveChangesAsync();

        return MapPlan(plan);
    }

    public async Task<List<PlanCuotasDto>> GetAllAsync(int emisorId, bool soloConSaldo)
    {
        var query = _ctx.Set<PlanCuotas>()
            .Include(p => p.Cuotas)
            .Include(p => p.Receptor)
            .Where(p => p.EmisorId == emisorId);
        if (soloConSaldo)
            query = query.Where(p => p.SaldoAdeudado > 0);

        var planes = await query.OrderByDescending(p => p.FechaCreacion).ToListAsync();
        return planes.Select(MapPlan).ToList();
    }

    public async Task<PlanCuotasDto?> GetByIdAsync(int planId, int emisorId)
    {
        var plan = await _ctx.Set<PlanCuotas>()
            .Include(p => p.Cuotas)
            .Include(p => p.Receptor)
            .FirstOrDefaultAsync(p => p.Id == planId && p.EmisorId == emisorId);
        return plan is null ? null : MapPlan(plan);
    }

    public async Task<PlanCuotasDto> PagarCuotaAsync(
        int planId, int numeroCuota, RegistrarPagoCuotaDto dto, int emisorId)
    {
        var plan = await _ctx.Set<PlanCuotas>()
            .Include(p => p.Cuotas)
            .Include(p => p.Receptor)
            .FirstOrDefaultAsync(p => p.Id == planId && p.EmisorId == emisorId)
            ?? throw new InvalidOperationException("Plan de cuotas no encontrado.");

        // Buscar solo entre cuotas activas
        var cuota = plan.Cuotas.FirstOrDefault(c => c.Activo && c.Numero == numeroCuota)
            ?? throw new InvalidOperationException($"La cuota {numeroCuota} no existe en el plan.");

        if (cuota.Estado == EstadoCuota.Pagada)
            throw new InvalidOperationException($"La cuota {numeroCuota} ya fue pagada.");

        // Validar orden solo entre cuotas activas pendientes
        var anteriorPendiente = plan.Cuotas
            .Where(c => c.Activo && c.Numero < numeroCuota && c.Estado != EstadoCuota.Pagada)
            .OrderBy(c => c.Numero).FirstOrDefault();
        if (anteriorPendiente != null)
            throw new InvalidOperationException(
                $"Debe pagar primero la cuota {anteriorPendiente.Numero}.");

        var venta = JsonSerializer.Deserialize<CreateFacturaElectronicaDto>(plan.VentaSnapshotJson)
            ?? throw new InvalidOperationException("Snapshot de venta inválido.");

        await EmitirCuotaAsync(plan, cuota, venta, dto.CatFormaPagoId, dto.Referencia);

        RecalcularPlan(plan);
        await _ctx.SaveChangesAsync();

        return MapPlan(plan);
    }

    public async Task<PlanCuotasDto> RefinanciarPlanAsync(int planId, RefinanciarPlanDto dto, int emisorId, int? usuarioId)
    {
        var plan = await _ctx.Set<PlanCuotas>()
            .Include(p => p.Cuotas)
            .Include(p => p.Receptor)
            .FirstOrDefaultAsync(p => p.Id == planId && p.EmisorId == emisorId)
            ?? throw new InvalidOperationException("Plan de cuotas no encontrado.");

        var pendientes = plan.Cuotas.Where(c => c.Activo && c.Estado != EstadoCuota.Pagada).OrderBy(c => c.Numero).ToList();
        if (pendientes.Count == 0)
            throw new InvalidOperationException("El plan no tiene cuotas pendientes para refinanciar.");

        if (dto.NuevasCuotas.Count < 1)
            throw new InvalidOperationException("Debe definir al menos una cuota nueva.");

        decimal saldoPendiente = decimal.Round(pendientes.Sum(c => c.Monto), 2);
        decimal sumaNuevas = decimal.Round(dto.NuevasCuotas.Sum(c => MontoDeCuota(c, saldoPendiente)), 2);
        if (sumaNuevas != saldoPendiente)
            throw new InvalidOperationException(
                $"La suma de las nuevas cuotas ({sumaNuevas:0.00}) no coincide con el saldo pendiente ({saldoPendiente:0.00}).");

        // Snapshot anterior
        var planAnterior = pendientes.Select(c => new { c.Numero, c.Monto, c.FechaPactada }).ToList();

        // Desactivar pendientes
        foreach (var c in pendientes) c.Activo = false;

        // La cuota final previa ya no lo es
        foreach (var c in plan.Cuotas.Where(c => !c.Activo)) c.EsCuotaFinal = false;

        // Crear nuevas con numeración continua
        int maxNumero = plan.Cuotas.Max(c => c.Numero);
        var nuevasOrden = dto.NuevasCuotas.OrderBy(c => c.Numero).ToList();
        var nuevasEntidades = new List<Cuota>();
        for (int i = 0; i < nuevasOrden.Count; i++)
        {
            var def = nuevasOrden[i];
            var nueva = new Cuota
            {
                PlanCuotasId = plan.Id,
                Numero = maxNumero + i + 1,
                Monto = MontoDeCuota(def, saldoPendiente),
                Porcentaje = def.Porcentaje,
                FechaPactada = DateTime.SpecifyKind(def.FechaPactada, DateTimeKind.Utc),
                Estado = EstadoCuota.Pendiente,
                EsCuotaFinal = (i == nuevasOrden.Count - 1),
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };
            plan.Cuotas.Add(nueva);
            nuevasEntidades.Add(nueva);
        }

        _ctx.Set<HistorialRefinanciamiento>().Add(new HistorialRefinanciamiento
        {
            PlanCuotasId = plan.Id,
            UsuarioId = usuarioId,
            Motivo = dto.Motivo,
            PlanAnteriorJson = JsonSerializer.Serialize(planAnterior),
            PlanNuevoJson = JsonSerializer.Serialize(nuevasEntidades.Select(c => new { c.Numero, c.Monto, c.FechaPactada })),
            FechaCreacion = DateTime.UtcNow
        });

        RecalcularPlan(plan);
        await _ctx.SaveChangesAsync();
        return MapPlan(plan);
    }

    public async Task<MoraEstimadaDto> EstimarMoraCuotaAsync(int planId, int numero, int emisorId)
    {
        var plan = await _ctx.Set<PlanCuotas>()
            .Include(p => p.Cuotas)
            .FirstOrDefaultAsync(p => p.Id == planId && p.EmisorId == emisorId)
            ?? throw new InvalidOperationException("Plan de cuotas no encontrado.");

        var cuota = plan.Cuotas.FirstOrDefault(c => c.Activo && c.Numero == numero)
            ?? throw new InvalidOperationException($"La cuota {numero} no existe en el plan.");

        var cfg = await _ctx.Set<ConfiguracionCuotas>().FirstOrDefaultAsync(c => c.EmisorId == plan.EmisorId);
        decimal tasa = cfg?.TasaMoraMensual ?? 0.03m;
        int diasGracia = cfg?.DiasGracia ?? 3;
        bool moraHabilitada = cfg?.MoraHabilitada ?? true;

        var ahora = DateTime.UtcNow;
        decimal interesMora = _moraService.CalcularMora(
            cuota.Monto, cuota.FechaPactada, ahora, tasa, diasGracia, moraHabilitada);
        int diasAtraso = Math.Max(0, (ahora.Date - cuota.FechaPactada.Date).Days - diasGracia);

        return new MoraEstimadaDto
        {
            Numero = cuota.Numero,
            Monto = cuota.Monto,
            FechaPactada = cuota.FechaPactada,
            DiasAtraso = diasAtraso,
            InteresMora = interesMora,
            MoraHabilitada = moraHabilitada
        };
    }

    // --- helpers privados ---

    private async Task EmitirCuotaAsync(
        PlanCuotas plan, Cuota cuota, CreateFacturaElectronicaDto venta,
        int catFormaPagoId, string? referencia)
    {
        // Solo contar cuotas activas para el total y para netoPrevio
        int total = plan.Cuotas.Count(c => c.Activo);
        decimal netoPrevio = plan.Cuotas
            .Where(c => c.Activo && c.Numero < cuota.Numero && c.Estado == EstadoCuota.Pagada)
            .Sum(c => NetoSinIva(c.Monto));

        // Config de mora del emisor (usar defaults si no existe registro)
        var cfg = await _ctx.Set<ConfiguracionCuotas>().FirstOrDefaultAsync(c => c.EmisorId == plan.EmisorId);
        decimal tasa = cfg?.TasaMoraMensual ?? 0.03m;
        int diasGracia = cfg?.DiasGracia ?? 3;
        bool moraHabilitada = cfg?.MoraHabilitada ?? true;

        decimal interesMora = _moraService.CalcularMora(
            cuota.Monto, cuota.FechaPactada, DateTime.UtcNow, tasa, diasGracia, moraHabilitada);

        var dteDto = _strategy.ConstruirDteCuota(
            venta, cuota.Monto, cuota.Numero, total, netoPrevio, catFormaPagoId, referencia,
            esCuotaFinal: cuota.EsCuotaFinal, interesMora);

        var resp = await _facturaService.CreateAsync(dteDto, plan.EmisorId);

        cuota.InteresMora = interesMora;
        cuota.FacturaId = resp.Id;
        cuota.Estado = EstadoCuota.Pagada;
        cuota.FechaPago = DateTime.UtcNow;
    }

    private void RecalcularPlan(PlanCuotas plan)
    {
        // Filtrar por Activo && Pagada: hoy las cuotas pagadas no se desactivan,
        // pero la guarda explícita protege ante futuros cambios de flujo.
        plan.MontoPagado = plan.Cuotas.Where(c => c.Activo && c.Estado == EstadoCuota.Pagada).Sum(c => c.Monto);
        plan.SaldoAdeudado = decimal.Round(plan.MontoTotal - plan.MontoPagado, 2);

        if (plan.SaldoAdeudado <= 0)
            plan.EstadoCobro = EstadoCobroPlan.Pagada;
        else if (plan.MontoPagado > 0)
            plan.EstadoCobro = EstadoCobroPlan.Parcial;
        else
            plan.EstadoCobro = EstadoCobroPlan.Pendiente;
    }

    private static decimal MontoDeCuota(DefinicionCuotaDto def, decimal total)
    {
        if (def.Monto.HasValue) return decimal.Round(def.Monto.Value, 2);
        if (def.Porcentaje.HasValue) return decimal.Round(total * def.Porcentaje.Value / 100m, 2);
        throw new InvalidOperationException($"La cuota {def.Numero} no tiene monto ni porcentaje.");
    }

    private static decimal NetoSinIva(decimal montoConIva) =>
        decimal.Round(montoConIva / (1 + TasaIva), 2);

    private static int PrimerFormaPago(CreateFacturaElectronicaDto venta) =>
        venta.Resumen.Pagos.FirstOrDefault()?.CatFormaPagoId ?? 1; // 1 = billetes/efectivo por defecto

    private static PlanCuotasDto MapPlan(PlanCuotas p) => new()
    {
        Id = p.Id,
        ReceptorId = p.ReceptorId,
        ReceptorNombre = p.Receptor?.NombreRazonSocial,
        CondicionOperacion = p.CondicionOperacion,
        MontoTotal = p.MontoTotal,
        MontoPagado = p.MontoPagado,
        SaldoAdeudado = p.SaldoAdeudado,
        EstadoCobro = p.EstadoCobro.ToString(),
        // Solo exponer cuotas activas; las refinanciadas (Activo=false) quedan ocultas.
        Cuotas = p.Cuotas.Where(c => c.Activo).OrderBy(c => c.Numero).Select(c => new CuotaDto
        {
            Numero = c.Numero,
            Monto = c.Monto,
            FechaPactada = c.FechaPactada,
            Estado = c.Estado.ToString(),
            FechaPago = c.FechaPago,
            FacturaId = c.FacturaId,
            EsCuotaFinal = c.EsCuotaFinal,
            InteresMora = c.InteresMora
        }).ToList()
    };
}
