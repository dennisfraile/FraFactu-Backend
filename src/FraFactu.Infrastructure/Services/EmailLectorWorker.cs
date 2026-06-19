using System.Text;
using FraFactu.Application.Common.Settings;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Http;
using FraFactu.Infrastructure.Persistence;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace FraFactu.Infrastructure.Services;

/// <summary>
/// F2: ejecuta una lectura de Gmail en background sobre un
/// <see cref="Domain.Entities.LecturaCorreoJob"/> ya iniciado. Pagina la
/// bandeja con <c>pageToken</c> (sin el tope de 100 mensajes que tenía la
/// versión síncrona) y actualiza los contadores del job cada N mensajes
/// para que la UI los vea en vivo.
/// </summary>
public class EmailLectorWorker : IEmailLectorWorker
{
    private const int LoteParaPersistirContadores = 10;

    private readonly ApplicationDbContext _context;
    private readonly IDteIngestaService _ingesta;
    private readonly IEncryptionService _encryptionService;
    private readonly GoogleAuthSettings _googleSettings;
    private readonly ILogger<EmailLectorWorker> _logger;
    private readonly GoogleAuthorizationCodeFlow _flow;
    private readonly IAsyncPolicy _gmailRetry = HttpPolicies.GmailRetry();

    public EmailLectorWorker(
        ApplicationDbContext context,
        IDteIngestaService ingesta,
        IEncryptionService encryptionService,
        IOptions<GoogleAuthSettings> googleSettings,
        ILogger<EmailLectorWorker> logger)
    {
        _context = context;
        _ingesta = ingesta;
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

    public async Task EjecutarAsync(int jobId, CancellationToken ct = default)
    {
        var job = await _context.LecturaCorreoJobs
            .Include(j => j.Emisor)
            .FirstOrDefaultAsync(j => j.Id == jobId, ct)
            ?? throw new InvalidOperationException($"Job {jobId} no encontrado.");

        var emisor = job.Emisor
            ?? throw new InvalidOperationException($"Emisor {job.EmisorId} no encontrado.");

        if (!emisor.LecturaCorreoHabilitada)
            throw new InvalidOperationException("La lectura de correo no está habilitada para este emisor.");

        if (!emisor.GmailConectado || string.IsNullOrEmpty(emisor.GmailRefreshToken))
            throw new InvalidOperationException("Gmail OAuth2 no está configurado. Conecte su cuenta de Gmail primero.");

        var gmailService = CreateGmailService(emisor.GmailRefreshToken);

        // F3: el rango lo decide quien encola (default = mes en curso). Jobs
        // anteriores a F3 no tienen rango: por compatibilidad caemos al mes actual.
        var (desde, hasta) = ResolverRango(job);

        var query = $"in:inbox has:attachment filename:json " +
                    $"after:{desde:yyyy/MM/dd} before:{hasta:yyyy/MM/dd}";

        string? pageToken = null;
        var procesadosDesdeUltimaPersistencia = 0;

        try
        {
            do
            {
                ct.ThrowIfCancellationRequested();

                var listRequest = gmailService.Users.Messages.List("me");
                listRequest.Q = query;
                listRequest.MaxResults = 100;
                listRequest.PageToken = pageToken;

                // F4: la SDK de Google no integra Polly automaticamente; envolvemos
                // la llamada para reintentar fallos transitorios (5xx/429/red).
                var messageList = await _gmailRetry.ExecuteAsync(
                    token => listRequest.ExecuteAsync(token), ct);
                pageToken = messageList.NextPageToken;

                if (messageList.Messages == null || messageList.Messages.Count == 0)
                    break;

                foreach (var messageRef in messageList.Messages)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        var message = await _gmailRetry.ExecuteAsync(
                            token => gmailService.Users.Messages.Get("me", messageRef.Id).ExecuteAsync(token), ct);
                        await ProcesarMensajeAsync(gmailService, message, emisor.Id, emisor.Nit, job, ct);
                    }
                    catch (TokenResponseException)
                    {
                        // OAuth revocado/expirado: hay que abortar el job entero
                        // y desconectar Gmail. Lo maneja el catch externo.
                        throw;
                    }
                    catch (Exception ex)
                    {
                        job.Errores++;
                        _logger.LogWarning(ex,
                            "[EmailLectorWorker] Error procesando correo {MessageId} (job {JobId})",
                            messageRef.Id, jobId);
                    }

                    job.CorreosProcesados++;
                    procesadosDesdeUltimaPersistencia++;

                    // Persistir contadores periódicamente para que la UI los vea avanzar.
                    if (procesadosDesdeUltimaPersistencia >= LoteParaPersistirContadores)
                    {
                        await _context.SaveChangesAsync(ct);
                        procesadosDesdeUltimaPersistencia = 0;
                    }
                }
            }
            while (!string.IsNullOrEmpty(pageToken));
        }
        catch (TokenResponseException ex) when (
            string.Equals(ex.Error?.Error, "invalid_grant", StringComparison.OrdinalIgnoreCase))
        {
            // Refresh token revocado por el usuario o caducado (>6 meses sin uso).
            // Desconectar Gmail para que el scheduler ya no intente este emisor
            // hasta que reconecten; el job termina FALLIDO con mensaje claro.
            emisor.GmailConectado = false;
            await _context.SaveChangesAsync(CancellationToken.None);

            _logger.LogWarning(ex,
                "[EmailLectorWorker] OAuth invalid_grant para emisor {EmisorId}; GmailConectado=false",
                emisor.Id);

            throw new InvalidOperationException(
                "La conexión a Gmail expiró o fue revocada. Reconecte la cuenta de Gmail desde Configuración.",
                ex);
        }

