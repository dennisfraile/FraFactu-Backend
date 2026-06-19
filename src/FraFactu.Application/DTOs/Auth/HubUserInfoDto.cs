namespace FraFactu.Application.DTOs.Auth
{
    /// <summary>
    /// Datos del usuario hidratados desde la respuesta del endpoint /api/auth/validate-code
    /// de SmartHub. Es el contrato espejo de SmartHub.ValidateCodeResponse para uso
    /// interno en Smartix (no se devuelve al frontend).
    /// </summary>
    public class HubUserInfoDto
    {
        /// <summary>Id del usuario en SmartHub.</summary>
        public int HubUsuarioId { get; set; }

        public string Email { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Id del Hub activo del usuario en SmartHub. En Smartix mapea 1:1 con
        /// Emisor.HubId (decision F6: Hub = Emisor).
        /// </summary>
        public int? OrganizacionId { get; set; }

        /// <summary>
        /// Sucursales activas del usuario en el Hub. Smartix las usa para construir
        /// los claims SucursalId del JWT propio.
        /// </summary>
        public List<int> SucursalIds { get; set; } = new();

        /// <summary>
        /// Rol del Hub: <c>SuperAdmin</c> | <c>AdminOrg</c> | <c>Usuario</c>.
        /// Mapea a roles de Smartix segun: SuperAdmin -> SuperAdmin,
        /// AdminOrg -> EmisorAdmin, Usuario -> EmisorAdmin (default).
        /// </summary>
        public string Rol { get; set; } = string.Empty;

        /// <summary>
        /// Version de token al momento del exchange. Reservado para revocacion
        /// cross-app via OnTokenValidated (no se emite todavia en el JWT de Smartix).
        /// </summary>
        public int TokenVersion { get; set; }

        /// <summary>
        /// UsuarioCompartido + Plan B Hub-as-Emisor: lista de SmartixEmisorId que
        /// la sesion del usuario puede facturar dentro de su Grupo activo. Calculado
        /// por SmartHub via <c>EmisoresAccesiblesCalculator</c> y enviado en el
        /// response de <c>/api/auth/validate-code</c> para que Smartix lo emita como
        /// claim <c>emisores_accesibles</c> en su propio JWT.
        ///
        /// Back-compat: vacia o ausente cuando SmartHub todavia no propaga el
        /// campo (deploy parcial). En ese caso Smartix cae al fallback
        /// <c>[usuario.EmisorId]</c> al generar el JWT.
        /// </summary>
        public List<int> EmisoresAccesibles { get; set; } = new();
    }
}
