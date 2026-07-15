using FraFactu.Application.Common.Interfaces;
using FraFactu.Application.DTOs.Facturas;
using FraFactu.Application.Interfaces;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Services
{
    /// <summary>
    /// Jobs de fondo de contingencia (Fase 4 del refactor de FacturaService):
    /// control de plazo 72 h e Informe Técnico. Movidos verbatim desde FacturaService.
    /// </summary>
    public class FacturaJobsService : IFacturaJobsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FacturaJobsService> _logger;
        private readonly ITelemetryService _telemetry;

        public FacturaJobsService(
            ApplicationDbContext context,
            ILogger<FacturaJobsService> logger,
            ITelemetryService telemetry)
        {
            _context = context;
            _logger = logger;
            _telemetry = telemetry;
        }

        // Plazo máximo (horas) para transmitir un DTE en contingencia desde el sello del Evento
        // de Contingencia (Normativa DTE, Cuadro 4 y regla 13.2.2: 72 h desde el Sello de Recepción
        // del Evento de Contingencia).
        private const int PlazoTransmisionHoras = 72;
        // Antelación con la que se empieza a avisar que el plazo de 72 h está por vencer.
        private static readonly TimeSpan VentanaAvisoPlazo72h = TimeSpan.FromHours(12);
        // Marca idempotente que se deja en Observaciones para no re-marcar/avisar en cada pasada.
        internal const string MarcaPlazo72hVencido = "[PLAZO-72H-VENCIDO]";
        // Días de contingencia consecutivos que obligan a presentar el Informe Técnico (regla 13.2.1.1).
        private const int DiasContingenciaInformeTecnico = 3;

        public async Task<ControlPlazo72hResultadoDto> MarcarDiferidosPorVencer72hAsync(CancellationToken cancellationToken = default)
        {
            var ahora = DateTime.UtcNow;
            var resultado = new ControlPlazo72hResultadoDto();

            // Facturas en contingencia (PENDIENTE_LOTE) asociadas a un Evento de Contingencia.
            var facturas = await _context.Facturas
                .Where(f => f.EstadoHacienda == "PENDIENTE_LOTE" && f.EventoContingenciaId != null)
                .ToListAsync(cancellationToken);

            if (facturas.Count == 0)
                return resultado;

            // El plazo de 72 h corre desde el sello del Evento de Contingencia (FechaTransmisionMH).
            var eventoIds = facturas.Select(f => f.EventoContingenciaId!.Value).Distinct().ToList();
            var eventos = await _context.EventosContingencia
                .Where(e => eventoIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, cancellationToken);

            foreach (var factura in facturas)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (!eventos.TryGetValue(factura.EventoContingenciaId!.Value, out var evento))
                    continue;

                // El plazo de 72 h arranca con el sello del evento; sin sello aún no corre.
                if (evento.FechaTransmisionMH == null)
                    continue;

                var limite = evento.FechaTransmisionMH.Value.AddHours(PlazoTransmisionHoras);
                var restante = limite - ahora;

                if (restante <= TimeSpan.Zero)
                {
                    resultado.Vencidas++;

                    // Marcar una sola vez (idempotente por el texto en Observaciones).
                    var yaMarcada = factura.Observaciones != null
                        && factura.Observaciones.Contains(MarcaPlazo72hVencido);
                    if (!yaMarcada)
                    {
                        resultado.NuevasVencidas++;
                        var aviso = $"{MarcaPlazo72hVencido} El plazo de 72 h para transmitir este DTE en contingencia " +
                                    $"venció el {limite:yyyy-MM-dd HH:mm} UTC (sello del evento: {evento.FechaTransmisionMH:yyyy-MM-dd HH:mm} UTC). " +
                                    "Debe invalidarse y reemitirse.";
                        factura.Observaciones = string.IsNullOrEmpty(factura.Observaciones)
                            ? aviso
                            : $"{factura.Observaciones} {aviso}";

                        _logger.LogWarning(
                            "[PLAZO-72H] Factura {FacturaId} ({NumeroControl}) superó el plazo de 72 h desde el sello del evento {EventoId} (selló {Sello:u}, venció {Limite:u})",
                            factura.Id, factura.NumeroControl, evento.Id, evento.FechaTransmisionMH, limite);

                        _telemetry.TrackEvent("smartix.contingencia.plazo72h_vencido",
                            properties: new Dictionary<string, string>
                            {
                                ["facturaId"] = factura.Id.ToString(),
                                ["numeroControl"] = factura.NumeroControl ?? string.Empty,
                                ["emisorId"] = factura.EmisorId.ToString(),
                                ["eventoContingenciaId"] = evento.Id.ToString()
                            });
                    }
                }
                else if (restante <= VentanaAvisoPlazo72h)
                {
                    resultado.PorVencer++;
                    _logger.LogWarning(
                        "[PLAZO-72H] Factura {FacturaId} ({NumeroControl}) por vencer el plazo de 72 h en {Horas:F1} h (sello del evento {EventoId})",
                        factura.Id, factura.NumeroControl, restante.TotalHours, evento.Id);
                }
            }

            if (resultado.NuevasVencidas > 0)
                await _context.SaveChangesAsync(cancellationToken);

            if (resultado.Vencidas > 0 || resultado.PorVencer > 0)
            {
                _logger.LogInformation(
                    "[PLAZO-72H] Control de plazo: {Vencidas} vencidas ({Nuevas} nuevas), {PorVencer} por vencer",
                    resultado.Vencidas, resultado.NuevasVencidas, resultado.PorVencer);
            }

            return resultado;
        }

        public async Task<InformeTecnicoContingenciaResultadoDto> DetectarContingenciasParaInformeTecnicoAsync(CancellationToken cancellationToken = default)
        {
            var ahora = DateTime.UtcNow;
            var resultado = new InformeTecnicoContingenciaResultadoDto();
            var umbral = ahora.AddDays(-DiasContingenciaInformeTecnico);

            // Contingencia en curso por sujeto pasivo (emisor): facturas aún en PENDIENTE_LOTE
            // sin transmitir. Si la contingencia del emisor (desde la factura más antigua que entró
            // en contingencia) persiste > 3 días, debe presentar el Informe Técnico a MH antes de
            // transmitir el Evento de Contingencia (regla 13.2.1.1).
            var porEmisor = await _context.Facturas
                .Where(f => f.EstadoHacienda == "PENDIENTE_LOTE" && f.FechaErrorEnvio != null)
                .GroupBy(f => f.EmisorId)
                .Select(g => new
                {
                    EmisorId = g.Key,
                    Inicio = g.Min(f => f.FechaErrorEnvio),
                    Cantidad = g.Count()
                })
                .ToListAsync(cancellationToken);

            foreach (var grupo in porEmisor)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (grupo.Inicio == null || grupo.Inicio.Value > umbral)
                    continue;

                resultado.EmisoresQueRequierenInforme++;
                var dias = (ahora - grupo.Inicio.Value).TotalDays;

                _logger.LogWarning(
                    "[INFORME-TECNICO] El emisor {EmisorId} tiene una contingencia que persiste {Dias:F1} días (> {Umbral}) con {Cantidad} DTE en PENDIENTE_LOTE. " +
                    "Debe presentar el Informe Técnico de Contingencia a Hacienda antes de transmitir el Evento de Contingencia (regla 13.2.1.1).",
                    grupo.EmisorId, dias, DiasContingenciaInformeTecnico, grupo.Cantidad);

                _telemetry.TrackEvent("smartix.contingencia.informe_tecnico_requerido",
                    properties: new Dictionary<string, string>
                    {
                        ["emisorId"] = grupo.EmisorId.ToString(),
                        ["diasContingencia"] = dias.ToString("F1"),
                        ["dtePendientes"] = grupo.Cantidad.ToString()
                    });
            }

            return resultado;
        }
    }
}
