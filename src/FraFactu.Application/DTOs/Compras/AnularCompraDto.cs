namespace FraFactu.Application.DTOs.Compras;

/// <summary>
/// DTO para anular una compra (revertir movimientos de inventario)
/// </summary>
public class AnularCompraDto
{
    /// <summary>
    /// Motivo de la anulación (obligatorio)
    /// </summary>
    public string Motivo { get; set; } = string.Empty;
}
