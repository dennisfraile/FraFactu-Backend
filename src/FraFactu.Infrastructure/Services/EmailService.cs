using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;
using FraFactu.Application.Common.Settings;
using FraFactu.Application.Interfaces;
using FraFactu.Infrastructure.Persistence;

namespace FraFactu.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmailService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEncryptionService _encryptionService;
    private readonly DefaultEmailSettings _defaultEmail;
    private readonly IGmailApiService _gmailApiService;

    public EmailService(
        ApplicationDbContext context,
        ILogger<EmailService> logger,
        IHttpClientFactory httpClientFactory,
        IEncryptionService encryptionService,
        IOptions<DefaultEmailSettings> defaultEmailSettings,
        IGmailApiService gmailApiService)
    {
        _context = context;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _encryptionService = encryptionService;
        _defaultEmail = defaultEmailSettings.Value;
        _gmailApiService = gmailApiService;

        // Configurar licencia de QuestPDF (Community)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ====== VALIDACION GMAIL / SMTP ======

    private bool ValidarGmailEmisor(Domain.Entities.Emisor emisor)
    {
        return emisor.EmailHabilitado
            && emisor.GmailConectado
            && !string.IsNullOrEmpty(emisor.GmailRefreshToken)
            && !string.IsNullOrEmpty(emisor.GmailEmail);
    }

    private async Task<bool> EnviarEmailViaGmailAsync(
        Domain.Entities.Emisor emisor,
        string destinatario,
        string asunto,
        string cuerpoHtml,
        List<(byte[] contenido, string nombre, string mimeType)>? adjuntos = null)
    {
        try
        {
            await _gmailApiService.EnviarEmailGmailAsync(
                emisor.GmailRefreshToken!, emisor.GmailEmail!,
                destinatario, asunto, cuerpoHtml, adjuntos);

            _logger.LogInformation("[EMAIL] Correo enviado via Gmail API a {Email} desde emisor {EmisorId}",
                destinatario, emisor.Id);
            return true;
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning(ex, "[EMAIL] Gmail OAuth2 token revocado para emisor {EmisorId}, fallback a SMTP", emisor.Id);
            emisor.GmailConectado = false;
            await _context.SaveChangesAsync();
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMAIL] Error enviando via Gmail API para emisor {EmisorId}, marcando como desconectado y fallback a SMTP", emisor.Id);
            emisor.GmailConectado = false;
            await _context.SaveChangesAsync();
            return false;
        }
    }

    private bool ValidarSmtpEmisor(Domain.Entities.Emisor emisor)
    {
        if (!emisor.EmailHabilitado)
        {
            _logger.LogWarning("[EMAIL] Envio de correo deshabilitado para emisor {EmisorId} ({Nombre})",
                emisor.Id, emisor.NombreRazonSocial);
            return false;
        }

        if (string.IsNullOrEmpty(emisor.SmtpUser) || string.IsNullOrEmpty(emisor.SmtpPassword))
        {
            _logger.LogWarning("[EMAIL] Emisor {EmisorId} ({Nombre}) no tiene credenciales SMTP configuradas",
                emisor.Id, emisor.NombreRazonSocial);
            return false;
        }

        if (string.IsNullOrEmpty(emisor.SmtpHost))
        {
            _logger.LogWarning("[EMAIL] Emisor {EmisorId} ({Nombre}) no tiene SmtpHost configurado",
                emisor.Id, emisor.NombreRazonSocial);
            return false;
        }

        return true;
    }

    // ====== METODOS GENERICOS ======

    public async Task EnviarFacturaPorEmailAsync(int facturaId, string emailDestinatario, int emisorId)
    {
        var emisor = await _context.Emisores.FindAsync(emisorId);
        if (emisor == null)
            throw new InvalidOperationException($"Emisor {emisorId} no encontrado");

        var asunto = $"Factura Electronica #{facturaId}";
        var cuerpo = $@"
            <h2>Factura Electronica</h2>
            <p>Estimado cliente,</p>
            <p>Adjunto encontrara su factura electronica #{facturaId}.</p>
            <p>Gracias por su preferencia.</p>
            <hr>
            <small>Este es un email automatico, por favor no responder.</small>
        ";

        await EnviarEmailConEmisorAsync(emisor, emailDestinatario, asunto, cuerpo);
    }

    public async Task EnviarAlertaStockBajoAsync(List<string> productos, string emailDestinatario, int emisorId)
    {
        var emisor = await _context.Emisores.FindAsync(emisorId);
        if (emisor == null)
            throw new InvalidOperationException($"Emisor {emisorId} no encontrado");

        var asunto = "Alerta de Stock Bajo";
        var listaProductos = string.Join("</li><li>", productos);
        var cuerpo = $@"
            <h2>Alerta de Inventario</h2>
            <p>Los siguientes productos estan por debajo del stock minimo:</p>
            <ul>
                <li>{listaProductos}</li>
            </ul>
            <p>Por favor, considere realizar un pedido de reposicion.</p>
        ";

        await EnviarEmailConEmisorAsync(emisor, emailDestinatario, asunto, cuerpo);
    }

    public async Task EnviarNotificacionGenericaAsync(string asunto, string cuerpo, string emailDestinatario, int emisorId)
    {
        var emisor = await _context.Emisores.FindAsync(emisorId);
        if (emisor == null)
            throw new InvalidOperationException($"Emisor {emisorId} no encontrado");

        await EnviarEmailConEmisorAsync(emisor, emailDestinatario, asunto, cuerpo);
    }

    // ====== METODOS PRIVADOS DE ENVIO ======

    /// <summary>
    /// Determina si se debe usar el SMTP por defecto como fallback
    /// </summary>
    private bool ValidarSmtpDefault()
    {
        if (!_defaultEmail.Habilitado)
            return false;

        if (string.IsNullOrEmpty(_defaultEmail.SmtpHost) ||
            string.IsNullOrEmpty(_defaultEmail.SmtpUser) ||
            string.IsNullOrEmpty(_defaultEmail.SmtpPassword))
            return false;

        return true;
    }

    private string DecryptSmtpPassword(string value)
    {
        try
        {
            return _encryptionService.Decrypt(value);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException(
                "Error al desencriptar 'SmtpPassword' del emisor. " +
                "Verifique que la Encryption:MasterKey en Azure App Settings coincida con la que se usó al guardar las credenciales.", ex);
        }
    }

    /// <summary>
    /// Crea un SmtpClient y MailAddress usando las credenciales del emisor o el SMTP por defecto
    /// </summary>
    private (SmtpClient client, MailAddress from, bool usaDefault)? ObtenerSmtpClient(Domain.Entities.Emisor emisor)
    {
        // Prioridad 1: SMTP propio del emisor
        if (ValidarSmtpEmisor(emisor))
        {
            var client = new SmtpClient(emisor.SmtpHost!, emisor.SmtpPort ?? 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(emisor.SmtpUser!, DecryptSmtpPassword(emisor.SmtpPassword!))
            };
            var from = new MailAddress(emisor.EmailRemitente ?? emisor.SmtpUser!);
            return (client, from, false);
        }

        // Prioridad 2: SMTP por defecto de la plataforma
        if (ValidarSmtpDefault())
        {
            _logger.LogInformation("[EMAIL] Emisor {EmisorId} ({Nombre}) sin SMTP propio, usando correo por defecto de la plataforma",
                emisor.Id, emisor.NombreRazonSocial);

            var client = new SmtpClient(_defaultEmail.SmtpHost!, _defaultEmail.SmtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_defaultEmail.SmtpUser!, _defaultEmail.SmtpPassword!)
            };
            var from = new MailAddress(_defaultEmail.EmailRemitente ?? _defaultEmail.SmtpUser!);
            return (client, from, true);
        }

        _logger.LogWarning("[EMAIL] No hay SMTP disponible para emisor {EmisorId} ({Nombre}): ni propio ni por defecto",
            emisor.Id, emisor.NombreRazonSocial);
        return null;
    }

    private async Task EnviarEmailConEmisorAsync(
        Domain.Entities.Emisor emisor,
        string destinatario,
        string asunto,
        string cuerpoHtml)
    {
        // Prioridad 1: Gmail API OAuth2
        if (ValidarGmailEmisor(emisor))
        {
            var sent = await EnviarEmailViaGmailAsync(emisor, destinatario, asunto, cuerpoHtml);
            if (sent) return;
        }

        // Prioridad 2/3: SMTP del emisor o SMTP por defecto
        var smtp = ObtenerSmtpClient(emisor);
        if (smtp == null) return;

        var (client, from, usaDefault) = smtp.Value;
        try
        {
            using (client)
            {
                var message = new MailMessage
                {
                    From = from,
                    Subject = asunto,
                    Body = cuerpoHtml,
                    IsBodyHtml = true
                };

                message.To.Add(destinatario);

                await client.SendMailAsync(message);
                _logger.LogInformation("[EMAIL] Correo enviado a {Email} desde emisor {EmisorId}{Via}",
                    destinatario, emisor.Id, usaDefault ? " (via SMTP por defecto)" : "");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMAIL] Error enviando email a {Email} desde emisor {EmisorId}", destinatario, emisor.Id);
            throw;
        }
    }

    private async Task EnviarEmailConAdjuntosEmisorAsync(
        Domain.Entities.Emisor emisor,
        string destinatario,
        string asunto,
        string cuerpoHtml,
        List<(byte[] contenido, string nombre, string mimeType)> adjuntos)
    {
        // Prioridad 1: Gmail API OAuth2
        if (ValidarGmailEmisor(emisor))
        {
            var sent = await EnviarEmailViaGmailAsync(emisor, destinatario, asunto, cuerpoHtml, adjuntos);
            if (sent) return;
        }

        // Prioridad 2/3: SMTP del emisor o SMTP por defecto
        var smtp = ObtenerSmtpClient(emisor);
        if (smtp == null) return;

        var (client, from, usaDefault) = smtp.Value;
        try
        {
            using (client)
            {
                var message = new MailMessage
                {
                    From = from,
                    Subject = asunto,
                    Body = cuerpoHtml,
                    IsBodyHtml = true
                };

                message.To.Add(destinatario);

                // Adjuntar archivos
                var streams = new List<MemoryStream>();
                foreach (var adjunto in adjuntos)
                {
                    var stream = new MemoryStream(adjunto.contenido);
                    streams.Add(stream);
                    var attachment = new Attachment(stream, adjunto.nombre, adjunto.mimeType);
                    message.Attachments.Add(attachment);
                }

                await client.SendMailAsync(message);

                // Liberar streams
                foreach (var stream in streams)
                    stream.Dispose();

                _logger.LogInformation("[EMAIL] Correo con {Count} adjunto(s) enviado a {Email} desde emisor {EmisorId}{Via}",
                    adjuntos.Count, destinatario, emisor.Id, usaDefault ? " (via SMTP por defecto)" : "");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMAIL] Error enviando email con adjuntos a {Email} desde emisor {EmisorId}",
                destinatario, emisor.Id);
            throw;
        }
    }

    // ====== METODOS PARA DTEs ======

    /// <summary>
    /// Envia el DTE al correo del receptor con PDF adjunto
    /// </summary>
    public async Task EnviarDteAsync(int facturaId, string emailReceptor)
    {
        try
        {
            _logger.LogInformation("[EMAIL-DTE] Iniciando envio de DTE para factura {FacturaId} a {Email}",
                facturaId, emailReceptor);

            // Obtener datos de la factura
            var factura = await _context.Facturas
                .Include(f => f.Emisor)
                .Include(f => f.Receptor)
                .FirstOrDefaultAsync(f => f.Id == facturaId);

            if (factura == null)
            {
                _logger.LogWarning("[EMAIL-DTE] Factura {FacturaId} no encontrada", facturaId);
                throw new InvalidOperationException($"Factura {facturaId} no encontrada");
            }

            // Validar que hay algun SMTP disponible (emisor o por defecto)
            if (!ValidarGmailEmisor(factura.Emisor) && !ValidarSmtpEmisor(factura.Emisor) && !ValidarSmtpDefault()) return;

            // Generar PDF
            var pdfBytes = await GenerarPdfDteAsync(facturaId);

            // Preparar adjuntos (PDF + JSON firmado)
            var adjuntos = new List<(byte[] contenido, string nombre, string mimeType)>
            {
                (pdfBytes, $"DTE-{factura.NumeroControl}.pdf", "application/pdf")
            };

            // Adjuntar JSON legible + firma electrónica
            var jsonFirmado = factura.JsonFirmado;

            // Si está vacío, intentar recargar
            if (string.IsNullOrWhiteSpace(jsonFirmado))
            {
                _logger.LogWarning("[EMAIL-DTE] JsonFirmado está vacío para factura {FacturaId}. Intentando recargar...", facturaId);
                jsonFirmado = await _context.Facturas
                    .AsNoTracking()
                    .Where(f => f.Id == facturaId)
                    .Select(f => f.JsonFirmado)
                    .FirstOrDefaultAsync();
            }

            if (!string.IsNullOrWhiteSpace(jsonFirmado))
            {
                var partes = jsonFirmado.Split('.');
                if (partes.Length == 3)
                {
                    // Decodificar payload del JWT
                    var payloadBase64 = partes[1].PadRight(partes[1].Length + (4 - partes[1].Length % 4) % 4, '=');
                    var dteJson = Encoding.UTF8.GetString(Convert.FromBase64String(payloadBase64));

                    // Parsear el DTE y agregar firmaElectronica
                    using var doc = JsonDocument.Parse(dteJson);

                    using var stream = new MemoryStream();
                    using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                    {
                        writer.WriteStartObject();
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            prop.WriteTo(writer);
                        }
                        writer.WriteString("firmaElectronica", jsonFirmado);
                        writer.WriteEndObject();
                    }

                    var jsonBytes = stream.ToArray();
                    adjuntos.Add((jsonBytes, $"DTE-{factura.NumeroControl}.json", "application/json"));
                    _logger.LogInformation("[EMAIL-DTE] JSON con firma adjuntado ({Size} bytes)", jsonBytes.Length);
                }
                else
                {
                    // Fallback: no es JWT, adjuntar tal cual
                    var jsonBytes = Encoding.UTF8.GetBytes(jsonFirmado);
                    adjuntos.Add((jsonBytes, $"DTE-{factura.NumeroControl}.json", "application/json"));
                    _logger.LogInformation("[EMAIL-DTE] JSON adjuntado sin decodificar ({Size} bytes)", jsonBytes.Length);
                }
            }
            else
            {
                _logger.LogWarning("[EMAIL-DTE] No se pudo obtener JsonFirmado para factura {FacturaId}", facturaId);
            }

            // Preparar email
            var emisorNombreAsunto = factura.Emisor.NombreComercial ?? factura.Emisor.NombreRazonSocial;
            var asunto = $"Factura electr\u00f3nica de {emisorNombreAsunto} - {factura.NumeroControl}";
            var cuerpo = GenerarPlantillaEmailDte(factura);

            // Enviar con adjuntos usando credenciales del emisor
            await EnviarEmailConAdjuntosEmisorAsync(factura.Emisor, emailReceptor, asunto, cuerpo, adjuntos);

            // Actualizar estado de envio
            factura.CorreoEnviado = true;
            factura.FechaEnvioCorreo = DateTime.UtcNow;
            factura.EmailReceptor = emailReceptor;
            await _context.SaveChangesAsync();

            _logger.LogInformation("[EMAIL-DTE] DTE enviado exitosamente a {Email}", emailReceptor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMAIL-DTE] Error enviando DTE para factura {FacturaId}", facturaId);
            throw;
        }
    }

    /// <summary>
    /// Reenvia el DTE al mismo correo configurado en la factura
    /// </summary>
    public async Task ReenviarDteAsync(int facturaId)
    {
        _logger.LogInformation("[EMAIL-DTE] Reenviando DTE para factura {FacturaId}", facturaId);

        var factura = await _context.Facturas
            .FirstOrDefaultAsync(f => f.Id == facturaId);

        if (factura == null)
            throw new InvalidOperationException($"Factura {facturaId} no encontrada");

        if (string.IsNullOrWhiteSpace(factura.EmailReceptor))
        {
            // Si no hay email guardado, usar el del receptor
            var facturaConReceptor = await _context.Facturas
                .Include(f => f.Receptor)
                .FirstOrDefaultAsync(f => f.Id == facturaId);

            var emailReceptor = facturaConReceptor?.Receptor?.CorreoElectronico;

            if (string.IsNullOrWhiteSpace(emailReceptor))
                throw new InvalidOperationException("No hay correo electronico del receptor configurado");

            await EnviarDteAsync(facturaId, emailReceptor);
        }
        else
        {
            await EnviarDteAsync(facturaId, factura.EmailReceptor);
        }
    }

    /// <summary>
    /// Genera el PDF del DTE usando QuestPDF
    /// </summary>
    public async Task<byte[]> GenerarPdfDteAsync(int facturaId)
    {
        _logger.LogInformation("[PDF-DTE] Generando PDF para factura {FacturaId}", facturaId);

        var factura = await _context.Facturas
            .Include(f => f.Emisor).ThenInclude(e => e.TipoEstablecimiento)
            .Include(f => f.Receptor)
            .Include(f => f.Detalles)
            .Include(f => f.Tributos)
            .Include(f => f.Pagos).ThenInclude(p => p.FormaPago)
            .Include(f => f.Pagos).ThenInclude(p => p.Plazo)
            .Include(f => f.Extension)
            .Include(f => f.Apendices)
            .Include(f => f.OtrosDocumentos)
            .Include(f => f.VentaTercero)
            .Include(f => f.DocumentosRelacionados).ThenInclude(d => d.TipoDocumento)
            .Include(f => f.CondicionOperacion)
            .Include(f => f.Sucursal!).ThenInclude(s => s.TipoEstablecimiento)
            .Include(f => f.Sucursal!).ThenInclude(s => s.Departamento)
            .Include(f => f.Sucursal!).ThenInclude(s => s.Municipio)
            .FirstOrDefaultAsync(f => f.Id == facturaId);

        if (factura == null)
            throw new InvalidOperationException($"Factura {facturaId} no encontrada");

        // Descargar logo si existe
        byte[]? logoBytes = null;
        if (!string.IsNullOrEmpty(factura.Emisor.LogoUrl))
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                logoBytes = await client.GetByteArrayAsync(factura.Emisor.LogoUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error descargando logo del emisor desde {Url}", factura.Emisor.LogoUrl);
                // Continuar sin logo
            }
        }

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(1.0f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                // Encabezado dentro del Content para que NO se repita en cada página
                page.Content().Column(mainCol =>
                {
                    mainCol.Item().Element(header => ComponerEncabezado(header, factura, logoBytes));
                    mainCol.Item().Element(content => ComponerContenido(content, factura));
                });
                page.Footer().Element(footer => ComponerPiePagina(footer, factura));
            });
        }).GeneratePdf();

        _logger.LogInformation("[PDF-DTE] PDF generado exitosamente ({Size} bytes)", pdfBytes.Length);
        return pdfBytes;
    }

    // ====== METODOS AUXILIARES PARA PDF ======

    private void ComponerEncabezado(IContainer container, Domain.Entities.FacturaElectronica factura, byte[]? logoBytes)
    {
        container.Column(column =>
        {
            // ===== FILA SUPERIOR: Logo/Sello | Códigos | Datos Emisión =====
            column.Item().Border(1).BorderColor(Colors.Grey.Darken1).Row(row =>
            {
                // COLUMNA 1: Logo y Sello de Recepción
                row.ConstantItem(150).BorderRight(1).BorderColor(Colors.Grey.Darken1).Column(col1 =>
                {
                    // Logo del emisor
                    if (logoBytes != null)
                    {
                        col1.Item().Padding(5).AlignCenter().Height(50).Image(logoBytes).FitArea();
                    }

                    // Sello de Recepción
                    col1.Item().Padding(5).Border(1).BorderColor(Colors.Grey.Lighten1).Column(selloBox =>
                    {
                        selloBox.Item().AlignCenter().Text("SELLO DE RECEPCIÓN").FontSize(8).Bold();
                        if (!string.IsNullOrEmpty(factura.SelloRecibido))
                        {
                            selloBox.Item().AlignCenter().Text(factura.SelloRecibido).FontSize(6).FontFamily("Courier New");
                        }
                        else
                        {
                            selloBox.Item().AlignCenter().Text("Pendiente").FontSize(8).Italic().FontColor(Colors.Grey.Medium);
                        }
                    });
                });

                // COLUMNA 2: Título y Códigos (centro)
                row.RelativeItem().BorderRight(1).BorderColor(Colors.Grey.Darken1).Padding(8).Column(col2 =>
                {
                    var tituloDoc = factura.CatTipoDocumentoId == 2 ? "CRÉDITO FISCAL" : "DOCUMENTO TRIBUTARIO ELECTRÓNICO";
                    col2.Item().AlignCenter().Text(tituloDoc).FontSize(11).Bold();
                    col2.Item().AlignCenter().Text(ObtenerTipoDte(factura)).FontSize(13).Bold().FontColor(Colors.Blue.Darken2);
                    col2.Item().PaddingTop(8).Column(codigos =>
                    {
                        codigos.Item().Row(r => {
                            r.RelativeItem().Text("Código de Generación:").FontSize(8).Bold();
                            r.RelativeItem().Text(factura.CodigoGeneracion ?? "N/A").FontSize(7).FontFamily("Courier New");
                        });
                        codigos.Item().Row(r => {
                            r.RelativeItem().Text("Número de Control:").FontSize(8).Bold();
                            r.RelativeItem().Text(factura.NumeroControl ?? "N/A").FontSize(7).FontFamily("Courier New");
                        });
                        codigos.Item().Row(r => {
                            r.RelativeItem().Text("Sello de Recepción:").FontSize(8).Bold();
                            r.RelativeItem().Text(factura.SelloRecibido ?? "N/A").FontSize(7).FontFamily("Courier New");
                        });
                    });
                });

                // COLUMNA 3: Datos de emisión y QR
                row.ConstantItem(160).Padding(8).Column(col3 =>
                {
                    col3.Item().Row(r => {
                        r.RelativeItem().Text("Modelo:").FontSize(8).Bold();
                        r.RelativeItem().Text(factura.CatModeloFacturacionId == 2 ? "Diferido" : "Previo").FontSize(8);
                    });
                    col3.Item().Row(r => {
                        r.RelativeItem().Text("Transmisión:").FontSize(8).Bold();
                        r.RelativeItem().Text(factura.CatTipoTransmisionId == 2 ? "Contingencia" : "Normal").FontSize(8);
                    });
                    col3.Item().Row(r => {
                        r.RelativeItem().Text("Fecha:").FontSize(8).Bold();
                        r.RelativeItem().Text($"{factura.FechaEmision:dd/MM/yyyy} {factura.HoraEmision}").FontSize(8);
                    });

                    // QR de verificación
                    var urlVerificacion = $"https://admin.factura.gob.sv/consultaPublica?ambiente={factura.Ambiente}&codGen={factura.CodigoGeneracion}&fechaEmi={factura.FechaEmision:yyyy-MM-dd}";
                    var qrBytes = GenerarCodigoQr(urlVerificacion);
                    if (qrBytes != null)
                    {
                        col3.Item().PaddingTop(3).AlignCenter().Height(55).Image(qrBytes);
                    }
                });
            });

            // ===== FILA EMISOR Y RECEPTOR =====
            column.Item().PaddingTop(2).Border(1).BorderColor(Colors.Grey.Darken1).Row(row =>
            {
                // EMISOR
                row.RelativeItem().BorderRight(1).BorderColor(Colors.Grey.Darken1).Column(emisorCol =>
                {
                    emisorCol.Item().Background(Colors.Grey.Lighten3).Padding(3).Text("EMISOR").FontSize(9).Bold();
                    emisorCol.Item().Padding(3).Column(datos =>
                    {
                        datos.Item().Text(text => { text.Span("Nombre / Razón Social: ").FontSize(8).Bold(); text.Span(factura.Emisor.NombreRazonSocial ?? "-").FontSize(8); });
                        datos.Item().Text(text => { text.Span("NIT: ").FontSize(8).Bold(); text.Span(factura.Emisor.Nit ?? "-").FontSize(8); });
                        datos.Item().Text(text => { text.Span("NRC: ").FontSize(8).Bold(); text.Span(factura.Emisor.Nrc ?? "-").FontSize(8); });
                        datos.Item().Text(text => { text.Span("Actividad: ").FontSize(8).Bold(); text.Span(factura.Emisor.DescripcionActividad ?? "-").FontSize(8); });
                        datos.Item().Text(text => { text.Span("Dirección: ").FontSize(8).Bold(); text.Span(factura.Sucursal?.Direccion ?? factura.Emisor.Direccion ?? "-").FontSize(8); });
                        datos.Item().Text(text => { text.Span("Teléfono: ").FontSize(8).Bold(); text.Span(factura.Sucursal?.Telefono ?? factura.Emisor.Telefono ?? "-").FontSize(8); });
                        datos.Item().Text(text => { text.Span("Correo: ").FontSize(8).Bold(); text.Span(factura.Sucursal?.CorreoElectronico ?? factura.Emisor.CorreoElectronico ?? "-").FontSize(8); });
                        datos.Item().Text(text => { text.Span("Nombre Comercial: ").FontSize(8).Bold(); text.Span(factura.Emisor.NombreComercial ?? "-").FontSize(8); });
                        datos.Item().Text(text => { text.Span("Tipo Establecimiento: ").FontSize(8).Bold(); text.Span(factura.Sucursal?.TipoEstablecimiento?.Valor ?? "-").FontSize(8); });
                    });
                });

                // RECEPTOR
                row.RelativeItem().Column(receptorCol =>
                {
                    receptorCol.Item().Background(Colors.Grey.Lighten3).Padding(3).Text("RECEPTOR").FontSize(9).Bold();
                    receptorCol.Item().Padding(3).Column(datos =>
                    {
                        datos.Item().Text(text => { text.Span("Nombre / Razón Social: ").FontSize(8).Bold(); text.Span(factura.Receptor?.NombreRazonSocial ?? "Consumidor Final").FontSize(8); });
                        datos.Item().Text(text => { text.Span("Tipo de Documento: ").FontSize(8).Bold(); text.Span(ObtenerTipoDocReceptor(factura.Receptor)).FontSize(8); });
                        datos.Item().Text(text => { text.Span("No. de Documento: ").FontSize(8).Bold(); text.Span(factura.Receptor?.NumeroDocumento ?? "-").FontSize(8); });
                        datos.Item().Text(text => { text.Span("Dirección: ").FontSize(8).Bold(); text.Span(factura.Receptor?.Direccion ?? "-").FontSize(8); });
                        datos.Item().Text(text => { text.Span("Teléfono: ").FontSize(8).Bold(); text.Span(factura.Receptor?.Telefono ?? "-").FontSize(8); });
                        datos.Item().Text(text => { text.Span("Correo: ").FontSize(8).Bold(); text.Span(factura.Receptor?.CorreoElectronico ?? "-").FontSize(8); });
                    });
                });
            });
        });
    }

    // Método auxiliar para tipo de DTE
    // Los IDs corresponden a cat_tipo_doc.Id (NO al código DTE de Hacienda)
    // Id=1(01)=Factura, Id=2(03)=CCF, Id=3(04)=NR, Id=4(05)=NC, Id=5(06)=ND
    // Id=9(11)=Exportación, Id=10(14)=Sujeto Excluido
    private string ObtenerTipoDte(Domain.Entities.FacturaElectronica factura)
    {
        return factura.CatTipoDocumentoId switch
        {
            1 => "FACTURA",
            2 => "COMPROBANTE DE CRÉDITO FISCAL",
            3 => "NOTA DE REMISIÓN",
            4 => "NOTA DE CRÉDITO",
            5 => "NOTA DE DÉBITO",
            9 => "FACTURA DE EXPORTACIÓN",
            10 => "FACTURA DE SUJETO EXCLUIDO",
            _ => "DOCUMENTO TRIBUTARIO"
        };
    }

    // Método auxiliar para tipo de documento receptor
    private string ObtenerTipoDocReceptor(Domain.Entities.Receptor? receptor)
    {
        if (receptor == null) return "-";
        var codigo = receptor.TipoDocumento?.Codigo;
        return codigo switch
        {
            "13" => "DUI",
            "36" => "NIT",
            "02" => "Carnet de Residente",
            "03" => "Pasaporte",
            _ => receptor.TipoDocumento?.Valor ?? "-"
        };
    }

    private void ComponerContenido(IContainer container, Domain.Entities.FacturaElectronica factura)
    {
        container.PaddingTop(2).Column(column =>
        {
            // Título de la sección
            column.Item().Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Darken1)
                .Padding(2).Text("DETALLE DE PRODUCTOS Y/O SERVICIOS").FontSize(8).Bold();

            // Tabla de detalles
            column.Item().Border(1).BorderColor(Colors.Grey.Darken1).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(25);   // N°
                    columns.ConstantColumn(35);   // Cantidad
                    columns.ConstantColumn(45);   // Unidad
                    columns.RelativeColumn(3);    // Descripción
                    columns.RelativeColumn(1);    // Precio Unit.
                    columns.RelativeColumn(1);    // Descuento
                    columns.RelativeColumn(1);    // Otros no afectos
                    columns.RelativeColumn(1);    // No Sujetas
                    columns.RelativeColumn(1);    // Exentas
                    columns.RelativeColumn(1);    // Gravadas
                });

                // Encabezado
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(3).AlignCenter().Text("N°").FontSize(7).Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(3).AlignCenter().Text("Cant.").FontSize(7).Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(3).AlignCenter().Text("Unidad").FontSize(7).Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(3).Text("Descripción").FontSize(7).Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(3).AlignRight().Text("P. Unit.").FontSize(7).Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(3).AlignRight().Text("Desc.").FontSize(7).Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(3).AlignRight().Text("Otros").FontSize(7).Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(3).AlignRight().Text("No Suj.").FontSize(7).Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(3).AlignRight().Text("Exentas").FontSize(7).Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(3).AlignRight().Text("Gravadas").FontSize(7).Bold();
                });

                // Filas de detalles
                int numItem = 1;
                foreach (var detalle in factura.Detalles)
                {
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignCenter().Text(numItem.ToString()).FontSize(7);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignCenter().Text(detalle.Cantidad.ToString("N2")).FontSize(7);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignCenter().Text(detalle.UnidadMedida?.Valor ?? "Otro").FontSize(7);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(detalle.Descripcion ?? "").FontSize(7);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"${detalle.PrecioUnitario:N2}").FontSize(7);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"${detalle.MontoDescuento:N2}").FontSize(7);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text(detalle.NoGravado > 0 ? $"${detalle.NoGravado:N2}" : "-").FontSize(7);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"${detalle.VentaNoSujeta:N2}").FontSize(7);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"${detalle.VentaExenta:N2}").FontSize(7);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"${detalle.VentaGravada:N2}").FontSize(7);
                    numItem++;
                }
            });

            // Resumen de totales
            column.Item().PaddingTop(2).Row(row =>
            {
                // Columna izquierda: Observaciones y condición
                row.RelativeItem(2).Column(izq =>
                {
                    izq.Item().Text(text => { text.Span("Valor en Letras: ").FontSize(8).Bold(); text.Span(factura.TotalLetras ?? NumeroALetras(factura.TotalPagar)).FontSize(8); });
                    izq.Item().Text(text => { text.Span("Condición: ").FontSize(8).Bold(); text.Span(factura.CatCondicionOperacionId == 2 ? "Crédito" : "Contado").FontSize(8); });
                });

                // Columna derecha: Totales
                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Darken1).Column(der =>
                {
                    der.Item().Row(r => { r.RelativeItem().Padding(2).Text("Suma de Ventas No Sujetas:").FontSize(7); r.ConstantItem(80).Padding(2).AlignRight().Text($"${factura.TotalNoSujeto:N2}").FontSize(7); });
                    der.Item().Row(r => { r.RelativeItem().Padding(2).Text("Suma de Ventas Exentas:").FontSize(7); r.ConstantItem(80).Padding(2).AlignRight().Text($"${factura.TotalExento:N2}").FontSize(7); });
                    der.Item().Row(r => { r.RelativeItem().Padding(2).Text("Suma de Ventas Gravadas:").FontSize(7); r.ConstantItem(80).Padding(2).AlignRight().Text($"${factura.TotalGravado:N2}").FontSize(7); });
                    der.Item().Row(r => { r.RelativeItem().Padding(2).Text("(-) Desc. ventas no sujetas:").FontSize(7); r.ConstantItem(80).Padding(2).AlignRight().Text($"${factura.DescuentoNoSujeto:N2}").FontSize(7); });
                    der.Item().Row(r => { r.RelativeItem().Padding(2).Text("(-) Desc. ventas exentas:").FontSize(7); r.ConstantItem(80).Padding(2).AlignRight().Text($"${factura.DescuentoExento:N2}").FontSize(7); });
                    der.Item().Row(r => { r.RelativeItem().Padding(2).Text("(-) Desc. ventas gravadas:").FontSize(7); r.ConstantItem(80).Padding(2).AlignRight().Text($"${factura.DescuentoGravado:N2}").FontSize(7); });
                    der.Item().Row(r => { r.RelativeItem().Padding(2).Text("Sub-Total:").FontSize(7); r.ConstantItem(80).Padding(2).AlignRight().Text($"${factura.SubTotal:N2}").FontSize(7); });

                    // ═══ TRIBUTOS ADICIONALES (FOVIAL, Turismo, CO-TRANS, etc.) ═══
                    if (factura.Tributos != null && factura.Tributos.Any())
                    {
                        var tributosAdicionales = factura.Tributos
                            .Where(t => t.CodigoAttribute != "20")
                            .ToList();

                        if (tributosAdicionales.Any())
                        {
                            der.Item().Background(Colors.Grey.Lighten3).Row(r =>
                            {
                                r.RelativeItem().Padding(2).Text("Nombre del Tributo").FontSize(7).Bold();
                                r.ConstantItem(80).Padding(2).AlignRight().Text("Valor").FontSize(7).Bold();
                            });

                            foreach (var tributo in tributosAdicionales)
                            {
                                der.Item().Row(r =>
                                {
                                    r.RelativeItem().Padding(2).Text(tributo.Descripcion).FontSize(7);
                                    r.ConstantItem(80).Padding(2).AlignRight().Text($"${tributo.Valor:N2}").FontSize(7);
                                });
                            }
                        }
                    }

                    // IVA: solo mostrar para tipos que lo desglosan (CCF, etc.), no para Factura (tipo 1) donde va incluido en el precio
                    if (factura.CatTipoDocumentoId != 1)
                    {
                        der.Item().Row(r => { r.RelativeItem().Padding(2).Text("IVA 13%:").FontSize(7); r.ConstantItem(80).Padding(2).AlignRight().Text($"${factura.TotalIva:N2}").FontSize(7); });
                    }
                    der.Item().Row(r => { r.RelativeItem().Padding(2).Text("(+) IVA Percibido:").FontSize(7); r.ConstantItem(80).Padding(2).AlignRight().Text($"${factura.IvaPercibido:N2}").FontSize(7); });
                    der.Item().Row(r => { r.RelativeItem().Padding(2).Text("(-) IVA Retenido:").FontSize(7); r.ConstantItem(80).Padding(2).AlignRight().Text($"${factura.IvaRetenido:N2}").FontSize(7); });
                    der.Item().Row(r => { r.RelativeItem().Padding(2).Text("(-) Retención Renta:").FontSize(7); r.ConstantItem(80).Padding(2).AlignRight().Text($"${factura.RetencionRenta:N2}").FontSize(7); });
                    der.Item().Row(r => { r.RelativeItem().Padding(2).Text("Monto Total Operación:").FontSize(7); r.ConstantItem(80).Padding(2).AlignRight().Text($"${factura.MontoTotalOperacion:N2}").FontSize(7); });
                    der.Item().Row(r => { r.RelativeItem().Padding(2).Text("Total Otros montos no afectos:").FontSize(7); r.ConstantItem(80).Padding(2).AlignRight().Text($"${factura.TotalNoGravado:N2}").FontSize(7); });
                    der.Item().Background(Colors.Blue.Lighten4).Row(r => { r.RelativeItem().Padding(2).Text("TOTAL A PAGAR:").FontSize(8).Bold(); r.ConstantItem(80).Padding(2).AlignRight().Text($"${factura.TotalPagar:N2}").FontSize(8).Bold(); });
                });
            });

            // ═══ SECCIÓN: EXTENSIÓN (Firmas Entrega/Recepción) ═══
            if (factura.Extension != null)
            {
                column.Item().PaddingTop(5).Column(extCol =>
                {
                    // Observaciones y Placa
                    if (!string.IsNullOrEmpty(factura.Extension.Observaciones) || !string.IsNullOrEmpty(factura.Extension.PlacaVehiculo))
                    {
                        extCol.Item().Padding(5).Row(obsRow =>
                        {
                            if (!string.IsNullOrEmpty(factura.Extension.Observaciones))
                                obsRow.RelativeItem().Text(text => { text.Span("Observaciones: ").FontSize(8).Bold(); text.Span(factura.Extension.Observaciones).FontSize(8); });
                            if (!string.IsNullOrEmpty(factura.Extension.PlacaVehiculo))
                                obsRow.ConstantItem(200).Text(text => { text.Span("Placa Vehículo: ").FontSize(8).Bold(); text.Span(factura.Extension.PlacaVehiculo).FontSize(8); });
                        });
                    }

                    // Firmas: Entrega y Recepción
                    extCol.Item().Border(1).BorderColor(Colors.Grey.Darken1).Row(firmasRow =>
                    {
                        // Responsable de Entrega
                        firmasRow.RelativeItem().BorderRight(1).BorderColor(Colors.Grey.Darken1).Column(entrega =>
                        {
                            entrega.Item().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("RESPONSABLE DE ENTREGA").FontSize(8).Bold();
                            entrega.Item().PaddingTop(10).PaddingHorizontal(15).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                            entrega.Item().Padding(5).Text(text => { text.Span("Nombre: ").FontSize(8).Bold(); text.Span(factura.Extension.NombEntrega ?? "_______________________").FontSize(8); });
                            entrega.Item().Padding(5).Text(text => { text.Span("N° Documento: ").FontSize(8).Bold(); text.Span(factura.Extension.DocuEntrega ?? "_______________________").FontSize(8); });
                        });

                        // Responsable de Recepción
                        firmasRow.RelativeItem().Column(recepcion =>
                        {
                            recepcion.Item().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("RESPONSABLE DE RECEPCIÓN").FontSize(8).Bold();
                            recepcion.Item().PaddingTop(10).PaddingHorizontal(15).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                            recepcion.Item().Padding(5).Text(text => { text.Span("Nombre: ").FontSize(8).Bold(); text.Span(factura.Extension.NombRecibe ?? factura.Receptor?.NombreRazonSocial ?? "_______________________").FontSize(8); });
                            recepcion.Item().Padding(5).Text(text => { text.Span("N° Documento: ").FontSize(8).Bold(); text.Span(factura.Extension.DocuRecibe ?? factura.Receptor?.NumeroDocumento ?? "_______________________").FontSize(8); });
                        });
                    });
                });
            }

            // ═══ SECCIÓN: FORMA DE PAGO ═══
            if (factura.Pagos != null && factura.Pagos.Any())
            {
                column.Item().PaddingTop(2).Column(pagoCol =>
                {
                    pagoCol.Item().Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Darken1)
                        .Padding(2).Text("FORMA DE PAGO").FontSize(8).Bold();

                    pagoCol.Item().Border(1).BorderColor(Colors.Grey.Darken1).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);  // Forma de Pago
                            columns.RelativeColumn(1);  // Monto
                            columns.RelativeColumn(1);  // Referencia
                            columns.RelativeColumn(1);  // Plazo
                            columns.RelativeColumn(1);  // Periodo
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(3).Text("Forma de Pago").FontSize(7).Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(3).AlignRight().Text("Monto").FontSize(7).Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(3).Text("Referencia").FontSize(7).Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(3).Text("Plazo").FontSize(7).Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(3).Text("Periodo").FontSize(7).Bold();
                        });

                        foreach (var pago in factura.Pagos)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(pago.FormaPago?.Valor ?? "-").FontSize(7);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"${pago.Monto:N2}").FontSize(7);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(pago.Referencia ?? "-").FontSize(7);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(pago.Plazo?.Valor ?? "-").FontSize(7);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(pago.Periodo.HasValue ? $"{pago.Periodo} días" : "-").FontSize(7);
                        }
                    });

                    // Número de Pago Electrónico
                    if (!string.IsNullOrEmpty(factura.NumPagoElectronico))
                    {
                        pagoCol.Item().Padding(5).Text(text => { text.Span("Ref. Pago Electrónico: ").FontSize(8).Bold(); text.Span(factura.NumPagoElectronico).FontSize(8); });
                    }
                });
            }

            // ═══ SECCIÓN: OTROS DOCUMENTOS ASOCIADOS ═══
            if (factura.OtrosDocumentos != null && factura.OtrosDocumentos.Any())
            {
                column.Item().PaddingTop(5).Column(otrosCol =>
                {
                    otrosCol.Item().Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Darken1)
                        .Padding(5).Text("OTROS DOCUMENTOS ASOCIADOS").FontSize(9).Bold();

                    otrosCol.Item().Border(1).BorderColor(Colors.Grey.Darken1).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1);  // Identificación
                            columns.RelativeColumn(2);  // Descripción
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(3).Text("Identificación").FontSize(7).Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(3).Text("Descripción").FontSize(7).Bold();
                        });

                        foreach (var doc in factura.OtrosDocumentos)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(doc.DescDocumento ?? "-").FontSize(7);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(doc.DetalleDocumento ?? "-").FontSize(7);
                        }
                    });
                });
            }

            // ═══ SECCIÓN: VENTA A CUENTA DE TERCEROS ═══
            if (factura.VentaTercero != null)
            {
                column.Item().PaddingTop(5).Column(terceroCol =>
                {
                    terceroCol.Item().Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Darken1)
                        .Padding(5).Text("VENTA A CUENTA DE TERCEROS").FontSize(9).Bold();

                    terceroCol.Item().Border(1).BorderColor(Colors.Grey.Darken1).Padding(5).Row(r =>
                    {
                        r.RelativeItem().Text(text => { text.Span("NIT: ").FontSize(8).Bold(); text.Span(factura.VentaTercero.Nit).FontSize(8); });
                        r.RelativeItem().Text(text => { text.Span("Nombre: ").FontSize(8).Bold(); text.Span(factura.VentaTercero.Nombre).FontSize(8); });
                    });
                });
            }

            // ═══ SECCIÓN: DOCUMENTOS RELACIONADOS ═══
            if (factura.DocumentosRelacionados != null && factura.DocumentosRelacionados.Any())
            {
                column.Item().PaddingTop(5).Column(docRelCol =>
                {
                    docRelCol.Item().Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Darken1)
                        .Padding(5).Text("DOCUMENTOS RELACIONADOS").FontSize(9).Bold();

                    docRelCol.Item().Border(1).BorderColor(Colors.Grey.Darken1).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);  // Tipo de Documento
                            columns.RelativeColumn(2);  // N° de Documento
                            columns.RelativeColumn(1);  // Fecha de Emisión
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(3).Text("Tipo de Documento").FontSize(7).Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(3).Text("N° de Documento").FontSize(7).Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(3).Text("Fecha de Emisión").FontSize(7).Bold();
                        });

                        foreach (var doc in factura.DocumentosRelacionados)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(doc.TipoDocumento?.Valor ?? "-").FontSize(7);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(doc.NumeroDocumento ?? "-").FontSize(7);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{doc.FechaEmision:dd/MM/yyyy}").FontSize(7);
                        }
                    });
                });
            }

            // ═══ SECCIÓN: APÉNDICE ═══
            if (factura.Apendices != null && factura.Apendices.Any())
            {
                column.Item().PaddingTop(5).Column(apendiceCol =>
                {
                    apendiceCol.Item().Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Darken1)
                        .Padding(5).Text("APÉNDICE").FontSize(9).Bold();

                    apendiceCol.Item().Border(1).BorderColor(Colors.Grey.Darken1).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1);  // Campo
                            columns.RelativeColumn(1);  // Etiqueta
                            columns.RelativeColumn(2);  // Valor
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(3).Text("Campo").FontSize(7).Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(3).Text("Etiqueta").FontSize(7).Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(3).Text("Valor").FontSize(7).Bold();
                        });

                        foreach (var item in factura.Apendices)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(item.Campo).FontSize(7);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(item.Etiqueta).FontSize(7);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(item.Valor).FontSize(7);
                        }
                    });
                });
            }
        });
    }

    // Método auxiliar para convertir número a letras (simplificado)
    private string NumeroALetras(decimal numero)
    {
        return $"{numero:N2} DÓLARES";
    }

    private byte[]? GenerarCodigoQr(string contenido)
    {
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(5);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error generando QR");
            return null;
        }
    }

    private void ComponerPiePagina(IContainer container, Domain.Entities.FacturaElectronica factura)
    {
        container.Column(column =>
        {
            var pieTitulo = factura.CatTipoDocumentoId == 2 ? "CRÉDITO FISCAL" : "DOCUMENTO TRIBUTARIO ELECTRÓNICO";
            column.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);
            column.Item().PaddingTop(3).AlignCenter().Text(pieTitulo).FontSize(8).Bold();
            column.Item().AlignCenter().Text($"Sello de Recepción MH: {factura.SelloRecibido ?? "Pendiente"}").FontSize(7);
            column.Item().AlignCenter().Text($"Fecha de Generación: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(7).Italic().FontColor(Colors.Grey.Medium);
        });
    }

    /// <summary>
    /// Genera la plantilla HTML para el email del DTE
    /// </summary>
    private string GenerarPlantillaEmailDte(Domain.Entities.FacturaElectronica factura)
    {
        var emisorNombre = factura.Emisor.NombreComercial ?? factura.Emisor.NombreRazonSocial;
        var receptorNombre = factura.Receptor?.NombreRazonSocial ?? "Cliente";

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"es\"><head><meta charset=\"utf-8\"><style>");
        html.AppendLine("body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }");
        html.AppendLine(".container { max-width: 600px; margin: 0 auto; padding: 20px; }");
        html.AppendLine(".header { background: #2c3e50; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }");
        html.AppendLine(".content { background: #f9f9f9; padding: 20px; margin: 0; }");
        html.AppendLine(".footer { text-align: center; font-size: 12px; color: #777; padding: 15px; border-top: 1px solid #eee; }");
        html.AppendLine("table { width: 100%; border-collapse: collapse; margin: 10px 0; }");
        html.AppendLine("td { padding: 8px; border-bottom: 1px solid #ddd; }");
        html.AppendLine(".label { font-weight: bold; width: 40%; }");
        html.AppendLine("</style></head><body>");
        html.AppendLine("<div class='container'>");

        // Encabezado
        html.AppendLine("<div class='header'>");
        html.AppendLine($"<h1>Documento Tributario Electr\u00f3nico</h1>");
        html.AppendLine($"<p>{emisorNombre}</p>");
        html.AppendLine("</div>");

        // Contenido
        html.AppendLine("<div class='content'>");
        html.AppendLine($"<p>Estimado(a) <strong>{receptorNombre}</strong>,</p>");
        html.AppendLine($"<p>Adjunto encontrar\u00e1 su Documento Tributario Electr\u00f3nico (DTE) emitido por <strong>{emisorNombre}</strong> en formato PDF.</p>");
        html.AppendLine("<table>");
        html.AppendLine($"<tr><td class='label'>N\u00famero de Control:</td><td>{factura.NumeroControl}</td></tr>");
        html.AppendLine($"<tr><td class='label'>C\u00f3digo de Generaci\u00f3n:</td><td>{factura.CodigoGeneracion}</td></tr>");
        html.AppendLine($"<tr><td class='label'>Fecha de Emisi\u00f3n:</td><td>{factura.FechaEmision:dd/MM/yyyy}</td></tr>");
        html.AppendLine($"<tr><td class='label'>Total a Pagar:</td><td><strong>${factura.TotalPagar:N2}</strong></td></tr>");
        html.AppendLine("</table>");
        html.AppendLine("<p>Gracias por su preferencia.</p>");
        html.AppendLine("</div>");

        // Footer
        html.AppendLine("<div class='footer'>");
        html.AppendLine($"<p>{emisorNombre} &middot; Facturaci\u00f3n Electr\u00f3nica</p>");
        html.AppendLine($"<p>&copy; {DateTime.Now.Year} {factura.Emisor.NombreRazonSocial}</p>");
        html.AppendLine("</div>");

        html.AppendLine("</div></body></html>");

        return html.ToString();
    }

    // ====== METODO PARA INVALIDACION ======

    /// <summary>
    /// Envia notificacion de invalidacion (anulacion) al receptor
    /// </summary>
    public async Task EnviarNotificacionInvalidacionAsync(int facturaId, string emailReceptor, string motivoAnulacion)
    {
        try
        {
            _logger.LogInformation("[EMAIL-INVALIDACION] Enviando notificacion de invalidacion para factura {FacturaId} a {Email}",
                facturaId, emailReceptor);

            var factura = await _context.Facturas
                .Include(f => f.Emisor)
                .Include(f => f.Receptor)
                .FirstOrDefaultAsync(f => f.Id == facturaId);

            if (factura == null)
            {
                _logger.LogWarning("[EMAIL-INVALIDACION] Factura {FacturaId} no encontrada", facturaId);
                return;
            }

            // Validar que hay algun SMTP disponible (emisor o por defecto)
            if (!ValidarGmailEmisor(factura.Emisor) && !ValidarSmtpEmisor(factura.Emisor) && !ValidarSmtpDefault()) return;

            var emisorNombreInv = factura.Emisor.NombreComercial ?? factura.Emisor.NombreRazonSocial;
            var asunto = $"Invalidaci\u00f3n de documento - {emisorNombreInv} - {factura.NumeroControl}";
            var cuerpo = GenerarPlantillaEmailInvalidacion(factura, motivoAnulacion);

            await EnviarEmailConEmisorAsync(factura.Emisor, emailReceptor, asunto, cuerpo);

            _logger.LogInformation("[EMAIL-INVALIDACION] Notificacion enviada exitosamente a {Email}", emailReceptor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMAIL-INVALIDACION] Error enviando notificacion para factura {FacturaId}", facturaId);
            throw;
        }
    }

    private string GenerarPlantillaEmailInvalidacion(Domain.Entities.FacturaElectronica factura, string motivoAnulacion)
    {
        var emisorNombre = factura.Emisor.NombreComercial ?? factura.Emisor.NombreRazonSocial;
        var receptorNombre = factura.Receptor?.NombreRazonSocial ?? "Cliente";

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"es\"><head><meta charset=\"utf-8\"><style>");
        html.AppendLine("body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }");
        html.AppendLine(".container { max-width: 600px; margin: 0 auto; padding: 20px; }");
        html.AppendLine(".header { background: #c0392b; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }");
        html.AppendLine(".content { background: #f9f9f9; padding: 20px; margin: 0; }");
        html.AppendLine(".footer { text-align: center; font-size: 12px; color: #777; padding: 15px; border-top: 1px solid #eee; }");
        html.AppendLine("table { width: 100%; border-collapse: collapse; margin: 10px 0; }");
        html.AppendLine("td { padding: 8px; border-bottom: 1px solid #ddd; }");
        html.AppendLine(".label { font-weight: bold; width: 40%; }");
        html.AppendLine("</style></head><body>");
        html.AppendLine("<div class='container'>");

        html.AppendLine("<div class='header'>");
        html.AppendLine($"<h1>Notificaci\u00f3n de Invalidaci\u00f3n</h1>");
        html.AppendLine($"<p>{emisorNombre}</p>");
        html.AppendLine("</div>");

        html.AppendLine("<div class='content'>");
        html.AppendLine($"<p>Estimado(a) <strong>{receptorNombre}</strong>,</p>");
        html.AppendLine($"<p>Le informamos que el siguiente Documento Tributario Electr\u00f3nico emitido por <strong>{emisorNombre}</strong> ha sido <strong>invalidado</strong>:</p>");
        html.AppendLine("<table>");
        html.AppendLine($"<tr><td class='label'>N\u00famero de Control:</td><td>{factura.NumeroControl}</td></tr>");
        html.AppendLine($"<tr><td class='label'>C\u00f3digo de Generaci\u00f3n:</td><td>{factura.CodigoGeneracion}</td></tr>");
        html.AppendLine($"<tr><td class='label'>Fecha de Emisi\u00f3n:</td><td>{factura.FechaEmision:dd/MM/yyyy}</td></tr>");
        html.AppendLine($"<tr><td class='label'>Total:</td><td><strong>${factura.TotalPagar:N2}</strong></td></tr>");
        html.AppendLine($"<tr><td class='label'>Motivo de Invalidaci\u00f3n:</td><td>{motivoAnulacion}</td></tr>");
        html.AppendLine("</table>");
        html.AppendLine($"<p>Este documento ya no tiene validez fiscal. Si tiene alguna consulta, por favor comun\u00edquese con nosotros.</p>");
        html.AppendLine("</div>");

        html.AppendLine("<div class='footer'>");
        html.AppendLine($"<p>{emisorNombre} &middot; Facturaci\u00f3n Electr\u00f3nica</p>");
        html.AppendLine($"<p>&copy; {DateTime.Now.Year} {factura.Emisor.NombreRazonSocial}</p>");
        html.AppendLine("</div>");

        html.AppendLine("</div></body></html>");
        return html.ToString();
    }

    /// <summary>
    /// Envía un email genérico con adjuntos usando el SMTP por defecto del sistema.
    /// Usado para emails de suscripción que no pertenecen a un emisor específico.
    /// </summary>
    public async Task EnviarEmailGenericoAsync(
        string destinatario,
        string asunto,
        string cuerpoHtml,
        List<(byte[] contenido, string nombre, string mimeType)> adjuntos)
    {
        if (!ValidarSmtpDefault())
        {
            _logger.LogWarning("[EMAIL-GENERICO] No hay SMTP por defecto configurado. No se puede enviar email a {Email}", destinatario);
            return;
        }

        var client = new SmtpClient(_defaultEmail.SmtpHost!, _defaultEmail.SmtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_defaultEmail.SmtpUser!, _defaultEmail.SmtpPassword!)
        };

        try
        {
            using (client)
            {
                var from = new MailAddress(_defaultEmail.EmailRemitente ?? _defaultEmail.SmtpUser!);
                var message = new MailMessage
                {
                    From = from,
                    Subject = asunto,
                    Body = cuerpoHtml,
                    IsBodyHtml = true
                };

                message.To.Add(destinatario);

                var streams = new List<MemoryStream>();
                foreach (var adjunto in adjuntos)
                {
                    var stream = new MemoryStream(adjunto.contenido);
                    streams.Add(stream);
                    var attachment = new Attachment(stream, adjunto.nombre, adjunto.mimeType);
                    message.Attachments.Add(attachment);
                }

                await client.SendMailAsync(message);

                foreach (var stream in streams)
                    stream.Dispose();

                _logger.LogInformation("[EMAIL-GENERICO] Email con {Count} adjunto(s) enviado a {Email}",
                    adjuntos.Count, destinatario);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMAIL-GENERICO] Error enviando email a {Email}", destinatario);
            throw;
        }
    }
}
