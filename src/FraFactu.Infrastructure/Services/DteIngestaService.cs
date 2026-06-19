using FraFactu.Application.DTOs.DtesRecibidos;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FraFactu.Infrastructure.Services;

/// <summary>
/// Pipeline único de ingesta de DTEs recibidos (ver <see cref="IDteIngestaService"/>).
/// Extrae la lógica que antes vivía acoplada dentro de
/// <c>EmailReaderService.ProcesarMensajeAsync</c> para que la lectura de
/// correo y la carga manual compartan exactamente las mismas reglas de
/// validación, deduplicación y persistencia.
/// </summary>
public class DteIngestaService : IDteIngestaService
{
    private const string TipoDteCCF = "03";

    private readonly ApplicationDbContext _context;
    private readonly IDteParserService _dteParser;
    private readonly ILogger<DteIngestaService> _logger;

    public DteIngestaService(
        ApplicationDbContext context,
        IDteParserService dteParser,
        ILogger<DteIngestaService> logger)
    {
        _context = context;
        _dteParser = dteParser;
        _logger = logger;
    }

    public async Task<IngestaDteResultado> IngestarAsync(
        string contenidoCrudo, IngestaContexto contexto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(contenidoCrudo) || !_dteParser.EsDteValido(contenidoCrudo))
            return IngestaDteResultado.De(ResultadoIngestaDte.ContenidoInvalido,
                "El contenido no es un DTE válido (no es JSON/JWT parseable o le faltan secciones).");

        DteRecibidoParsedDto dteParsed;
        try
        {
            dteParsed = contenidoCrudo.TrimStart().StartsWith('{')
                ? _dteParser.ParsearDteJson(contenidoCrudo)
                : _dteParser.ParsearDteJwt(contenidoCrudo);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Ingesta] No se pudo parsear el DTE para emisor {EmisorId}", contexto.EmisorId);
            return IngestaDteResultado.De(ResultadoIngestaDte.ContenidoInvalido,
                $"No se pudo parsear el DTE: {ex.Message}");
        }

        // Solo se aceptan Comprobantes de Crédito Fiscal.
        if (dteParsed.TipoDte != TipoDteCCF)
            return IngestaDteResultado.De(ResultadoIngestaDte.NoEsCCF,
                $"El documento es tipo '{dteParsed.TipoDte}'; solo se aceptan CCF (tipo 03).",
                dteParsed.CodigoGeneracion);

        // El emisor debe ser el receptor del DTE (es un documento recibido, no emitido).
        // Se preserva la regla original: solo se rechaza si ambos NIT están
        // presentes y difieren (un receptor nulo no bloquea la ingesta).
        if (!string.IsNullOrEmpty(contexto.EmisorNit) && !string.IsNullOrEmpty(dteParsed.ReceptorNit)
            && Normalizar(dteParsed.ReceptorNit) != Normalizar(contexto.EmisorNit))
        {
            _logger.LogDebug(
                "[Ingesta] DTE {CodigoGen} descartado: receptor no coincide con el emisor",
                dteParsed.CodigoGeneracion);
            return IngestaDteResultado.De(ResultadoIngestaDte.ReceptorInvalido,
                "El receptor del DTE no coincide con el NIT del emisor.",
                dteParsed.CodigoGeneracion);
        }

        // Fast-path: evita la excepción en el caso común de relectura.
        var yaExiste = await _context.DtesRecibidos.AsNoTracking()
            .AnyAsync(d => d.EmisorId == contexto.EmisorId
                && d.CodigoGeneracion == dteParsed.CodigoGeneracion, ct);
        if (yaExiste)
            return IngestaDteResultado.De(ResultadoIngestaDte.Duplicado,
                "El DTE ya estaba registrado para este emisor.", dteParsed.CodigoGeneracion);

        var dte = new DteRecibido
        {
            EmisorId = contexto.EmisorId,
            CodigoGeneracion = dteParsed.CodigoGeneracion,
            SelloRecibido = dteParsed.SelloRecibido,
            TipoDte = dteParsed.TipoDte,
            NumeroControl = dteParsed.NumeroControl,
            FechaEmision = DateTime.SpecifyKind(dteParsed.FechaEmision, DateTimeKind.Utc),
            EmisorNit = dteParsed.EmisorNit,
            EmisorNombre = dteParsed.EmisorNombre,
            EmisorNrc = dteParsed.EmisorNrc,
            ReceptorNit = dteParsed.ReceptorNit,
            ReceptorNombre = dteParsed.ReceptorNombre,
            JsonDte = dteParsed.JsonOriginal,
            MontoGravado = dteParsed.MontoGravado,
            MontoExento = dteParsed.MontoExento,
            MontoNoSujeto = dteParsed.MontoNoSujeto,
            SubTotal = dteParsed.SubTotal,
            IVA = dteParsed.IVA,
            Total = dteParsed.Total,
            Estado = EstadoDteRecibido.PENDIENTE,
            FuenteRecepcion = contexto.Fuente,
            CargadoPorUsuarioId = contexto.CargadoPorUsuarioId,
            EmailOrigen = contexto.EmailOrigen,
            FechaRecepcionEmail = contexto.FechaRecepcionEmail,
            FechaCreacion = DateTime.UtcNow
        };

        _context.DtesRecibidos.Add(dte);

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (EsViolacionDeUnicidad(ex))
        {
            // Race condition: otra ejecución insertó el mismo DTE entre el
            // fast-path y el SaveChanges. El índice único lo bloqueó; se trata
            // como duplicado en vez de abortar el lote completo.
            _context.Entry(dte).State = EntityState.Detached;
            _logger.LogInformation(
                "[Ingesta] DTE {CodigoGen} ya existía (carrera resuelta por índice único) para emisor {EmisorId}",
                dteParsed.CodigoGeneracion, contexto.EmisorId);
            return IngestaDteResultado.De(ResultadoIngestaDte.Duplicado,
                "El DTE ya estaba registrado para este emisor.", dteParsed.CodigoGeneracion);
        }

        _logger.LogInformation(
            "[Ingesta] DTE {CodigoGen} cargado para emisor {EmisorId} (fuente {Fuente})",
            dte.CodigoGeneracion, contexto.EmisorId, contexto.Fuente);

        return IngestaDteResultado.De(ResultadoIngestaDte.Cargado,
            "DTE cargado correctamente.", dte.CodigoGeneracion, dte.Id);
    }

    private static string Normalizar(string nit) => nit.Replace("-", "").Trim();

    /// <summary>
    /// Detecta la violación del índice único (EmisorId, CodigoGeneracion).
    /// PostgreSQL devuelve SqlState 23505 (unique_violation).
    /// </summary>
    private static bool EsViolacionDeUnicidad(DbUpdateException ex)
        => ex.InnerException is PostgresException pg && pg.SqlState == "23505";
}
