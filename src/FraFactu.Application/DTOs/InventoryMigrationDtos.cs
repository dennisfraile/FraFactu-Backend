namespace FraFactu.Application.DTOs;

// Coincide con SmartHub.Application.DTOs.SmartixExportRequest
public class InventoryExportRequestDto
{
    public int HubId { get; set; }
    public Guid MigrationId { get; set; }
}

// Coincide con SmartHub.Application.DTOs.SmartixExportResponse
public class InventoryExportResponseDto
{
    public List<ProductoMigracionDto> Productos { get; set; } = new();
    public List<BodegaMigracionDto> Bodegas { get; set; } = new();
    public List<StockMigracionDto> Stock { get; set; } = new();
}

// Coincide con SmartHub.Application.DTOs.SmartixImportRequest
public class InventoryImportRequestDto
{
    public int HubId { get; set; }
    public Guid MigrationId { get; set; }
    public List<ProductoExportadoMigracionDto> Productos { get; set; } = new();
    public List<BodegaMigracionDto> Bodegas { get; set; } = new();
    public List<StockMigracionDto> Stock { get; set; } = new();
}

// Coincide con SmartHub.Application.DTOs.MigracionProductoDto
public class ProductoMigracionDto
{
    /// <summary>
    /// Id del ProductoServicio en Smartix. Viaja por toda la cadena de migracion
    /// (Smartix -> SmartHub -> SmartInventory) para que SmartInventory guarde la
    /// identidad estable cross-app (ProductoIdExterno) y la replica de compras
    /// matchee por id y no por codigo (que colisiona entre apps).
    /// </summary>
    public int? ProductoIdExterno { get; set; }

    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Tipo { get; set; } = "Bien";
    public decimal PrecioVenta { get; set; }
    public decimal? PrecioCosto { get; set; }
    public string UnidadMedida { get; set; } = "Unidad";
    public int StockMinimo { get; set; }
    public string? CodigoBarras { get; set; }
}

// Coincide con SmartHub.Application.DTOs.MigracionProductoExportadoDto
public class ProductoExportadoMigracionDto : ProductoMigracionDto
{
    public int StockTotal { get; set; }
    public decimal? CostoPromedioActual { get; set; }
}

// Coincide con SmartHub.Application.DTOs.MigracionBodegaDto
public class BodegaMigracionDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Ubicacion { get; set; }
    public bool EsPrincipal { get; set; }

    /// <summary>
    /// <c>HubSucursalId</c> de la sucursal de la bodega (Id de la Sucursal en SmartHub).
    /// Viaja Smartix -> SmartHub -> SmartInventory para que la bodega importada quede con
    /// <c>SucursalId</c> y no la oculte el filtro del Sub-plan 8. Null si la sucursal de
    /// la bodega no esta vinculada a SmartHub.
    /// </summary>
    public int? SucursalIdExterno { get; set; }
}

// Coincide con SmartHub.Application.DTOs.MigracionStockDto
public class StockMigracionDto
{
    public string ProductoCodigo { get; set; } = string.Empty;
    public string BodegaNombre { get; set; } = string.Empty;
    public int CantidadDisponible { get; set; }
    public decimal? CostoPromedio { get; set; }
}
