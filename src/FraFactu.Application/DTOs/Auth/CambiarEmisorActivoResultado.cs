namespace FraFactu.Application.DTOs.Auth
{
    /// <summary>
    /// Outcome del cambio de Emisor activo. Lo serializa
    /// <c>AuthService.CambiarEmisorActivoAsync</c> y lo mapea
    /// <c>AuthController</c> a status codes HTTP.
    /// </summary>
    public enum CambiarEmisorActivoStatus
    {
        /// <summary>El cambio se persistio y <c>Response</c> trae el JWT reemitido.</summary>
        Ok,
        /// <summary>El Emisor solicitado no esta en <c>emisores_accesibles</c> del JWT actual.</summary>
        NoAutorizado,
        /// <summary>El Emisor solicitado no existe en BD.</summary>
        NoExiste
    }

    /// <summary>
    /// Resultado del cambio de Emisor activo via UsuarioCompartido.
    /// </summary>
    public class CambiarEmisorActivoResultado
    {
        public CambiarEmisorActivoStatus Status { get; init; }
        public LoginResponseDto? Response { get; init; }

        public static CambiarEmisorActivoResultado Ok(LoginResponseDto response) =>
            new() { Status = CambiarEmisorActivoStatus.Ok, Response = response };

        public static CambiarEmisorActivoResultado NoAutorizado() =>
            new() { Status = CambiarEmisorActivoStatus.NoAutorizado };

        public static CambiarEmisorActivoResultado NoExiste() =>
            new() { Status = CambiarEmisorActivoStatus.NoExiste };
    }
}
