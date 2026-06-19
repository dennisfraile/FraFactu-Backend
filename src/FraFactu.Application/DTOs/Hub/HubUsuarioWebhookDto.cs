namespace FraFactu.Application.DTOs.Hub;

/// <summary>
/// Payload que envía SmartHub a Smartix vía POST /api/hub/{accion} cuando un admin
/// del Hub gestiona usuarios (asignar app, cambiar rol, deshabilitar, habilitar).
/// Mirror del DTO NotificacionAppRequest del Hub — no se referencia directamente
/// para mantener los dos backends desacoplados a nivel de tipo.
/// </summary>
public class HubUsuarioWebhookDto
{
    public int HubUsuarioId { get; set; }

    /// <summary>Email del usuario en el Hub. Solo viene poblado en la acción 'asignar'.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Nombre completo. Solo viene poblado en 'asignar'.</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Nombre del rol dentro de Smartix (ej: "EmisorAdmin", "Cajero"). Requerido en 'asignar' y 'cambiar-rol'.</summary>
    public string? RolEnApp { get; set; }

    /// <summary>HubId de la organización (= Emisor.HubId en Smartix). Requerido en 'asignar'.</summary>
    public int? OrganizacionId { get; set; }

    /// <summary>Acción ejecutada por el Hub. Redundante con el path pero útil para logging/audit.</summary>
    public string Accion { get; set; } = string.Empty;
}
