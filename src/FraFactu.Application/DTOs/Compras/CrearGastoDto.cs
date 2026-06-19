namespace FraFactu.Application.DTOs.Compras;

/// <summary>
/// DTO para crear un gasto administrativo
/// </summary>
public class CrearGastoDto
{
    /// <summary>
    /// ID del tipo de gasto (catálogo)
    /// </summary>
    public int? CatTipoGastoId { get; set; }

    /// <summary>
    /// Descripción del gasto
    /// </summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>
    /// Monto del gasto
    /// </summary>
    public decimal Monto { get; set; }

    /// <summary>
    /// Centro de costo (opcional)
    /// </summary>
    public string? CentroCosto { get; set; }

    /// <summary>
    /// Cuenta contable (opcional)
    /// </summary>
    public string? CuentaContable { get; set; }
}
