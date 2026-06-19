namespace FraFactu.Application.Interfaces
{
    /// <summary>
    /// Valida el claim <c>token_version</c> de un JWT contra el valor actual
    /// almacenado en el usuario. Es la pieza de revocación local que reemplaza
    /// al antiguo check contra el Hub (eliminado en F1): cuando el usuario hace
    /// logout, cambia su contraseña, cambia de rol o es desactivado, su
    /// <c>TokenVersion</c> se incrementa y todos los JWT emitidos antes dejan de
    /// ser válidos.
    /// </summary>
    public interface ITokenVersionValidator
    {
        /// <summary>
        /// Indica si el <paramref name="tokenVersionClaim"/> del JWT sigue vigente
        /// para el usuario <paramref name="usuarioId"/>.
        ///
        /// Semántica fail-closed: devuelve <c>false</c> (token rechazado) si el
        /// claim falta, si el usuario no existe, o si el claim no coincide con el
        /// <c>TokenVersion</c> actual del usuario.
        /// </summary>
        Task<bool> IsCurrentAsync(int usuarioId, int? tokenVersionClaim);
    }
}
