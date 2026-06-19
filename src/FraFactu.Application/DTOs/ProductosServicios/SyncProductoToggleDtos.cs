namespace FraFactu.Application.DTOs.ProductosServicios;

/// <summary>
/// Sync server-to-server desde SmartInventory (via SmartHub) cuando alguien
/// (des)activa un producto en Inventory. Smartix busca por (EmisorId, Codigo)
/// y deja el flag Activo en el valor solicitado. Idempotente.
/// </summary>
public class SyncProductoToggleRequestDto
{
    public int EmisorId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public bool Activo { get; set; }
}

public class SyncProductoToggleResponseDto
{
    /// <summary>Estado final del producto en Smartix tras la operacion.</summary>
    public bool Activo { get; set; }

    /// <summary>True si Smartix mato/encontro el producto. False si no existe (no es error fatal).</summary>
    public bool Encontrado { get; set; }

    /// <summary>True si la llamada provoco un cambio real. False si ya estaba en el estado pedido.</summary>
    public bool Cambio { get; set; }

    /// <summary>Id local del producto en Smartix cuando Encontrado=true.</summary>
    public int? Id { get; set; }
}

/// <summary>
/// Snapshot mandado por SmartInventory cuando se crea o actualiza un producto.
/// Smartix lo upserta por (EmisorId, Codigo). Idempotente. Solo se reciben
/// campos comunes a ambas apps; los catalogos especificos de Smartix
/// (CatTipoItemId, CatUnidadMedidaId) se resuelven aqui con defaults
/// razonables (Producto/Servicio segun Tipo, UnidadMedida por Valor o
/// fallback al primer disponible).
/// </summary>
public class SyncProductoUpsertRequestDto
{
    public int EmisorId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    /// <summary>
    /// Codigo ANTERIOR cuando el upsert es un renombrado (el usuario cambio el codigo
    /// en SmartInventory). Si viene y difiere de Codigo, Smartix busca el producto por
    /// este codigo viejo y le cambia el Codigo al nuevo (en vez de crear un duplicado).
    /// Nullable/opcional: callers que no renombran (o frontends viejos) no lo mandan y
    /// el comportamiento es el upsert normal por Codigo. Es transitorio, no se persiste.
    /// </summary>
    public string? CodigoAnterior { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    /// <summary>"Producto" | "Servicio" (case-insensitive).</summary>
    public string Tipo { get; set; } = "Producto";
    public decimal PrecioVenta { get; set; }
    public decimal? PrecioCosto { get; set; }
    public string UnidadMedida { get; set; } = "Unidad";
    public string? CodigoBarras { get; set; }
    public int StockMinimo { get; set; }
    public bool Activo { get; set; } = true;

    /// <summary>Tipo de impuesto (1=Gravado, 2=Exento, 3=NoSujeto). Default 1.
    /// El service valida rango defensivo: valores fuera de {1,2,3} caen a Gravado.</summary>
    public int TipoImpuesto { get; set; } = 1;

    /// <summary>Porcentaje IVA cuando TipoImpuesto=Gravado, null sino. Default 13.
    /// El service valida (0,100]; fuera de rango cae a 13.</summary>
    public decimal? PorcentajeIVA { get; set; } = 13m;
}

public class SyncProductoUpsertResponseDto
{
    /// <summary>True si Smartix creo el producto, False si solo actualizo.</summary>
    public bool Creado { get; set; }

    /// <summary>True si la operacion provoco cambios en BD; false si los datos ya estaban iguales.</summary>
    public bool Cambio { get; set; }

    /// <summary>Id local del producto en Smartix.</summary>
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public bool Activo { get; set; }
}
