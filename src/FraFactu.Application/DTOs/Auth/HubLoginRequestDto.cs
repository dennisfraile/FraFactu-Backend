namespace FraFactu.Application.DTOs.Auth
{
    /// <summary>
    /// Request del frontend Smartix para canjear un code de SSO emitido por SmartHub
    /// y obtener un JWT propio de Smartix. El code llega del Hub via redirect a
    /// /auth/hub?code=... y el frontend lo POSTea aqui.
    /// </summary>
    public class HubLoginRequestDto
    {
        /// <summary>
        /// Exchange code emitido por SmartHub. SmartHub valida el code (y su firma)
        /// cuando Smartix llama a /api/auth/validate-code con header X-Api-Key.
        /// </summary>
        public string Code { get; set; } = string.Empty;
    }
}
