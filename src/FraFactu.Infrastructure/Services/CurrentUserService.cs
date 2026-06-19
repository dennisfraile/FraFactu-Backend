using FraFactu.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace FraFactu.Infrastructure.Services;

/// <summary>
/// Implementación del servicio que obtiene información del usuario actual
/// desde los claims del token JWT
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? GetUsuarioId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User
            ?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userIdClaim != null && int.TryParse(userIdClaim, out int userId))
        {
            return userId;
        }

        return null;
    }

    public string? GetUsuarioNombre()
    {
        // Primero intentar con el claim Name
        var nombre = _httpContextAccessor.HttpContext?.User
            ?.FindFirst(ClaimTypes.Name)?.Value;

        // Si no existe, intentar con Email
        if (string.IsNullOrEmpty(nombre))
        {
            nombre = _httpContextAccessor.HttpContext?.User
                ?.FindFirst(ClaimTypes.Email)?.Value;
        }

        return nombre;
    }

    public int GetEmisorId()
    {
        var emisorIdClaim = _httpContextAccessor.HttpContext?.User
            ?.FindFirst("EmisorId")?.Value;

        if (string.IsNullOrEmpty(emisorIdClaim) || !int.TryParse(emisorIdClaim, out int emisorId))
        {
            throw new UnauthorizedAccessException("EmisorId no encontrado en el token");
        }

        return emisorId;
    }

    public bool IsAuthenticated()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    }
}
