using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Contingencia;
using FraFactu.Application.DTOs.Lotes;
using FraFactu.Application.Interfaces;
using FraFactu.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Helpers.Pagination;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

using FraFactu.Application.Interfaces.Hacienda;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Services;

/// <summary>
/// Implementación del servicio de Eventos de Contingencia
/// </summary>
public class EventoContingenciaService : IEventoContingenciaService
{
    private readonly ApplicationDbContext _context;
    private readonly IHaciendaApiService _haciendaApiService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventoContingenciaService> _logger;
    private readonly ISmartCareWebhookService _smartCareWebhook;

    public EventoContingenciaService(
        ApplicationDbContext context,
        IHaciendaApiService haciendaApiService,
        IServiceProvider serviceProvider,
        ILogger<EventoContingenciaService> logger,
        ISmartCareWebhookService smartCareWebhook)
    {
        _context = context;
        _haciendaApiService = haciendaApiService;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _smartCareWebhook = smartCareWebhook;
    }

    public async Task<EventoContingenciaDto> CrearEventoAsync(CrearEventoContingenciaDto dto, int emisorId)
    {
        // 1. VALIDAR QUE LAS FACTURAS EXISTEN Y PERTENECEN AL EMISOR
        var facturas = await _context.Facturas
            .Include(f => f.Emisor)
            .Include(f => f.Receptor)
            .Include(f => f.Sucursal!)
                .ThenInclude(s => s.TipoEstablecimiento)
            .Include(f => f.Caja)
            .Include(f => f.TipoDocumento)
            .Where(f => dto.FacturaIds.Contains(f.Id) && f.EmisorId == emisorId)
            .ToListAsync();

        if (facturas.Count != dto.FacturaIds.Count)
        {
            throw new InvalidOperationException(
                $"Una o más facturas no existen o no pertenecen al emisor. " +
                $"Se encontraron {facturas.Count} de {dto.FacturaIds.Count} facturas solicitadas.");
        }

        // 2. VALIDAR QUE LAS FACTURAS TIENEN CONFIGURACIÓN DE CONTINGENCIA
        var facturasInvalidas = facturas.Where(f =>
            f.CatModeloFacturacionId != 2 ||
            f.CatTipoTransmisionId != 2)
            .ToList();

        if (facturasInvalidas.Any())
        {
            throw new InvalidOperationException(
                "Las facturas deben tener ModeloFacturacion=2 (Diferido) y TipoTransmision=2 (Contingencia). " +
                $"Facturas inválidas: {string.Join(", ", facturasInvalidas.Select(f => f.Id))}");
        }

        // 3. OBTENER DATOS DEL EMISOR
        var emisor = await _context.Emisores
            .Include(e => e.AmbienteDestino)
            .FirstOrDefaultAsync(e => e.Id == emisorId);
        if (emisor == null)
        {
            throw new InvalidOperationException($"Emisor con ID {emisorId} no encontrado");
        }

        // 3.1 CARGAR CATÁLOGOS NECESARIOS PARA EL JSON
        var tipoDocResponsable = await _context.CatDocsIdentidadReceptor
            .FirstOrDefaultAsync(t => t.Id == dto.CatTipoDocResponsableId)
            ?? throw new InvalidOperationException($"Tipo de documento responsable {dto.CatTipoDocResponsableId} no encontrado");
        var primeraSucursal = facturas.First().Sucursal;
        var tipoEstablecimientoId = primeraSucursal?.CatTipoEstablecimientoId ?? emisor.CatTipoEstablecimientoId ?? 1;
        var tipoEstablecimiento = await _context.CatTiposEstablecimiento
            .FirstOrDefaultAsync(t => t.Id == tipoEstablecimientoId)
            ?? throw new InvalidOperationException($"Tipo de establecimiento no encontrado");

        // 4. CREAR EL EVENTO
        var ahora = ObtenerHoraElSalvador();
        var evento = new EventoContingencia
        {
            // Identificación
            EmisorId = emisorId,
            Version = 4,
            Ambiente = emisor.AmbienteDestino.Codigo, // "00" = Pruebas, "01" = Producción
            CodigoGeneracion = Guid.NewGuid().ToString().ToUpper(),
            FechaTransmision = ahora.Date,
            HoraTransmision = ahora.TimeOfDay,

            // Responsable
            NombreResponsable = dto.NombreResponsable,
            CatTipoDocResponsableId = dto.CatTipoDocResponsableId,
            TipoDocResponsable = tipoDocResponsable,
            NumeroDocResponsable = dto.NumeroDocResponsable,
            CatTipoEstablecimientoId = primeraSucursal?.CatTipoEstablecimientoId ?? emisor.CatTipoEstablecimientoId ?? 1,
            TipoEstablecimiento = tipoEstablecimiento,
            CodigoEstablecimientoMH = facturas.First().Sucursal?.CodigoEstablecimiento,
            CodigoPuntoVenta = facturas.First().Caja?.CodPuntoVenta,

            // Motivo
            FechaInicioContingencia = dto.FechaInicioContingencia,
            FechaFinContingencia = dto.FechaFinContingencia,
            HoraInicioContingencia = dto.HoraInicioContingencia,
            HoraFinContingencia = dto.HoraFinContingencia,
            TipoContingencia = dto.TipoContingencia,
            MotivoContingencia = dto.MotivoContingencia,

            // Estado inicial
            EstadoHacienda = "PENDIENTE"
        };

        // 5. CREAR LOS DETALLES (RELACIÓN CON FACTURAS)
        int noItem = 1;
        foreach (var factura in facturas.OrderBy(f => f.Id))
        {
            var detalle = new ContingenciaDetalle
            {
                EventoContingencia = evento,
                FacturaElectronicaId = factura.Id,
                NoItem = noItem++,
                CodigoGeneracion = factura.CodigoGeneracion,
                CatTipoDocumentoId = factura.CatTipoDocumentoId,  // FK directo
                TipoDocumento = factura.TipoDocumento
            };

            evento.Detalles.Add(detalle);
        }

        // 6. GENERAR EL JSON DEL EVENTO (DTO)
        var eventoDtoPayload = GenerarJsonEvento(evento, emisor, facturas);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
            // V2.0: nulls explícitos (no se omiten). El esquema marca los campos anulables como required.
        };

