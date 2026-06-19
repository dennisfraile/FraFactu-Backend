namespace FraFactu.Domain.Enums
{
    /// <summary>
    /// Estado del usuario en el sistema
    /// </summary>
    public enum EstadoUsuario
    {
        /// <summary>
        /// Usuario activo, puede acceder normalmente
        /// </summary>
        Activo = 1,

        /// <summary>
        /// Usuario bloqueado por intentos fallidos
        /// </summary>
        Bloqueado = 2,

        /// <summary>
        /// Usuario suspendido administrativamente
        /// </summary>
        Suspendido = 3,

        /// <summary>
        /// Usuario inactivo (desactivado)
        /// </summary>
        Inactivo = 4
    }
}
