using FraFactu.Application.DTOs.Hacienda;
using FraFactu.Application.DTOs.Lotes;
using FraFactu.Application.Interfaces;
using FraFactu.Application.Interfaces.Hacienda;
using FraFactu.Application.Services;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Jobs
{
    public class ContingenciaAutoReportService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ContingenciaAutoReportService> _logger;

        public ContingenciaAutoReportService(
            IServiceScopeFactory scopeFactory,
            ILogger<ContingenciaAutoReportService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.LogInformation("ContingenciaAutoReportService iniciado.");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await VerificarYReportarContingenciasAsync(ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en ejecución de ContingenciaAutoReportService");
                }

                // Esperar 5 minutos antes de la siguiente ejecución
                await Task.Delay(TimeSpan.FromMinutes(5), ct);
            }
        }

        private async Task VerificarYReportarContingenciasAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var haciendaService = scope.ServiceProvider.GetRequiredService<IHaciendaApiService>();

            // 0. Marcar facturas PENDIENTE_ENVIO vencidas como PENDIENTE_LOTE (MH 3.1 Holguras)
            await MarcarFacturasVencidasAsync(scope, context, ct);

            // 0.1 Control del plazo de 72 h (desde el sello del Evento de Contingencia, regla 13.2.2)
            // e Informe Técnico de Contingencia >3 días (regla 13.2.1.1). Solo marca/alerta; no bloquea.
            // Aislado para no romper el resto.
            try
            {
                var facturaService = scope.ServiceProvider.GetRequiredService<IFacturaService>();
                await facturaService.MarcarDiferidosPorVencer72hAsync(ct);
                await facturaService.DetectarContingenciasParaInformeTecnicoAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CONTINGENCIA] Error al controlar plazos de contingencia (72 h / Informe Técnico)");
            }

            // 1. Buscar facturas en contingencia pendientes de asociar a evento
            // Deben estar en estado PENDIENTE_LOTE (marcado por FacturaService) y tener sugerencia de contingencia
            var facturasPendientes = await context.Facturas
                .Where(f => f.EstadoHacienda == "PENDIENTE_LOTE"
                            && f.TipoContingenciaSugerido != null
                            && f.EventoContingenciaId == null)
                .ToListAsync(ct);

            // 1.1 Reintentar creación de lote para eventos RECIBIDO sin lote asociado
            await ReintentarCreacionLotesAsync(scope, context, ct);

            if (!facturasPendientes.Any()) return;

            _logger.LogInformation("Se encontraron {Count} facturas en estado de contingencia pendientes de reporte.", facturasPendientes.Count);

            // 2. Verificar si MH ya responde
            bool mhResponde = await VerificarMHDisponibleAsync();
            if (!mhResponde)
            {
                _logger.LogInformation("MH no responde. Se continuará acumulando facturas en contingencia.");
                return; // Contingencia sigue activa
            }

            _logger.LogInformation("Conectividad con MH restablecida. Iniciando generación de eventos de contingencia.");

            // 3. Agrupar facturas por Emisor, Tipo de Contingencia, Sucursal y Caja
            var grupos = facturasPendientes
                .GroupBy(f => new { f.EmisorId, Tipo = f.TipoContingenciaSugerido!.Value, f.SucursalId, f.CajaId })
                .ToList();

            foreach (var grupo in grupos)
            {
                // MH permite máximo 5,000 DTEs por evento
                var facturasTodas = grupo.ToList();
                var chunks = facturasTodas
                    .Select((f, i) => new { f, i })
                    .GroupBy(x => x.i / 5000)
                    .Select(g => g.Select(x => x.f).ToList())
                    .ToList();

                foreach (var chunk in chunks)
                {
                    await ProcesarGrupoAsync(grupo.Key.EmisorId, grupo.Key.Tipo, chunk, context, haciendaService, scope, ct);
                }
            }
        }

        private async Task ProcesarGrupoAsync(int emisorId, int tipoContingencia, List<FacturaElectronica> facturas,
            ApplicationDbContext context, IHaciendaApiService haciendaService, IServiceScope scope, CancellationToken ct)
        {
            try
            {
                var fechaInicio = facturas.Min(f => f.FechaErrorEnvio) ?? DateTime.UtcNow;
                var fechaFin = facturas.Max(f => f.FechaErrorEnvio) ?? DateTime.UtcNow;

                // Margen de seguridad: Si la diferencia es muy corta, extender fecha fin 5 minutos para cubrir el gap
                if ((fechaFin - fechaInicio).TotalMinutes < 1)
                {
                    fechaFin = fechaInicio.AddMinutes(5);
                }

                // Asegurar que fechaFin no sea futura (aunque UtcNow lo hace dificil, pero por si acaso)
                if (fechaFin > DateTime.UtcNow) fechaFin = DateTime.UtcNow;

                var emisor = await context.Emisores.FindAsync(new object[] { emisorId }, ct);
                if (emisor == null)
                {
                    _logger.LogError("Emisor {EmisorId} no encontrado para procesar contingencia", emisorId);
                    return;
                }

                // Cargar primera factura con Sucursal/Caja para obtener treasury codes
                var primeraFactura = await context.Facturas
                    .Include(f => f.Sucursal!)
                        .ThenInclude(s => s.TipoEstablecimiento)
                    .Include(f => f.Caja)
                    .FirstOrDefaultAsync(f => f.Id == facturas.First().Id, ct);

                if (primeraFactura == null || primeraFactura.Sucursal == null || primeraFactura.Caja == null)
                {
                    _logger.LogError("No se pudo cargar Sucursal/Caja de la primera factura para contingencia");
                    return;
                }

                // 4. Crear Evento
                // Resolver CatTipoDocResponsableId desde el código guardado en Sucursal
                var codigoTipoDoc = primeraFactura.Sucursal.ContingenciaTipoDocResponsable ?? "36"; // Default: NIT
                var catTipoDoc = await context.CatDocsIdentidadReceptor
                    .FirstOrDefaultAsync(t => t.Codigo == codigoTipoDoc, ct);

                var evento = new EventoContingencia
                {
                    Version = 3,
                    Ambiente = emisor.AmbienteDestino.Codigo,
                    CodigoGeneracion = Guid.NewGuid().ToString().ToUpper(),
                    FechaTransmision = DateTime.UtcNow,
                    HoraTransmision = DateTime.UtcNow.TimeOfDay,
                    EmisorId = emisorId,
                    NombreResponsable = primeraFactura.Sucursal.ContingenciaNombreResponsable ?? "Departamento de IT",
                    CatTipoDocResponsableId = catTipoDoc?.Id ?? 1, // Fallback: Id=1 (NIT)
                    NumeroDocResponsable = primeraFactura.Sucursal.ContingenciaNumeroDocResponsable ?? emisor.Nit,
                    CatTipoEstablecimientoId = primeraFactura.Sucursal.CatTipoEstablecimientoId,
                    CodigoEstablecimientoMH = primeraFactura.Sucursal.CodigoEstablecimiento,
                    CodigoPuntoVenta = primeraFactura.Caja.CodPuntoVentaMH,
                    FechaInicioContingencia = fechaInicio.Date,
                    HoraInicioContingencia = fechaInicio.TimeOfDay,
                    FechaFinContingencia = fechaFin.Date,
                    HoraFinContingencia = fechaFin.TimeOfDay,
                    TipoContingencia = tipoContingencia,
                    MotivoContingencia = tipoContingencia == 1
                        ? "No disponibilidad de sistema del MH - Detectado autom."
                        : "Falla de conexión a Internet - Detectado autom.",
                    EstadoHacienda = "PENDIENTE",
                    CreadoAutomaticamente = true,
                    FechaCreacion = DateTime.UtcNow,
                    Activo = true
                };

                // Ajustes finos: NombreResponsable y Doc deberian venir de config real.
                // Usamos valores del emisor como fallback.

                // Corrección ortográfica en propiedad: 'MotivoContingencia'
                evento.MotivoContingencia = evento.MotivoContingencia;

                context.EventosContingencia.Add(evento);
                await context.SaveChangesAsync(ct);

                // 5. Asociar facturas
                foreach (var f in facturas)
                {
                    f.EventoContingenciaId = evento.Id;
                }
                await context.SaveChangesAsync(ct);

                // 5.1 Notificar a SmartCare por cada factura que provenga de un
                // prefill SmartCare — ViewUrl cambia de /facturas-pendientes a
                // /contingencia ahora que están asociadas al evento.
                var smartCareWebhook = scope.ServiceProvider.GetRequiredService<ISmartCareWebhookService>();
                foreach (var f in facturas.Where(x => !string.IsNullOrEmpty(x.SmartCareCorrelationId)))
                {
                    try
                    {
                        await smartCareWebhook.NotificarCambioEstadoAsync(f);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "[SmartCare-Webhook] Falla al notificar asociación automática de factura {FacturaId} al evento {EventoId}.",
                            f.Id, evento.Id);
                    }
                }

                // 6. Construir DTO y Enviar a Hacienda
                var eventoDto = new EventoContingenciaDto
                {
                    Identificacion = new IdentificacionContingenciaDto
                    {
                        Version = evento.Version,
                        Ambiente = evento.Ambiente,
                        CodigoGeneracion = evento.CodigoGeneracion,
                        FTransmision = evento.FechaTransmision.ToString("yyyy-MM-dd"),
                        HTransmision = evento.HoraTransmision.ToString(@"hh\:mm\:ss")
                    },
                    Emisor = new EmisorContingenciaDto
                    {
                        Nit = emisor.Nit,
                        Nombre = emisor.NombreRazonSocial,
                        NombreResponsable = evento.NombreResponsable,
                        TipoDocResponsable = primeraFactura.Sucursal.ContingenciaTipoDocResponsable ?? "36",
                        NumeroDocResponsable = evento.NumeroDocResponsable,
                        TipoEstablecimiento = primeraFactura.Sucursal.TipoEstablecimiento?.Codigo ?? "01",
                        CodEstableMH = primeraFactura.Sucursal.CodigoEstablecimiento,
                        CodPuntoVentaMH = primeraFactura.Caja.CodPuntoVentaMH,
                        Telefono = emisor.Telefono,
                        Correo = emisor.CorreoElectronico
                    },
                    DetalleDTE = facturas.Select((f, index) => new DetalleDteContingenciaDto
                    {
                        NoItem = index + 1,
                        CodigoGeneracion = f.CodigoGeneracion,
                        TipoDoc = f.TipoDocumento?.Codigo ?? "01"
                    }).ToList(),
                    Motivo = new MotivoContingenciaDto
                    {
                        FInicio = evento.FechaInicioContingencia.ToString("yyyy-MM-dd"),
                        HInicio = evento.HoraInicioContingencia.ToString(@"hh\:mm\:ss"),
                        FFin = evento.FechaFinContingencia.ToString("yyyy-MM-dd"),
                        HFin = evento.HoraFinContingencia.ToString(@"hh\:mm\:ss"),
                        TipoContingencia = evento.TipoContingencia,
                        MotivoContingencia = evento.MotivoContingencia
                    }
                };

                // Enviar
                try
                {
                    var respuesta = await haciendaService.EnviarEventoContingenciaAsync(emisorId, eventoDto);

                    if (respuesta.Estado == "RECIBIDO")
                    {
                        evento.EstadoHacienda = "RECIBIDO";
                        evento.SelloRecibido = respuesta.SelloRecibido;
                        evento.FechaTransmisionMH = DateTime.UtcNow;
                        _logger.LogInformation("Evento de contingencia {Id} enviado y RECIBIDO por MH.", evento.Id);

                        // Crear y enviar lote de contingencia automáticamente
                        await IntentarCrearLoteAsync(scope, emisorId, evento, facturas.Select(f => f.Id).ToList(), ct);
                    }
                    else
                    {
                        evento.EstadoHacienda = "RECHAZADO";
                        // Log detalle...
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al enviar evento de contingencia {Id} a MH", evento.Id);
                    // Queda en estado PENDIENTE para reintento manual o futuro
                }

                await context.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando grupo de contingencia para emisor {EmisorId}", emisorId);
            }
        }

        /// <summary>
        /// MH 3.1 Holguras: Marca facturas PENDIENTE_ENVIO como PENDIENTE_LOTE si su plazo de transmisión venció.
        /// Días normales: 24 horas. Último día del período fiscal: 30 minutos.
        /// No establece TipoContingenciaSugerido → el usuario debe crear el evento manualmente.
        /// </summary>
        private async Task MarcarFacturasVencidasAsync(IServiceScope scope, ApplicationDbContext context, CancellationToken ct)
        {
            var nowUtc = DateTime.UtcNow;
            var nowSV = nowUtc.AddHours(-6); // El Salvador UTC-6
            var ultimoDiaMes = DateTime.DaysInMonth(nowSV.Year, nowSV.Month);
            var esUltimoDia = nowSV.Day == ultimoDiaMes;

            var limiteHolgura = esUltimoDia
                ? nowUtc.AddMinutes(-30)
                : nowUtc.AddHours(-24);

            // FechaEmision (fecha) + HoraEmision (TimeSpan) = momento exacto de emisión
            // EF Core podría no soportar .Add() en query, así que filtramos con margen y refinamos en memoria
            var candidatas = await context.Facturas
                .Where(f => f.EstadoHacienda == "PENDIENTE_ENVIO"
                            && f.FechaEmision <= limiteHolgura.Date)
                .ToListAsync(ct);

            var facturasVencidas = candidatas
                .Where(f => f.FechaEmision.Add(f.HoraEmision) < limiteHolgura)
                .ToList();

            if (!facturasVencidas.Any()) return;

            foreach (var f in facturasVencidas)
            {
                f.EstadoHacienda = "PENDIENTE_LOTE";
                f.FechaErrorEnvio = nowUtc;
                f.DetalleErrorEnvio = "Plazo de transmisión vencido (holgura MH 3.1)";
            }

            await context.SaveChangesAsync(ct);
            _logger.LogWarning("[CONTINGENCIA-AUTO] {Count} facturas PENDIENTE_ENVIO marcadas como vencidas (PENDIENTE_LOTE)",
                facturasVencidas.Count);

            // Aviso a SmartCare por cada factura proveniente de un prefill SmartCare.
            // ViewUrl no cambia funcionalmente (PENDIENTE_LOTE sin evento sigue
            // yendo a /facturas-pendientes igual que PENDIENTE_ENVIO), pero el
            // factura_estado sí (de 'pendiente_envio' a 'pendiente_lote'), y el
            // badge en SmartCare pasa a "Pendiente (contingencia)".
            var smartCareWebhook = scope.ServiceProvider.GetRequiredService<ISmartCareWebhookService>();
            foreach (var f in facturasVencidas.Where(x => !string.IsNullOrEmpty(x.SmartCareCorrelationId)))
            {
                try
                {
                    await smartCareWebhook.NotificarCambioEstadoAsync(f);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "[SmartCare-Webhook] Falla al notificar vencimiento de factura {FacturaId} a SmartCare.",
                        f.Id);
                }
            }
        }

        /// <summary>
        /// Busca eventos RECIBIDO que no tienen lote asociado y con reintentos < 3, e intenta crear el lote.
        /// </summary>
        private async Task ReintentarCreacionLotesAsync(IServiceScope scope, ApplicationDbContext context, CancellationToken ct)
        {
            var eventossinLote = await context.EventosContingencia
                .Where(e => e.EstadoHacienda == "RECIBIDO"
                            && e.ReintentosLote < 3
                            && !context.Lotes.Any(l => l.EventoContingenciaId == e.Id))
                .Include(e => e.Detalles)
                .ToListAsync(ct);

            foreach (var evento in eventossinLote)
            {
                var facturaIds = evento.Detalles.Select(d => d.FacturaElectronicaId).ToList();
                if (!facturaIds.Any()) continue;

                await IntentarCrearLoteAsync(scope, evento.EmisorId, evento, facturaIds, ct);
                await context.SaveChangesAsync(ct);
            }
        }

        /// <summary>
        /// Intenta crear y enviar un lote de contingencia para el evento dado.
        /// Incrementa ReintentosLote en caso de fallo.
        /// </summary>
        private async Task IntentarCrearLoteAsync(IServiceScope scope, int emisorId, EventoContingencia evento, List<int> facturaIds, CancellationToken ct)
        {
            try
            {
                var loteService = scope.ServiceProvider.GetRequiredService<ILoteService>();

                var crearLoteDto = new CrearLoteDto
                {
                    FacturaIds = facturaIds,
                    EsContingencia = true,
                    EventoContingenciaId = evento.Id
                };

                var loteCreado = await loteService.CrearLoteAsync(emisorId, crearLoteDto);
                var loteEnviado = await loteService.EnviarLoteAsync(loteCreado.Id);

                _logger.LogInformation(
                    "Lote {LoteId} creado y enviado automáticamente para evento de contingencia {EventoId}. Estado: {Estado}",
                    loteEnviado.Id, evento.Id, loteEnviado.Estado);
            }
            catch (Exception ex)
            {
                evento.ReintentosLote++;
                _logger.LogError(ex,
                    "Error creando/enviando lote para evento de contingencia {EventoId}. Intento {N}/3.",
                    evento.Id, evento.ReintentosLote);
            }
        }

        private async Task<bool> VerificarMHDisponibleAsync()
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                // URL de prueba de recepción DTE para verificar si el servidor responde (aunque sea 401 Unauthorized)
                // Usamos la URL de producción o pruebas según configuración?
                // Para simple check de conectividad general a MH, la URL base pública suele funcionar.
                // Usaremos una URL hardcoded conocida o inyectada.

                // Intentamos conectar a google primero para descartar internet local
                try
                {
                    await client.GetAsync("https://www.google.com");
                }
                catch
                {
                    return false; // Si no hay google, asumimos "no mh" (contingencia por internet)
                }

                var response = await client.GetAsync("https://api.ministerio-hacienda.gob.sv/"); // URL Base
                // Si responde cualquier cosa (200, 404, 403), el servidor está arriba.
                // Si tira excepción, está abajo.
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
