using FraFactu.Application.DTOs.Common;
using FraFactu.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace FraFactu.Infrastructure.Services
{
    public class ContingenciaDiagnosticoService : IContingenciaDiagnosticoService
    {
        private readonly ILogger<ContingenciaDiagnosticoService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public ContingenciaDiagnosticoService(
            ILogger<ContingenciaDiagnosticoService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<DiagnosticoConectividad> DiagnosticarFalloAsync(Exception ex)
        {
            _logger.LogInformation("Iniciando diagnóstico de conectividad por error: {Mensaje}", ex.Message);

            // Caso 1: MH respondió con error HTTP (500, 503, etc.)
            // -> Definitivamente tipo 1, certeza = true
            if (ex is HttpRequestException httpEx && httpEx.StatusCode.HasValue)
            {
                return new DiagnosticoConectividad
                {
                    TipoContingenciaSugerido = 1,
                    EsCerteza = true,
                    Razon = $"Hacienda respondió con error HTTP {(int)httpEx.StatusCode}",
                    DetalleError = ex.Message
                };
            }

            // Caso 2: Error de red (timeout, DNS, conexión rechazada)
            // -> Necesitamos distinguir: ¿MH caído o internet caído?
            if (ex is HttpRequestException || ex is TaskCanceledException || ex is SocketException)
            {
                try
                {
                    // Intentar alcanzar un servicio externo confiable
                    // Usamos cliente "named" o default. Si no hay named, usa CreateClient().
                    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

                    // Google DNS check (8.8.8.8 port 53) es más rápido pero HTTP es más fácil de implementar sin sockets raw
                    // Usamos google.com o cloudflare.com
                    var response = await client.GetAsync("https://www.google.com");

                    // Si llegamos aquí: internet funciona, MH no -> tipo 1 (MH Caído)
                    return new DiagnosticoConectividad
                    {
                        TipoContingenciaSugerido = 1,
                        EsCerteza = true,
                        Razon = "Internet funciona pero Hacienda no responde (Connection Refused/Timeout)",
                        DetalleError = ex.Message
                    };
                }
                catch
                {
                    // Google tampoco responde -> probablemente internet caído -> tipo 3
                    return new DiagnosticoConectividad
                    {
                        TipoContingenciaSugerido = 3,
                        EsCerteza = false, // No 100% seguro, podría ser firewall local, pero es alta prob.
                        Razon = "No se pudo alcanzar servicios externos. Probable falla de internet",
                        DetalleError = ex.Message
                    };
                }
            }

            // Caso 3: Cualquier otra excepción -> no podemos determinar
            return new DiagnosticoConectividad
            {
                TipoContingenciaSugerido = null,
                EsCerteza = false,
                Razon = "Error no clasificado (Lógica o Datos)",
                DetalleError = ex.Message
            };
        }
    }
}
