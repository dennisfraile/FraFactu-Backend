using System.Net.Http.Json;
using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using FraFactu.Application.Common.Settings;
using FraFactu.Application.Interfaces;
using FraFactu.Infrastructure.Http;
using Polly;

namespace FraFactu.Infrastructure.Services;

public class GmailApiService : IGmailApiService
{
    private readonly GoogleAuthSettings _googleSettings;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<GmailApiService> _logger;
    private readonly GoogleAuthorizationCodeFlow _flow;
    private readonly IAsyncPolicy _gmailRetry = HttpPolicies.GmailRetry();

    public GmailApiService(
        IOptions<GoogleAuthSettings> googleSettings,
        IEncryptionService encryptionService,
        ILogger<GmailApiService> logger)
    {
        _googleSettings = googleSettings.Value;
        _encryptionService = encryptionService;
        _logger = logger;

        _flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = _googleSettings.ClientId,
                ClientSecret = _googleSettings.ClientSecret
            },
            Scopes = new[] { GmailService.Scope.GmailSend, GmailService.Scope.GmailReadonly, "email" }
        });
    }

    public string GenerateAuthorizationUrl(string encryptedState)
    {
        var authUri = _flow.CreateAuthorizationCodeRequest(_googleSettings.GmailRedirectUri);
        authUri.State = encryptedState;

        // Forzar selección de cuenta y consent para obtener refresh token
        var uriBuilder = new UriBuilder(authUri.Build());
        var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
        query["access_type"] = "offline";
        query["prompt"] = "consent";
        uriBuilder.Query = query.ToString();

        return uriBuilder.ToString();
    }

    public async Task<(string refreshToken, string email)> ExchangeCodeForTokensAsync(string authorizationCode)
    {
        var tokenResponse = await _flow.ExchangeCodeForTokenAsync(
            userId: "user",
            code: authorizationCode,
            redirectUri: _googleSettings.GmailRedirectUri,
            CancellationToken.None);

        if (string.IsNullOrEmpty(tokenResponse.RefreshToken))
        {
            throw new InvalidOperationException(
                "No se recibió refresh token de Google. Asegúrese de que prompt=consent esté configurado.");
        }

        // Obtener email via Google userinfo endpoint (no requiere scope de Gmail)
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);
        var userInfoResponse = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo");
        var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var email = userInfo.GetProperty("email").GetString() ?? throw new InvalidOperationException("No se pudo obtener el email del usuario");

        // Encriptar refresh token antes de retornar
        var encryptedRefreshToken = _encryptionService.Encrypt(tokenResponse.RefreshToken);

        _logger.LogInformation("[GMAIL-API] Token obtenido para {Email}", email);

        return (encryptedRefreshToken, email);
    }

    public async Task EnviarEmailGmailAsync(
        string refreshTokenEncrypted,
        string fromEmail,
        string to,
        string subject,
        string htmlBody,
        List<(byte[] content, string name, string mimeType)>? attachments = null)
    {
        var gmailService = CreateGmailService(refreshTokenEncrypted);

        // Construir mensaje MIME
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress("", fromEmail));
        mimeMessage.To.Add(new MailboxAddress("", to));
        mimeMessage.Subject = subject;

        if (attachments != null && attachments.Count > 0)
        {
            var multipart = new Multipart("mixed");

            // Cuerpo HTML
            var htmlPart = new TextPart("html") { Text = htmlBody };
            multipart.Add(htmlPart);

            // Adjuntos
            foreach (var (content, name, mimeType) in attachments)
            {
                var parts = mimeType.Split('/');
                var attachment = new MimePart(parts[0], parts.Length > 1 ? parts[1] : "octet-stream")
                {
                    Content = new MimeContent(new MemoryStream(content)),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = name
                };
                multipart.Add(attachment);
            }

            mimeMessage.Body = multipart;
        }
        else
        {
            mimeMessage.Body = new TextPart("html") { Text = htmlBody };
        }

        // Convertir a raw base64url
        using var memoryStream = new MemoryStream();
        await mimeMessage.WriteToAsync(memoryStream);
        var rawMessage = Convert.ToBase64String(memoryStream.ToArray())
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        var gmailMessage = new Message { Raw = rawMessage };
        // F4: reintentos automaticos en errores transitorios de Gmail (5xx/429/red).
        await _gmailRetry.ExecuteAsync(
            () => gmailService.Users.Messages.Send(gmailMessage, "me").ExecuteAsync());

        _logger.LogInformation("[GMAIL-API] Correo enviado a {To} desde {From}", to, fromEmail);
    }

    public async Task<bool> ValidateRefreshTokenAsync(string refreshTokenEncrypted)
    {
        try
        {
            var decryptedToken = _encryptionService.Decrypt(refreshTokenEncrypted);
            var tokenResponse = new TokenResponse { RefreshToken = decryptedToken };
            var credential = new UserCredential(_flow, "user", tokenResponse);

            // Intentar refrescar el token
            await credential.RefreshTokenAsync(CancellationToken.None);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[GMAIL-API] Refresh token inválido");
            return false;
        }
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