        // Marcar la lectura como finalizada en el emisor y persistir el remanente.
        // F3: UltimaLecturaCorreo ya no controla el filtro Gmail (el rango va en
        // el job); se conserva como información histórica y para el cooldown del
        // scheduler automático.
        emisor.UltimaLecturaCorreo = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "[EmailLectorWorker] Job {JobId} terminado: {Correos} correos, {Nuevos} DTEs nuevos, {Duplicados} duplicados, {Errores} errores",
            jobId, job.CorreosProcesados, job.DtesNuevos, job.DtesDuplicados, job.Errores);
    }

    /// <summary>
    /// Devuelve el rango a leer. Si el job no tiene rango (job pre-F3 o flujo
    /// que omitió el cálculo), default = mes en curso.
    /// </summary>
    internal static (DateTime desde, DateTime hasta) ResolverRango(Domain.Entities.LecturaCorreoJob job)
    {
        if (job.RangoDesde.HasValue && job.RangoHasta.HasValue)
            return (job.RangoDesde.Value, job.RangoHasta.Value);

        var hoy = DateTime.UtcNow;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return (inicioMes, inicioMes.AddMonths(1));
    }

    /// <summary>
    /// Suma 1 al contador del job que corresponda al resultado de la ingesta.
    /// Cargado/Duplicado cuentan como "DTE encontrado"; NoEsCCF, ReceptorInvalido
    /// y ContenidoInvalido caen en DtesIgnorados (visibles en UI pero no rojos).
    /// </summary>
    internal static void MapearResultadoIngesta(
        ResultadoIngestaDte resultado, Domain.Entities.LecturaCorreoJob job)
    {
        switch (resultado)
        {
            case ResultadoIngestaDte.Cargado:
                job.DtesEncontrados++;
                job.DtesNuevos++;
                break;
            case ResultadoIngestaDte.Duplicado:
                job.DtesEncontrados++;
                job.DtesDuplicados++;
                break;
            case ResultadoIngestaDte.NoEsCCF:
            case ResultadoIngestaDte.ReceptorInvalido:
            case ResultadoIngestaDte.ContenidoInvalido:
                job.DtesIgnorados++;
                break;
        }
    }

    private async Task ProcesarMensajeAsync(
        GmailService gmailService,
        Message message,
        int emisorId,
        string emisorNit,
        Domain.Entities.LecturaCorreoJob job,
        CancellationToken ct)
    {
        if (message.Payload?.Parts == null) return;

        var emailFrom = ObtenerEmailRemitente(message);
        var fechaRecepcion = ObtenerFechaMensaje(message);

        foreach (var part in message.Payload.Parts)
        {
            if (part.Filename == null || !part.Filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                continue;
            if (string.IsNullOrEmpty(part.Body?.AttachmentId))
                continue;

            var attachment = await _gmailRetry.ExecuteAsync(
                token => gmailService.Users.Messages.Attachments
                    .Get("me", message.Id, part.Body.AttachmentId)
                    .ExecuteAsync(token), ct);

            var data = Convert.FromBase64String(attachment.Data.Replace('-', '+').Replace('_', '/'));
            var jsonContent = Encoding.UTF8.GetString(data);

            var ingesta = await _ingesta.IngestarAsync(jsonContent, new IngestaContexto
            {
                EmisorId = emisorId,
                EmisorNit = emisorNit,
                Fuente = FuenteRecepcionDte.CORREO,
                EmailOrigen = emailFrom,
                FechaRecepcionEmail = fechaRecepcion
            }, ct);

            // F4: NoEsCCF/ReceptorInvalido/ContenidoInvalido suman a DtesIgnorados
            // para que la UI los vea (antes se descartaban en silencio).
            MapearResultadoIngesta(ingesta.Resultado, job);
        }
    }

    private static string? ObtenerEmailRemitente(Message message)
    {
        var fromHeader = message.Payload?.Headers?
            .FirstOrDefault(h => h.Name.Equals("From", StringComparison.OrdinalIgnoreCase));
        return fromHeader?.Value;
    }

    private static DateTime? ObtenerFechaMensaje(Message message)
    {
        if (message.InternalDate.HasValue)
            return DateTimeOffset.FromUnixTimeMilliseconds(message.InternalDate.Value).UtcDateTime;
        return null;
    }

    private GmailService CreateGmailService(string refreshTokenEncrypted)
    {
        var decryptedToken = _encryptionService.Decrypt(refreshTokenEncrypted);
        var tokenResponse = new TokenResponse { RefreshToken = decryptedToken };
        var credential = new UserCredential(_flow, "user", tokenResponse);

        return new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Smartix"
        });
    }
}
