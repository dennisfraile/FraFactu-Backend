namespace FraFactu.Application.DTOs.Hub;

/// <summary>
/// Item de la respuesta de GET /api/hub/usuarios-roles. Lo consume el Hub:
/// (a) AplicacionesController.GetUsuariosRolesDesdeApps para poblar la UI de
/// asignaciones, y (b) el endpoint POST /api/aplicaciones/{codigo}/reconciliar
/// para detectar drift y huérfanos. Por eso ahora incluye Email y los huérfanos
/// (HubUsuarioId=null) — sin ellos el reconciliar no puede ver usuarios pre-SSO.
/// </summary>
public class HubUsuarioRolInfoDto
{
    /// <summary>Id del usuario en SmartHub. Null para usuarios pre-SSO sin vincular ('huérfanos').</summary>
    public int? HubUsuarioId { get; set; }

    /// <summary>Email del usuario en Smartix; clave para matchear huérfanos contra el catálogo del Hub.</summary>
    public string Email { get; set; } = string.Empty;

    public string RolEnApp { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}
