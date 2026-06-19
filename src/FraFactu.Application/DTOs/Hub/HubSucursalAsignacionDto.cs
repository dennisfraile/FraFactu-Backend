namespace FraFactu.Application.DTOs.Hub;

/// <summary>
/// Payload que envía SmartHub a Smartix vía POST /api/hub/sucursal-asignacion-cambio
/// cuando un admin asigna o remueve la sucursal asignada a un usuario.
/// Mirror de SucursalAsignacionCambioPayload del Hub.
/// </summary>
public class HubSucursalAsignacionDto
{
    /// <summary>Email del usuario afectado (Hub identifica por email en este caso).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Id de la sucursal en SmartHub. Asumimos paridad de IDs con Smartix (igual que en el SSO).</summary>
    public int SucursalId { get; set; }

    /// <summary>"asignar" o "remover".</summary>
    public string Accion { get; set; } = string.Empty;

    /// <summary>HubId de la organización (= Emisor.HubId).</summary>
    public int HubId { get; set; }
}
