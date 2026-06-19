namespace FraFactu.Application.DTOs.Compras;

/// <summary>
/// DTO para mostrar un gasto administrativo
/// </summary>
public class GastoAdministrativoDto
{
    public int Id { get; set; }
    public int CompraExternaId { get; set; }

    public int? CatTipoGastoId { get; set; }
    public string? TipoGastoNombre { get; set; }

    public string Descripcion { get; set; } = string.Empty;
    public decimal Monto { get; set; }

    public string? CentroCosto { get; set; }
    public string? CuentaContable { get; set; }

    public DateTime FechaCreacion { get; set; }
}
