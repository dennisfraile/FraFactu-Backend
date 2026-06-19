namespace FraFactu.Application.Common.Settings
{
    public class DefaultEmailSettings
    {
        public string? SmtpHost { get; set; }
        public int SmtpPort { get; set; } = 587;
        public string? SmtpUser { get; set; }
        public string? SmtpPassword { get; set; }
        public string? EmailRemitente { get; set; }
        public bool Habilitado { get; set; } = false;
    }
}
