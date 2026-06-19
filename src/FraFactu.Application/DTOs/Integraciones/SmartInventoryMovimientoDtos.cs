namespace FraFactu.Application.DTOs.Integraciones;

/// <summary>
/// F3 (Plan inventario desde DTE): payload que Smartix envia a SmartInventory
/// para replicar una entrada de inventario (de momento, COMPRA_EXTERNA).
/// El contrato esta acordado con el dev de SmartInventory (commit F3 #5).
/// </summary>
public class SmartInventoryMovimientoRequestDto
{
    /// <summary>HubId del SmartHub al que pertenece el Emisor.</summary>
    public int OrganizacionId { get; set; }

    /// <summary>
    /// ID opaco del movimiento desde Smartix (<c>{compraId}-{detalleId}</c>).
    /// SmartInventory lo usa para idempotencia: si llega dos veces el mismo,
    /// la segunda devuelve 409 y Smartix interpreta eso como exito.
    /// </summary>
    public string MovimientoIdExterno { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de documento que origina la entrada. Hoy solo <c>COMPRA_EXTERNA</c>;
    /// se deja como string para sumar otros tipos en F4 sin breaking change.
    /// </summary>
    public string TipoDocumento { get; set; } = "COMPRA_EXTERNA";

    /// <summary>ID del documento en Smartix (CompraExterna.Id).</summary>
    public int DocumentoOrigenId { get; set; }

    /// <summary>Numero del documento (NumeroFactura) para mostrar en SmartInventory.</summary>
    public string? NumeroDocumento { get; set; }

    public DateTime FechaMovimiento { get; set; } = DateTime.UtcNow;

    public string? Observaciones { get; set; }

    public List<SmartInventoryMovimientoItemDto> Items { get; set; } = new();
}

public class SmartInventoryMovimientoItemDto
{
    /// <summary>
    /// Id del ProductoServicio en Smartix. Identidad ESTABLE cross-app:
    /// SmartInventory matchea por (OrganizacionId, ProductoIdExterno) y crea con su
    /// propio codigo si no existe. Resuelve la colision de codigos (cada app tiene su
    /// propia secuencia PROD-xxxxx). El <see cref="CodigoProducto"/> queda como dato
    /// informativo / fallback para envios viejos.
    /// </summary>
    public int ProductoIdExterno { get; set; }

    /// <summary>Codigo del producto en Smartix; SmartInventory hace upsert.</summary>
    public string CodigoProducto { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del producto. Util cuando SmartInventory tiene que crear el
    /// producto porque no existia (post-migracion inicial, productos creados
    /// despues en Smartix).
    /// </summary>
    public string NombreProducto { get; set; } = string.Empty;

    /// <summary>Nombre de la bodega; matchea <c>Bodega.Nombre</c> en SmartInventory.</summary>
    public string BodegaNombre { get; set; } = string.Empty;

    /// <summary>
    /// <c>HubSucursalId</c> de la sucursal de la compra (Id de la Sucursal en SmartHub,
    /// 1:1). SmartInventory lo guarda como <c>Bodega.SucursalId</c> al crear la bodega
    /// para que NO quede con SucursalId NULL: el filtro del Sub-plan 8 (claim
    /// <c>sucursal_ids</c> del JWT, que viene del mismo Id de SmartHub) excluye las
    /// bodegas sin sucursal y la dejaria invisible. Null si la sucursal aun no esta
    /// vinculada a SmartHub; SmartInventory entonces hereda la de una bodega existente.
    /// </summary>
    public int? SucursalIdExterno { get; set; }

    public decimal Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }

    /// <summary>Mismos valores que el enum <c>TipoInventario</c> en ambos lados.</summary>
    public string TipoInventario { get; set; } = "Ventas";

    // ==========================================
    // Activo fijo (solo cuando TipoInventario = MobiliarioEquipo)
    // ==========================================
    public DateTime? FechaAdquisicion { get; set; }
    public int? AniosVidaUtil { get; set; }
    public decimal? ValorActual { get; set; }
    public decimal? ValorResidual { get; set; }
}

/// <summary>Respuesta exitosa de SmartInventory al recibir el movimiento.</summary>
public class SmartInventoryMovimientoResponseDto
{
    public int Procesados { get; set; }
}
