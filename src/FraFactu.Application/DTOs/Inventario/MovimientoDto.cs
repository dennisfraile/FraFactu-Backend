namespace FraFactu.Application.DTOs.Inventario;

/// <summary>
/// DTO para mostrar movimientos de inventario
/// </summary>
public class MovimientoDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string ProductoCodigo { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;

    public int BodegaId { get; set; }
    public string BodegaNombre { get; set; } = string.Empty;

    /// <summary>
    /// ENTRADA, SALIDA, AJUSTE, TRASLADO
    /// </summary>
    public string TipoMovimiento { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal CostoTotal { get; set; }

    public string? TipoDocumento { get; set; }
    public string? NumeroDocumento { get; set; }
    public int? DocumentoId { get; set; }

    // Para traslados
    public int? BodegaDestinoId { get; set; }
    public string? BodegaDestinoNombre { get; set; }

    public DateTime FechaMovimiento { get; set; }
    public string? Observaciones { get; set; }
    public string? UsuarioRegistro { get; set; }

    // Para kardex
    public decimal? SaldoAnterior { get; set; }
    public decimal? NuevoSaldo { get; set; }
    public decimal? CostoPromedioAnterior { get; set; }
    public decimal? NuevoCostoPromedio { get; set; }
}
