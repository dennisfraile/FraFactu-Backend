using FraFactu.Application.DTOs;
using FraFactu.Application.DTOs.FromSmartCare;

namespace FraFactu.Application.Interfaces;

public interface IFromSmartCarePrefillService
{
    /// <summary>
    /// Crea (o reutiliza por idempotencia) un Prefill para que la UI de Smartix
    /// abra el wizard de factura con los datos pre-cargados.
    /// </summary>
    Task<FromSmartCarePrefillResponseDto> CrearAsync(FromSmartCareInvoiceRequestDto request);

    /// <summary>
    /// Recupera un Prefill por Id (usado por Smartix Vue al hidratar el wizard).
    /// Devuelve null si el prefill no existe.
    /// </summary>
    Task<FromSmartCarePrefillReadDto?> ObtenerAsync(int prefillId);

    /// <summary>
    /// Marca el Prefill como consumido y adjunta los metadatos SmartCare a la
    /// factura recién creada. Llamado por FacturasController.Create cuando se
    /// recibe el header X-SmartCare-Prefill-Id. No tira excepción si el prefill
    /// no existe o ya fue consumido — solo registra warning.
    /// </summary>
    Task ConsumirAsync(int prefillId, int facturaId);

    /// <summary>
    /// Lookup de Factura por <c>SmartCareCorrelationId</c>. Permite a SmartCare
    /// hacer pull-mode polling cuando su <c>smartix_invoice_id</c> esta null
    /// (caso comun: el webhook no llego o se perdio). El correlationId siempre
    /// lo conoce SmartCare desde que crea el prefill. Devuelve null si no hay
    /// factura con ese correlation.
    /// </summary>
    Task<FromSmartCareFacturaEstadoDto?> ObtenerEstadoFacturaPorCorrelationAsync(string correlationId);

    /// <summary>
    /// Busca productos/servicios activos accesibles desde la sucursal indicada,
    /// con filtro opcional de texto (coincide en Codigo o Nombre). Aplica el
    /// mismo filtro de acceso que <c>ListarServicios</c>
    /// (AccesoTodasSucursales OR ProductosServiciosSucursales puente). Los
    /// precios devueltos en <c>PrecioUnitario</c> incluyen IVA para ítems
    /// Gravado (base × (1 + PorcentajeIVA/100)). Devuelve null si la sucursal
    /// no existe o está inactiva (el caller mapea null → 404).
    /// Usado por SmartCare para poblar el selector "Productos adicionales" en BillingStep.
    /// </summary>
    /// <param name="tipo">Filtro opcional: "producto" (CatTipoItemId=1), "servicio" (CatTipoItemId=2),
    /// o null para devolver ambos (comportamiento anterior).</param>
    Task<CatalogoBusquedaResponseDto?> BuscarCatalogoAsync(int sucursalSmartixId, string? search, int limit = 50, string? tipo = null);

    /// <summary>
    /// Resuelve el EmisorId a partir del Id de una Sucursal Smartix.
    /// Devuelve null si la sucursal no existe.
    /// </summary>
    Task<int?> ResolverEmisorPorSucursalAsync(int sucursalSmartixId);
}
