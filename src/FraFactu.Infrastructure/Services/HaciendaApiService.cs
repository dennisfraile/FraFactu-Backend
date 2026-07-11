using System.Net.Http.Json;
using System.Text.Json;
using FraFactu.Application.DTOs.Hacienda;
using FraFactu.Application.Interfaces;
using FraFactu.Application.Interfaces.Hacienda;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Services
{
    public class HaciendaApiService : IHaciendaApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHaciendaAuthService _authService;
        private readonly IDteSignerService _signerService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HaciendaApiService> _logger;
        private readonly IEncryptionService _encryptionService;

        private const string UrlRecepcionPruebas = "https://apitest.dtes.mh.gob.sv/fesv/recepciondte";
        private const string UrlRecepcionProduccion = "https://api.dtes.mh.gob.sv/fesv/recepciondte";
        private static int _idEnvioCounter = 0;

        public HaciendaApiService(
            HttpClient httpClient,
            IHaciendaAuthService authService,
            IDteSignerService signerService,
            ApplicationDbContext context,
            ILogger<HaciendaApiService> logger,
            IEncryptionService encryptionService)
        {
            _httpClient = httpClient;
            _authService = authService;
            _signerService = signerService;
            _context = context;
            _logger = logger;
            _encryptionService = encryptionService;
        }

        private string DecryptField(string? value, string fieldName, bool optional = false)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (optional) return string.Empty;
                throw new InvalidOperationException(
                    $"El campo '{fieldName}' del emisor está vacío o no configurado. " +
                    "Configure las credenciales de Hacienda en la sección del emisor.");
            }

            try
            {
                return _encryptionService.Decrypt(value);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(
                    $"Error al desencriptar '{fieldName}' del emisor. " +
                    "Verifique que la Encryption:MasterKey en Azure App Settings coincida con la que se usó al guardar las credenciales.", ex);
            }
        }

        private (string llavePrivada, string passPrivada) ObtenerCredencialesFirma(Emisor emisor)
        {
            var esProd = emisor.CatAmbienteDestinoId == 2;
            var llavePrivadaEnc = esProd ? emisor.MhLlavePrivadaProd : emisor.MhLlavePrivada;
            var passPrivadaEnc = esProd ? emisor.MhPassPrivadaProd : emisor.MhPassPrivada;

            var llavePrivada = DecryptField(llavePrivadaEnc, esProd ? "MhLlavePrivadaProd" : "MhLlavePrivada");
            var passPrivada = DecryptField(passPrivadaEnc, esProd ? "MhPassPrivadaProd" : "MhPassPrivada", optional: true);

            return (llavePrivada, passPrivada);
        }

        public async Task<RecepcionResponseDto> TransmitirDteAsync(int emisorId, DteBaseDto dte)
        {
            // 1. Obtener Emisor (para llaves y ambiente)
            var emisor = await _context.Emisores
                .Include(e => e.AmbienteDestino)
                .FirstOrDefaultAsync(e => e.Id == emisorId);
            if (emisor == null) throw new Exception("Emisor no encontrado");

            _logger.LogInformation("=== INICIO TRANSMISIÓN DTE ===");
            _logger.LogInformation("[HACIENDA-DEBUG] Emisor ID: {EmisorId}, NIT: {Nit}", emisorId, emisor.Nit);
            _logger.LogInformation("[HACIENDA-DEBUG] Ambiente Destino ID: {AmbienteId}, Codigo: {Codigo}",
                emisor.CatAmbienteDestinoId, emisor.AmbienteDestino?.Codigo);

            // 2. Obtener Token de Auth (Bearer)
            var token = await _authService.ObtenerTokenAsync(emisorId);

            // Log detallado del token
            _logger.LogInformation("[HACIENDA-DEBUG] Token obtenido exitosamente");
            _logger.LogInformation("[HACIENDA-DEBUG] Token (longitud total: {Length} chars)", token.Length);
            _logger.LogInformation("[HACIENDA-DEBUG] Token (primeros 30 chars): '{TokenPrefix}...'",
                token.Length > 30 ? token.Substring(0, 30) : token);
            _logger.LogInformation("[HACIENDA-DEBUG] Token (últimos 30 chars): '...{TokenSuffix}'",
                token.Length > 30 ? token.Substring(token.Length - 30) : token);

            // Verificar que el token no contenga espacios o caracteres extraños
            if (token.Contains(" ") || token.Contains("\n") || token.Contains("\r"))
            {
                _logger.LogWarning("[HACIENDA-DEBUG] ADVERTENCIA: El token contiene espacios en blanco o saltos de línea!");
            }

            // 3. Serializar DTE a JSON
            var jsonDte = JsonSerializer.Serialize(dte);
            _logger.LogInformation("[HACIENDA-DEBUG] DTE JSON sin firmar (longitud: {Length} bytes)", jsonDte.Length);

            // 4. Firmar DTE
            var (llavePrivada, passPrivada) = ObtenerCredencialesFirma(emisor);
            var documentoFirmado = _signerService.FirmarDocumento(jsonDte, llavePrivada, passPrivada);
            _logger.LogInformation("[HACIENDA-DEBUG] Documento firmado (longitud: {Length} chars)", documentoFirmado.Length);

            // 5. Construir Payload de envío según especificaciones oficiales MH
            var idEnvio = Guid.NewGuid().ToString().ToUpperInvariant();
            var payloadEnvio = new
            {
                ambiente = emisor.AmbienteDestino?.Codigo ?? string.Empty,
                idEnvio = idEnvio,
                version = dte.Identificacion.Version,
                tipoDte = dte.Identificacion.TipoDte,
                documento = documentoFirmado,
                codigoGeneracion = dte.Identificacion.CodigoGeneracion
            };

            _logger.LogInformation("[HACIENDA-DEBUG] Payload - ambiente: {Ambiente}", payloadEnvio.ambiente);
            _logger.LogInformation("[HACIENDA-DEBUG] Payload - idEnvio: {IdEnvio}", payloadEnvio.idEnvio);
            _logger.LogInformation("[HACIENDA-DEBUG] Payload - version: {Version}", payloadEnvio.version);
            _logger.LogInformation("[HACIENDA-DEBUG] Payload - tipoDte: {TipoDte}", payloadEnvio.tipoDte);
            _logger.LogInformation("[HACIENDA-DEBUG] Payload - codigoGeneracion: {CodigoGeneracion}", payloadEnvio.codigoGeneracion);

            // 6. Configurar Headers
            var url = emisor.CatAmbienteDestinoId == 1 ? UrlRecepcionPruebas : UrlRecepcionProduccion;
            _logger.LogInformation("[HACIENDA-DEBUG] URL destino: {Url}", url);

            // Limpiar headers y configurar correctamente
            _httpClient.DefaultRequestHeaders.Clear();

            // Configurar Authorization header con Bearer token
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Agregar User-Agent
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Facturacion-Backend-v1");

            // Agregar Accept header explícitamente
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            // Log de todos los headers que se enviarán
            _logger.LogInformation("=== REQUEST HEADERS ===");
            foreach (var header in _httpClient.DefaultRequestHeaders)
            {
                var headerValue = string.Join(", ", header.Value);
                // No mostrar el token completo por seguridad, solo los primeros caracteres
                if (header.Key == "Authorization")
                {
                    var authValue = headerValue.Length > 50 ? headerValue.Substring(0, 50) + "..." : headerValue;
                    _logger.LogInformation("[HEADER] {Key}: {Value}", header.Key, authValue);
                }
                else
                {
                    _logger.LogInformation("[HEADER] {Key}: {Value}", header.Key, headerValue);
                }
            }

            // 7. Enviar request con HttpRequestMessage para control total de headers y versión HTTP
            var payloadJson = JsonSerializer.Serialize(payloadEnvio);
            _logger.LogInformation("=== PAYLOAD COMPLETO A HACIENDA ===");
            _logger.LogInformation("[HACIENDA-PAYLOAD] {Payload}", payloadJson);
            _logger.LogInformation("=== ENVIANDO REQUEST A HACIENDA ===");

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
            requestMessage.Version = new Version(1, 1); // Forzar HTTP/1.1
            var content = new StringContent(payloadJson, System.Text.Encoding.UTF8);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"); // Sin charset
            requestMessage.Content = content;

            // Copiar headers de autenticación al request message
            if (_httpClient.DefaultRequestHeaders.Authorization != null)
            {
                requestMessage.Headers.Authorization = _httpClient.DefaultRequestHeaders.Authorization;
            }
            requestMessage.Headers.Add("User-Agent", "Facturacion-Backend-v1");
            requestMessage.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            _logger.LogInformation("[HACIENDA-DEBUG] Content-Type: {ContentType}", content.Headers.ContentType);
            _logger.LogInformation("[HACIENDA-DEBUG] Content-Length: {ContentLength}", content.Headers.ContentLength);
            _logger.LogInformation("[HACIENDA-DEBUG] HTTP Version: {Version}", requestMessage.Version);

            var response = await _httpClient.SendAsync(requestMessage);

            // Log de respuesta completa
            _logger.LogInformation("=== RESPONSE DE HACIENDA ===");
            _logger.LogInformation("[HACIENDA-DEBUG] HTTP Status: {StatusCode} ({StatusCodeNumber})",
                response.ReasonPhrase, (int)response.StatusCode);

            // Log de response headers
            _logger.LogInformation("=== RESPONSE HEADERS ===");
            foreach (var header in response.Headers)
            {
                _logger.LogInformation("[RESPONSE-HEADER] {Key}: {Value}",
                    header.Key, string.Join(", ", header.Value));
            }

            // Log de content headers
            foreach (var header in response.Content.Headers)
            {
                _logger.LogInformation("[CONTENT-HEADER] {Key}: {Value}",
                    header.Key, string.Join(", ", header.Value));
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("[HACIENDA-DEBUG] Response Body:\n{ResponseBody}", responseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("=== ERROR EN TRANSMISIÓN ===");
                _logger.LogError("[ERROR] HTTP Status: {StatusCode}", (int)response.StatusCode);
                _logger.LogError("[ERROR] Reason: {ReasonPhrase}", response.ReasonPhrase);
                _logger.LogError("[ERROR] Response Body: {ErrorContent}", responseBody);

                throw new Exception($"Error enviando DTE: HTTP {(int)response.StatusCode} - {response.ReasonPhrase}. Detalle: {responseBody}");
            }

            // 8. Procesar respuesta exitosa
            var resultado = await response.Content.ReadFromJsonAsync<RecepcionResponseDto>();

            // Devolver el documento firmado para guardarlo en BD
            if (resultado != null)
            {
                resultado.DocumentoFirmado = documentoFirmado;
            }

            _logger.LogInformation("=== RESPUESTA PROCESADA DE HACIENDA ===");

            if (resultado != null)
            {
                _logger.LogInformation("DATOS DE LA RESPUESTA:");
                _logger.LogInformation("   ├─ Version: {Version}", resultado.Version);
                _logger.LogInformation("   ├─ Ambiente: {Ambiente}", resultado.Ambiente);
                _logger.LogInformation("   ├─ VersionApp: {VersionApp}", resultado.VersionApp);
                _logger.LogInformation("   ├─ Estado: {Estado}", resultado.Estado);
                _logger.LogInformation("   ├─ CodigoGeneracion: {CodigoGeneracion}", resultado.CodigoGeneracion);
                _logger.LogInformation("   ├─ SelloRecibido: {SelloRecibido}", resultado.SelloRecibido);
                _logger.LogInformation("   ├─ FhProcesamiento: {FhProcesamiento}", resultado.FhProcesamiento);
                _logger.LogInformation("   ├─ ClasificaMsg: {ClasificaMsg}", resultado.ClasificaMsg);
                _logger.LogInformation("   ├─ CodigoMsg: {CodigoMsg}", resultado.CodigoMsg);
                _logger.LogInformation("   └─ DescripcionMsg: {DescripcionMsg}", resultado.DescripcionMsg);

                if (resultado.Observaciones != null && resultado.Observaciones.Any())
                {
                    _logger.LogWarning("OBSERVACIONES DE HACIENDA:");
                    for (int i = 0; i < resultado.Observaciones.Count; i++)
                    {
                        _logger.LogWarning("   [{Index}] {Observacion}", i + 1, resultado.Observaciones[i]);
                    }
                }
                else
                {
                    _logger.LogInformation("Sin observaciones");
                }

                // Log adicional según el estado
                if (resultado.Estado == "PROCESADO")
                {
                    _logger.LogInformation("FACTURA PROCESADA EXITOSAMENTE");
                }
                else if (resultado.Estado == "RECHAZADO")
                {
                    _logger.LogError("FACTURA RECHAZADA POR HACIENDA");
                    _logger.LogError("Código: {CodigoMsg} - {DescripcionMsg}",
                        resultado.CodigoMsg, resultado.DescripcionMsg);
                }
                else
                {
                    _logger.LogWarning("Estado desconocido: {Estado}", resultado.Estado);
                }
            }
            else
            {
                _logger.LogError("No se pudo deserializar la respuesta de Hacienda");
            }

            _logger.LogInformation("=== FIN TRANSMISIÓN DTE ===");

            return resultado ?? new RecepcionResponseDto { Estado = "ERROR_DESERIALIZACION" };
        }

        public async Task<RecepcionResponseDto> TransmitirDteJsonAsync(int emisorId, string jsonDte, int version, string tipoDte, string codigoGeneracion)
        {
            // 1. Obtener Emisor (para llaves y ambiente)
            var emisor = await _context.Emisores
                .Include(e => e.AmbienteDestino)
                .FirstOrDefaultAsync(e => e.Id == emisorId);
            if (emisor == null) throw new Exception("Emisor no encontrado");

            _logger.LogInformation("=== INICIO TRANSMISIÓN DTE (JSON directo) ===");
            _logger.LogInformation("[HACIENDA-DEBUG] Emisor ID: {EmisorId}, NIT: {Nit}", emisorId, emisor.Nit);

            // 2. Obtener Token de Auth (Bearer)
            var token = await _authService.ObtenerTokenAsync(emisorId);
            _logger.LogInformation("[HACIENDA-DEBUG] Token obtenido exitosamente (longitud: {Length})", token.Length);

            // 3. Usar el JSON pre-construido directamente (sin round-trip por DTOs)
            _logger.LogInformation("[HACIENDA-DEBUG] DTE JSON sin firmar:\n{JsonDte}", jsonDte);

            // 4. Firmar DTE
            var (llavePrivada, passPrivada) = ObtenerCredencialesFirma(emisor);
            var documentoFirmado = _signerService.FirmarDocumento(jsonDte, llavePrivada, passPrivada);
            _logger.LogInformation("[HACIENDA-DEBUG] Documento firmado (longitud: {Length} chars)", documentoFirmado.Length);

            // 5. Construir Payload de envío según Manual Técnico MH 2023 (Sección 4.2.1, pág. 21)
            var idEnvio = Interlocked.Increment(ref _idEnvioCounter);
            _logger.LogInformation("[HACIENDA-DEBUG] Version para envelope: {Version}, idEnvio: {IdEnvio}, codigoGeneracion: {CodigoGen}",
                version, idEnvio, codigoGeneracion);
            var payloadEnvio = new
            {
                ambiente = emisor.AmbienteDestino?.Codigo ?? string.Empty,
                idEnvio = idEnvio,
                version = version,
                tipoDte = tipoDte,
                documento = documentoFirmado,
                codigoGeneracion = codigoGeneracion
            };

            var payloadParaLog = new { payloadEnvio.ambiente, payloadEnvio.idEnvio, payloadEnvio.version, payloadEnvio.tipoDte, payloadEnvio.codigoGeneracion, documentoLength = documentoFirmado.Length };
            _logger.LogInformation("[HACIENDA-DEBUG] Payload completo (sin documento): {PayloadJson}",
                JsonSerializer.Serialize(payloadParaLog));

            // 6. Serializar payload manualmente para control total del Content-Type
            var payloadJson = JsonSerializer.Serialize(payloadEnvio, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _logger.LogInformation("[HACIENDA-DEBUG] Payload JSON completo (primeros 500 chars):\n{PayloadPreview}",
                payloadJson.Length > 500 ? payloadJson.Substring(0, 500) + "..." : payloadJson);

            // 7. Configurar Headers
            var url = emisor.CatAmbienteDestinoId == 1 ? UrlRecepcionPruebas : UrlRecepcionProduccion;
            _logger.LogInformation("[HACIENDA-DEBUG] URL destino: {Url}", url);

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Facturacion-Backend-v1");

            // 8. Crear request con Content-Type exacto "application/json" (sin charset)
            using var content = new StringContent(payloadJson, System.Text.Encoding.UTF8);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            // Log de headers exactos
            _logger.LogInformation("=== REQUEST HEADERS EXACTOS ===");
            foreach (var h in _httpClient.DefaultRequestHeaders)
            {
                var val = h.Key == "Authorization"
                    ? "Bearer " + (token.Length > 20 ? token.Substring(0, 20) + "..." : token)
                    : string.Join(", ", h.Value);
                _logger.LogInformation("[REQ-HEADER] {Key}: {Value}", h.Key, val);
            }
            _logger.LogInformation("[REQ-HEADER] Content-Type: {ContentType}", content.Headers.ContentType?.ToString());
            _logger.LogInformation("[REQ-HEADER] Content-Length: {ContentLength}", content.Headers.ContentLength);

            // 9. Enviar request
            _logger.LogInformation("=== ENVIANDO REQUEST A HACIENDA ===");
            var response = await _httpClient.PostAsync(url, content);

            // 10. Procesar respuesta
            _logger.LogInformation("[HACIENDA-DEBUG] HTTP Status: {StatusCode} ({StatusCodeNumber})",
                response.ReasonPhrase, (int)response.StatusCode);

            _logger.LogInformation("=== RESPONSE HEADERS ===");
            foreach (var h in response.Headers)
                _logger.LogInformation("[RES-HEADER] {Key}: {Value}", h.Key, string.Join(", ", h.Value));
            foreach (var h in response.Content.Headers)
                _logger.LogInformation("[RES-CONTENT-HEADER] {Key}: {Value}", h.Key, string.Join(", ", h.Value));

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("[HACIENDA-DEBUG] Response Body:\n{ResponseBody}", responseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[ERROR] HTTP {StatusCode} - {ReasonPhrase}. Body: {ErrorContent}",
                    (int)response.StatusCode, response.ReasonPhrase, responseBody);

                // MH devuelve 400 para rechazos con body JSON válido.
                // Intentar parsear para detectar rechazos conocidos (ej: "YA EXISTE" código 004)
                if ((int)response.StatusCode == 400)
                {
                    try
                    {
                        var rechazo = System.Text.Json.JsonSerializer.Deserialize<RecepcionResponseDto>(
                            responseBody, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (rechazo != null && rechazo.Estado == "RECHAZADO")
                        {
                            _logger.LogWarning("[HACIENDA-RECHAZO] DTE rechazado por MH. Código: {Codigo}, Mensaje: {Mensaje}",
                                rechazo.CodigoMsg, rechazo.DescripcionMsg);
                            rechazo.DocumentoFirmado = documentoFirmado;
                            return rechazo;
                        }
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        // Body no es JSON válido, continuar con el throw original
                    }
                }

                throw new Exception($"Error enviando DTE: HTTP {(int)response.StatusCode} - {response.ReasonPhrase}. Detalle: {responseBody}");
            }

            var resultado = await response.Content.ReadFromJsonAsync<RecepcionResponseDto>();

            // Devolver el documento firmado para guardarlo en BD
            if (resultado != null)
            {
                resultado.DocumentoFirmado = documentoFirmado;

                _logger.LogInformation("Estado: {Estado}, CodigoMsg: {CodigoMsg}, Descripcion: {Desc}",
                    resultado.Estado, resultado.CodigoMsg, resultado.DescripcionMsg);

                if (resultado.Observaciones != null && resultado.Observaciones.Any())
                {
                    foreach (var obs in resultado.Observaciones)
                        _logger.LogWarning("[OBSERVACION] {Obs}", obs);
                }
            }

            _logger.LogInformation("=== FIN TRANSMISIÓN DTE (JSON directo) ===");
            return resultado ?? new RecepcionResponseDto { Estado = "ERROR_DESERIALIZACION" };
        }

        public async Task<RecepcionLoteResponseDto> TransmitirDteLoteAsync(int emisorId, List<DteBaseDto> dtes)
        {
            // 1. Obtener Emisor
            var emisor = await _context.Emisores
                .Include(e => e.AmbienteDestino)
                .FirstOrDefaultAsync(e => e.Id == emisorId);
            if (emisor == null) throw new Exception("Emisor no encontrado");

            // 2. Obtener Token de Auth
            var token = await _authService.ObtenerTokenAsync(emisorId);

            // 3. Firmar cada DTE y construir array de documentos firmados
            var (llavePrivada, passPrivada) = ObtenerCredencialesFirma(emisor);
            var documentosFirmados = new List<string>();
            foreach (var dte in dtes)
            {
                var jsonDte = JsonSerializer.Serialize(dte);
                var documentoFirmado = _signerService.FirmarDocumento(jsonDte, llavePrivada, passPrivada);
                documentosFirmados.Add(documentoFirmado);
            }

            // 4. Generar UUID v4 en MAYÚSCULAS para idEnvio
            var idEnvio = Guid.NewGuid().ToString().ToUpperInvariant();

            // 5. Construir Payload de envío en lote
            var payloadEnvio = new
            {
                ambiente = emisor.AmbienteDestino.Codigo,
                idEnvio = idEnvio,
                version = 2, // Version para lotes
                nitEmisor = emisor.Nit.Replace("-", ""),
                documentos = documentosFirmados
            };

            // 6. Configurar Headers y enviar
            var url = emisor.CatAmbienteDestinoId == 1
                ? "https://apitest.dtes.mh.gob.sv/fesv/recepcionlote/"
                : "https://api.dtes.mh.gob.sv/fesv/recepcionlote/";

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Facturacion-Backend-v1");
            }

            var response = await _httpClient.PostAsJsonAsync(url, payloadEnvio);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error enviando lote DTE: {StatusCode} - {Content}", response.StatusCode, errorContent);
                throw new Exception($"Error enviando lote: {response.ReasonPhrase}");
            }

            // 7. Procesar respuesta
            var resultado = await response.Content.ReadFromJsonAsync<RecepcionLoteResponseDto>();
            return resultado ?? new RecepcionLoteResponseDto { Estado = "ERROR_DESERIALIZACION" };
        }

        public async Task<RecepcionLoteResponseDto> TransmitirLoteDtesFirmadosAsync(int emisorId, List<string> documentosFirmados)
        {
            // 1. Obtener Emisor
            var emisor = await _context.Emisores
                .Include(e => e.AmbienteDestino)
                .FirstOrDefaultAsync(e => e.Id == emisorId);
            if (emisor == null) throw new Exception("Emisor no encontrado");

            // 2. Obtener Token de Auth
            var token = await _authService.ObtenerTokenAsync(emisorId);

            // 3. Generar UUID v4 en MAYÚSCULAS para idEnvio
            var idEnvio = Guid.NewGuid().ToString().ToUpperInvariant();

            _logger.LogInformation("[HACIENDA-LOTE] Enviando lote con {Count} documentos. IdEnvio: {IdEnvio}",
                documentosFirmados.Count, idEnvio);

            // 4. Construir Payload de envío en lote
            var payloadEnvio = new
            {
                ambiente = emisor.AmbienteDestino.Codigo,
                idEnvio = idEnvio,
                version = 2, // Version para lotes
                nitEmisor = emisor.Nit.Replace("-", ""),
                documentos = documentosFirmados
            };

            // 5. Configurar Headers y enviar
            var url = emisor.CatAmbienteDestinoId == 1
                ? "https://apitest.dtes.mh.gob.sv/fesv/recepcionlote/"
                : "https://api.dtes.mh.gob.sv/fesv/recepcionlote/";

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Facturacion-Backend-v1");
            }

            var response = await _httpClient.PostAsJsonAsync(url, payloadEnvio);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error enviando lote DTE: {StatusCode} - {Content}", response.StatusCode, errorContent);
                throw new Exception($"Error enviando lote: {response.ReasonPhrase}");
            }

            // 6. Procesar respuesta
            var resultado = await response.Content.ReadFromJsonAsync<RecepcionLoteResponseDto>();
            return resultado ?? new RecepcionLoteResponseDto { Estado = "ERROR_DESERIALIZACION" };
        }

        public async Task<ContingenciaResponseDto> EnviarEventoContingenciaAsync(int emisorId, EventoContingenciaDto eventoContingencia)
        {
            // 1. Obtener Emisor
            var emisor = await _context.Emisores
                .Include(e => e.AmbienteDestino)
                .FirstOrDefaultAsync(e => e.Id == emisorId);
            if (emisor == null) throw new Exception("Emisor no encontrado");

            // 2. Obtener Token de Auth
            var token = await _authService.ObtenerTokenAsync(emisorId);

            // 3. Serializar evento y firmar
            var jsonEvento = JsonSerializer.Serialize(eventoContingencia);
            var (llavePrivada, passPrivada) = ObtenerCredencialesFirma(emisor);
            var documentoFirmado = _signerService.FirmarDocumento(jsonEvento, llavePrivada, passPrivada);

            // 4. Construir Payload
            var payloadEnvio = new
            {
                nit = emisor.Nit.Replace("-", ""),
                documento = documentoFirmado
            };

            // 5. Configurar Headers y enviar
            var url = emisor.CatAmbienteDestinoId == 1
                ? "https://apitest.dtes.mh.gob.sv/fesv/contingencia"
                : "https://api.dtes.mh.gob.sv/fesv/contingencia";

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Facturacion-Backend-v1");
            }

            var response = await _httpClient.PostAsJsonAsync(url, payloadEnvio);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error enviando evento contingencia: {StatusCode} - {Content}", response.StatusCode, errorContent);
                throw new Exception($"Error enviando contingencia: {response.ReasonPhrase}");
            }

            // 6. Procesar respuesta
            var resultado = await response.Content.ReadFromJsonAsync<ContingenciaResponseDto>();
            return resultado ?? new ContingenciaResponseDto { Estado = "ERROR_DESERIALIZACION" };
        }

        public async Task<OperacionesEspecialesResponseDto> EnviarEventoOperacionesEspecialesAsync(int emisorId, EventoOperacionesEspecialesDto evento)
        {
            // 1. Obtener Emisor
            var emisor = await _context.Emisores
                .Include(e => e.AmbienteDestino)
                .FirstOrDefaultAsync(e => e.Id == emisorId);
            if (emisor == null) throw new Exception("Emisor no encontrado");

            // 2. Obtener Token de Auth
            var token = await _authService.ObtenerTokenAsync(emisorId);

            // 3. Serializar evento y firmar (serializador por defecto: escribe nulls explícitos)
            var jsonEvento = JsonSerializer.Serialize(evento);
            _logger.LogInformation("JSON evento operaciones especiales (pre-firma): {Json}", jsonEvento);
            var (llavePrivada, passPrivada) = ObtenerCredencialesFirma(emisor);
            var documentoFirmado = _signerService.FirmarDocumento(jsonEvento, llavePrivada, passPrivada);

            // 4. Generar idEnvio incremental
            var idEnvio = Interlocked.Increment(ref _idEnvioCounter);

            // 5. Construir Payload (mismo patrón que invalidación/contingencia)
            var payloadEnvio = new
            {
                ambiente = emisor.AmbienteDestino.Codigo,
                idEnvio = idEnvio,
                version = evento.Identificacion.Version,
                documento = documentoFirmado
            };

            // TODO confirmar endpoint MH para eventos 17/18 (recepcionevento) contra el manual técnico.
            var url = emisor.CatAmbienteDestinoId == 1
                ? "https://apitest.dtes.mh.gob.sv/fesv/recepcionevento"
                : "https://api.dtes.mh.gob.sv/fesv/recepcionevento";

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Facturacion-Backend-v1");

            var jsonPayload = JsonSerializer.Serialize(payloadEnvio);
            _logger.LogInformation("Enviando operaciones especiales a {Url}: {Payload}", url, jsonPayload);

            using var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var response = await _httpClient.PostAsync(url, content);

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Respuesta operaciones especiales: {StatusCode} - {Content}", (int)response.StatusCode, responseBody);

            OperacionesEspecialesResponseDto? resultado = null;
            try
            {
                resultado = JsonSerializer.Deserialize<OperacionesEspecialesResponseDto>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializando respuesta de Hacienda (operaciones especiales): {Body}", responseBody);
            }

            return resultado ?? new OperacionesEspecialesResponseDto { Estado = "ERROR_DESERIALIZACION" };
        }

        public async Task<RetornoResponseDto> EnviarEventoRetornoAsync(int emisorId, EventoRetornoDto evento)
        {
            // 1. Obtener Emisor
            var emisor = await _context.Emisores
                .Include(e => e.AmbienteDestino)
                .FirstOrDefaultAsync(e => e.Id == emisorId);
            if (emisor == null) throw new Exception("Emisor no encontrado");

            // 2. Obtener Token de Auth
            var token = await _authService.ObtenerTokenAsync(emisorId);

            // 3. Serializar evento y firmar (serializador por defecto: escribe nulls explícitos)
            var jsonEvento = JsonSerializer.Serialize(evento);
            _logger.LogInformation("JSON evento retorno (pre-firma): {Json}", jsonEvento);
            var (llavePrivada, passPrivada) = ObtenerCredencialesFirma(emisor);
            var documentoFirmado = _signerService.FirmarDocumento(jsonEvento, llavePrivada, passPrivada);

            // 4. Generar idEnvio incremental
            var idEnvio = Interlocked.Increment(ref _idEnvioCounter);

            // 5. Construir Payload (mismo patrón que invalidación/contingencia)
            var payloadEnvio = new
            {
                ambiente = emisor.AmbienteDestino.Codigo,
                idEnvio = idEnvio,
                version = evento.Identificacion.Version,
                documento = documentoFirmado
            };

            // TODO confirmar endpoint MH para eventos 17/18 (recepcionevento) contra el manual técnico.
            var url = emisor.CatAmbienteDestinoId == 1
                ? "https://apitest.dtes.mh.gob.sv/fesv/recepcionevento"
                : "https://api.dtes.mh.gob.sv/fesv/recepcionevento";

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Facturacion-Backend-v1");

            var jsonPayload = JsonSerializer.Serialize(payloadEnvio);
            _logger.LogInformation("Enviando retorno a {Url}: {Payload}", url, jsonPayload);

            using var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var response = await _httpClient.PostAsync(url, content);

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Respuesta retorno: {StatusCode} - {Content}", (int)response.StatusCode, responseBody);

            RetornoResponseDto? resultado = null;
            try
            {
                resultado = JsonSerializer.Deserialize<RetornoResponseDto>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializando respuesta de Hacienda (retorno): {Body}", responseBody);
            }

            return resultado ?? new RetornoResponseDto { Estado = "ERROR_DESERIALIZACION" };
        }

        public async Task<InvalidacionResponseDto> AnularDteAsync(int emisorId, EventoInvalidacionDto eventoInvalidacion)
        {
            // 1. Obtener Emisor
            var emisor = await _context.Emisores
                .Include(e => e.AmbienteDestino)
                .FirstOrDefaultAsync(e => e.Id == emisorId);
            if (emisor == null) throw new Exception("Emisor no encontrado");

            // 2. Obtener Token de Auth
            var token = await _authService.ObtenerTokenAsync(emisorId);

            // 3. Serializar evento y firmar
            var jsonEvento = JsonSerializer.Serialize(eventoInvalidacion);
            _logger.LogInformation("JSON evento invalidación (pre-firma): {Json}", jsonEvento);
            var (llavePrivada, passPrivada) = ObtenerCredencialesFirma(emisor);
            var documentoFirmado = _signerService.FirmarDocumento(jsonEvento, llavePrivada, passPrivada);

            // 4. Generar idEnvio incremental (entero, igual que DTE regular)
            var idEnvio = Interlocked.Increment(ref _idEnvioCounter);

            // 5. Construir Payload según especificaciones de Invalidación
            var payloadEnvio = new
            {
                ambiente = emisor.AmbienteDestino.Codigo,
                idEnvio = idEnvio,
                version = 3,
                documento = documentoFirmado
            };

            // 6. Configurar Headers y enviar (mismo patrón que TransmitirDteJsonAsync)
            var url = emisor.CatAmbienteDestinoId == 1
                ? "https://apitest.dtes.mh.gob.sv/fesv/anulardte"
                : "https://api.dtes.mh.gob.sv/fesv/anulardte";

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Facturacion-Backend-v1");

            // Serializar manualmente y usar Content-Type sin charset (como DTE regular)
            var jsonPayload = JsonSerializer.Serialize(payloadEnvio);
            _logger.LogInformation("Enviando invalidación a {Url}: {Payload}", url, jsonPayload);

            using var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var response = await _httpClient.PostAsync(url, content);

            // 7. Procesar respuesta (Hacienda devuelve 400 para RECHAZADO, pero el body es JSON válido)
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Respuesta invalidación: {StatusCode} - {Content}", (int)response.StatusCode, responseBody);

            InvalidacionResponseDto? resultado = null;
            try
            {
                resultado = JsonSerializer.Deserialize<InvalidacionResponseDto>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializando respuesta de Hacienda: {Body}", responseBody);
            }

            if (resultado == null)
            {
                throw new Exception($"Error anulando DTE: {response.StatusCode} - {responseBody}");
            }

            return resultado;
        }

        public async Task<ConsultaEstadoDteResponseDto> ConsultarEstadoDteAsync(int emisorId, string codigoGeneracion, string tipoDte)
        {
            // 1. Obtener Emisor
            var emisor = await _context.Emisores
                .Include(e => e.AmbienteDestino)
                .FirstOrDefaultAsync(e => e.Id == emisorId);
            if (emisor == null) throw new Exception("Emisor no encontrado");

            // 2. Obtener Token de Auth
            var token = await _authService.ObtenerTokenAsync(emisorId);

            // 3. Construir URL de consulta y Payload
            // Manual Técnico MH sección 4.3.1: POST /fesv/recepcion/consultadte/
            var url = emisor.CatAmbienteDestinoId == 1
                ? "https://apitest.dtes.mh.gob.sv/fesv/recepcion/consultadte/"
                : "https://api.dtes.mh.gob.sv/fesv/recepcion/consultadte/";

            var payload = new
            {
                nitEmisor = emisor.Nit.Replace("-", ""),
                tdte = tipoDte,
                codigoGeneracion = codigoGeneracion
            };

            // 4. Configurar Headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Facturacion-Backend-v1");
            }

            // 5. Realizar petición POST
            var response = await _httpClient.PostAsJsonAsync(url, payload);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error consultando estado DTE: {StatusCode} - {Content}", response.StatusCode, errorContent);

                // Si es 404, el DTE no existe
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new ConsultaEstadoDteResponseDto
                    {
                        Estado = "NO_EXISTE",
                        CodigoGeneracion = codigoGeneracion,
                        DescripcionMsg = "DTE no encontrado en registros de MH"
                    };
                }

                throw new Exception($"Error consultando estado DTE: {response.ReasonPhrase}");
            }

            // 6. Procesar respuesta
            var resultado = await response.Content.ReadFromJsonAsync<ConsultaEstadoDteResponseDto>();
            return resultado ?? new ConsultaEstadoDteResponseDto { Estado = "ERROR_DESERIALIZACION" };
        }

        public async Task<ConsultaLoteResponseDto> ConsultarEstadoLoteAsync(int emisorId, string codigoGeneracionLote)
        {
            // 1. Obtener Emisor
            var emisor = await _context.Emisores
                .Include(e => e.AmbienteDestino)
                .FirstOrDefaultAsync(e => e.Id == emisorId);
            if (emisor == null) throw new Exception("Emisor no encontrado");

            // 2. Obtener Token de Auth
            var token = await _authService.ObtenerTokenAsync(emisorId);

            // 3. Construir URL de consulta
            // Endpoint correcto: /fesv/recepcion/consultadtelote/{codigoLote}
            var url = emisor.CatAmbienteDestinoId == 1
                ? $"https://apitest.dtes.mh.gob.sv/fesv/recepcion/consultadtelote/{codigoGeneracionLote}"
                : $"https://api.dtes.mh.gob.sv/fesv/recepcion/consultadtelote/{codigoGeneracionLote}";

            // 4. Configurar Headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Facturacion-Backend-v1");
            }

            // 5. Realizar petición GET
            var response = await _httpClient.GetAsync(url);

            // HTTP 204 = Hacienda aún no procesó el lote (sigue en cola)
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                _logger.LogInformation("Lote {CodigoLote} aún no procesado por Hacienda (HTTP 204)", codigoGeneracionLote);
                return new ConsultaLoteResponseDto { EstadoLote = "EN_PROCESO" };
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error consultando estado LOTE: {StatusCode} - {Content}", response.StatusCode, errorContent);
                throw new Exception($"Error consultando lote: {response.ReasonPhrase}");
            }

            // 6. Procesar respuesta
            var resultado = await response.Content.ReadFromJsonAsync<ConsultaLoteResponseDto>();
            return resultado ?? new ConsultaLoteResponseDto { EstadoLote = "ERROR_DESERIALIZACION" };
        }
    }
}
