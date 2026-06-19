namespace FraFactu.Application.DTOs.Auth
{
    /// <summary>
    /// Request para POST /api/auth/cambiar-emisor-activo.
    /// UsuarioCompartido + Plan B Hub-as-Emisor: permite al usuario alternar entre
    /// los Emisores listados en el claim <c>emisores_accesibles</c> del JWT actual
    /// sin tener que re-SSO desde el Hub. El endpoint persiste el cambio en
    /// <c>Usuarios.EmisorId</c> y reemite el JWT con el nuevo Emisor activo
    /// (misma lista de emisores accesibles, mismo TokenVersion — el cambio de
    /// scope no invalida la sesion).
    /// </summary>
    public class CambiarEmisorActivoRequestDto
    {
        /// <summary>
        /// ID del Emisor al que se quiere cambiar. Debe estar dentro de
        /// <c>emisores_accesibles</c> del JWT actual y existir en BD.
        /// </summary>
        public int EmisorId { get; set; }
    }
}
