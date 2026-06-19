namespace FraFactu.API.Middleware
{
    /// <summary>
    /// Restringe el acceso de los usuarios con cambio de contraseña obligatorio
    /// pendiente (token de primer ingreso, claim <c>pwd_change_required=true</c>).
    /// Mientras no cambien su clave temporal, solo pueden llamar al endpoint de
    /// cambio obligatorio y al de logout; cualquier otra ruta devuelve 403.
    /// </summary>
    public class RequirePasswordChangeMiddleware
    {
        private readonly RequestDelegate _next;

        public RequirePasswordChangeMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var user = context.User;
            var requierePwdChange =
                user?.Identity?.IsAuthenticated == true
                && user.FindFirst("pwd_change_required")?.Value == "true";

            if (requierePwdChange && !IsAllowedDuringPasswordChange(context.Request.Path))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    "{\"message\":\"Debe cambiar su contraseña temporal antes de continuar.\"}");
                return;
            }

            await _next(context);
        }

        /// <summary>
        /// Rutas permitidas mientras hay un cambio de contraseña pendiente.
        /// </summary>
        public static bool IsAllowedDuringPasswordChange(PathString path)
        {
            var value = path.Value ?? string.Empty;
            return value.EndsWith("/auth/change-password-first-login", StringComparison.OrdinalIgnoreCase)
                || value.EndsWith("/auth/logout", StringComparison.OrdinalIgnoreCase);
        }
    }
}
