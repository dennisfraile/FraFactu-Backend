namespace FraFactu.Application.Interfaces;

/// <summary>
/// Interfaz para envío de emails
/// </summary>
public interface IEmailService
{
    Task EnviarFacturaPorEmailAsync(int facturaId, string emailDestinatario, int emisorId);
    Task EnviarAlertaStockBajoAsync(List<string> productos, string emailDestinatario, int emisorId);
    Task EnviarNotificacionGenericaAsync(string asunto, string cuerpo, string emailDestinatario, int emisorId);

    // ====== Métodos para envío de DTEs ======

    /// <summary>
    /// Envía el DTE (Documento Tributario Electrónico) al cliente por email con PDF adjunto
    /// </summary>
    Task EnviarDteAsync(int facturaId, string emailReceptor);

    /// <summary>
    /// Reenvía el DTE al mismo correo receptor configurado en la factura
    /// </summary>
    Task ReenviarDteAsync(int facturaId);

    /// <summary>
    /// Genera el PDF del DTE para adjuntar al email
    /// </summary>
    Task<byte[]> GenerarPdfDteAsync(int facturaId);

    /// <summary>
    /// Envía notificación de invalidación (anulación) al receptor
    /// </summary>
    Task EnviarNotificacionInvalidacionAsync(int facturaId, string emailReceptor, string motivoAnulacion);

    /// <summary>
    /// Envía un email genérico con adjuntos usando el SMTP por defecto del sistema.
    /// Usado para emails de suscripción que no pertenecen a un emisor específico.
    /// </summary>
    Task EnviarEmailGenericoAsync(
        string destinatario,
        string asunto,
        string cuerpoHtml,
        List<(byte[] contenido, string nombre, string mimeType)> adjuntos);
}
