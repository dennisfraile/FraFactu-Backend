using FraFactu.Application.DTOs.Lotes;
using FraFactu.Application.DTOs.Common;
using FraFactu.Infrastructure.Helpers.Pagination;
using FraFactu.Application.Services;
using FraFactu.Application.Interfaces;
using FraFactu.Application.Interfaces.Hacienda;
using FraFactu.Application.DTOs.Hacienda;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace FraFactu.Infrastructure.Services;

/// <summary>
/// Implementación del servicio de lotes
/// </summary>
public class LoteService : ILoteService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LoteService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHaciendaApiService _haciendaApiService;
    private readonly IFacturaService _facturaService;
    private readonly IDteSignerService _signerService;
    private readonly IHaciendaAuthService _authService;
    private readonly IInventarioIntegrationService _inventarioService;
    private readonly IEmailService _emailService;
    private readonly IEncryptionService _encryptionService;
    private readonly ISmartCareWebhookService _smartCareWebhook;

    public LoteService(
        ApplicationDbContext context,
        ILogger<LoteService> logger,
        IHttpClientFactory httpClientFactory,
        ICurrentUserService currentUserService,
        IHaciendaApiService haciendaApiService,
        IFacturaService facturaService,
        IDteSignerService signerService,
        IHaciendaAuthService authService,
        IInventarioIntegrationService inventarioService,
        IEmailService emailService,
        IEncryptionService encryptionService,
        ISmartCareWebhookService smartCareWebhook)
    {
        _context = context;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _currentUserService = currentUserService;
        _haciendaApiService = haciendaApiService;
        _facturaService = facturaService;
        _signerService = signerService;
        _authService = authService;
        _inventarioService = inventarioService;
        _emailService = emailService;
        _encryptionService = encryptionService;
        _smartCareWebhook = smartCareWebhook;
    }

    // Tras procesar un lote, dispara webhook a SmartCare por cada factura
    // (a) que provenga de un prefill SmartCare (SmartCareCorrelationId != null), y
    // (b) cuyo estado quedó en PROCESADO o RECHAZADO tras el envio.
    // Fire-and-forget por detalle: una excepcion en uno no aborta los demás ni el flujo del lote.
    private async Task NotificarSmartCareDeLoteAsync(Lote lote)
    {
        foreach (var detalle in lote.Detalles)
        {
            var factura = detalle.FacturaElectronica;
            if (factura == null) continue;
            if (string.IsNullOrEmpty(factura.SmartCareCorrelationId)) continue;
            if (factura.EstadoHacienda is not "PROCESADO" and not "RECHAZADO") continue;

            try
            {
                await _smartCareWebhook.NotificarCambioEstadoAsync(factura);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[SmartCare-Webhook] Falla al notificar resultado de lote {LoteId} para factura {FacturaId} (CorrelationId={Cid}). El boton 'Ver en Smartix' en SmartCare puede quedar apuntando al estado anterior hasta el siguiente cambio.",
                    lote.Id, factura.Id, factura.SmartCareCorrelationId);
            }
        }
    }

    private string DecryptField(string? value, string fieldName, bool optional = false)
    {
        if (string.IsNullOrEmpty(value))
        {
            if (optional) return string.Empty;
            throw new InvalidOperationException(
                $"El campo '{fieldName}' del emisor está vacío o no configurado. " +
                "Configure las credenciales de Hacienda en la sección del emisor.");
        }

        try
        {
            return _encryptionService.Decrypt(value);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException(
                $"Error al desencriptar '{fieldName}' del emisor. " +
                "Verifique que la Encryption:MasterKey en Azure App Settings coincida con la que se usó al guardar las credenciales.", ex);
        }
    }

    public async Task<LoteDto> CrearLoteAsync(int emisorId, CrearLoteDto dto)
    {
        _logger.LogInformation("Creando lote para emisor {EmisorId} con {Total} facturas",
            emisorId, dto.FacturaIds.Count);

        // 1. Validar límite de 100 DTEs
        if (dto.FacturaIds.Count > 100)
            throw new InvalidOperationException("El lote no puede exceder 100 facturas");

        // 2. Obtener facturas y validar que existan y pertenezcan al emisor
        var facturas = await _context.Facturas
            .Where(f => dto.FacturaIds.Contains(f.Id) && f.EmisorId == emisorId)
            .Include(f => f.TipoDocumento)
            .Include(f => f.Emisor)
                .ThenInclude(e => e.AmbienteDestino)
            .ToListAsync();

        if (facturas.Count != dto.FacturaIds.Count)
        {
            var faltantes = dto.FacturaIds.Count - facturas.Count;
            throw new InvalidOperationException(
                $"{faltantes} facturas no existen o no pertenecen al emisor");
        }

        // 3. Validar que todas estén firmadas (requerido para envío en lotes)
        var noFirmadas = facturas.Where(f => string.IsNullOrEmpty(f.JsonFirmado)).ToList();
        if (noFirmadas.Any())
        {
            // Para lotes de contingencia, firmamos automáticamente
            if (dto.EsContingencia)
            {
                _logger.LogInformation(
                    "Firmando {Count} facturas de contingencia antes de crear lote",
                    noFirmadas.Count);

                var firstEmisor = noFirmadas.First().Emisor;
                var llavePrivada = DecryptField(firstEmisor.MhLlavePrivada, "MhLlavePrivada");
                var passPrivada = DecryptField(firstEmisor.MhPassPrivada, "MhPassPrivada", optional: true);

                foreach (var factura in noFirmadas)
                {
                    var jsonDte = await _facturaService.GenerateJsonDteAsync(factura.Id, emisorId);
                    var documentoFirmado = _signerService.FirmarDocumento(
                        jsonDte, llavePrivada, passPrivada);
                    factura.JsonFirmado = documentoFirmado;
                    _logger.LogInformation("Factura {FacturaId} firmada para contingencia", factura.Id);
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                var ids = string.Join(", ", noFirmadas.Select(f => f.Id));
                throw new InvalidOperationException(
                    $"{noFirmadas.Count} facturas no están firmadas. IDs: {ids}");
            }
        }

        // 4. Validar que no estén en otro lote pendiente o procesado
        var enOtroLote = facturas.Where(f => f.LoteId.HasValue).ToList();
        if (enOtroLote.Any())
        {
            var ids = string.Join(", ", enOtroLote.Select(f => f.Id));
            throw new InvalidOperationException(
                $"{enOtroLote.Count} facturas ya están en otro lote. IDs: {ids}");
        }

        // 5. Validar que todas sean del mismo ambiente (obtener del emisor)
        var ambientes = facturas.Select(f => f.Emisor.AmbienteDestino.Codigo).Distinct().ToList();
        if (ambientes.Count > 1)
        {
            throw new InvalidOperationException(
                "Todas las facturas deben ser del mismo ambiente (Pruebas o Producción)");
        }

        // VALIDACIONES DE CONTINGENCIA
        if (dto.EsContingencia)
        {
            // Validación 1: EventoContingenciaId es obligatorio
            if (!dto.EventoContingenciaId.HasValue)
            {
                throw new InvalidOperationException(
                    "Para lotes de contingencia es obligatorio especificar el EventoContingenciaId");
            }

            // Cargar evento de contingencia
            var evento = await _context.EventosContingencia
                .Include(e => e.Detalles)
                .FirstOrDefaultAsync(e => e.Id == dto.EventoContingenciaId.Value && e.EmisorId == emisorId);

            if (evento == null)
            {
                throw new InvalidOperationException(
                    $"Evento de contingencia {dto.EventoContingenciaId} no encontrado");
            }

            // Validación 2: El evento debe tener sello de Hacienda
            if (string.IsNullOrEmpty(evento.SelloRecibido))
            {
                throw new InvalidOperationException(
                    "No se puede crear lote: el evento de contingencia aún no tiene sello de Hacienda. " +
                    "Primero debe transmitir el evento y obtener su sello.");
            }

            // Validación 3: Validar que todos los documentos pertenezcan al evento
            var documentosEvento = evento.Detalles
                .Select(d => d.CodigoGeneracion)
                .ToHashSet();

            var codigosFacturas = facturas.Select(f => f.CodigoGeneracion).ToList();
            var docsInvalidos = codigosFacturas
                .Where(codigo => !documentosEvento.Contains(codigo))
                .ToList();

            if (docsInvalidos.Any())
            {
                throw new InvalidOperationException(
                    $"Las siguientes facturas NO pertenecen al evento de contingencia: {string.Join(", ", docsInvalidos)}. " +
                    $"Solo puede incluir documentos listados en ContingenciaDetalle del evento.");
            }

            // Validación 4: Validar plazo de 72 horas desde sello del evento
            if (evento.FechaTransmisionMH.HasValue)
            {
                DateTime fechaHoraEvento = evento.FechaTransmisionMH.Value;
                TimeSpan tiempoTranscurrido = DateTime.UtcNow - fechaHoraEvento;

                if (tiempoTranscurrido.TotalHours > 72)
                {
                    throw new InvalidOperationException(
                        $"El plazo para enviar lote del evento ha expirado. " +
                        $"Han transcurrido {tiempoTranscurrido.TotalHours:F1} horas desde que se obtuvo el sello del evento. " +
                        $"El plazo máximo es de 72 horas.");
                }

                _logger.LogInformation(
                    "Lote de contingencia dentro de plazo: {HorasTranscurridas:F1} de 72 horas",
                    tiempoTranscurrido.TotalHours);
            }
        }

        // 6. Crear lote (obtener ambiente del emisor)
        var emisor = facturas.First().Emisor;
        var lote = new Lote
        {
            EmisorId = emisorId,
            CodigoLote = Guid.NewGuid(),
            TotalDtes = facturas.Count,
            Ambiente = emisor.AmbienteDestino.Codigo,
            EsContingencia = dto.EsContingencia,
            EventoContingenciaId = dto.EventoContingenciaId,
            Estado = "Pendiente",
            TotalPendientes = facturas.Count,
            CreadoPor = _currentUserService.GetUsuarioNombre() ?? "Sistema" // Usuario autenticado o Sistema si no hay usuario
        };

        _context.Lotes.Add(lote);
        await _context.SaveChangesAsync();

        // 7. Crear detalles y marcar facturas
        int numeroItem = 1;
        foreach (var factura in facturas.OrderBy(f => f.Id))
        {
            var detalle = new LoteDetalle
            {
                LoteId = lote.Id,
                FacturaElectronicaId = factura.Id,
                NumeroItem = numeroItem++,
                EstadoDte = "Pendiente"
            };

            _context.LoteDetalles.Add(detalle);

            // Marcar factura como parte del lote
            factura.LoteId = lote.Id;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Lote {LoteId} creado exitosamente con {Total} facturas",
            lote.Id, lote.TotalDtes);

        return await ObtenerLoteAsync(lote.Id);
    }

    public async Task<LoteDto> EnviarLoteAsync(int loteId)
    {
        _logger.LogInformation("Iniciando envío de lote {LoteId}", loteId);

        // 1. Obtener lote con todos los datos necesarios
        var lote = await _context.Lotes
            .Include(l => l.Emisor)
                .ThenInclude(e => e.AmbienteDestino)
            .Include(l => l.Detalles)
                .ThenInclude(d => d.FacturaElectronica)
            .FirstOrDefaultAsync(l => l.Id == loteId);

        if (lote == null)
            throw new KeyNotFoundException($"Lote {loteId} no encontrado");

        // 2. Validar estado
        if (lote.Estado != "Pendiente")
            throw new InvalidOperationException(
                $"El lote no puede enviarse en estado '{lote.Estado}'");

        // 3. Validar horario permitido
        if (!EstaEnHorarioPermitido(lote.Ambiente, lote.EsContingencia))
        {
            var mensaje = lote.Ambiente == "00"
                ? "Horario permitido: 08:00 - 17:00"
                : "Horario permitido: 22:00 - 05:00";
            throw new InvalidOperationException(
                $"Fuera del horario permitido para envío de lotes. {mensaje}");
        }

        // 4. Construir payload con los documentos ya firmados
        var documentosFirmados = lote.Detalles
            .OrderBy(d => d.NumeroItem)
            .Select(d => d.FacturaElectronica.JsonFirmado)
            .Where(json => !string.IsNullOrEmpty(json))
            .Select(json => json!)
            .ToList();

        // 5. Actualizar estado a Enviando
        // No guardamos jsonLote completo todavía pues el ID Envio lo genera el servicio ahora
        lote.Estado = "Enviando";
        lote.FechaModificacion = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        try
        {
            // 6. Enviar a MH usando Servicio
            _logger.LogInformation("Enviando lote {CodigoLote} a MH usando HaciendaApiService", lote.CodigoLote);

            var respuesta = await _haciendaApiService.TransmitirLoteDtesFirmadosAsync(lote.EmisorId, documentosFirmados);

            // 7. Procesar respuesta
            // Mapear estado de Hacienda al estado interno
            // - "RECIBIDO" = lote en cola, esperando procesamiento → "Enviado"
            // - "PROCESADO" = lote procesado por Hacienda → "Procesado"
            lote.Estado = respuesta.Estado == "PROCESADO" ? "Procesado" : "Enviado";
            lote.FechaEnvio = DateTime.UtcNow;

            // IdEnvio viene de la respuesta de Hacienda
            if (Guid.TryParse(respuesta.IdEnvio, out var idEnvioParsed))
            {
                lote.IdEnvio = idEnvioParsed;
            }

            // Proteger FhProcesamiento si viene vacío (backup con DateTime.UtcNow)
            // Usar ParseExact con formato dd/MM/yyyy HH:mm:ss (formato de Hacienda)
            // para evitar fallos por cultura del servidor (en-US en Azure interpreta MM/dd/yyyy)
            lote.FhProcesamiento = !string.IsNullOrEmpty(respuesta.FhProcesamiento)
                ? DateTime.SpecifyKind(
                    DateTime.ParseExact(respuesta.FhProcesamiento, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture),
                    DateTimeKind.Utc)
                : DateTime.UtcNow;

            lote.CodigoRespuesta = respuesta.CodigoMsg;
            lote.DescripcionRespuesta = respuesta.DescripcionMsg;
            lote.JsonRespuesta = JsonSerializer.Serialize(respuesta);

            _logger.LogInformation("Lote {LoteId} procesado. Código: {Codigo}",
                loteId, respuesta.CodigoMsg);
        }
        catch (Exception ex)
        {
            lote.Estado = "Error";
            lote.DescripcionRespuesta = ex.Message;
            _logger.LogError(ex, "Error al enviar lote {LoteId}", loteId);
        }

        lote.FechaModificacion = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // 8. Solo consultar estados individuales si Hacienda ya procesó el lote
        if (lote.Estado == "Procesado")
        {
            _logger.LogInformation("Consultando estados individuales para lote {LoteId}", loteId);
            await ConsultarEstadosIndividualesAsync(loteId);
        }
        else
        {
            _logger.LogInformation(
                "Lote {LoteId} fue recibido por MH (estado: {Estado}). Consultar estados más tarde cuando Hacienda lo procese.",
                loteId, lote.Estado);
        }

        return await ObtenerLoteAsync(loteId);
    }

    public async Task<LoteDto> ConsultarEstadosIndividualesAsync(int loteId)
    {
        _logger.LogInformation("Consultando estados individuales de lote {LoteId}", loteId);

        var lote = await _context.Lotes
            .Include(l => l.Emisor)
                .ThenInclude(e => e.AmbienteDestino)
            .Include(l => l.Detalles)
                .ThenInclude(d => d.FacturaElectronica)
                    .ThenInclude(f => f.TipoDocumento)
            .FirstOrDefaultAsync(l => l.Id == loteId);

        if (lote == null)
            throw new KeyNotFoundException($"Lote {loteId} no encontrado");

        // Permitir consultar estados individuales si el lote ya fue enviado a Hacienda
        // - "Enviado": Usuario quiere verificar si Hacienda ya procesó los DTEs
        // - "Enviando": Lote en proceso de transmisión
        // - "Procesado": Hacienda ya procesó el lote
        if (!new[] { "Enviado", "Enviando", "Procesado" }.Contains(lote.Estado))
            throw new InvalidOperationException(
                $"El lote no puede ser consultado. Estado actual: {lote.Estado}");

        int aprobados = 0;
        int rechazados = 0;
        int errores = 0;
        var facturasParaNuevoLote = new List<int>();

        // Timeout: si el lote lleva más de 48 horas en "Enviado" con DTEs pendientes,
        // liberar los pendientes y cerrar el lote
        if (lote.Estado == "Enviado" && lote.FechaEnvio.HasValue)
        {
            var horasDesdeEnvio = (DateTime.UtcNow - lote.FechaEnvio.Value).TotalHours;
            if (horasDesdeEnvio > 48)
            {
                _logger.LogWarning("Lote {LoteId} lleva {Horas:F1}h en estado Enviado. Cerrando por timeout.", loteId, horasDesdeEnvio);

                foreach (var detalle in lote.Detalles)
                {
                    if (detalle.EstadoDte == "PROCESADO" || detalle.EstadoDte == "RECHAZADO")
                        continue;

                    detalle.EstadoDte = "RECHAZADO";
                    detalle.FechaConsulta = DateTime.UtcNow;
                    detalle.ObservacionesRechazo = "Timeout: sin respuesta de Hacienda después de 48 horas";

                    // Liberar factura del lote para que pueda re-enviarse
                    detalle.FacturaElectronica.LoteId = null;

                    // Liberar stock reservado
                    await _inventarioService.LiberarReservasAsync(detalle.FacturaElectronicaId);
                }

                lote.TotalAprobados = lote.Detalles.Count(d => d.SelloRecibido != null);
                lote.TotalRechazados = lote.Detalles.Count(d => d.EstadoDte == "RECHAZADO");
                lote.TotalPendientes = 0;
                lote.Estado = "Procesado";
                lote.FechaUltimaConsulta = DateTime.UtcNow;
                lote.FechaModificacion = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogWarning("Lote {LoteId} cerrado por timeout (48h). DTEs pendientes liberados.", loteId);
                return await ObtenerLoteAsync(loteId);
            }
        }

        try
        {
            // CORRECCION: Usar IdEnvio (UUID de Hacienda) en lugar de CodigoLote (UUID interno)
            // Manual Hacienda: GET /fesv/recepcion/consultadtelote/{idEnvio}
            var idConsulta = lote.IdEnvio != Guid.Empty ? lote.IdEnvio.ToString() : lote.CodigoLote.ToString();
            var resultadoLote = await _haciendaApiService.ConsultarEstadoLoteAsync(lote.EmisorId, idConsulta!);

            _logger.LogInformation("Consulta Lote {Codigo} (IdEnvio={IdEnvio}): Estado={Estado}", lote.CodigoLote, idConsulta, resultadoLote.EstadoLote);

            if (resultadoLote.EstadoLote == "EN_PROCESO")
            {
                // Fallback: consultar cada DTE individualmente ya que el endpoint de lote
                // puede devolver 204/EN_PROCESO aunque los DTEs ya estén procesados
                _logger.LogInformation("Lote {Id} sin respuesta de lote, consultando DTEs individualmente...", lote.Id);

                foreach (var detalle in lote.Detalles)
                {
                    if (detalle.EstadoDte == "PROCESADO" || detalle.EstadoDte == "RECHAZADO")
                        continue;

                    var codigoGeneracion = detalle.FacturaElectronica.CodigoGeneracion;
                    var tipoDte = detalle.FacturaElectronica.TipoDocumento?.Codigo ?? "01";

                    try
                    {
                        var resultadoDte = await _haciendaApiService.ConsultarEstadoDteAsync(
                            lote.EmisorId,
                            codigoGeneracion.ToString(),
                            tipoDte
                        );

                        if (resultadoDte == null) continue;

                        if (resultadoDte.Estado == "PROCESADO")
                        {
                            // Si MH dice PROCESADO pero no devuelve sello, marcar como RECHAZADO
                            if (string.IsNullOrEmpty(resultadoDte.SelloRecibido))
                            {
                                _logger.LogWarning("[LOTE] DTE {CodigoGeneracion} reportado como PROCESADO sin sello. Se marca como RECHAZADO.",
                                    detalle.FacturaElectronica.CodigoGeneracion);

                                detalle.EstadoDte = "RECHAZADO";
                                detalle.FechaConsulta = DateTime.UtcNow;
                                detalle.ObservacionesRechazo = "MH respondió PROCESADO pero no devolvió sello de recepción";

                                detalle.FacturaElectronica.EstadoHacienda = "RECHAZADO";
                                detalle.FacturaElectronica.Observaciones = "MH respondió PROCESADO pero no devolvió sello de recepción";

                                await _context.SaveChangesAsync();
                                continue;
                            }

                            detalle.EstadoDte = "PROCESADO";
                            detalle.SelloRecibido = resultadoDte.SelloRecibido;
                            detalle.FechaConsulta = DateTime.UtcNow;

                            detalle.FacturaElectronica.EstadoHacienda = "PROCESADO";
                            detalle.FacturaElectronica.SelloRecibido = resultadoDte.SelloRecibido;
                            detalle.FacturaElectronica.FechaTransmision = DateTime.UtcNow;
                            detalle.FacturaElectronica.HoraTransmision = DateTime.UtcNow.TimeOfDay;

                            await _context.SaveChangesAsync();

                            if (resultadoDte.SelloRecibido != null)
                            {
                                await _inventarioService.ConfirmarVentaAsync(detalle.FacturaElectronicaId);
                                await IntentarEnviarEmailDteLoteAsync(detalle.FacturaElectronicaId, detalle.FacturaElectronica);
                                aprobados++;
                            }
                        }
                        else if (resultadoDte.Estado == "RECHAZADO")
                        {
                            // Si es NumeroControl duplicado → regenerar y agrupar para nuevo lote
                            if (resultadoDte.CodigoMsg == "004"
                                && resultadoDte.DescripcionMsg?.Contains("numeroControl", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                _logger.LogWarning(
                                    "[LOTE-DTE] DTE {CodigoGen} rechazado por NumeroControl duplicado (fallback). Regenerando para nuevo lote...",
                                    codigoGeneracion);

                                await _facturaService.RegenerarSiNumeroControlDuplicadoAsync(
                                    detalle.FacturaElectronicaId, resultadoDte.CodigoMsg, resultadoDte.DescripcionMsg);

                                // Limpiar JSON firmado para que se re-firme en el nuevo lote
                                detalle.FacturaElectronica.JsonFirmado = null;
                                detalle.FacturaElectronica.EstadoHacienda = "PENDIENTE_LOTE";
                                detalle.FacturaElectronica.LoteId = null;
                                detalle.EstadoDte = "RECHAZADO";
                                detalle.FechaConsulta = DateTime.UtcNow;
                                detalle.CodigoRechazo = resultadoDte.CodigoMsg;
                                detalle.ObservacionesRechazo = "NumeroControl duplicado - reagrupado en nuevo lote";
                                await _context.SaveChangesAsync();

                                facturasParaNuevoLote.Add(detalle.FacturaElectronicaId);
                                continue;
                            }

                            detalle.EstadoDte = "RECHAZADO";
                            detalle.FechaConsulta = DateTime.UtcNow;
                            detalle.CodigoRechazo = resultadoDte.CodigoMsg;
                            var obs = resultadoDte.Observaciones != null ? string.Join("; ", resultadoDte.Observaciones) : "Sin observaciones";
                            detalle.ObservacionesRechazo = $"{resultadoDte.DescripcionMsg} - {obs}";

                            detalle.FacturaElectronica.EstadoHacienda = "RECHAZADO";
                            detalle.FacturaElectronica.LoteId = null;

                            await _context.SaveChangesAsync();
                            await _inventarioService.LiberarReservasAsync(detalle.FacturaElectronicaId);

                            rechazados++;
                        }
                    }
                    catch (Exception exDte)
                    {
                        _logger.LogWarning(exDte, "Error consultando DTE individual {CodigoGeneracion}", codigoGeneracion);
                        errores++;
                    }
                }

                // Recalcular totales del lote
                lote.TotalAprobados = lote.Detalles.Count(d => d.SelloRecibido != null);
                lote.TotalRechazados = lote.Detalles.Count(d => d.EstadoDte == "RECHAZADO");
                lote.TotalPendientes = lote.Detalles.Count(d => d.EstadoDte == "Pendiente" || d.EstadoDte == null);

                if (lote.TotalPendientes == 0)
                    lote.Estado = "Procesado";

                lote.FechaUltimaConsulta = DateTime.UtcNow;
                lote.FechaModificacion = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await NotificarSmartCareDeLoteAsync(lote);
                return await ObtenerLoteAsync(loteId);
            }

            // Crear diccionarios para busqueda rapida
            var procesadosDict = resultadoLote.Procesados?.ToDictionary(x => x.CodigoGeneracion ?? "") ?? new Dictionary<string, Application.DTOs.Hacienda.DteProcesadoDto>();
            var rechazadosDict = resultadoLote.Rechazados?.ToDictionary(x => x.CodigoGeneracion ?? "") ?? new Dictionary<string, Application.DTOs.Hacienda.DteRechazadoDto>();

            // Iterar sobre los detalles locales y actualizar segun respuesta del lote
            foreach (var detalle in lote.Detalles)
            {
                if (detalle.EstadoDte == "PROCESADO" || detalle.EstadoDte == "RECHAZADO")
                    continue;

                var codigoGen = detalle.FacturaElectronica.CodigoGeneracion;

                try
                {
                    if (procesadosDict.TryGetValue(codigoGen, out var procesado))
                    {
                        // Validar que MH haya devuelto sello de recepción
                        if (string.IsNullOrEmpty(procesado.SelloRecibido))
                        {
                            _logger.LogWarning("[LOTE] MH respondió PROCESADO pero sin sello para DTE {CodigoGen} en lote {LoteId}", codigoGen, loteId);
                            detalle.EstadoDte = "RECHAZADO";
                            detalle.FechaConsulta = DateTime.UtcNow;
                            detalle.ObservacionesRechazo = "MH respondió PROCESADO pero no devolvió sello de recepción";
                            detalle.FacturaElectronica.EstadoHacienda = "RECHAZADO";
                            detalle.FacturaElectronica.Observaciones = "MH respondió PROCESADO pero no devolvió sello de recepción (lote)";
                            await _context.SaveChangesAsync();
                            rechazados++;
                            continue;
                        }

                        // APROBADO con sello válido
                        detalle.EstadoDte = "PROCESADO";
                        detalle.FechaConsulta = DateTime.UtcNow;
                        detalle.SelloRecibido = procesado.SelloRecibido;
                        detalle.JsonRespuestaIndividual = JsonSerializer.Serialize(procesado);

                        // Actualizar factura
                        detalle.FacturaElectronica.EstadoHacienda = "PROCESADO";
                        detalle.FacturaElectronica.SelloRecibido = procesado.SelloRecibido;
                        detalle.FacturaElectronica.FechaTransmision = DateTime.UtcNow;
                        detalle.FacturaElectronica.HoraTransmision = DateTime.UtcNow.TimeOfDay;

                        // Guardar estado de factura antes de operaciones que pueden fallar
                        await _context.SaveChangesAsync();

                        // Confirmar venta (stock reservado -> salida)
                        await _inventarioService.ConfirmarVentaAsync(detalle.FacturaElectronicaId);

                        // Enviar email DTE al receptor (no bloqueante)
                        await IntentarEnviarEmailDteLoteAsync(detalle.FacturaElectronicaId, detalle.FacturaElectronica);

                        aprobados++;
                    }
                    else if (rechazadosDict.TryGetValue(codigoGen, out var rechazado))
                    {
                        // Si es NumeroControl duplicado → regenerar y agrupar para nuevo lote
                        if (rechazado.CodigoMsg == "004"
                            && rechazado.DescripcionMsg?.Contains("numeroControl", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            _logger.LogWarning(
                                "[LOTE-DTE] DTE {CodigoGen} rechazado por NumeroControl duplicado en lote. Regenerando para nuevo lote...",
                                codigoGen);

                            await _facturaService.RegenerarSiNumeroControlDuplicadoAsync(
                                detalle.FacturaElectronicaId, rechazado.CodigoMsg, rechazado.DescripcionMsg);

                            // Limpiar JSON firmado para que se re-firme en el nuevo lote
                            detalle.FacturaElectronica.JsonFirmado = null;
                            detalle.FacturaElectronica.EstadoHacienda = "PENDIENTE_LOTE";
                            detalle.FacturaElectronica.LoteId = null;
                            detalle.EstadoDte = "RECHAZADO";
                            detalle.FechaConsulta = DateTime.UtcNow;
                            detalle.CodigoRechazo = rechazado.CodigoMsg;
                            detalle.ObservacionesRechazo = "NumeroControl duplicado - reagrupado en nuevo lote";
                            detalle.JsonRespuestaIndividual = JsonSerializer.Serialize(rechazado);
                            await _context.SaveChangesAsync();

                            facturasParaNuevoLote.Add(detalle.FacturaElectronicaId);
                            continue;
                        }

                        // RECHAZADO (otro motivo)
                        detalle.EstadoDte = "RECHAZADO";
                        detalle.FechaConsulta = DateTime.UtcNow;
                        detalle.JsonRespuestaIndividual = JsonSerializer.Serialize(rechazado);

                        detalle.CodigoRechazo = rechazado.CodigoMsg;
                        var observaciones = rechazado.Observaciones != null ? string.Join("; ", rechazado.Observaciones) : "Sin observaciones";
                        detalle.ObservacionesRechazo = $"{rechazado.DescripcionMsg} - {observaciones}";

                        detalle.FacturaElectronica.EstadoHacienda = "RECHAZADO";
                        detalle.FacturaElectronica.LoteId = null;

                        await _inventarioService.LiberarReservasAsync(detalle.FacturaElectronicaId);

                        rechazados++;
                    }
                    else
                    {
                        // No aparece ni en procesados ni rechazados -> Sigue procesando o no existe
                        // Si el lote está "PROCESADO" y no está en listas, algo raro pasó.
                        // Mantenemos estado actual.
                    }
                }
                catch (Exception exDte)
                {
                    _logger.LogWarning(exDte, "Error procesando DTE {CodigoGeneracion} del lote {LoteId}", codigoGen, loteId);
                    errores++;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando estado del lote {Codigo}", lote.CodigoLote);
            errores++;
            // Re-throw para manejar en nivel superior si es necesario, o continuar loop de lotes
        }


        // Actualizar estadísticas del lote desde las entidades (estado acumulado real)
        lote.TotalAprobados = lote.Detalles.Count(d => d.SelloRecibido != null);
        lote.TotalRechazados = lote.Detalles.Count(d => d.EstadoDte == "RECHAZADO");
        lote.TotalPendientes = lote.Detalles.Count(d => d.EstadoDte == "Pendiente" || d.EstadoDte == null);
        lote.FechaUltimaConsulta = DateTime.UtcNow;
        lote.FechaModificacion = DateTime.UtcNow;

        // Actualizar estado si ya no hay pendientes
        if (lote.TotalPendientes == 0 && lote.Estado == "Enviado")
            lote.Estado = "Procesado";

        await _context.SaveChangesAsync();

        _logger.LogInformation(
                    "Consulta completada. Lote {LoteId}: {Aprobados} aprobados, {Rechazados} rechazados, {Errores} errores",
                    loteId, aprobados, rechazados, errores);

        // Si hay facturas con NumeroControl duplicado, crear nuevo lote y repetir hasta que ya no dé ese error
        if (facturasParaNuevoLote.Count > 0)
        {
            await ReenviarEnNuevoLoteAsync(lote.EmisorId, facturasParaNuevoLote, lote.EsContingencia, lote.EventoContingenciaId);
        }

        await NotificarSmartCareDeLoteAsync(lote);
        return await ObtenerLoteAsync(loteId);
    }

    /// <summary>
    /// Crea un nuevo lote con las facturas que tuvieron error 004/NumeroControl duplicado,
    /// lo envía y consulta resultados. Repite hasta que no queden 004 de NumeroControl.
    /// </summary>
    private async Task ReenviarEnNuevoLoteAsync(int emisorId, List<int> facturaIds, bool esContingencia, int? eventoContingenciaId)
    {
        _logger.LogInformation(
            "[LOTE-RETRY] Creando nuevo lote con {Count} facturas con NumeroControl regenerado",
            facturaIds.Count);

        try
        {
            // Re-firmar facturas con el nuevo NumeroControl/CodigoGeneracion
            var facturas = await _context.Facturas
                .Where(f => facturaIds.Contains(f.Id))
                .Include(f => f.Emisor)
                .ToListAsync();

            var primerEmisor = facturas.First().Emisor;
            var llavePrivada = DecryptField(primerEmisor.MhLlavePrivada, "MhLlavePrivada");
            var passPrivada = DecryptField(primerEmisor.MhPassPrivada, "MhPassPrivada", optional: true);

            foreach (var factura in facturas)
            {
                var jsonDte = await _facturaService.GenerateJsonDteAsync(factura.Id, emisorId);
                factura.JsonFirmado = _signerService.FirmarDocumento(jsonDte, llavePrivada, passPrivada);
                _logger.LogInformation("[LOTE-RETRY] Factura {FacturaId} re-firmada con nuevo NumeroControl", factura.Id);
            }
            await _context.SaveChangesAsync();

            var nuevoLoteDto = new CrearLoteDto
            {
                FacturaIds = facturaIds,
                EsContingencia = esContingencia,
                EventoContingenciaId = eventoContingenciaId
            };

            var nuevoLote = await CrearLoteAsync(emisorId, nuevoLoteDto);
            _logger.LogInformation("[LOTE-RETRY] Nuevo lote {LoteId} creado. Enviando...", nuevoLote.Id);

            // Enviar el nuevo lote
            await EnviarLoteAsync(nuevoLote.Id);

            // Esperar para que MH procese
            await Task.Delay(3000);

            // Consultar resultados del nuevo lote
            // Si aún hay 004/NumeroControl, ConsultarEstadosIndividualesAsync
            // volverá a llamar a este método recursivamente
            await ConsultarEstadosIndividualesAsync(nuevoLote.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LOTE-RETRY] Error al reenviar lote con facturas regeneradas. Quedan como PENDIENTE_LOTE para reintento manual.");
        }
    }

    public async Task<LoteDto> ObtenerLoteAsync(int loteId)
    {
        var lote = await _context.Lotes
            .Include(l => l.Detalles)
                .ThenInclude(d => d.FacturaElectronica)
                    .ThenInclude(f => f.Receptor)
            .Include(l => l.Detalles)
                .ThenInclude(d => d.FacturaElectronica)
                    .ThenInclude(f => f.TipoDocumento)
            .FirstOrDefaultAsync(l => l.Id == loteId);

        if (lote == null)
            throw new KeyNotFoundException($"Lote {loteId} no encontrado");

        return MapearLoteDto(lote);
    }

    public async Task<PaginatedResponse<LoteDto>> ObtenerLotesPorEmisorAsync(PaginatedRequest request, int emisorId, List<int>? sucursalIds = null, bool? esContingencia = null, int? usuarioId = null, string? estado = null, string? ambiente = null, DateTime? fechaDesde = null, DateTime? fechaHasta = null, string? search = null)
    {
        var query = _context.Lotes
            .Where(l => l.EmisorId == emisorId)
            .Include(l => l.Detalles)
                .ThenInclude(d => d.FacturaElectronica)
            .OrderByDescending(l => l.FechaCreacion)
            .AsQueryable();

        if (sucursalIds != null && sucursalIds.Count > 0)
        {
            query = query.Where(l => l.Detalles.Any(d => d.FacturaElectronica.SucursalId.HasValue && sucursalIds.Contains(d.FacturaElectronica.SucursalId.Value)));
        }

        if (esContingencia.HasValue)
        {
            query = query.Where(l => l.EsContingencia == esContingencia.Value);
        }

        if (usuarioId.HasValue)
        {
            query = query.Where(l => l.Detalles.Any(d => d.FacturaElectronica.UsuarioId == usuarioId.Value));
        }

        if (!string.IsNullOrEmpty(estado))
            query = query.Where(l => l.Estado == estado);

        if (!string.IsNullOrEmpty(ambiente))
            query = query.Where(l => l.Ambiente == ambiente);

        // Búsqueda por código de lote (Guid como string)
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(l => l.CodigoLote.ToString().ToLower().Contains(search.ToLower()));
        }

        if (fechaDesde.HasValue)
        {
            var desdeUtc = DateTime.SpecifyKind(fechaDesde.Value.Date, DateTimeKind.Utc);
            query = query.Where(l => l.FechaCreacion >= desdeUtc);
        }

        if (fechaHasta.HasValue)
        {
            var hastaUtc = DateTime.SpecifyKind(fechaHasta.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(l => l.FechaCreacion < hastaUtc);
        }

        var pagedItems = await query.ToPaginatedListAsync(request.PageNumber, request.PageSize);

        var itemsDto = pagedItems.Select(MapearLoteDto).ToList();

        return new PaginatedResponse<LoteDto>
        {
            Items = itemsDto,
            CurrentPage = pagedItems.CurrentPage,
            PageSize = pagedItems.PageSize,
            TotalCount = pagedItems.TotalCount,
            TotalPages = pagedItems.TotalPages
        };
    }

    public async Task<LoteDto> EnviarLoteManualAsync(int emisorId, EnviarLoteManualDto dto)
    {
        _logger.LogInformation("Iniciando envío manual de lote para emisor {EmisorId} con {Total} facturas",
            emisorId, dto.FacturaIds.Count);

        // 1. Validar que se especificaron facturas
        if (dto.FacturaIds == null || !dto.FacturaIds.Any())
            throw new InvalidOperationException("Debe especificar al menos una factura");

        // 2. Validar límite de 100 DTEs (requisito de MH para lotes)
        if (dto.FacturaIds.Count > 100)
            throw new InvalidOperationException("El lote no puede exceder 100 facturas");

        // 3. Obtener facturas y validar que existan y pertenezcan al emisor
        var facturas = await _context.Facturas
            .Where(f => dto.FacturaIds.Contains(f.Id) && f.EmisorId == emisorId)
            .Include(f => f.TipoDocumento)
            .Include(f => f.Emisor)
                .ThenInclude(e => e.AmbienteDestino)
            .ToListAsync();

        if (facturas.Count != dto.FacturaIds.Count)
        {
            var faltantes = dto.FacturaIds.Count - facturas.Count;
            throw new InvalidOperationException(
                $"{faltantes} facturas no existen o no pertenecen al emisor");
        }

        // 4. Validar que todas estén en estado PENDIENTE_ENVIO
        var noPendientes = facturas
            .Where(f => f.EstadoHacienda != "PENDIENTE_ENVIO")
            .ToList();

        if (noPendientes.Any())
        {
            var ids = string.Join(", ", noPendientes.Select(f => f.Id));
            var estados = string.Join(", ", noPendientes.Select(f => $"{f.Id}:{f.EstadoHacienda}"));
            throw new InvalidOperationException(
                $"{noPendientes.Count} facturas no están en estado PENDIENTE_ENVIO. Estados: {estados}");
        }

        // 5. Validar que todas sean del mismo ambiente
        var ambientes = facturas.Select(f => f.Emisor.AmbienteDestino.Codigo).Distinct().ToList();
        if (ambientes.Count > 1)
        {
            throw new InvalidOperationException(
                "Todas las facturas deben ser del mismo ambiente (Pruebas o Producción)");
        }

        // 6. Liberar facturas de lotes fallidos y validar lotes activos
        var facturasEnLote = facturas.Where(f => f.LoteId.HasValue).ToList();
        if (facturasEnLote.Any())
        {
            // Cargar los lotes para verificar su estado
            var loteIds = facturasEnLote.Select(f => f.LoteId!.Value).Distinct().ToList();
            var lotes = await _context.Lotes
                .Where(l => loteIds.Contains(l.Id))
                .ToDictionaryAsync(l => l.Id, l => l.Estado);

            // Separar facturas según el estado de su lote
            var enLoteFallido = facturasEnLote
                .Where(f => lotes.TryGetValue(f.LoteId!.Value, out var estado) &&
                           (estado == "Error" || estado == "Cancelado" || estado == "Enviando"))
                .ToList();

            var enLoteActivo = facturasEnLote
                .Where(f => lotes.TryGetValue(f.LoteId!.Value, out var estado) &&
                           estado != "Error" && estado != "Cancelado" && estado != "Enviando")
                .ToList();

            // Si hay facturas en lotes activos, rechazar
            if (enLoteActivo.Any())
            {
                var detalles = string.Join(", ", enLoteActivo
                    .Select(f => $"{f.Id} (Lote {f.LoteId}, Estado: {lotes[f.LoteId!.Value]})"));
                throw new InvalidOperationException(
                    $"{enLoteActivo.Count} facturas ya están en lotes activos. Detalles: {detalles}");
            }

            // Liberar automáticamente facturas de lotes fallidos
            if (enLoteFallido.Any())
            {
                _logger.LogInformation(
                    "Liberando {Count} facturas de lotes fallidos (IDs: {Ids})",
                    enLoteFallido.Count,
                    string.Join(", ", enLoteFallido.Select(f => f.Id)));

                foreach (var factura in enLoteFallido)
                {
                    factura.LoteId = null;
                }
                await _context.SaveChangesAsync();
            }
        }

        // 7. FIRMAR todas las facturas que no estén firmadas
        // (las facturas en PENDIENTE_ENVIO no tienen JsonFirmado aún)
        foreach (var factura in facturas)
        {
            if (string.IsNullOrEmpty(factura.JsonFirmado))
            {
                _logger.LogInformation("Generando JSON del DTE para factura {FacturaId}", factura.Id);

                // Generar JSON del DTE usando el servicio de facturas
                var jsonDte = await _facturaService.GenerateJsonDteAsync(factura.Id, emisorId);

                // Firmar con el servicio de firma
                _logger.LogInformation("Firmando factura {FacturaId}", factura.Id);
                var llavePrivada = DecryptField(factura.Emisor.MhLlavePrivada, "MhLlavePrivada");
                var passPrivada = DecryptField(factura.Emisor.MhPassPrivada, "MhPassPrivada", optional: true);
                var documentoFirmado = _signerService.FirmarDocumento(
                    jsonDte, llavePrivada, passPrivada);

                // Guardar JsonFirmado en BD
                factura.JsonFirmado = documentoFirmado;

                _logger.LogInformation("Factura {FacturaId} firmada exitosamente", factura.Id);
            }
        }

        // Guardar cambios de facturas firmadas
        await _context.SaveChangesAsync();

        // 8. Crear el lote usando el DTO existente
        var crearLoteDto = new CrearLoteDto
        {
            FacturaIds = dto.FacturaIds,
            EsContingencia = false, // Envío manual siempre es normal, no contingencia
            EventoContingenciaId = null
        };

        var lote = await CrearLoteAsync(emisorId, crearLoteDto);

        // 9. Enviar el lote inmediatamente
        try
        {
            lote = await EnviarLoteAsync(lote.Id);

            _logger.LogInformation(
                "Envío manual completado. Lote {LoteId} en estado {Estado}",
                lote.Id, lote.Estado);

            return lote;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en envío manual de lote {LoteId}", lote.Id);
            throw;
        }
    }

    // ==========================================
    // MÉTODOS PRIVADOS / HELPERS
    // ==========================================

    private bool EstaEnHorarioPermitido(string ambiente, bool esContingencia)
    {
        // Los lotes (incluidos los de contingencia) siguen el mismo horario de recepción de MH.
        // Solo los EVENTOS de contingencia se reportan 24/7, no los lotes.

        // Se debe usar la hora de El Salvador (UTC-6), no la hora del servidor
        var utcNow = DateTime.UtcNow;
        // "Central America Standard Time" es la zona horaria de Centroamérica (UTC-6)
        // Si el sistema no la encuentra, se puede usar un offset fijo, pero TimeZoneInfo es más robusto
        TimeZoneInfo timeZoneInfo;
        try
        {
            timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            // Fallback para entornos donde no esté instalada la zona horaria (ej. contenedores linux a veces diferentes)
            // Windows: "Central America Standard Time", Linux: "America/El_Salvador"
            try
            {
                timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("America/El_Salvador");
            }
            catch
            {
                // Fallback final manual UTC-6
                timeZoneInfo = TimeZoneInfo.CreateCustomTimeZone("Central America Standard Time", new TimeSpan(-6, 0, 0), "Central America Standard Time", "Central America Standard Time");
            }
        }

        var horaSv = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZoneInfo);
        var ahora = horaSv.TimeOfDay;

        if (ambiente == "00") // Pruebas: 08:00 - 17:00
        {
            var inicio = new TimeSpan(8, 0, 0);
            var fin = new TimeSpan(17, 0, 0);
            return ahora >= inicio && ahora <= fin;
        }
        else // Producción: 22:00 - 05:00 (horario nocturno)
        {
            var inicio = new TimeSpan(22, 0, 0);
            var fin = new TimeSpan(5, 0, 0);

            // Horario nocturno requiere lógica especial
            return ahora >= inicio || ahora <= fin;
        }
    }



    private LoteDto MapearLoteDto(Lote lote)
    {
        return new LoteDto
        {
            Id = lote.Id,
            CodigoLote = lote.CodigoLote.ToString(),
            TotalDtes = lote.TotalDtes,
            Estado = lote.Estado,
            Ambiente = lote.Ambiente,
            EsContingencia = lote.EsContingencia,
            SucursalId = lote.Detalles.FirstOrDefault()?.FacturaElectronica?.SucursalId,
            FechaCreacion = lote.FechaCreacion,
            FechaEnvio = lote.FechaEnvio,
            FhProcesamiento = lote.FhProcesamiento,
            CodigoRespuesta = lote.CodigoRespuesta,
            DescripcionRespuesta = lote.DescripcionRespuesta,
            TotalAprobados = lote.TotalAprobados,
            TotalRechazados = lote.TotalRechazados,
            TotalPendientes = lote.TotalPendientes,
            Detalles = lote.Detalles.Select(d => new LoteDetalleDto
            {
                FacturaId = d.FacturaElectronicaId,
                NumeroControl = d.FacturaElectronica.NumeroControl,
                CodigoGeneracion = d.FacturaElectronica.CodigoGeneracion,
                TipoDte = d.FacturaElectronica.TipoDocumento?.Codigo,
                ReceptorNombre = d.FacturaElectronica.Receptor?.NombreRazonSocial,
                TotalPagar = d.FacturaElectronica.TotalPagar,
                EstadoDte = d.EstadoDte,
                SelloRecibido = d.SelloRecibido,
                FechaConsulta = d.FechaConsulta,
                CodigoRechazo = d.CodigoRechazo,
                ObservacionesRechazo = d.ObservacionesRechazo
            }).ToList()
        };
    }

    /// <summary>
    /// Envía email DTE de forma no bloqueante para lotes.
    /// </summary>
    private async Task IntentarEnviarEmailDteLoteAsync(int facturaId, FacturaElectronica factura)
    {
        // Check in-memory first (fast path)
        if (factura.CorreoEnviado)
        {
            _logger.LogDebug("[EMAIL-LOTE] Factura {FacturaId} ya tiene correo enviado, omitiendo", facturaId);
            return;
        }

        // Fresh DB reload to catch race conditions and stale tracked entities
        await _context.Entry(factura).ReloadAsync();
        if (factura.CorreoEnviado)
        {
            _logger.LogDebug("[EMAIL-LOTE] Factura {FacturaId} ya tiene correo enviado (DB reload), omitiendo", facturaId);
            return;
        }

        try
        {
            var receptor = factura.ReceptorId > 0
                ? await _context.Receptores.FindAsync(factura.ReceptorId)
                : null;

            var emailReceptor = receptor?.CorreoElectronico;

            if (string.IsNullOrWhiteSpace(emailReceptor))
            {
                _logger.LogDebug("[EMAIL-LOTE] Factura {FacturaId} sin correo receptor, omitiendo email", facturaId);
                return;
            }

            await _emailService.EnviarDteAsync(facturaId, emailReceptor);
            _logger.LogInformation("[EMAIL-LOTE] Email DTE enviado para factura {FacturaId} a {Email}", facturaId, emailReceptor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMAIL-LOTE] Error al enviar email DTE para factura {FacturaId} en lote. No afecta procesamiento del lote.", facturaId);
        }
    }
}
