using System.Text.Json;
using FraFactu.Application.DTOs.Hacienda;
using FraFactu.Application.DTOs.OperacionesEspeciales;
using FraFactu.Application.Interfaces;
using FraFactu.Application.Interfaces.Hacienda;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Helpers;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Services;

/// <summary>
/// Implementación del servicio de Eventos de Operaciones Especiales (tipoEvento "17").
/// </summary>
public class EventoOperacionEspecialService : IEventoOperacionEspecialService
{
    private readonly ApplicationDbContext _context;
    private readonly IHaciendaApiService _haciendaApiService;
    private readonly ILogger<EventoOperacionEspecialService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
        // V2.0: nulls explícitos (no se omiten).
    };

    public EventoOperacionEspecialService(
        ApplicationDbContext context,
        IHaciendaApiService haciendaApiService,
        ILogger<EventoOperacionEspecialService> logger)
    {
        _context = context;
        _haciendaApiService = haciendaApiService;
        _logger = logger;
    }

    public async Task<EventoOperacionEspecialResultDto> CrearEventoAsync(CrearEventoOperacionEspecialDto dto, int emisorId)
    {
        if (dto.Items == null || dto.Items.Count == 0)
            throw new InvalidOperationException("Debe incluir al menos 1 ítem en el evento");

        var emisor = await _context.Emisores
            .Include(e => e.AmbienteDestino)
            .FirstOrDefaultAsync(e => e.Id == emisorId)
            ?? throw new InvalidOperationException($"Emisor con ID {emisorId} no encontrado");

        var ahora = ObtenerHoraElSalvador();

        var evento = new EventoOperacionEspecial
        {
            EmisorId = emisorId,
            Version = 1,
            Ambiente = emisor.AmbienteDestino?.Codigo ?? "00",
            TipoModelo = 1,
            TipoOperacion = 1,
            TipoEvento = "17",
            TipoMoneda = "USD",
            CodigoGeneracion = Guid.NewGuid().ToString().ToUpper(),
            FechaEmision = ahora.Date,
            HoraEmision = ahora.TimeOfDay,
            EstadoHacienda = "PENDIENTE"
        };

        int numItem = 1;
        foreach (var item in dto.Items)
        {
            evento.Detalles.Add(new OperacionEspecialDetalle
            {
                EventoOperacionEspecial = evento,
                NumItem = numItem++,
                CodigoGeneracionRef = item.CodigoGeneracionRef,
                TipoDocumento = item.TipoDocumento,
                NumDocumento = item.NumDocumento,
                FechaEmisionDoc = item.FechaEmision,
                Cantidad = item.Cantidad,
                Descripcion = item.Descripcion,
                DocDel = item.DocDel,
                DocAl = item.DocAl,
                PrecioUni = item.PrecioUni,
                VentaNoSuj = item.VentaNoSuj,
                VentaExenta = item.VentaExenta,
                VentaGravada = item.VentaGravada,
                TributosJson = (item.Tributos != null && item.Tributos.Count > 0)
                    ? JsonSerializer.Serialize(item.Tributos)
                    : null
            });
        }

        // Cálculo del resumen a partir de los ítems
        evento.TotalNoSuj = decimal.Round(dto.Items.Sum(i => i.VentaNoSuj), 2, MidpointRounding.AwayFromZero);
        evento.TotalExenta = decimal.Round(dto.Items.Sum(i => i.VentaExenta), 2, MidpointRounding.AwayFromZero);
        evento.TotalGravada = decimal.Round(dto.Items.Sum(i => i.VentaGravada), 2, MidpointRounding.AwayFromZero);
        evento.SubTotal = evento.TotalNoSuj + evento.TotalExenta + evento.TotalGravada;

        var totalTributos = dto.Tributos?.Sum(t => t.Valor) ?? 0m;
        evento.Total = evento.SubTotal + decimal.Round(totalTributos, 2, MidpointRounding.AwayFromZero);
        evento.TotalLetras = FacturaCalculosHelper.ConvertirMontoALetras(evento.Total);

        evento.ResumenTributosJson = (dto.Tributos != null && dto.Tributos.Count > 0)
            ? JsonSerializer.Serialize(dto.Tributos)
            : null;
        evento.ApendiceJson = (dto.Apendice != null && dto.Apendice.Count > 0)
            ? JsonSerializer.Serialize(dto.Apendice)
            : null;

        // Construir el JSON del evento y persistir
        var payload = GenerarPayload(evento, emisor);
        evento.JsonEvento = JsonSerializer.Serialize(payload, JsonOptions);

        _context.Set<EventoOperacionEspecial>().Add(evento);
        await _context.SaveChangesAsync();

        // Transmitir a Hacienda (no falla la creación si MH no responde)
        try
        {
            _logger.LogInformation(
                "Transmitiendo evento de operaciones especiales {Codigo} con {Items} ítems. Ambiente={Ambiente}",
                evento.CodigoGeneracion, evento.Detalles.Count, evento.Ambiente);

            var respuesta = await _haciendaApiService.EnviarEventoOperacionesEspecialesAsync(emisorId, payload);

            if (respuesta.Estado is "RECIBIDO" or "PROCESADO")
            {
                evento.EstadoHacienda = respuesta.Estado;
                evento.SelloRecibido = respuesta.SelloRecibido;
                evento.JsonRespuesta = JsonSerializer.Serialize(respuesta);
                evento.FechaTransmisionMH = DateTime.UtcNow;
            }
            else
            {
                evento.EstadoHacienda = "RECHAZADO";
                evento.JsonRespuesta = JsonSerializer.Serialize(respuesta);
                _logger.LogWarning(
                    "Evento de operaciones especiales {Codigo} rechazado por MH. Mensaje: {Mensaje}",
                    evento.CodigoGeneracion, respuesta.Mensaje);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error transmitiendo evento de operaciones especiales {Codigo}. Queda como PENDIENTE.",
                evento.CodigoGeneracion);
        }

        return MapearAResultado(evento);
    }

    public async Task<EventoOperacionEspecialResultDto> ObtenerPorIdAsync(int id, int emisorId)
    {
        var evento = await _context.Set<EventoOperacionEspecial>()
            .Include(e => e.Detalles)
            .FirstOrDefaultAsync(e => e.Id == id && e.EmisorId == emisorId)
            ?? throw new InvalidOperationException($"Evento de operaciones especiales con ID {id} no encontrado");

        return MapearAResultado(evento);
    }

    public async Task<string> GenerarJsonEventoAsync(int id, int emisorId)
    {
        var evento = await _context.Set<EventoOperacionEspecial>()
            .Include(e => e.Emisor)
            .Include(e => e.Detalles)
            .FirstOrDefaultAsync(e => e.Id == id && e.EmisorId == emisorId)
            ?? throw new InvalidOperationException($"Evento de operaciones especiales con ID {id} no encontrado");

        var payload = GenerarPayload(evento, evento.Emisor);
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    // ==========================================
    // MÉTODOS PRIVADOS
    // ==========================================

    private static EventoOperacionesEspecialesDto GenerarPayload(EventoOperacionEspecial evento, Emisor emisor)
    {
        var tributosResumen = Deserializar<List<TributoResumenInputDto>>(evento.ResumenTributosJson);
        var apendice = Deserializar<List<ApendiceInputDto>>(evento.ApendiceJson);

        return new EventoOperacionesEspecialesDto
        {
            Identificacion = new IdentificacionOperacionEspecialDto
            {
                Version = evento.Version,
                Ambiente = evento.Ambiente,
                TipoModelo = evento.TipoModelo,
                TipoOperacion = evento.TipoOperacion,
                TipoEvento = evento.TipoEvento,
                CodigoGeneracion = evento.CodigoGeneracion,
                FecEmi = evento.FechaEmision.ToString("yyyy-MM-dd"),
                HorEmi = FormatearHora(evento.HoraEmision),
                TipoMoneda = evento.TipoMoneda
            },
            Emisor = new EmisorOperacionEspecialDto
            {
                Nit = emisor.Nit?.Replace("-", "") ?? string.Empty,
                Nombre = emisor.NombreComercial ?? emisor.NombreRazonSocial
            },
            CuerpoDocumento = evento.Detalles.OrderBy(d => d.NumItem).Select(d => new CuerpoOperacionEspecialDto
            {
                NumItem = d.NumItem,
                CodigoGeneracionRef = d.CodigoGeneracionRef,
                TipoDocumento = d.TipoDocumento,
                NumDocumento = d.NumDocumento,
                FechaEmision = d.FechaEmisionDoc?.ToString("yyyy-MM-dd"),
                Cantidad = d.Cantidad,
                Descripcion = d.Descripcion,
                DocDel = d.DocDel,
                DocAl = d.DocAl,
                PrecioUni = d.PrecioUni,
                VentaNoSuj = d.VentaNoSuj,
                VentaExenta = d.VentaExenta,
                VentaGravada = d.VentaGravada,
                Tributos = Deserializar<List<string>>(d.TributosJson)
            }).ToList(),
            Resumen = new ResumenOperacionEspecialDto
            {
                TotalNoSuj = evento.TotalNoSuj,
                TotalExenta = evento.TotalExenta,
                TotalGravada = evento.TotalGravada,
                SubTotal = evento.SubTotal,
                Tributos = tributosResumen?.Select(t => new TributoResumenOperacionEspecialDto
                {
                    Codigo = t.Codigo,
                    Descripcion = t.Descripcion,
                    Valor = t.Valor
                }).ToList(),
                Total = evento.Total,
                TotalLetras = evento.TotalLetras
            },
            Apendice = apendice?.Select(a => new ApendiceOperacionEspecialDto
            {
                Campo = a.Campo,
                Etiqueta = a.Etiqueta,
                Valor = a.Valor
            }).ToList()
        };
    }

    private static EventoOperacionEspecialResultDto MapearAResultado(EventoOperacionEspecial evento) => new()
    {
        Id = evento.Id,
        CodigoGeneracion = evento.CodigoGeneracion,
        Version = evento.Version,
        Ambiente = evento.Ambiente,
        FechaEmision = evento.FechaEmision.ToString("yyyy-MM-dd"),
        HoraEmision = FormatearHora(evento.HoraEmision),
        TotalNoSuj = evento.TotalNoSuj,
        TotalExenta = evento.TotalExenta,
        TotalGravada = evento.TotalGravada,
        SubTotal = evento.SubTotal,
        Total = evento.Total,
        TotalLetras = evento.TotalLetras,
        TotalItems = evento.Detalles.Count,
        EstadoHacienda = evento.EstadoHacienda ?? "PENDIENTE",
        SelloRecibido = evento.SelloRecibido,
        JsonEvento = evento.JsonEvento,
        JsonRespuesta = evento.JsonRespuesta
    };

    private static T? Deserializar<T>(string? json) where T : class =>
        string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<T>(json);

    private static string FormatearHora(TimeSpan hora) =>
        $"{(int)hora.TotalHours:D2}:{hora.Minutes:D2}:{hora.Seconds:D2}";

    private static DateTime ObtenerHoraElSalvador()
    {
        var zona = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zona);
    }
}
