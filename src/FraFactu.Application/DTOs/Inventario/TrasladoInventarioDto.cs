namespace FraFactu.Application.DTOs.Inventario;

/// <summary>
/// DTO para traslados entre bodegas
/// </summary>
public class TrasladoInventarioDto
{
    public int ProductoId { get; set; }

    /// <summary>
    /// Bodega origen (desde donde sale)
    /// </summary>
    public int BodegaOrigenId { get; set; }

    /// <summary>
    /// Bodega destino (hacia donde va)
    /// </summary>
    public int BodegaDestinoId { get; set; }

    /// <summary>
    /// Cantidad a trasladar
    /// </summary>
    public decimal Cantidad { get; set; }

    /// <summary>
    /// Observaciones del traslado
    /// </summary>
    public string? Observaciones { get; set; }
}
