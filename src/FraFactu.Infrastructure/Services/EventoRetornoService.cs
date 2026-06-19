using System.Text.Json;
using FraFactu.Application.DTOs.Hacienda;
using FraFactu.Application.DTOs.Retorno;
using FraFactu.Application.Interfaces;
using FraFactu.Application.Interfaces.Hacienda;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Helpers;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Services;

/// <summary>
/// Implementación del servicio de Eventos de Retorno (tipoEvento "18").
/// Los totales del resumen se calculan a partir de los ítems. La aritmética fina del resumen
/// (saldos, no onerosas, compras excluidas) se ajusta con los datos provistos por el emisor y
/// se valida contra apitest del MH. NO reconcilia saldos vivos de los DTE relacionados.
/// </summary>
public class EventoRetornoService : IEventoRetornoService
{
    private readonly ApplicationDbContext _context;
    private readonly IHaciendaApiService _haciendaApiService;
    private readonly ILogger<EventoRetornoService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
        // V2.0: nulls explícitos (no se omiten).
    };

    public EventoRetornoService(
        ApplicationDbContext context,
        IHaciendaApiService haciendaApiService,
        ILogger<EventoRetornoService> logger)
    {
        _context = context;
        _haciendaApiService = haciendaApiService;
        _logger = logger;
    }

    public async Task<EventoRetornoResultDto> CrearEventoAsync(CrearEventoRetornoDto dto, int emisorId)
    {
        if (dto.Items == null || dto.Items.Count == 0)
            throw new InvalidOperationException("Debe incluir al menos 1 ítem en el evento");
        if (dto.DocumentoRelacionado == null || dto.DocumentoRelacionado.Count == 0)
            throw new InvalidOperationException("Debe incluir al menos 1 documento relacionado");
        if (dto.VentaTercero != null && dto.CompraTercero != null)
            throw new InvalidOperationException("ventaTercero y compraTercero son mutuamente excluyentes");

        var emisor = await _context.Emisores
            .Include(e => e.AmbienteDestino)
            .FirstOrDefaultAsync(e => e.Id == emisorId)
            ?? throw new InvalidOperationException($"Emisor con ID {emisorId} no encontrado");

        var ahora = ObtenerHoraElSalvador();

        var evento = new EventoRetorno
        {
            EmisorId = emisorId,
            Version = 1,
            Ambiente = emisor.AmbienteDestino?.Codigo ?? "00",
            TipoModelo = dto.TipoModelo,
            TipoOperacion = dto.TipoOperacion,
            TipoEvento = "18",
            TipoContingencia = dto.TipoContingencia,
            MotivoContin = dto.MotivoContin,
            CodigoGeneracion = Guid.NewGuid().ToString().ToUpper(),
            FechaEmision = ahora.Date,
            HoraEmision = ahora.TimeOfDay,
            Fusion = dto.Fusion,
            TipoMoneda = "USD",
            CodEstableMH = dto.CodEstableMH,
            CodEstable = dto.CodEstable,
            CodPuntoVentaMH = dto.CodPuntoVentaMH,
            CodPuntoVenta = dto.CodPuntoVenta,
            RecintoFiscal = dto.RecintoFiscal,
            TipoRegimen = dto.TipoRegimen,
            Regimen = dto.Regimen,
            TipoItemExpor = dto.TipoItemExpor,
            DocumentoRelacionadoJson = JsonSerializer.Serialize(dto.DocumentoRelacionado),
            DocumentoReceptorJson = dto.Documento != null ? JsonSerializer.Serialize(dto.Documento) : null,
            VentaTerceroJson = dto.VentaTercero != null ? JsonSerializer.Serialize(dto.VentaTercero) : null,
            CompraTerceroJson = dto.CompraTercero != null ? JsonSerializer.Serialize(dto.CompraTercero) : null,
            ResumenTributosJson = (dto.Tributos != null && dto.Tributos.Count > 0) ? JsonSerializer.Serialize(dto.Tributos) : null,
            ApendiceJson = (dto.Apendice != null && dto.Apendice.Count > 0) ? JsonSerializer.Serialize(dto.Apendice) : null,
            TotalNoOnerosas = dto.TotalNoOnerosas,
            TotalCompraExcluidos = dto.TotalCompraExcluidos,
            SaldoFavor = dto.SaldoFavor,
            EstadoHacienda = "PENDIENTE"
        };

        int numItem = 1;
        foreach (var item in dto.Items)
        {
            evento.Detalles.Add(new RetornoDetalle
            {
                EventoRetorno = evento,
                NumItem = numItem++,
                TipoItem = item.TipoItem,
                CodigoGeneracion = item.CodigoGeneracion,
                Cantidad = item.Cantidad,
                PrecioUni = item.PrecioUni,
                Descripcion = item.Descripcion,
                Codigo = item.Codigo,
                UniMedida = item.UniMedida,
                MontoDescu = item.MontoDescu,
                CodTributo = item.CodTributo,
                VentaNoSuj = item.VentaNoSuj,
                VentaExenta = item.VentaExenta,
                VentaGravada = item.VentaGravada,
                Compra = item.Compra,
                TributosJson = (item.Tributos != null && item.Tributos.Count > 0) ? JsonSerializer.Serialize(item.Tributos) : null,
                Psv = item.Psv,
                IvaItem = item.IvaItem,
                NoGravado = item.NoGravado,
                Seguro = item.Seguro,
                Flete = item.Flete,
                IvaRete = item.IvaRete,
                ReteRenta = item.ReteRenta
            });
        }

        CalcularResumen(evento, dto);

        var payload = GenerarPayload(evento, emisor);
        evento.JsonEvento = JsonSerializer.Serialize(payload, JsonOptions);

        _context.Set<EventoRetorno>().Add(evento);
        await _context.SaveChangesAsync();

        try
        {
            _logger.LogInformation(
                "Transmitiendo evento de retorno {Codigo} con {Items} ítems. Ambiente={Ambiente}",
                evento.CodigoGeneracion, evento.Detalles.Count, evento.Ambiente);

            var respuesta = await _haciendaApiService.EnviarEventoRetornoAsync(emisorId, payload);

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
                _logger.LogWarning("Evento de retorno {Codigo} rechazado por MH. Mensaje: {Mensaje}",
                    evento.CodigoGeneracion, respuesta.Mensaje);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transmitiendo evento de retorno {Codigo}. Queda como PENDIENTE.", evento.CodigoGeneracion);
        }

        return MapearAResultado(evento);
    }

    public async Task<EventoRetornoResultDto> ObtenerPorIdAsync(int id, int emisorId)
    {
        var evento = await _context.Set<EventoRetorno>()
            .Include(e => e.Detalles)
            .FirstOrDefaultAsync(e => e.Id == id && e.EmisorId == emisorId)
            ?? throw new InvalidOperationException($"Evento de retorno con ID {id} no encontrado");

        return MapearAResultado(evento);
    }

    public async Task<string> GenerarJsonEventoAsync(int id, int emisorId)
    {
        var evento = await _context.Set<EventoRetorno>()
            .Include(e => e.Emisor)
            .Include(e => e.Detalles)
            .FirstOrDefaultAsync(e => e.Id == id && e.EmisorId == emisorId)
            ?? throw new InvalidOperationException($"Evento de retorno con ID {id} no encontrado");

        var payload = GenerarPayload(evento, evento.Emisor);
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    // ==========================================
    // MÉTODOS PRIVADOS
    // ==========================================

    private static void CalcularResumen(EventoRetorno evento, CrearEventoRetornoDto dto)
    {
        var items = dto.Items;

        evento.TotalNoSuj = Redondear(items.Sum(i => i.VentaNoSuj));
        evento.TotalExenta = Redondear(items.Sum(i => i.VentaExenta));
        evento.TotalGravada = Redondear(items.Sum(i => i.VentaGravada));
        evento.SubTotalVentas = evento.TotalNoSuj + evento.TotalExenta + evento.TotalGravada;
        evento.TotalIva = Redondear(items.Sum(i => i.IvaItem));
        evento.TotalNoGravado = Redondear(items.Sum(i => i.NoGravado));
        evento.IvaRete = Redondear(items.Sum(i => i.IvaRete));
        evento.ReteRenta = Redondear(items.Sum(i => i.ReteRenta));

        var seguro = Redondear(items.Sum(i => i.Seguro));
        var flete = Redondear(items.Sum(i => i.Flete));
        evento.TotalSeguro = seguro != 0 ? seguro : null;
        evento.TotalFlete = flete != 0 ? flete : null;

        evento.MontoTotalOperacion = evento.SubTotalVentas + evento.TotalIva + seguro + flete + evento.TotalNoGravado;
        evento.TotalPagar = evento.MontoTotalOperacion - evento.IvaRete - (evento.ReteRenta ?? 0);
        evento.TotalLetras = FacturaCalculosHelper.ConvertirMontoALetras(evento.TotalPagar);
    }

    private static EventoRetornoDto GenerarPayload(EventoRetorno evento, Emisor emisor)
    {
        var documentosRelacionados = Deserializar<List<DocumentoRelacionadoInputDto>>(evento.DocumentoRelacionadoJson) ?? new();
        var receptor = Deserializar<DocumentoReceptorInputDto>(evento.DocumentoReceptorJson);
        var ventaTercero = Deserializar<VentaTerceroInputDto>(evento.VentaTerceroJson);
        var compraTercero = Deserializar<CompraTerceroInputDto>(evento.CompraTerceroJson);
        var tributosResumen = Deserializar<List<TributoResumenInputDto>>(evento.ResumenTributosJson);
        var apendice = Deserializar<List<ApendiceInputDto>>(evento.ApendiceJson);

        return new EventoRetornoDto
        {
            Identificacion = new IdentificacionRetornoDto
            {
                Version = evento.Version,
                Ambiente = evento.Ambiente,
                TipoModelo = evento.TipoModelo,
                TipoOperacion = evento.TipoOperacion,
                TipoEvento = evento.TipoEvento,
                TipoContingencia = evento.TipoContingencia,
                MotivoContin = evento.MotivoContin,
                CodigoGeneracion = evento.CodigoGeneracion,
                FecEmi = evento.FechaEmision.ToString("yyyy-MM-dd"),
                HorEmi = FormatearHora(evento.HoraEmision),
                Fusion = evento.Fusion,
                TipoMoneda = evento.TipoMoneda
            },
            DocumentoRelacionado = documentosRelacionados.Select(d => new DocumentoRelacionadoRetornoDto
            {
                TipoDocumento = d.TipoDocumento,
                CodigoGeneracion = d.CodigoGeneracion,
                FechaEmision = d.FechaEmision.ToString("yyyy-MM-dd")
            }).ToList(),
            Emisor = new EmisorRetornoDto
            {
                Nit = emisor.Nit?.Replace("-", "") ?? string.Empty,
                Nombre = emisor.NombreComercial ?? emisor.NombreRazonSocial,
                CodEstableMH = evento.CodEstableMH,
                CodEstable = evento.CodEstable,
                CodPuntoVentaMH = evento.CodPuntoVentaMH,
                CodPuntoVenta = evento.CodPuntoVenta,
                RecintoFiscal = evento.RecintoFiscal,
                TipoRegimen = evento.TipoRegimen,
                Regimen = evento.Regimen,
                TipoItemExpor = evento.TipoItemExpor
            },
            Documento = receptor == null ? null : new DocumentoReceptorRetornoDto
            {
                TipoDocumento = receptor.TipoDocumento,
                NumDocumento = receptor.NumDocumento,
                Nombre = receptor.Nombre,
                CodPais = receptor.CodPais,
                NombrePais = receptor.NombrePais,
                Telefono = receptor.Telefono,
                Correo = receptor.Correo
            },
            VentaTercero = ventaTercero == null ? null : new VentaTerceroRetornoDto
            {
                Nit = ventaTercero.Nit,
                Nombre = ventaTercero.Nombre,
                CodDomiciliado = ventaTercero.CodDomiciliado
            },
            CompraTercero = compraTercero == null ? null : new CompraTerceroRetornoDto
            {
                NumDocumento = compraTercero.NumDocumento,
                Nombre = compraTercero.Nombre
            },
            CuerpoDocumento = evento.Detalles.OrderBy(d => d.NumItem).Select(d => new CuerpoRetornoDto
            {
                NumItem = d.NumItem,
                TipoItem = d.TipoItem,
                CodigoGeneracion = d.CodigoGeneracion,
                Cantidad = d.Cantidad,
                PrecioUni = d.PrecioUni,
                Descripcion = d.Descripcion,
                Codigo = d.Codigo,
                UniMedida = d.UniMedida,
                MontoDescu = d.MontoDescu,
                CodTributo = d.CodTributo,
                VentaNoSuj = d.VentaNoSuj,
                VentaExenta = d.VentaExenta,
                VentaGravada = d.VentaGravada,
                Compra = d.Compra,
                Tributos = Deserializar<List<string>>(d.TributosJson),
                Psv = d.Psv,
                IvaItem = d.IvaItem,
                NoGravado = d.NoGravado,
                Seguro = d.Seguro,
                Flete = d.Flete,
                IvaRete = d.IvaRete,
                ReteRenta = d.ReteRenta
            }).ToList(),
            Resumen = new ResumenRetornoDto
            {
                TotalNoSuj = evento.TotalNoSuj,
                TotalExenta = evento.TotalExenta,
                TotalGravada = evento.TotalGravada,
                TotalCompraExcluidos = evento.TotalCompraExcluidos,
                SubTotalVentas = evento.SubTotalVentas,
                Tributos = tributosResumen?.Select(t => new TributoResumenRetornoDto
                {
                    Codigo = t.Codigo,
                    Descripcion = t.Descripcion,
                    Valor = t.Valor
                }).ToList(),
                TotalSeguro = evento.TotalSeguro,
                TotalFlete = evento.TotalFlete,
                MontoTotalOperacion = evento.MontoTotalOperacion,
                IvaRete = evento.IvaRete,
                ReteRenta = evento.ReteRenta,
                TotalNoGravado = evento.TotalNoGravado,
                TotalPagar = evento.TotalPagar,
                TotalLetras = evento.TotalLetras,
                TotalNoOnerosas = evento.TotalNoOnerosas,
                TotalIva = evento.TotalIva,
                SaldoFavor = evento.SaldoFavor
            },
            Apendice = apendice?.Select(a => new ApendiceRetornoDto
            {
                Campo = a.Campo,
                Etiqueta = a.Etiqueta,
                Valor = a.Valor
            }).ToList()
        };
    }

    private static EventoRetornoResultDto MapearAResultado(EventoRetorno evento)
    {
        var docsRel = Deserializar<List<DocumentoRelacionadoInputDto>>(evento.DocumentoRelacionadoJson);
        return new EventoRetornoResultDto
        {
            Id = evento.Id,
            CodigoGeneracion = evento.CodigoGeneracion,
            Version = evento.Version,
            Ambiente = evento.Ambiente,
            FechaEmision = evento.FechaEmision.ToString("yyyy-MM-dd"),
            HoraEmision = FormatearHora(evento.HoraEmision),
            SubTotalVentas = evento.SubTotalVentas,
            MontoTotalOperacion = evento.MontoTotalOperacion,
            TotalIva = evento.TotalIva,
            TotalPagar = evento.TotalPagar,
            TotalLetras = evento.TotalLetras,
            TotalItems = evento.Detalles.Count,
            TotalDocumentosRelacionados = docsRel?.Count ?? 0,
            EstadoHacienda = evento.EstadoHacienda ?? "PENDIENTE",
            SelloRecibido = evento.SelloRecibido,
            JsonEvento = evento.JsonEvento,
            JsonRespuesta = evento.JsonRespuesta
        };
    }

    private static T? Deserializar<T>(string? json) where T : class =>
        string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<T>(json);

    private static decimal Redondear(decimal valor) => decimal.Round(valor, 2, MidpointRounding.AwayFromZero);

    private static string FormatearHora(TimeSpan hora) =>
        $"{(int)hora.TotalHours:D2}:{hora.Minutes:D2}:{hora.Seconds:D2}";

    private static DateTime ObtenerHoraElSalvador()
    {
        var zona = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zona);
    }
}
