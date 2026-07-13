using System.Text.Json;
using FraFactu.Application.Common.Interfaces;
using FraFactu.Application.DTOs.Invalidacion;
using FraFactu.Application.Interfaces;
using FraFactu.Application.Interfaces.Hacienda;
using FraFactu.Application.Services;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Services
{
    /// <summary>
    /// Implementación de anulación/invalidación/descarte de facturas (Fase 3 del refactor).
    /// </summary>
    public class FacturaInvalidacionService : IFacturaInvalidacionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHaciendaApiService _haciendaApiService;
        private readonly IInventarioIntegrationService _inventarioService;
        private readonly ISaldoDteService _saldoDteService;
        private readonly IEmailService _emailService;
        private readonly ILogger<FacturaInvalidacionService> _logger;
        private readonly IFacturaLoteSync _loteSync;

        public FacturaInvalidacionService(
            ApplicationDbContext context,
            IHaciendaApiService haciendaApiService,
            IInventarioIntegrationService inventarioService,
            ISaldoDteService saldoDteService,
            IEmailService emailService,
            ILogger<FacturaInvalidacionService> logger,
            IFacturaLoteSync loteSync)
        {
            _context = context;
            _haciendaApiService = haciendaApiService;
            _inventarioService = inventarioService;
            _saldoDteService = saldoDteService;
            _emailService = emailService;
            _logger = logger;
            _loteSync = loteSync;
        }

        public async Task<bool> AnularAsync(int facturaId, int emisorId, string motivo)
        {
            var factura = await _context.Facturas
                .FirstOrDefaultAsync(f => f.Id == facturaId && f.EmisorId == emisorId);

            if (factura == null)
                return false;

            factura.EstadoHacienda = "ANULADO";
            factura.Observaciones = $"ANULADO: {motivo}";
            factura.Activo = false;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ActualizarEstadoHaciendaAsync(int facturaId, int emisorId, string estado, string? selloRecepcion = null)
        {
            var factura = await _context.Facturas
                .FirstOrDefaultAsync(f => f.Id == facturaId && f.EmisorId == emisorId);

            if (factura == null)
                return false;

            var estadoAnterior = factura.EstadoHacienda;
            factura.EstadoHacienda = estado;
            if (selloRecepcion != null)
                factura.SelloRecibido = selloRecepcion;

            await _context.SaveChangesAsync();

            // Inicializar saldo cuando un DTE pasa a PROCESADO (para soporte de NC futuras)
            if (estado == "PROCESADO" && estadoAnterior != "PROCESADO")
            {
                var tiposConSaldo = new[] { "01", "03", "14" };
                var tipoDteCodigo = factura.TipoDocumento?.Codigo
                    ?? (await _context.Facturas.Include(f => f.TipoDocumento)
                        .FirstAsync(f => f.Id == facturaId)).TipoDocumento?.Codigo;

                if (tipoDteCodigo != null && tiposConSaldo.Contains(tipoDteCodigo))
                {
                    try
                    {
                        var montoTotal = factura.MontoTotalOperacion > 0 ? factura.MontoTotalOperacion : factura.TotalPagar;
                        await _saldoDteService.InicializarSaldoAsync(
                            factura.CodigoGeneracion, tipoDteCodigo, montoTotal, emisorId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[SALDO-DTE] Error inicializando saldo en ActualizarEstado para DTE {CodigoGeneracion}",
                            factura.CodigoGeneracion);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Obtiene el código MH (ej "36") dado un ID de catálogo
        /// </summary>
        private async Task<string> ObtenerCodigoTipoDocumentoIdentificacion(int id)
        {
            var tipo = await _context.CatDocsIdentidadReceptor.FindAsync(id);
            return tipo?.Codigo ?? "36"; // Default NIT
        }

        /// <summary>
        /// Invalida una factura electrónica con integración de inventario y envío a Hacienda
        /// </summary>
        public async Task<FraFactu.Application.DTOs.Invalidacion.InvalidacionDto> InvalidarFacturaAsync(
            int facturaId,
            FraFactu.Application.DTOs.Invalidacion.AnularFacturaDto dto,
            int emisorId)
        {
            var factura = await _context.Facturas
                .Include(f => f.Receptor!)
                    .ThenInclude(r => r.TipoDocumento)
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.AmbienteDestino)
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.TipoEstablecimiento) // Importante para construir DTO
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.TipoEstablecimiento)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.Departamento)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.Municipio)
                .Include(f => f.Caja)
                .FirstOrDefaultAsync(f => f.Id == facturaId && f.EmisorId == emisorId);

            if (factura == null)
                throw new InvalidOperationException("Factura no encontrada");

            if (string.IsNullOrEmpty(factura.SelloRecibido))
                throw new InvalidOperationException("Solo se pueden invalidar facturas con sello de recepción");

            var yaInvalidada = await _context.Invalidaciones
                .AnyAsync(i => i.FacturaElectronicaId == facturaId);

            if (yaInvalidada)
                throw new InvalidOperationException("La factura ya está invalidada");

            // =================================================================================
            // 1. VALIDACIÓN DE PLAZOS (CORREGIDO SEGÚN MANUAL HACIENDA)
            // =================================================================================
            // Grupo 1: CCF (03), NRE (04), NCE (05), NDE (06), CRE (07), CLE (08), DCLE (09), CDE (15)
            // Plazo: 1 día (hasta las 23:59:59 del día siguiente a la fecha de transmisión)
            //
            // Grupo 2: FE (01), FEXE (11), FSEE (14)
            // Plazo: 3 meses desde la fecha de transmisión

            // Obtener tipo de documento
            var tipoDocumento = await _context.CatTiposDocumento
                .FirstOrDefaultAsync(t => t.Id == factura.CatTipoDocumentoId);
            string codigoTipoDte = tipoDocumento?.Codigo ?? "01";

            DateTime fechaTransmision = factura.FechaTransmision ?? factura.FechaEmision;
            if (factura.HoraTransmision.HasValue)
                fechaTransmision = fechaTransmision.Date.Add(factura.HoraTransmision.Value);

            DateTime fechaLimite;
            string descripcionPlazo;

            // Tipos con plazo de 3 meses (Grupo 2)
            var tiposTresMeses = new[] { "01", "11", "14" };

            if (tiposTresMeses.Contains(codigoTipoDte))
            {
                fechaLimite = fechaTransmision.AddMonths(3);
                descripcionPlazo = "3 meses";
            }
            else
            {
                // Grupo 1: Hasta 23:59:59 del día siguiente
                fechaLimite = fechaTransmision.Date.AddDays(1).Add(new TimeSpan(23, 59, 59));
                descripcionPlazo = "1 día (hasta el día siguiente)";
            }

            if (DateTime.UtcNow > fechaLimite)
            {
                throw new InvalidOperationException(
                    $"El plazo para invalidar ha expirado. " +
                    $"Tipo DTE: {codigoTipoDte}. Plazo máximo: {descripcionPlazo}. " +
                    $"Fecha límite: {fechaLimite:dd/MM/yyyy HH:mm:ss} UTC");
            }

            // =================================================================================
            // 2. VALIDACIONES DE LOGICA DE NEGOCIO (REEMPLAZOS Y RECEPTORES)
            // =================================================================================

            // Validación de Factura de Reemplazo para tipos 1 (Error) y 3 (Otro)
            // Excepción: Nota de Crédito (05), Nota de Débito (06) y Comp. Liquidación (08) NO requieren reemplazo
            bool requiereReemplazo = (dto.TipoAnulacion == 1 || dto.TipoAnulacion == 3)
                                     && codigoTipoDte != "05" && codigoTipoDte != "06" && codigoTipoDte != "08";

            FacturaElectronica? facturaReemplazo = null;

            if (requiereReemplazo)
            {
                if (!dto.FacturaReemplazoId.HasValue)
                    throw new InvalidOperationException("Para este tipo de anulación debe especificar la factura de reemplazo/nuevo documento.");

                facturaReemplazo = await _context.Facturas
                    .FirstOrDefaultAsync(f => f.Id == dto.FacturaReemplazoId.Value && f.EmisorId == emisorId);

                if (facturaReemplazo == null)
                    throw new InvalidOperationException("La factura de reemplazo especificada no existe.");

                if (string.IsNullOrEmpty(facturaReemplazo.SelloRecibido))
                    throw new InvalidOperationException("La factura de reemplazo debe estar PROCESADA y tener Sello de Recepción antes de invalidar la original.");
            }

            // Para tipo 2 (Rescisión), el reemplazo debe ser nulo
            if (dto.TipoAnulacion == 2)
            {
                dto.FacturaReemplazoId = null;
                facturaReemplazo = null;
            }

            // Validación de fecha de anulación (fecAnula)
            // Hacienda espera fecha y hora en zona horaria de El Salvador (UTC-6)
            var zonaElSalvador = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
            DateTime fechaAnulacion = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaElSalvador);

            // =================================================================================
            // 3. CONSTRUCCIÓN DEL EVENTO JSON (EventoInvalidacionDto)
            // =================================================================================

            // Obtener códigos de catálogos
            string tipDocResponsable = await ObtenerCodigoTipoDocumentoIdentificacion(dto.CatTipoDocResponsableId);
            string tipDocSolicita = await ObtenerCodigoTipoDocumentoIdentificacion(dto.CatTipoDocSolicitaId);

            // Manejo de Receptor NULL (para facturas Consumidor Final sin datos)
            // Según manual: si no hay receptor, enviar cadena literal "null"
            bool receptorEsNull = factura.Receptor == null;
            string receptorTipoDoc = receptorEsNull ? "null" : (factura.Receptor?.TipoDocumento?.Codigo ?? "36");
            string receptorNumDoc = receptorEsNull ? "null" : (factura.Receptor?.NumeroDocumento ?? "null");
            string receptorNombre = receptorEsNull ? "null" : (factura.Receptor?.NombreRazonSocial ?? "null");
            string? receptorTelefono = receptorEsNull ? null : (string.IsNullOrEmpty(factura.Receptor?.Telefono) ? null : factura.Receptor?.Telefono);
            string? receptorCorreo = receptorEsNull ? null : factura.Receptor?.CorreoElectronico;

            // Construir DTO para Hacienda
            var eventoDto = new FraFactu.Application.DTOs.Hacienda.EventoInvalidacionDto
            {
                Identificacion = new FraFactu.Application.DTOs.Hacienda.IdentificacionInvalidacionDto
                {
                    Version = 3,
                    Ambiente = factura.Emisor.AmbienteDestino?.Codigo ?? "00", // "00": Pruebas, "01": Prod
                    CodigoGeneracion = Guid.NewGuid().ToString().ToUpper(), // Nuevo GUID para el evento
                    FecEmi = fechaAnulacion.ToString("yyyy-MM-dd"),
                    HorEmi = fechaAnulacion.ToString("HH:mm:ss"),
                    Fusion = null // Solo aplica en eventos de fusión de contribuyentes
                },
                Emisor = new FraFactu.Application.DTOs.Hacienda.EmisorInvalidacionDto
                {
                    Nit = factura.Emisor.Nit?.Replace("-", "") ?? "",
                    Nombre = factura.Emisor.NombreRazonSocial,
                    CodEstableMH = factura.Sucursal!.CodigoEstablecimiento,
                    CodEstable = factura.Sucursal!.Codigo ?? "",
                    CodPuntoVentaMH = factura.Caja!.CodPuntoVentaMH,
                    CodPuntoVenta = factura.Caja!.CodPuntoVenta,
                    Telefono = (factura.Sucursal?.Telefono ?? factura.Emisor.Telefono)?.Replace("-", ""),
                    Correo = factura.Sucursal?.CorreoElectronico ?? factura.Emisor.CorreoElectronico
                },
                Documento = new FraFactu.Application.DTOs.Hacienda.DocumentoInvalidacionDto
                {
                    TipoDte = codigoTipoDte,
                    CodigoGeneracion = factura.CodigoGeneracion,
                    SelloRecibido = factura.SelloRecibido,
                    NumeroControl = factura.NumeroControl,
                    FecEmi = factura.FechaEmision.ToString("yyyy-MM-dd"),
                    CodigoGeneracionR = facturaReemplazo?.CodigoGeneracion, // NULL si no aplica
                    TipoDocumento = receptorTipoDoc,
                    NumDocumento = receptorNumDoc,
                    Nombre = receptorNombre,
                    Telefono = receptorTelefono?.Replace("-", ""),
                    Correo = receptorCorreo
                },
                Motivo = new FraFactu.Application.DTOs.Hacienda.MotivoInvalidacionDto
                {
                    TipoAnulacion = dto.TipoAnulacion,
                    MotivoAnulacion = dto.MotivoAnulacion,
                    NombreResponsable = dto.NombreResponsable,
                    TipDocResponsable = tipDocResponsable,
                    NumDocResponsable = dto.NumDocResponsable,
                    NombreSolicita = dto.NombreSolicita,
                    TipDocSolicita = tipDocSolicita,
                    NumDocSolicita = dto.NumDocSolicita
                }
            };

            // =================================================================================
            // 4. TRANSMISIÓN A HACIENDA
            // =================================================================================

            // Llamar al servicio API (AnularDteAsync ya existe e implementa firma y envío)
            var responseHacienda = await _haciendaApiService.AnularDteAsync(emisorId, eventoDto);

            // Verificar respuesta
            if (responseHacienda.Estado == "RECHAZADO")
            {
                // Caso especial: Hacienda dice que el documento ya fue invalidado previamente
                // (puede pasar si un intento anterior llegó a Hacienda pero falló localmente)
                bool yaInvalidadoEnHacienda = responseHacienda.DescripcionMsg?.Contains("SE ENCUENTRA INVALIDADO", StringComparison.OrdinalIgnoreCase) == true;

                if (yaInvalidadoEnHacienda)
                {
                    _logger.LogWarning("Hacienda indica que el DTE ya está invalidado. Sincronizando estado local.");
                    factura.EstadoHacienda = "ANULADO";
                    factura.Observaciones = $"ANULADO (sincronizado): {dto.MotivoAnulacion}";
                    factura.Activo = false;

                    // Crear registro de invalidación local si no existe
                    var existeLocal = await _context.Invalidaciones.AnyAsync(i => i.FacturaElectronicaId == facturaId);
                    if (!existeLocal)
                    {
                        _context.Invalidaciones.Add(new Invalidacion
                        {
                            EmisorId = emisorId,
                            FacturaElectronicaId = facturaId,
                            FacturaReemplazoId = dto.FacturaReemplazoId,
                            CodigoGeneracion = eventoDto.Identificacion.CodigoGeneracion,
                            FechaAnulacion = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc),
                            HoraAnulacion = fechaAnulacion.TimeOfDay,
                            Ambiente = eventoDto.Identificacion.Ambiente,
                            TipoAnulacion = dto.TipoAnulacion,
                            MotivoAnulacion = dto.MotivoAnulacion,
                            NombreResponsable = dto.NombreResponsable,
                            CatTipoDocResponsableId = dto.CatTipoDocResponsableId,
                            NumDocResponsable = dto.NumDocResponsable,
                            NombreSolicita = dto.NombreSolicita,
                            CatTipoDocSolicitaId = dto.CatTipoDocSolicitaId,
                            NumDocSolicita = dto.NumDocSolicita,
                            CatTipoDocReceptorId = factura.Receptor?.CatTipoDocumentoIdentificacionReceptorId ?? 1,
                            NumDocReceptor = factura.Receptor?.NumeroDocumento ?? "",
                            NombreReceptor = factura.Receptor?.NombreRazonSocial ?? "",
                            MontoIva = factura.TotalIva,
                            CorreoReceptor = factura.Receptor?.CorreoElectronico,
                            TelefonoReceptor = receptorTelefono,
                            JsonEvento = JsonSerializer.Serialize(eventoDto),
                            JsonRespuesta = JsonSerializer.Serialize(responseHacienda),
                            EstadoHacienda = "ANULADO",
                            FechaTransmision = DateTime.UtcNow,
                            TipoInvalidacion = dto.TipoInvalidacion ?? "ERROR_FACTURA",
                            RevirtiInventario = dto.RevirtiInventario
                        });
                    }

                    await _context.SaveChangesAsync();

                    return new FraFactu.Application.DTOs.Invalidacion.InvalidacionDto
                    {
                        Identificacion = new FraFactu.Application.DTOs.Invalidacion.IdentificacionInvalidacionDto
                        {
                            CodigoGeneracion = eventoDto.Identificacion.CodigoGeneracion,
                            FecAnula = fechaAnulacion.ToString("yyyy-MM-dd"),
                            HorAnula = fechaAnulacion.ToString("HH:mm:ss")
                        },
                        Estado = "PROCESADO",
                        SelloRecibido = null
                    };
                }

                // Rechazo real: lanzar excepción con detalle
                string errorMsg = $"Hacienda rechazó la invalidación. Código: {responseHacienda.CodigoMsg}. Mensaje: {responseHacienda.DescripcionMsg}";
                if (responseHacienda.Observaciones != null && responseHacienda.Observaciones.Any())
                    errorMsg += ". Observaciones: " + string.Join("; ", responseHacienda.Observaciones);

                throw new InvalidOperationException(errorMsg);
            }

            // Si el estado no es procesado (y no es rechazado explícito), verificar
            if (responseHacienda.Estado != "PROCESADO")
            {
                // Si devuelve otro estado (ej: EN_COLA), advertir o manejar
                // Por ahora asumimos que si no es RECHAZADO es éxito
            }

            // =================================================================================
            // 5. PERSISTENCIA LOCAL
            // =================================================================================

            // Crear entidad de invalidación local
            var invalidacion = new Invalidacion
            {
                EmisorId = emisorId,
                FacturaElectronicaId = facturaId,
                FacturaReemplazoId = dto.FacturaReemplazoId,
                CodigoGeneracion = eventoDto.Identificacion.CodigoGeneracion, // GUID del evento
                FechaAnulacion = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc),
                HoraAnulacion = fechaAnulacion.TimeOfDay,
                Ambiente = eventoDto.Identificacion.Ambiente,
                TipoAnulacion = dto.TipoAnulacion,
                MotivoAnulacion = dto.MotivoAnulacion,
                NombreResponsable = dto.NombreResponsable,
                CatTipoDocResponsableId = dto.CatTipoDocResponsableId,
                NumDocResponsable = dto.NumDocResponsable,
                NombreSolicita = dto.NombreSolicita,
                CatTipoDocSolicitaId = dto.CatTipoDocSolicitaId,
                NumDocSolicita = dto.NumDocSolicita,
                CatTipoDocReceptorId = factura.Receptor?.CatTipoDocumentoIdentificacionReceptorId ?? 1, // Default NIT si null
                NumDocReceptor = factura.Receptor?.NumeroDocumento ?? "",
                NombreReceptor = factura.Receptor?.NombreRazonSocial ?? "",
                MontoIva = factura.TotalIva,
                CorreoReceptor = factura.Receptor?.CorreoElectronico,
                TelefonoReceptor = receptorTelefono,

                // GUARDAR DATOS DE RESPUESTA REAL
                SelloRecibido = responseHacienda.SelloRecibido,
                EstadoHacienda = responseHacienda.Estado, // Debería ser "PROCESADO"
                FechaTransmision = DateTime.UtcNow,
                JsonEvento = JsonSerializer.Serialize(eventoDto),
                JsonRespuesta = JsonSerializer.Serialize(responseHacienda),

                // CAMPOS DE INVENTARIO
                TipoInvalidacion = dto.TipoInvalidacion ?? "ERROR_FACTURA",
                RevirtiInventario = dto.RevirtiInventario
            };

            _context.Invalidaciones.Add(invalidacion);

            // ACTUALIZAR ESTADO DE LA FACTURA ORIGINAL
            // Solo marcar como ANULADO si Hacienda dijo PROCESADO
            if (responseHacienda.Estado == "PROCESADO")
            {
                factura.EstadoHacienda = "ANULADO";
                factura.Observaciones = $"ANULADO: {dto.MotivoAnulacion}";
                factura.Activo = false;
            }

            await _context.SaveChangesAsync();

            // Enviar notificación de invalidación al receptor (no bloqueante)
            if (responseHacienda.Estado == "PROCESADO")
            {
                await IntentarEnviarEmailInvalidacionAsync(factura.Id, factura.ReceptorId == 0 ? null : (int?)factura.ReceptorId, dto.MotivoAnulacion ?? "Sin motivo especificado");
            }

            // INTEGRACIÓN CON INVENTARIO
            if (invalidacion.RevirtiInventario && responseHacienda.Estado == "PROCESADO")
            {
                try
                {
                    await _inventarioService.RevertirVentaAsync(invalidacion.Id);
                }
                catch (Exception ex)
                {
                    // Log error pero no fallar la transacción de invalidación
                    _logger.LogError(ex, "Error al revertir inventario para invalidación {InvalidacionId}", invalidacion.Id);
                    // TODO: Implementar mecanismo de reintento o alerta
                }
            }

            // Mapear respuesta
            return new FraFactu.Application.DTOs.Invalidacion.InvalidacionDto
            {
                Identificacion = new FraFactu.Application.DTOs.Invalidacion.IdentificacionInvalidacionDto
                {
                    CodigoGeneracion = invalidacion.CodigoGeneracion,
                    FecAnula = invalidacion.FechaAnulacion.ToString("yyyy-MM-dd"),
                    HorAnula = invalidacion.HoraAnulacion.ToString(@"hh\:mm\:ss")
                },
                Estado = responseHacienda.Estado,
                SelloRecibido = responseHacienda.SelloRecibido
            };
        }

        public async Task DescartarFacturaRechazadaAsync(int facturaId, int emisorId)
        {
            var factura = await _context.Facturas
                .FirstOrDefaultAsync(f => f.Id == facturaId && f.EmisorId == emisorId);

            if (factura == null)
                throw new KeyNotFoundException("Factura no encontrada");

            if (factura.EstadoHacienda != "RECHAZADO" && factura.EstadoHacienda != "ERROR")
                throw new InvalidOperationException("Solo se pueden descartar facturas en estado RECHAZADO o ERROR");

            // Otras FKs con RESTRICT que bloquearian el delete: pre-chequear y dar mensaje
            // claro en vez del DATABASE_ERROR generico del middleware global.
            // - LoteDetalles.FacturaElectronicaId: la factura quedo en un lote (raro para
            //   RECHAZADO pero defensivo).
            // - ContingenciaDetalles.FacturaElectronicaId: la factura es parte de un
            //   evento de contingencia.
            // - Invalidaciones.{FacturaElectronicaId,FacturaReemplazoId}: la factura es
            //   origen o reemplazo de una invalidacion (no aplica normalmente a RECHAZADO).
            var enLote = await _context.LoteDetalles.AnyAsync(d => d.FacturaElectronicaId == facturaId);
            if (enLote)
                throw new InvalidOperationException(
                    "No se puede descartar: la factura forma parte de un lote enviado. " +
                    "Para anular este DTE use el flujo de Invalidacion, no Descartar.");

            var enContingencia = await _context.ContingenciaDetalles.AnyAsync(d => d.FacturaElectronicaId == facturaId);
            if (enContingencia)
                throw new InvalidOperationException(
                    "No se puede descartar: la factura esta vinculada a un evento de contingencia. " +
                    "Elimine primero el detalle del evento o use Invalidacion.");

            var tieneInvalidacion = await _context.Invalidaciones.AnyAsync(i =>
                i.FacturaElectronicaId == facturaId || i.FacturaReemplazoId == facturaId);
            if (tieneInvalidacion)
                throw new InvalidOperationException(
                    "No se puede descartar: existe una invalidacion que referencia esta factura " +
                    "(como origen o reemplazo). Revise el flujo de invalidaciones primero.");

            // Revertir stock (solo si no es FSE tipo 14, que nunca descontó stock)
            var tipoDocFact = await _context.CatTiposDocumento
                .Where(t => t.Id == factura.CatTipoDocumentoId)
                .Select(t => t.Codigo)
                .FirstOrDefaultAsync();

            if (tipoDocFact != "14")
            {
                await _inventarioService.RevertirStockFacturaAsync(facturaId);
            }

            // Eliminar la factura (FacturaApendices, FacturaDocumentosRelacionados,
            // FacturaExtensiones, FacturaPagos, FacturaTributos, factura_detalles,
            // OtrosDocumentos, VentaTerceros cascadean por FK CASCADE).
            _context.Facturas.Remove(factura);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Envía email de notificación de invalidación de forma no bloqueante.
        /// </summary>
        private async Task IntentarEnviarEmailInvalidacionAsync(int facturaId, int? receptorId, string motivoAnulacion)
        {
            try
            {
                if (!receptorId.HasValue || receptorId.Value == 0)
                {
                    _logger.LogDebug("[EMAIL-AUTO] Factura {FacturaId} sin receptor asignado, omitiendo email de invalidación", facturaId);
                    return;
                }

                var receptor = await _context.Receptores.FindAsync(receptorId.Value);
                var emailReceptor = receptor?.CorreoElectronico;

                if (string.IsNullOrWhiteSpace(emailReceptor))
                {
                    _logger.LogDebug("[EMAIL-AUTO] Receptor {ReceptorId} sin correo, omitiendo email de invalidación para factura {FacturaId}",
                        receptorId.Value, facturaId);
                    return;
                }

                await _emailService.EnviarNotificacionInvalidacionAsync(facturaId, emailReceptor, motivoAnulacion);
                _logger.LogInformation("[EMAIL-AUTO] Email de invalidación enviado para factura {FacturaId} a {Email}", facturaId, emailReceptor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EMAIL-AUTO] Error al enviar email de invalidación para factura {FacturaId}. El error no afecta la transaccion principal.", facturaId);
            }
        }
    }
}
