using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs.DtesRecibidos;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FraFactu.Infrastructure.Services;

/// <summary>
/// Fachada para la lectura de DTEs desde Gmail.
///
/// Desde F2 esta clase ya no procesa Gmail inline. Solo:
/// <list type="bullet">
///   <item>Encola un <see cref="LecturaCorreoJob"/> que el consumidor en background recoge.</item>
///   <item>Devuelve el estado del job más reciente para el polling de la UI.</item>
///   <item>Conserva las operaciones síncronas y rápidas: configurar la lectura y probar la conexión Gmail.</item>
/// </list>
///
/// La lógica de procesado vive en <see cref="EmailLectorWorker"/>.
/// </summary>
public class EmailReaderService : IEmailReaderService
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly GoogleAuthSettings _googleSettings;
    private readonly ILogger<EmailReaderService> _logger;
    private readonly GoogleAuthorizationCodeFlow _flow;

    public EmailReaderService(
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        IOptions<GoogleAuthSettings> googleSettings,
        ILogger<EmailReaderService> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _googleSettings = googleSettings.Value;
        _logger = logger;

        _flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = _googleSettings.ClientId,
                ClientSecret = _googleSettings.ClientSecret
            },
            Scopes = new[] { GmailService.Scope.GmailReadonly, "email" }
        });
    }

    public async Task<EncolarLecturaCorreoResponseDto> EncolarLecturaAsync(
        int emisorId,
        int? usuarioId = null,
        DateTime? rangoDesde = null,
        DateTime? rangoHasta = null,
        bool esAutomatico = false,
        CancellationToken ct = default)
    {
        var emisor = await _context.Emisores.FindAsync(new object[] { emisorId }, ct)
            ?? throw new KeyNotFoundException($"Emisor con ID {emisorId} no encontrado");

        if (!emisor.LecturaCorreoHabilitada)
            throw new InvalidOperationException("La lectura de correo no está habilitada para este emisor.");

        if (!emisor.GmailConectado || string.IsNullOrEmpty(emisor.GmailRefreshToken))
            throw new InvalidOperationException("Gmail OAuth2 no está configurado. Conecte su cuenta de Gmail primero.");

        // Si no se especifica rango, default = mes en curso (UTC). El scheduler
        // automático siempre llega sin rango; el endpoint manual lo respeta si el
        // usuario no llenó el selector.
        var (desde, hasta) = ResolverRangoUtc(rangoDesde, rangoHasta);

        // Idempotencia: si ya hay un job activo para este emisor, reutilizarlo
        // en vez de acumular peticiones duplicadas si el usuario presiona varias veces.
        var jobActivo = await _context.LecturaCorreoJobs
            .Where(j => j.EmisorId == emisorId
                && (j.Estado == EstadoLecturaCorreoJob.ENCOLADO
                    || j.Estado == EstadoLecturaCorreoJob.EN_PROGRESO))
            .OrderByDescending(j => j.FechaCreacion)
            .FirstOrDefaultAsync(ct);

        if (jobActivo != null)
        {
            return new EncolarLecturaCorreoResponseDto
            {
                JobId = jobActivo.Id,
                Estado = jobActivo.Estado.ToString(),
                Mensaje = "Ya hay una lectura en curso para este emisor; reutilizando."
            };
        }

        var nuevo = new LecturaCorreoJob
        {
            EmisorId = emisorId,
            Estado = EstadoLecturaCorreoJob.ENCOLADO,
            IniciadoPorUsuarioId = usuarioId,
            FechaCreacion = DateTime.UtcNow,
            RangoDesde = desde,
            RangoHasta = hasta,
            EsAutomatico = esAutomatico
        };
        _context.LecturaCorreoJobs.Add(nuevo);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "[EmailReaderService] Lectura encolada para emisor {EmisorId}: job {JobId} (rango {Desde:yyyy-MM-dd}..{Hasta:yyyy-MM-dd}, automatico={Automatico})",
            emisorId, nuevo.Id, desde, hasta, esAutomatico);

        return new EncolarLecturaCorreoResponseDto
        {
            JobId = nuevo.Id,
            Estado = nuevo.Estado.ToString(),
            Mensaje = "Lectura encolada. El procesado iniciará en segundos."
        };
    }

    /// <summary>
    /// Si el caller omite el rango, devolvemos [primer día del mes en curso UTC,
    /// primer día del mes siguiente UTC). Si pasa solo uno, se completa con el
    /// otro extremo del mismo mes. <c>rangoHasta</c> es exclusivo.
    /// </summary>
    private static (DateTime desde, DateTime hasta) ResolverRangoUtc(
        DateTime? rangoDesde, DateTime? rangoHasta)
    {
        if (rangoDesde.HasValue && rangoHasta.HasValue)
            return (DateTime.SpecifyKind(rangoDesde.Value, DateTimeKind.Utc),
                    DateTime.SpecifyKind(rangoHasta.Value, DateTimeKind.Utc));

        var hoy = DateTime.UtcNow;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var inicioMesSiguiente = inicioMes.AddMonths(1);

        return (
            rangoDesde.HasValue
                ? DateTime.SpecifyKind(rangoDesde.Value, DateTimeKind.Utc)
                : inicioMes,
            rangoHasta.HasValue
                ? DateTime.SpecifyKind(rangoHasta.Value, DateTimeKind.Utc)
                : inicioMesSiguiente);
    }

    public async Task<EstadoLecturaCorreoJobDto?> ObtenerEstadoLecturaAsync(
        int emisorId, CancellationToken ct = default)
    {
        var job = await _context.LecturaCorreoJobs
            .AsNoTracking()
            .Where(j => j.EmisorId == emisorId)
            .OrderByDescending(j => j.FechaCreacion)
            .FirstOrDefaultAsync(ct);

        if (job == null) return null;

        return new EstadoLecturaCorreoJobDto
        {
            JobId = job.Id,
            Estado = job.Estado.ToString(),
            FechaCreacion = job.FechaCreacion,
            FechaInicio = job.FechaInicio,
            FechaFin = job.FechaFin,
            CorreosProcesados = job.CorreosProcesados,
            DtesEncontrados = job.DtesEncontrados,
            DtesNuevos = job.DtesNuevos,
            DtesDuplicados = job.DtesDuplicados,
            DtesIgnorados = job.DtesIgnorados,
            Errores = job.Errores,
            MensajeError = job.MensajeError,
            RangoDesde = job.RangoDesde,
            RangoHasta = job.RangoHasta,
            EsAutomatico = job.EsAutomatico,
            Terminado = job.Estado == EstadoLecturaCorreoJob.COMPLETADO
                || job.Estado == EstadoLecturaCorreoJob.FALLIDO
        };
    }

    public async Task ConfigurarLecturaCorreoAsync(int emisorId, ConfiguracionLecturaCorreoDto dto)
    {
        var emisor = await _context.Emisores.FindAsync(emisorId)
            ?? throw new KeyNotFoundException($"Emisor con ID {emisorId} no encontrado");

        emisor.LecturaCorreoHabilitada = dto.LecturaCorreoHabilitada;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Lectura de correo {Estado} para emisor {EmisorId}",
            dto.LecturaCorreoHabilitada ? "habilitada" : "deshabilitada", emisorId);
    }

    public async Task<(bool exitoso, string mensaje)> ProbarConexionGmailAsync(int emisorId)
    {
        var emisor = await _context.Emisores.FindAsync(emisorId)
            ?? throw new KeyNotFoundException($"Emisor con ID {emisorId} no encontrado");

        if (!emisor.GmailConectado || string.IsNullOrEmpty(emisor.GmailRefreshToken))
            return (false, "Gmail OAuth2 no está configurado. Conecte su cuenta de Gmail primero.");

        try
        {
            var decryptedToken = _encryptionService.Decrypt(emisor.GmailRefreshToken);
            var tokenResponse = new TokenResponse { RefreshToken = decryptedToken };
            var credential = new UserCredential(_flow, "user", tokenResponse);
            var gmailService = new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Smartix"
            });

            var profile = await gmailService.Users.GetProfile("me").ExecuteAsync();

            return (true, $"Conexión exitosa. Cuenta: {profile.EmailAddress}, Mensajes totales: {profile.MessagesTotal}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error probando conexión Gmail para emisor {EmisorId}", emisorId);
            return (false, $"Error de conexión: {ex.Message}");
        }
    }
}
