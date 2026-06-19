namespace FraFactu.Application.DTOs.Integraciones;

/// <summary>
/// F4 (Plan inventario desde DTE): respuesta del endpoint
/// <c>GET /api/stock-snapshot</c> de SmartInventory que el job de
/// reconciliacion consume. Contrato espejo del DTO en SmartInventory.
/// </summary>
public class SmartInventorySnapshotResponseDto
{
    public int OrganizacionId { get; set; }
    public List<SmartInventorySnapshotItemDto> Items { get; set; } = new();

    /// <summary>Pasar tal cual como query param en la siguiente llamada; null = ultima pagina.</summary>
    public int? ContinuationToken { get; set; }

    public int TotalEnviado { get; set; }
}

public class SmartInventorySnapshotItemDto
{
    public string CodigoProducto { get; set; } = string.Empty;
    public string NombreProducto { get; set; } = string.Empty;
    public string NombreBodega { get; set; } = string.Empty;

    /// <summary>SucursalId asociado a la bodega en SmartInventory (puede venir null).</summary>
    public int? SucursalId { get; set; }

    public int Cantidad { get; set; }

    /// <summary>"Ventas" | "Insumos" (SmartInventory excluye MobiliarioEquipo del snapshot).</summary>
    public string TipoInventario { get; set; } = "Ventas";
}
