namespace FraFactu.Application.DTOs.DtesRecibidos;

/// <summary>
/// F1 (Plan inventario desde DTE): payload del wizard de mapeo. Cada item del
/// DTE original se asocia a un producto del catalogo del receptor (existente
/// o creado al vuelo) o se trata como gasto administrativo. Smartix decide
/// asi cuales lineas afectan stock al confirmar la compra.
/// </summary>
public class MapearDteCompraDto
{
    /// <summary>Sucursal donde se registrara la compra. Debe pertenecer al emisor.</summary>
    public int SucursalId { get; set; }

    /// <summary>
    /// Items mapeados. La suma de sus totales debe coincidir con
    /// <c>DteRecibido.Total</c> con tolerancia de 0.01.
    /// </summary>
    public List<MapearItemDto> Items { get; set; } = new();
}

/// <summary>
/// Decision del usuario para una linea del DTE.
/// </summary>
public class MapearItemDto
{
    /// <summary>
    /// Descripcion original tal como viene en el DTE. Se usa para crear el
    /// GastoAdministrativo cuando la accion es GASTO y para auditoria.
    /// </summary>
    public string DescripcionDte { get; set; } = string.Empty;

    /// <summary>
    /// Monto total del item segun el DTE (incluye IVA prorrateado por linea).
    /// Solo se usa cuando la accion es GASTO; para productos se recalcula
    /// con Cantidad x CostoUnitario.
    /// </summary>
    public decimal MontoDte { get; set; }

    /// <summary>
    /// PRODUCTO_EXISTENTE: usar producto del catalogo via <see cref="ProductoId"/>.
    /// PRODUCTO_NUEVO: crear ProductoServicio inline con <see cref="ProductoNuevo"/>.
    /// GASTO: registrar como GastoAdministrativo (no afecta stock).
    /// </summary>
    public string Accion { get; set; } = string.Empty;

    /// <summary>Requerido si Accion=PRODUCTO_EXISTENTE.</summary>
    public int? ProductoId { get; set; }

    /// <summary>Requerido si Accion=PRODUCTO_NUEVO.</summary>
    public ProductoNuevoMapeoDto? ProductoNuevo { get; set; }

    /// <summary>Requerido si Accion in (PRODUCTO_EXISTENTE, PRODUCTO_NUEVO). Mayor a 0.</summary>
    public decimal? Cantidad { get; set; }

    /// <summary>Requerido si Accion in (PRODUCTO_EXISTENTE, PRODUCTO_NUEVO). Bodega de la sucursal seleccionada.</summary>
    public int? BodegaId { get; set; }

    /// <summary>Requerido si Accion in (PRODUCTO_EXISTENTE, PRODUCTO_NUEVO). Mayor o igual a 0.</summary>
    public decimal? CostoUnitario { get; set; }

    /// <summary>Opcional si Accion=GASTO (catalogo de tipos de gasto).</summary>
    public int? CatTipoGastoId { get; set; }
}

/// <summary>
/// Datos minimos para crear un <c>ProductoServicio</c> al vuelo durante el
/// mapeo. El usuario completa el resto (precio venta, categoria, stock minimo)
/// despues en la pantalla de Productos.
/// </summary>
public class ProductoNuevoMapeoDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;

    /// <summary>FK a CatTipoItem (1 = Bien, 2 = Servicio).</summary>
    public int CatTipoItemId { get; set; }

    /// <summary>FK a CatUnidadMedida (catalogo CAT-14 de Hacienda).</summary>
    public int CatUnidadMedidaId { get; set; }

    /// <summary>Categoria del catalogo del emisor (opcional).</summary>
    public int? CategoriaId { get; set; }

    /// <summary>
    /// F2: clasificacion contable. 0=Ventas (default), 1=MobiliarioEquipo,
    /// 2=Insumos. Si es MobiliarioEquipo, el servicio inicializa los campos
    /// de activo fijo con la info de la compra (FechaAdquisicion=FechaEmision,
    /// ValorActual=CostoUnitario).
    /// </summary>
    public int TipoInventario { get; set; } = 0;

    /// <summary>
    /// F2: anios de vida util del activo. Solo aplica si
    /// <see cref="TipoInventario"/> = MobiliarioEquipo. Opcional; el usuario
    /// puede completarlo despues editando el producto.
    /// </summary>
    public int? AniosVidaUtil { get; set; }

    /// <summary>
    /// F2: piso de devaluacion. Solo aplica si MobiliarioEquipo. Default
    /// null (= 0); Smartix no devalua, este campo solo viaja al sync.
    /// </summary>
    public decimal? ValorResidual { get; set; }
}

/// <summary>
/// Respuesta del wizard. Incluye totales reconciliados para que el front muestre
/// confirmacion de exito.
/// </summary>
public class MapearDteCompraResponseDto
{
    public int CompraId { get; set; }
    public decimal TotalMapeado { get; set; }
    public decimal TotalDte { get; set; }
    public int ItemsProductos { get; set; }
    public int ItemsGastos { get; set; }
    public int ProductosCreados { get; set; }
}

/// <summary>Acciones validas para <see cref="MapearItemDto.Accion"/>.</summary>
public static class AccionMapeoItem
{
    public const string ProductoExistente = "PRODUCTO_EXISTENTE";
    public const string ProductoNuevo = "PRODUCTO_NUEVO";
    public const string Gasto = "GASTO";

    public static readonly string[] Valores = { ProductoExistente, ProductoNuevo, Gasto };
}