        evento.JsonEvento = JsonSerializer.Serialize(eventoDtoPayload, options);

        // 7. GUARDAR EN LA BASE DE DATOS
        _context.EventosContingencia.Add(evento);
        await _context.SaveChangesAsync();

        // 7.1 ACTUALIZAR EventoContingenciaId en todas las facturas incluidas
        foreach (var factura in facturas)
        {
            factura.EventoContingenciaId = evento.Id;
            factura.CatTipoContingenciaId = evento.TipoContingencia;
            if (evento.TipoContingencia == 5)
                factura.MotivoContingencia = evento.MotivoContingencia;
        }
        await _context.SaveChangesAsync();

        // 7.2 Las facturas ya estaban en PENDIENTE_LOTE pero recién ahora quedan
        // asociadas a un EventoContingenciaId. Avisamos a SmartCare para que el
        // botón "Ver en Smartix" reapunte de /facturas-pendientes a /contingencia
        // — sin esperar a que el lote se procese (que puede tomar minutos u horas
        // si MH responde RECIBIDO y el envío real lo hace un background job).
        foreach (var factura in facturas.Where(f => !string.IsNullOrEmpty(f.SmartCareCorrelationId)))
        {
            try
            {
                await _smartCareWebhook.NotificarCambioEstadoAsync(factura);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[SmartCare-Webhook] Falla al notificar asociación a evento {EventoId} de factura {FacturaId}.",
                    evento.Id, factura.Id);
            }
        }

        // 8. TRANSMITIR A HACIENDA AUTOMÁTICAMENTE
        try
        {
            _logger.LogInformation("Transmitiendo evento de contingencia {Codigo}...", evento.CodigoGeneracion);

            // Reutilizar lógica de transmisión existente en algún otro método o llamar directo al servicio MH (este servicio no tiene EnviarEvento)
            // Revisando diseño: EventoContingenciaService no tiene metodo Enviar, pero puede usar HaciendaApiService

            _logger.LogInformation(
                "Transmitiendo evento de contingencia {Codigo} con {NumDtes} DTEs al MH. Ambiente={Ambiente}",
                evento.CodigoGeneracion, evento.Detalles.Count, evento.Ambiente);

            var respuestaMh = await _haciendaApiService.EnviarEventoContingenciaAsync(emisorId, eventoDtoPayload);

            if (respuestaMh.Estado == "RECIBIDO")
            {
                evento.EstadoHacienda = "RECIBIDO";
                evento.SelloRecibido = respuestaMh.SelloRecibido;
                evento.JsonRespuesta = JsonSerializer.Serialize(respuestaMh);
                evento.FechaTransmisionMH = DateTime.UtcNow;

                if (string.IsNullOrEmpty(respuestaMh.SelloRecibido))
                    _logger.LogWarning("Evento {Codigo} recibido por MH pero SIN sello de recepción", evento.CodigoGeneracion);
                else
                    _logger.LogInformation("Evento {Codigo} enviado y recibido. Sello: {Sello}", evento.CodigoGeneracion, evento.SelloRecibido);

                await _context.SaveChangesAsync();

                // 8.1 CREAR Y ENVIAR LOTE AUTOMÁTICAMENTE CON LAS FACTURAS DEL EVENTO
                try
                {
                    _logger.LogInformation(
                        "Creando lote automático de contingencia para evento {EventoId} con {Total} facturas...",
                        evento.Id, evento.Detalles.Count);

                    var crearLoteDto = new CrearLoteDto
                    {
                        FacturaIds = evento.Detalles.Select(d => d.FacturaElectronicaId).ToList(),
                        EsContingencia = true,
                        EventoContingenciaId = evento.Id
                    };

                    var loteService = _serviceProvider.GetRequiredService<ILoteService>();

                    var loteCreado = await loteService.CrearLoteAsync(emisorId, crearLoteDto);
                    _logger.LogInformation("Lote {LoteId} creado. Enviando a Hacienda...", loteCreado.Id);

                    var loteEnviado = await loteService.EnviarLoteAsync(loteCreado.Id);
                    _logger.LogInformation(
                        "Lote {LoteId} enviado. Estado: {Estado}",
                        loteEnviado.Id, loteEnviado.Estado);
                }
                catch (Exception exLote)
                {
                    _logger.LogError(exLote,
                        "Error creando/enviando lote automático para evento {EventoId}. Se reintentará automáticamente.",
                        evento.Id);
                    // No fallamos: el evento ya fue recibido, el servicio en background reintentará la creación del lote
                }
            }
            else
            {
                evento.EstadoHacienda = "RECHAZADO";
                // Guardar la respuesta completa del MH
                evento.JsonRespuesta = JsonSerializer.Serialize(respuestaMh);
                _logger.LogWarning(
                    "Evento {Codigo} rechazado por MH. Mensaje: {Mensaje}, Observaciones: {Observaciones}",
                    evento.CodigoGeneracion,
                    respuestaMh.Mensaje,
                    string.Join(", ", respuestaMh.Observaciones ?? new List<string>()));

                // Liberar las facturas para que puedan ser incluidas en otro evento
                var facturasDelEvento = await _context.Facturas
                    .Where(f => f.EventoContingenciaId == evento.Id)
                    .ToListAsync();

                foreach (var factura in facturasDelEvento)
                {
                    factura.EventoContingenciaId = null;
                }

                _logger.LogInformation(
                    "Se liberaron {Count} facturas del evento rechazado {EventoId} para reutilización",
                    facturasDelEvento.Count,
                    evento.Id);

                await _context.SaveChangesAsync();

                // Notificar a SmartCare: al desasociar del evento, el ViewUrl de
                // cada factura vuelve a /facturas-pendientes (PENDIENTE_LOTE sin
                // evento). Si no avisamos, SmartCare seguiría enviando al usuario
                // a /contingencia, donde la factura ya no aparece.
                foreach (var factura in facturasDelEvento.Where(f => !string.IsNullOrEmpty(f.SmartCareCorrelationId)))
                {
                    try
                    {
                        await _smartCareWebhook.NotificarCambioEstadoAsync(factura);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "[SmartCare-Webhook] Falla al notificar liberación de factura {FacturaId} del evento {EventoId}.",
                            factura.Id, evento.Id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transmitiendo evento de contingencia {Codigo}. Se intentará más tarde.", evento.CodigoGeneracion);
            // No fallamos la creación, el evento queda guardado como PENDIENTE
        }

        // 9. RETORNAR DTO
        var resultado = MapearADto(evento, emisor);
        resultado.TieneLote = await _context.Lotes.AnyAsync(l => l.EventoContingenciaId == evento.Id);
        resultado.ReintentosLote = evento.ReintentosLote;
        return resultado;
    }

    public async Task<EventoContingenciaDto> ObtenerPorIdAsync(int id, int emisorId)
    {
        var evento = await _context.EventosContingencia
            .Include(e => e.Emisor)
            .Include(e => e.TipoDocResponsable)
            .Include(e => e.TipoEstablecimiento)
            .Include(e => e.Detalles)
                .ThenInclude(d => d.FacturaElectronica)
                    .ThenInclude(f => f.Caja)
            .Include(e => e.Detalles)
                .ThenInclude(d => d.FacturaElectronica)
                    .ThenInclude(f => f.Sucursal)
            .Include(e => e.Detalles)
                .ThenInclude(d => d.TipoDocumento)
            .FirstOrDefaultAsync(e => e.Id == id && e.EmisorId == emisorId);

        if (evento == null)
        {
            throw new InvalidOperationException($"Evento de contingencia con ID {id} no encontrado");
        }

        var dto = MapearADto(evento, evento.Emisor);
        dto.TieneLote = await _context.Lotes.AnyAsync(l => l.EventoContingenciaId == evento.Id);
        dto.ReintentosLote = evento.ReintentosLote;
        return dto;
    }

    public async Task<PaginatedResponse<EventoContingenciaDto>> ListarPorEmisorAsync(
        PaginatedRequest request, int emisorId, List<int>? sucursalIds = null, int? usuarioId = null,
        string? search = null, int? tipoContingencia = null, string? estadoHacienda = null,
        DateTime? fechaDesde = null, DateTime? fechaHasta = null)
    {
        var query = _context.EventosContingencia
            .Include(e => e.Emisor)
            .Include(e => e.TipoDocResponsable)
            .Include(e => e.TipoEstablecimiento)
            .Include(e => e.Detalles)
                .ThenInclude(d => d.TipoDocumento)
            .Include(e => e.Detalles)
                .ThenInclude(d => d.FacturaElectronica)
                    .ThenInclude(f => f.Caja)
            .Include(e => e.Detalles)
                .ThenInclude(d => d.FacturaElectronica)
                    .ThenInclude(f => f.Sucursal)
            .Where(e => e.EmisorId == emisorId);

        if (sucursalIds != null && sucursalIds.Count > 0)
        {
            query = query.Where(e => e.Detalles.Any(d => d.FacturaElectronica.SucursalId.HasValue && sucursalIds.Contains(d.FacturaElectronica.SucursalId.Value)));
        }

        if (usuarioId.HasValue)
        {
            query = query.Where(e => e.Detalles.Any(d => d.FacturaElectronica.UsuarioId == usuarioId.Value));
        }

        // Filtro por búsqueda (código de generación)
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e => e.CodigoGeneracion.ToLower().Contains(search.ToLower()));
        }

        // Filtro por tipo de contingencia (CAT-005: 1-5)
        if (tipoContingencia.HasValue)
        {
            query = query.Where(e => e.TipoContingencia == tipoContingencia.Value);
        }

        // Filtro por estado de Hacienda
        if (!string.IsNullOrWhiteSpace(estadoHacienda))
        {
            query = query.Where(e => e.EstadoHacienda == estadoHacienda);
        }

        // Filtro por rango de fechas (fecha de transmisión)
        if (fechaDesde.HasValue)
        {
            query = query.Where(e => e.FechaTransmision >= fechaDesde.Value.Date);
        }
        if (fechaHasta.HasValue)
        {
            query = query.Where(e => e.FechaTransmision < fechaHasta.Value.Date.AddDays(1));
        }

        var orderedQuery = query.OrderByDescending(e => e.FechaTransmision);

        var pagedItems = await orderedQuery.ToPaginatedListAsync(request.PageNumber, request.PageSize);
        var itemsDto = pagedItems.Select(e => MapearADto(e, e.Emisor)).ToList();

        // Cargar IDs de eventos que ya tienen lote asociado
        var eventoIds = itemsDto.Select(d => d.Id).ToList();
        var eventosConLote = await _context.Lotes
            .Where(l => l.EventoContingenciaId != null && eventoIds.Contains(l.EventoContingenciaId.Value))
            .Select(l => l.EventoContingenciaId!.Value)
            .Distinct()
            .ToListAsync();
        var eventosConLoteSet = new HashSet<int>(eventosConLote);

        foreach (var dto in itemsDto)
        {
            dto.TieneLote = eventosConLoteSet.Contains(dto.Id);
            var entidad = pagedItems.FirstOrDefault(e => e.Id == dto.Id);
            dto.ReintentosLote = entidad?.ReintentosLote ?? 0;
        }

        return new PaginatedResponse<EventoContingenciaDto>
        {
            Items = itemsDto,
            CurrentPage = pagedItems.CurrentPage,
            PageSize = pagedItems.PageSize,
            TotalCount = pagedItems.TotalCount,
            TotalPages = pagedItems.TotalPages
        };
    }

    public async Task<string> GenerarJsonEventoAsync(int eventoId, int emisorId)
    {
        var evento = await _context.EventosContingencia
            .Include(e => e.Emisor)
            .Include(e => e.TipoDocResponsable)
            .Include(e => e.TipoEstablecimiento)
            .Include(e => e.Detalles)
                .ThenInclude(d => d.FacturaElectronica)
                    .ThenInclude(f => f.Caja)
            .Include(e => e.Detalles)
                .ThenInclude(d => d.FacturaElectronica)
                    .ThenInclude(f => f.Sucursal)
            .Include(e => e.Detalles)
                .ThenInclude(d => d.TipoDocumento)
            .FirstOrDefaultAsync(e => e.Id == eventoId && e.EmisorId == emisorId);

        if (evento == null)
        {
            throw new InvalidOperationException($"Evento con ID {eventoId} no encontrado");
        }

        var facturas = evento.Detalles.Select(d => d.FacturaElectronica).ToList();
        var dto = GenerarJsonEvento(evento, evento.Emisor, facturas);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
            // V2.0: nulls explícitos (no se omiten). El esquema marca los campos anulables como required.
        };

        return JsonSerializer.Serialize(dto, options);
    }

    // ==========================================
    // MÉTODOS PRIVADOS
    // ==========================================

    private FraFactu.Application.DTOs.Hacienda.EventoContingenciaDto GenerarJsonEvento(EventoContingencia evento, Emisor emisor, List<FacturaElectronica> facturas)
    {
        var sucursal = facturas.FirstOrDefault()?.Sucursal;
        var caja = facturas.FirstOrDefault()?.Caja;
        var dto = new FraFactu.Application.DTOs.Hacienda.EventoContingenciaDto
        {
            Identificacion = new FraFactu.Application.DTOs.Hacienda.IdentificacionContingenciaDto
            {
                Version = evento.Version,
                Ambiente = evento.Ambiente,
                CodigoGeneracion = evento.CodigoGeneracion,
                FTransmision = evento.FechaTransmision.ToString("yyyy-MM-dd"),
                HTransmision = $"{(int)evento.HoraTransmision.TotalHours:D2}:{evento.HoraTransmision.Minutes:D2}:{evento.HoraTransmision.Seconds:D2}"
            },
            Emisor = new FraFactu.Application.DTOs.Hacienda.EmisorContingenciaDto
            {
                Nit = emisor.Nit,
                Nombre = emisor.NombreComercial ?? emisor.NombreRazonSocial,
                NombreResponsable = evento.NombreResponsable,
                TipoDocResponsable = evento.TipoDocResponsable.Codigo,
                NumeroDocResponsable = evento.NumeroDocResponsable,
                TipoEstablecimiento = evento.TipoEstablecimiento.Codigo,
                CodEstableMH = evento.CodigoEstablecimientoMH,
                CodPuntoVentaMH = caja?.CodPuntoVentaMH,
                Telefono = sucursal?.Telefono ?? emisor.Telefono ?? "",
                Correo = sucursal?.CorreoElectronico ?? emisor.CorreoElectronico
            },
            DetalleDTE = evento.Detalles.OrderBy(d => d.NoItem).Select(d => new FraFactu.Application.DTOs.Hacienda.DetalleDteContingenciaDto
            {
                NoItem = d.NoItem,
                CodigoGeneracion = d.CodigoGeneracion,
                TipoDoc = d.TipoDocumento.Codigo
            }).ToList(),
            Motivo = new FraFactu.Application.DTOs.Hacienda.MotivoContingenciaDto
            {
                FInicio = evento.FechaInicioContingencia.ToString("yyyy-MM-dd"),
                FFin = evento.FechaFinContingencia.ToString("yyyy-MM-dd"),
                HInicio = $"{(int)evento.HoraInicioContingencia.TotalHours:D2}:{evento.HoraInicioContingencia.Minutes:D2}:{evento.HoraInicioContingencia.Seconds:D2}",
                HFin = $"{(int)evento.HoraFinContingencia.TotalHours:D2}:{evento.HoraFinContingencia.Minutes:D2}:{evento.HoraFinContingencia.Seconds:D2}",
                TipoContingencia = evento.TipoContingencia,
                MotivoContingencia = evento.MotivoContingencia
            }
        };

        return dto;
    }

    private EventoContingenciaDto MapearADto(EventoContingencia evento, Emisor emisor)
    {
        var sucursal = evento.Detalles.FirstOrDefault()?.FacturaElectronica?.Sucursal;
        var caja = evento.Detalles.FirstOrDefault()?.FacturaElectronica?.Caja;
        return new EventoContingenciaDto
        {
            // METADATOS
            Id = evento.Id,
            EstadoHacienda = evento.EstadoHacienda ?? "PENDIENTE",
            SelloRecibido = evento.SelloRecibido ?? string.Empty,
            JsonEvento = evento.JsonEvento,
            JsonRespuesta = evento.JsonRespuesta,
            TotalDtes = evento.Detalles.Count,

            // ESTRUCTURA JSON
            Identificacion = new IdentificacionContingenciaDto
            {
                Version = evento.Version,
                Ambiente = evento.Ambiente,
                CodigoGeneracion = evento.CodigoGeneracion,
                FTransmision = evento.FechaTransmision.ToString("yyyy-MM-dd"),
                HTransmision = $"{(int)evento.HoraTransmision.TotalHours:D2}:{evento.HoraTransmision.Minutes:D2}:{evento.HoraTransmision.Seconds:D2}"
            },
            Emisor = new EmisorContingenciaDto
            {
                Nit = emisor.Nit,
                Nombre = emisor.NombreComercial ?? emisor.NombreRazonSocial,
                NombreResponsable = evento.NombreResponsable,
                TipoDocResponsable = evento.TipoDocResponsable.Codigo,
                NumeroDocResponsable = evento.NumeroDocResponsable,
                TipoEstablecimiento = evento.TipoEstablecimiento.Codigo,
                CodEstableMH = evento.CodigoEstablecimientoMH,
                CodEstable = sucursal?.Codigo,
                CodPuntoVenta = caja?.CodPuntoVenta,
                CodPuntoVentaMH = caja?.CodPuntoVentaMH,
                Telefono = sucursal?.Telefono ?? emisor.Telefono ?? "",
                Correo = sucursal?.CorreoElectronico ?? emisor.CorreoElectronico
            },
            DetalleDTE = evento.Detalles.OrderBy(d => d.NoItem).Select(d => new DetalleDocumentoDto
            {
                NoItem = d.NoItem,
                CodigoGeneracion = d.CodigoGeneracion,
                TipoDoc = d.TipoDocumento.Codigo
            }).ToList(),
            Motivo = new MotivoContingenciaDto
            {
                FInicio = evento.FechaInicioContingencia.ToString("yyyy-MM-dd"),
                FFin = evento.FechaFinContingencia.ToString("yyyy-MM-dd"),
                HInicio = $"{(int)evento.HoraInicioContingencia.TotalHours:D2}:{evento.HoraInicioContingencia.Minutes:D2}:{evento.HoraInicioContingencia.Seconds:D2}",
                HFin = $"{(int)evento.HoraFinContingencia.TotalHours:D2}:{evento.HoraFinContingencia.Minutes:D2}:{evento.HoraFinContingencia.Seconds:D2}",
                TipoContingencia = evento.TipoContingencia,
                MotivoContingencia = evento.MotivoContingencia
            }
        };
    }

    // ==========================================
    // MÉTODOS DE CONTINGENCIA AUTOMÁTICA Y MANUAL
    // ==========================================

    /// <summary>
    /// Obtiene la hora actual en zona horaria de El Salvador (UTC-6)
    /// </summary>
    private DateTime ObtenerHoraElSalvador()
    {
        var zonaElSalvador = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaElSalvador);
    }

    /// <summary>
    /// Obtiene o crea un evento de contingencia automático (tipo 1: MH no disponible)
    /// Reutiliza un evento existente si hay uno activo del mismo tipo en las últimas 24h
    /// </summary>
    public async Task<int> ObtenerOCrearEventoContingenciaAutomaticoAsync(int emisorId, int tipoContingencia, string motivo)
    {
        var ahora = ObtenerHoraElSalvador();
        var hace24h = ahora.AddHours(-24);

        // Buscar evento automático activo del mismo tipo, creado en las últimas 24h, que no haya sido enviado a MH
        var eventoExistente = await _context.EventosContingencia
            .Where(e => e.EmisorId == emisorId
                && e.TipoContingencia == tipoContingencia
                && e.CreadoAutomaticamente == true
                && e.FechaInicioContingencia >= hace24h
                && e.EstadoHacienda != "RECIBIDO")
            .OrderByDescending(e => e.FechaInicioContingencia)
            .FirstOrDefaultAsync();

        if (eventoExistente != null)
        {
            _logger.LogInformation(
                "[CONTINGENCIA-AUTO] Reutilizando evento existente {EventoId} para emisor {EmisorId}",
                eventoExistente.Id, emisorId);
            return eventoExistente.Id;
        }

        // Crear nuevo evento automático con datos mínimos
        var emisor = await _context.Emisores
            .Include(e => e.AmbienteDestino)
            .FirstOrDefaultAsync(e => e.Id == emisorId);

        if (emisor == null)
            throw new InvalidOperationException($"Emisor con ID {emisorId} no encontrado");

        var nuevoEvento = new EventoContingencia
        {
            EmisorId = emisorId,
            Version = 4,
            Ambiente = emisor.AmbienteDestino?.Codigo ?? "00",
            CodigoGeneracion = Guid.NewGuid().ToString().ToUpper(),
            FechaTransmision = ahora.Date,
            HoraTransmision = ahora.TimeOfDay,
            FechaInicioContingencia = ahora,
            FechaFinContingencia = ahora,
            HoraInicioContingencia = ahora.TimeOfDay,
            HoraFinContingencia = ahora.TimeOfDay,
            TipoContingencia = tipoContingencia,
            MotivoContingencia = motivo,
            EstadoHacienda = "PENDIENTE",
            CreadoAutomaticamente = true
        };

        _context.EventosContingencia.Add(nuevoEvento);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "[CONTINGENCIA-AUTO] Nuevo evento {EventoId} creado automáticamente para emisor {EmisorId}. Tipo: {Tipo}, Motivo: {Motivo}",
            nuevoEvento.Id, emisorId, tipoContingencia, motivo);

        return nuevoEvento.Id;
    }

    /// <summary>
    /// Marca un DTE como diferido por contingencia manual (tipos 2-5)
    /// </summary>
    public async Task MarcarDteDiferidoAsync(MarcarDteDiferidoRequestDto request, int emisorId)
    {
        var ahora = ObtenerHoraElSalvador();

        // Validar tipo de contingencia (2-5 para manual)
        if (request.TipoContingencia < 2 || request.TipoContingencia > 5)
            throw new InvalidOperationException("El tipo de contingencia manual debe ser entre 2 y 5");

        // Validar que la fecha de inicio no sea futura
        if (request.FechaInicioContingencia > ahora)
            throw new InvalidOperationException("La fecha de inicio de contingencia no puede ser futura");

        // Validar que no hayan pasado más de 24 horas desde el inicio
        if ((ahora - request.FechaInicioContingencia).TotalHours > 24)
            throw new InvalidOperationException("Han pasado más de 24 horas desde el inicio de la contingencia");

        // Validar motivo para tipo 5
        if (request.TipoContingencia == 5 && string.IsNullOrWhiteSpace(request.MotivoDetallado))
            throw new InvalidOperationException("El motivo detallado es obligatorio para tipo de contingencia 5 (Otro)");

        // Obtener la factura
        var factura = await _context.Facturas
            .FirstOrDefaultAsync(f => f.Id == request.FacturaId && f.EmisorId == emisorId);

        if (factura == null)
            throw new InvalidOperationException($"Factura con ID {request.FacturaId} no encontrada o no pertenece al emisor");

        // Buscar o crear evento de contingencia
        var eventoId = await ObtenerOCrearEventoManualAsync(
            emisorId, request.TipoContingencia, request.FechaInicioContingencia, request.MotivoDetallado);

        // Marcar factura como diferida
        factura.CatModeloFacturacionId = 2; // Diferido
        factura.CatTipoTransmisionId = 2;   // Contingencia
        factura.EventoContingenciaId = eventoId;
        factura.EstadoHacienda = "PENDIENTE_LOTE";

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "[CONTINGENCIA-MANUAL] Factura {FacturaId} marcada como diferida. Evento: {EventoId}, Tipo: {Tipo}",
            request.FacturaId, eventoId, request.TipoContingencia);

        // Si la factura proviene de un prefill SmartCare, avisar del cambio
        // a PENDIENTE_LOTE-con-EventoContingenciaId para que el botón "Ver en
        // Smartix" de SmartCare apunte a /contingencia (tab Diferidos) en vez
        // de seguir apuntando a /facturas-pendientes. Fire-and-forget.
        if (!string.IsNullOrEmpty(factura.SmartCareCorrelationId))
        {
            try
            {
                await _smartCareWebhook.NotificarCambioEstadoAsync(factura);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[SmartCare-Webhook] Falla al notificar diferimiento manual de factura {FacturaId} a SmartCare (CorrelationId={Cid}).",
                    factura.Id, factura.SmartCareCorrelationId);
            }
        }
    }

    /// <summary>
    /// Busca o crea un evento de contingencia manual para el emisor
    /// </summary>
    private async Task<int> ObtenerOCrearEventoManualAsync(
        int emisorId, int tipoContingencia, DateTime fechaInicio, string? motivo)
    {
        var hace24h = ObtenerHoraElSalvador().AddHours(-24);

        // Buscar evento existente del mismo tipo y fecha de inicio similar
        var eventoExistente = await _context.EventosContingencia
            .Where(e => e.EmisorId == emisorId
                && e.TipoContingencia == tipoContingencia
                && e.FechaInicioContingencia >= hace24h
                && e.EstadoHacienda != "RECIBIDO")
            .OrderByDescending(e => e.FechaInicioContingencia)
            .FirstOrDefaultAsync();

        if (eventoExistente != null)
            return eventoExistente.Id;

        var emisor = await _context.Emisores
            .Include(e => e.AmbienteDestino)
            .FirstOrDefaultAsync(e => e.Id == emisorId);

        if (emisor == null)
            throw new InvalidOperationException($"Emisor con ID {emisorId} no encontrado");

        var ahora = ObtenerHoraElSalvador();
        var nuevoEvento = new EventoContingencia
        {
            EmisorId = emisorId,
            Version = 4,
            Ambiente = emisor.AmbienteDestino?.Codigo ?? "00",
            CodigoGeneracion = Guid.NewGuid().ToString().ToUpper(),
            FechaTransmision = ahora.Date,
            HoraTransmision = ahora.TimeOfDay,
            FechaInicioContingencia = fechaInicio,
            FechaFinContingencia = fechaInicio,
            HoraInicioContingencia = fechaInicio.TimeOfDay,
            HoraFinContingencia = fechaInicio.TimeOfDay,
            TipoContingencia = tipoContingencia,
            MotivoContingencia = motivo,
            EstadoHacienda = "PENDIENTE",
            CreadoAutomaticamente = false
        };

        _context.EventosContingencia.Add(nuevoEvento);
        await _context.SaveChangesAsync();

        return nuevoEvento.Id;
    }

    /// <summary>
    /// Obtiene el tiempo restante del plazo de 24 horas para un evento de contingencia
    /// </summary>
    public async Task<TiempoRestanteContingenciaDto> ObtenerTiempoRestanteAsync(int eventoId, int emisorId)
    {
        var evento = await _context.EventosContingencia
            .FirstOrDefaultAsync(e => e.Id == eventoId && e.EmisorId == emisorId);

        if (evento == null)
            throw new InvalidOperationException($"Evento de contingencia con ID {eventoId} no encontrado");

        var fechaInicio = evento.FechaInicioContingencia.Date.Add(evento.HoraInicioContingencia);
        var fechaLimite = fechaInicio.AddHours(24);
        var ahora = ObtenerHoraElSalvador();
        var tiempoRestante = fechaLimite - ahora;

        return new TiempoRestanteContingenciaDto
        {
            FechaInicioContingencia = fechaInicio,
            FechaLimite = fechaLimite,
            HorasRestantes = Math.Max(0, tiempoRestante.TotalHours),
            MinutosRestantes = Math.Max(0, tiempoRestante.TotalMinutes),
            Vencido = tiempoRestante.TotalMilliseconds <= 0
        };
    }
}
