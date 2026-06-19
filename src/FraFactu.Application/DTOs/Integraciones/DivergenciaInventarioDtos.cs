using FraFactu.Domain.Enums;

namespace FraFactu.Application.DTOs.Integraciones;

/// <summary>
/// F4 (Plan inventario desde DTE): registro de divergencia listado por el
/// endpoint <c>GET /api/inventario/divergencias</c>. Cada fila representa
/// una observacion (codigo+bodega) que difiere entre Smartix y SmartInventory.
/// </summary>
public class DivergenciaInventarioDto
{
    public int Id { get; set; }
    public Guid EjecucionId { get; set; }
    public int EmisorId { get; set; }
    public string CodigoProducto { get; set; } = string.Empty;
    public string NombreBodega { get; set; } = string.Empty;
    public int StockSmartix { get; set; }
    public int StockSmartInventory { get; set; }
    public int Diff { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime UltimaDeteccion { get; set; }
    public DateTime? FechaResolucion { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class DivergenciasListadoDto
{
    public List<DivergenciaInventarioDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
}

public class DivergenciasFiltroDto
{
    public DateTime? DesdeFecha { get; set; }
    public DateTime? HastaFecha { get; set; }
    public EstadoDivergenciaInventario? Estado { get; set; }
    public Guid? EjecucionId { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 20;
}
