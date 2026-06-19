using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.ProductosServicios;

namespace FraFactu.Application.Interfaces
{
    /// <summary>
    /// Servicio para gestión de productos del emisor
    /// </summary>
    public interface IProductoServicioService
    {
        /// <summary>
        /// Obtiene un producto por su ID
        /// </summary>
        Task<ProductoServicioDto?> GetByIdAsync(int id, int emisorId);

        /// <summary>
        /// Obtiene todos los productos del emisor (paginado)
        /// </summary>
        Task<PaginatedResponse<ProductoServicioListDto>> GetAllAsync(PaginatedRequest request, int emisorId, int? sucursalId = null, int? categoriaId = null, int? marcaId = null, string? unidadMedida = null, string? tipo = null, bool incluirInactivos = false);

        /// <summary>
        /// Crea un nuevo producto
        /// </summary>
        Task<ProductoServicioDto> CreateAsync(CreateProductoServicioDto dto, int emisorId);

        /// <summary>
        /// Actualiza un producto existente
        /// </summary>
        Task<ProductoServicioDto> UpdateAsync(int id, UpdateProductoServicioDto dto, int emisorId);

        /// <summary>
        /// Activa/Desactiva un producto
        /// </summary>
        Task<bool> ToggleActiveAsync(int id, int emisorId);

        /// <summary>
        /// Busca productos por código o nombre
        /// </summary>
        Task<List<ProductoServicioListDto>> SearchAsync(string searchTerm, int emisorId, int? sucursalId = null);

        /// <summary>
        /// Sync server-to-server desde SmartInventory: busca por (EmisorId, Codigo)
        /// y fija Activo al valor pedido. Idempotente: si el producto no existe en
        /// Smartix devuelve Encontrado=false sin error; si ya estaba en el estado
        /// pedido devuelve Cambio=false. Pensado para que el caller (SmartHub) pueda
        /// decidir si loguea o no.
        /// </summary>
        Task<SyncProductoToggleResponseDto> SetActivoByCodigoAsync(int emisorId, string codigo, bool activo);

        /// <summary>
        /// Sync server-to-server desde SmartInventory: crea o actualiza un producto
        /// por (EmisorId, Codigo). Si no existe lo crea con defaults razonables
        /// (TipoImpuesto=Gravado, IVA=13%, AccesoTodasSucursales=true). Si existe
        /// actualiza Nombre, Descripcion, PrecioVenta, PrecioCosto, CodigoBarras,
        /// StockMinimo y Activo. Idempotente.
        /// </summary>
        Task<SyncProductoUpsertResponseDto> UpsertByCodigoAsync(SyncProductoUpsertRequestDto request);
    }
}
