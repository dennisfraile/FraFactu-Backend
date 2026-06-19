namespace FraFactu.Application.DTOs.Emisores
{
    /// <summary>
    /// UsuarioCompartido + Plan B Hub-as-Emisor: metadata minima de un Emisor
    /// para el selector del FE (dropdown post-SSO + alternancia desde prefills).
    /// </summary>
    public class EmisorResumenDto
    {
        public int Id { get; set; }
        public string NombreRazonSocial { get; set; } = string.Empty;
        public string Nit { get; set; } = string.Empty;
    }
}
