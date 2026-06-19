namespace FraFactu.Application.DTOs.Dashboard;

/// <summary>
/// DTO para estadísticas del dashboard de cajero
/// </summary>
public class DashboardCajeroDto
{
    /// <summary>
    /// Total de facturas creadas por el cajero
    /// </summary>
    public int TotalFacturas { get; set; }

    /// <summary>
    /// Monto total vendido
    /// </summary>
    public decimal MontoTotalVendido { get; set; }

    /// <summary>
    /// Promedio por venta
    /// </summary>
    public decimal PromedioVenta { get; set; }

    /// <summary>
    /// Ventas agrupadas por día
    /// </summary>
    public List<VentaPorDiaDto> VentasPorDia { get; set; } = new();
}

/// <summary>
/// DTO para ventas agrupadas por día
/// </summary>
public class VentaPorDiaDto
{
    public DateTime Fecha { get; set; }
    public int CantidadFacturas { get; set; }
    public decimal MontoTotal { get; set; }
}
