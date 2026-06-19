namespace FraFactu.Application.DTOs.Compras;

/// <summary>
/// DTO para confirmar una compra (cambiar estado a CONFIRMADA y afectar inventario)
/// </summary>
public class ConfirmarCompraDto
{
    /// <summary>
    /// Observaciones adicionales para la confirmación (opcional)
    /// </summary>
    public string? Observaciones { get; set; }
}
