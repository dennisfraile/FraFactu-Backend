using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FraFactu.Application.Common.Settings;
using FraFactu.Application.Interfaces;

namespace FraFactu.Infrastructure.Services
{
    public class SupabaseStorageService : ISupabaseStorageService
    {
        private readonly SupabaseSettings _settings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SupabaseStorageService> _logger;
        private const string BucketName = "logos";

        public SupabaseStorageService(
            IOptions<SupabaseSettings> settings,
            IHttpClientFactory httpClientFactory,
            ILogger<SupabaseStorageService> logger)
        {
            _settings = settings.Value;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<string> UploadLogoAsync(Stream fileStream, string fileName, string userId)
        {
            try
            {
                var extension = Path.GetExtension(fileName);
                var uniqueName = $"logo_{DateTime.UtcNow.Ticks}{extension}";
                var filePath = $"{userId}/{uniqueName}";

                _logger.LogInformation("Uploading logo for user {UserId} to Supabase Storage path: {Path}", userId, filePath);

                using var content = new MultipartFormDataContent();
                using var streamContent = new StreamContent(fileStream);

                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream"); // O detectar extensión
                if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase)) streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                else if (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)) streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

                content.Add(streamContent, "file", uniqueName);

                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri($"{_settings.Url}/storage/v1/object/");

                // Use Service Role Key to bypass RLS for backend upload, ensuring it always succeeds
                // if the backend implementation authorizes the request.
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ServiceRoleKey);
                client.DefaultRequestHeaders.Add("x-upsert", "true");

                var response = await client.PostAsync($"{BucketName}/{filePath}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error uploading to Supabase: {StatusCode} - {Error}", response.StatusCode, error);
                    throw new Exception($"Error uploading logo: {response.StatusCode} - {error}");
                }

                _logger.LogInformation("Logo uploaded successfully.");

                // Return Public URL
                var publicUrl = $"{_settings.Url}/storage/v1/object/public/{BucketName}/{filePath}";
                return publicUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in UploadLogoAsync");
                throw;
            }
        }

        public async Task<string> UploadSystemLogoAsync(Stream fileStream, string fileName)
        {
            try
            {
                var extension = Path.GetExtension(fileName);
                // Ruta fija sin extensión para que siempre se sobreescriba correctamente
                // independiente del formato de imagen que suba el usuario
                var filePath = "system/logo";

                _logger.LogInformation("Uploading system logo to Supabase Storage path: {Path}", filePath);

                using var content = new MultipartFormDataContent();
                using var streamContent = new StreamContent(fileStream);

                // Detectar Content-Type real según la extensión del archivo original
                streamContent.Headers.ContentType = extension.ToLowerInvariant() switch
                {
                    ".png" => new MediaTypeHeaderValue("image/png"),
                    ".jpg" or ".jpeg" => new MediaTypeHeaderValue("image/jpeg"),
                    ".svg" => new MediaTypeHeaderValue("image/svg+xml"),
                    ".webp" => new MediaTypeHeaderValue("image/webp"),
                    ".gif" => new MediaTypeHeaderValue("image/gif"),
                    _ => new MediaTypeHeaderValue("application/octet-stream")
                };

                content.Add(streamContent, "file", "logo");

                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri($"{_settings.Url}/storage/v1/object/");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ServiceRoleKey);

                // Usar modo upsert para reemplazar logo existente
                client.DefaultRequestHeaders.Add("x-upsert", "true");

                var response = await client.PostAsync($"{BucketName}/{filePath}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error uploading system logo to Supabase: {StatusCode} - {Error}", response.StatusCode, error);
                    throw new Exception($"Error uploading system logo: {response.ReasonPhrase}");
                }

                _logger.LogInformation("System logo uploaded successfully.");

                // Retornar URL pública (el frontend agrega timestamp para cache-busting)
                var publicUrl = $"{_settings.Url}/storage/v1/object/public/{BucketName}/{filePath}";
                return publicUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in UploadSystemLogoAsync");
                throw;
            }
        }

        public string GetSystemLogoUrl()
        {
            // Retorna la URL base del logo del sistema
            return $"{_settings.Url}/storage/v1/object/public/{BucketName}/system/logo";
        }
    }
}
