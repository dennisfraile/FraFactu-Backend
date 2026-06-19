namespace FraFactu.Application.DTOs.Inventario;

/// <summary>
/// DTO para kardex de producto
/// </summary>
public class KardexDto
{
    public int ProductoId { get; set; }
    public string ProductoCodigo { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;

    public int? BodegaId { get; set; }
    public string? BodegaNombre { get; set; }

    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }

    public decimal SaldoInicial { get; set; }
    public decimal CostoPromedioInicial { get; set; }

    public List<MovimientoKardex> Movimientos { get; set; } = new();

    public decimal SaldoFinal { get; set; }
    public decimal CostoPromedioFinal { get; set; }

    // Totalizadores
    public decimal TotalEntradas { get; set; }
    public decimal TotalSalidas { get; set; }
}

public class MovimientoKardex
{
    public DateTime Fecha { get; set; }
    public string TipoMovimiento { get; set; } = string.Empty;
    public string? Documento { get; set; }

    // Entrada
    public decimal CantidadEntrada { get; set; }
    public decimal CostoEntrada { get; set; }
    public decimal TotalEntrada { get; set; }

    // Salida
    public decimal CantidadSalida { get; set; }
    public decimal CostoSalida { get; set; }
    public decimal TotalSalida { get; set; }

    // Saldo
    public decimal Saldo { get; set; }
    public decimal CostoPromedio { get; set; }
    public decimal ValorSaldo { get; set; }
}
