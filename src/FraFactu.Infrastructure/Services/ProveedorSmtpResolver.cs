using DnsClient;
using FraFactu.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Services
{
    /// <summary>
    /// Implementación de <see cref="IProveedorSmtpResolver"/> basada en la
    /// resolución de registros MX (DnsClient). Permite autoconfigurar el SMTP
    /// de dominios corporativos alojados en Google Workspace / Microsoft 365 /
    /// Yahoo / Zoho, cuyo nombre de dominio no coincide con el del proveedor.
    /// </summary>
    public class ProveedorSmtpResolver : IProveedorSmtpResolver
    {
        private readonly ILookupClient _lookup;
        private readonly ILogger<ProveedorSmtpResolver> _logger;

        public ProveedorSmtpResolver(ILogger<ProveedorSmtpResolver> logger)
            : this(new LookupClient(new LookupClientOptions
            {
                // El MX se consulta solo para dominios no reconocidos al guardar
                // el emisor; mantenemos un timeout corto para no penalizar el guardado.
                Timeout = TimeSpan.FromSeconds(3),
                UseCache = true,
                Retries = 1
            }), logger)
        {
        }

        // Constructor para tests (LookupClient implementa ILookupClient).
        internal ProveedorSmtpResolver(ILookupClient lookup, ILogger<ProveedorSmtpResolver> logger)
        {
            _lookup = lookup;
            _logger = logger;
        }

        public async Task<(string host, int port)?> ResolverPorMxAsync(string dominio, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dominio))
                return null;

            try
            {
                var resultado = await _lookup.QueryAsync(dominio, QueryType.MX, cancellationToken: ct);

                var mxHosts = resultado.Answers
                    .MxRecords()
                    .Select(r => r.Exchange.Value.TrimEnd('.').ToLowerInvariant())
                    .ToList();

                if (mxHosts.Count == 0)
                {
                    _logger.LogInformation(
                        "[SMTP-MX] El dominio '{Dominio}' no tiene registros MX; no se autoconfigura SMTP", dominio);
                    return null;
                }

                var proveedor = MapearProveedorPorMx(mxHosts);
                if (proveedor is null)
                {
                    _logger.LogInformation(
                        "[SMTP-MX] MX de '{Dominio}' ({Mx}) no corresponde a un proveedor conocido", dominio, string.Join(", ", mxHosts));
                }

                return proveedor;
            }
            catch (Exception ex)
            {
                // Best-effort: nunca romper el guardado del emisor por un fallo de DNS.
                _logger.LogWarning(ex, "[SMTP-MX] Fallo resolviendo MX de '{Dominio}'", dominio);
                return null;
            }
        }

        /// <summary>
        /// Mapea los hosts MX al SMTP saliente del proveedor. Google Workspace y
        /// Microsoft 365 usan el mismo SMTP que sus versiones de consumo.
        /// </summary>
        internal static (string host, int port)? MapearProveedorPorMx(IEnumerable<string> mxHosts)
        {
            foreach (var mx in mxHosts)
            {
                if (mx.EndsWith(".google.com") || mx.EndsWith(".googlemail.com") || mx.EndsWith(".psmtp.com"))
                    return ("smtp.gmail.com", 587);

                if (mx.EndsWith(".outlook.com") || mx.EndsWith(".office365.com") || mx.EndsWith(".protection.outlook.com"))
                    return ("smtp.office365.com", 587);

                if (mx.EndsWith(".yahoodns.net"))
                    return ("smtp.mail.yahoo.com", 587);

                if (mx.EndsWith(".zoho.com") || mx.EndsWith(".zohomail.com"))
                    return ("smtp.zoho.com", 587);
            }

            return null;
        }
    }
}
