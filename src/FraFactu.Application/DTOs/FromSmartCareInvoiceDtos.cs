namespace FraFactu.Application.DTOs;

/// <summary>
/// Petición de SmartCare para crear un <em>prefill</em> en Smartix. El prefill es un
/// borrador temporal que la UI de Smartix usa para pre-hidratar el wizard de
/// factura. El usuario completa el wizard (caja, forma de pago, ajustes finos)
/// y emite manualmente desde la UI; Smartix manda un webhook a SmartCare al
/// cambiar el estado del DTE.
/// </summary>
public class FromSmartCareInvoiceRequestDto
{
    /// <summary>UUID generado por SmartCare. Garantiza idempotencia.</summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>URL de SmartCare que recibirá el webhook al cambiar el estado.</summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>
    /// ID de la clínica en SmartCare (para el webhook de retorno). String: SmartCare usa
    /// UUIDs, no enteros — por eso el campo es opaco para Smartix.
    /// </summary>
    public string? ClinicId { get; set; }

    /// <summary>
    /// ID de la visita en SmartCare (para el webhook de retorno). String: SmartCare usa
    /// UUIDs, no enteros — por eso el campo es opaco para Smartix.
    /// </summary>
    public string? VisitId { get; set; }

    /// <summary>ID de la Sucursal en Smartix desde donde se emite el DTE.</summary>
    public int SucursalSmartixId { get; set; }

    /// <summary>Tipo de DTE: "01" = Factura (CF), "03" = Comprobante de Crédito Fiscal (CCF).</summary>
    public string TipoDte { get; set; } = "01";

    /// <summary>Datos del receptor. Para CF se acepta sólo nombre. Para CCF requiere todos los campos fiscales.</summary>
    public FromSmartCareReceptorDto Receptor { get; set; } = new();

    /// <summary>Líneas a facturar (consulta + procedimientos + insumos).</summary>
    public List<FromSmartCareInvoiceLineDto> Lineas { get; set; } = new();

    /// <summary>Observaciones libres (opcional).</summary>
    public string? Observaciones { get; set; }

    /// <summary>
    /// Código de forma de pago sugerida (opcional). El usuario puede cambiarla en
    /// el wizard de Smartix. Si viene null, la UI usa su default ("01" Efectivo).
    /// </summary>
    public string? FormaPagoSugerida { get; set; }

    /// <summary>
    /// Plan B Hub-as-Emisor — Fase 2. Datos fiscales del Hub (Emisor) que SmartCare
    /// trae desde SmartHub al armar el prefill. Si viene populado, Smartix sincroniza
    /// el cache local del Emisor y guarda el snapshot point-in-time en FacturaPrefill.
    /// Si viene null, se usa el Emisor en BD (flujo legacy, pre-Fase 2).
    /// </summary>
    public EmisorFiscalDto? EmisorFiscal { get; set; }

    /// <summary>
    /// Plan B Hub-as-Emisor — Fase 2 (D14). Datos fiscales de la Sucursal del Hub.
    /// Si viene populado, Smartix hace lookup/create de la Sucursal Smartix por
    /// <see cref="SucursalFiscalDto.HubSucursalId"/> (auto-aprovisionar sin UI
    /// manual) y sincroniza el cache. Si viene null, se resuelve por <see cref="SucursalSmartixId"/>.
    /// </summary>
    public SucursalFiscalDto? SucursalFiscal { get; set; }
}

/// <summary>
/// Snapshot de los datos fiscales identitarios del Hub (Emisor) tal como existen
/// en SmartHub al armar el prefill. SmartCare lo trae via GET /api/internal/hubs|sucursales/{id}/fiscal-payload.
/// </summary>
public class EmisorFiscalDto
{
    public int HubId { get; set; }
    public string Nit { get; set; } = string.Empty;
    public string Nrc { get; set; } = string.Empty;
    public string NombreRazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public string CodActividadEconomica { get; set; } = string.Empty;
    public string DescActividadEconomica { get; set; } = string.Empty;
    /// <summary>
    /// Opcional: el DTE toma tipoEstablecimiento de la Sucursal, no del Emisor.
    /// SmartHub permite Hubs sin este campo (PR #14) y SmartCare lo propaga
    /// como null cuando viene vacio. La entidad Emisor.CatTipoEstablecimientoId
    /// ya es nullable.
    /// </summary>
    public string? CodTipoEstablecimiento { get; set; }
    public string CodDepartamento { get; set; } = string.Empty;
    public string CodMunicipio { get; set; } = string.Empty;
    public string DireccionComplemento { get; set; } = string.Empty;
    public string? TelefonoFiscal { get; set; }
    public string? CorreoFiscal { get; set; }
}

public class SucursalFiscalDto
{
    /// <summary>D14: identifica univocamente la Sucursal en SmartHub. Smartix usa
    /// este Id para lookup/create de TBL_Sucursales sin necesitar UI manual.</summary>
    public int HubSucursalId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string CodigoEstablecimientoMH { get; set; } = string.Empty;
    public string CodTipoEstablecimiento { get; set; } = string.Empty;
    public string CodDepartamento { get; set; } = string.Empty;
    public string CodMunicipio { get; set; } = string.Empty;
    public string DireccionComplemento { get; set; } = string.Empty;
    public string? TelefonoSucursal { get; set; }
    public string? CorreoSucursal { get; set; }
    public ContingenciaDto? Contingencia { get; set; }
}

public class ContingenciaDto
{
    public string NombreResponsable { get; set; } = string.Empty;
    public string TipoDocResponsable { get; set; } = string.Empty;
    public string NumeroDocResponsable { get; set; } = string.Empty;
}

public class FromSmartCareReceptorDto
{
    /// <summary>Tipo de documento: 36=NIT, 13=DUI, 02=Carnet Residente, 03=Pasaporte, 37=Otro.</summary>
    public string? TipoDocumento { get; set; }
    public string? NumeroDocumento { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Nrc { get; set; }
    public string? CodigoActividad { get; set; }
    public string? DescripcionActividad { get; set; }
    public int? DepartamentoId { get; set; }
    public int? MunicipioId { get; set; }
    public int? DistritoId { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }

    /// <summary>Si el receptor ya está registrado en Smartix, usar este ID (shortcut para CCF recurrentes).</summary>
    public int? ReceptorId { get; set; }
}

public class FromSmartCareInvoiceLineDto
{
    /// <summary>ID del producto/servicio en Smartix. Si está presente, se busca en catálogo y se hereda descripción/precio.</summary>
    public int? SmartixServicioId { get; set; }

    /// <summary>Descripción libre (usada para insumos sin smartixServicioId, o para sobrescribir el catálogo).</summary>
    public string Descripcion { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }

    /// <summary>Tipo de ítem: 1=Bien, 2=Servicio. Default 2 cuando hay smartixServicioId, 1 cuando no.</summary>
    public int? TipoItem { get; set; }

    /// <summary>
    /// Linkeo catálogo Smartix (2026-05-29): código del ProductoServicio resuelto en el ReadDto
    /// cuando hay <see cref="SmartixServicioId"/>. El FE lo usa para hidratar el item del wizard
    /// con su código y para preseleccionar el servicio en el dropdown. En el request lo enviado
    /// por SmartCare es opcional (no se valida).
    /// </summary>
    public string? Codigo { get; set; }

    /// <summary>
    /// Si true, <see cref="PrecioUnitario"/> ya incluye IVA y al armar el DTE Smartix
    /// debe extraer la base (precio / 1.13). Si false o null, <see cref="PrecioUnitario"/>
    /// es base sin IVA y Smartix suma IVA encima. Lo define
    /// SmartInventory.ProductoServicio.PrecioIncluyeIva — SmartCare lo propaga por línea
    /// al armar el payload. Null = comportamiento legacy (asumir base sin IVA), preserva
    /// compat con clientes SmartCare anteriores al rollout del Plan C1.
    /// </summary>
    public bool? PrecioIncluyeIva { get; set; }

    /// <summary>
    /// Régimen del impuesto: 1=Gravado, 2=Exento, 3=NoSujeto. Null = legacy
    /// (clientes SmartCare anteriores al cierre 2026-06-12 — el wizard asume
    /// Gravado por compat). El wizard Smartix usa este valor en la hidratación
    /// del prefill para inicializar <c>item.tipoVenta</c> con la opción
    /// correcta, evitando que ítems Exento/NoSujeto salgan facturados como
    /// VentaGravada por default. Feature TipoImpuesto+IVA 2026-06-09.
    /// </summary>
    public int? TipoImpuesto { get; set; }

    /// <summary>
    /// Porcentaje IVA por línea cuando <see cref="TipoImpuesto"/>=1. Null
    /// para Exento/NoSujeto o para clientes legacy (el wizard usa 13 por
    /// default). Permite que futuras tasas distintas a 13% se respeten sin
    /// cambiar contrato.
    /// </summary>
    public decimal? PorcentajeIVA { get; set; }
}

/// <summary>
/// Respuesta que Smartix devuelve a SmartCare al crear un prefill. La app cliente
/// (SmartCare) redirige al usuario a <see cref="RedirectUrl"/>, donde la UI de
/// Smartix levanta el wizard pre-cargado con los datos del prefill.
/// </summary>
public class FromSmartCarePrefillResponseDto
{
    public int PrefillId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string RedirectUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Read-DTO que Smartix Vue consume al abrir el wizard pre-hidratado.
/// Es el contenido deserializado del prefill, listo para popular el formulario.
/// </summary>
public class FromSmartCarePrefillReadDto
{
    public int Id { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    /// <summary>
    /// EmisorId resuelto al momento de crear el prefill desde la clinica de la
    /// visita en SmartCare (a traves del Hub vinculado). El wizard del FE DEBE
    /// usar este Emisor para el preview y para el payload de emision en lugar
    /// del Emisor del JWT del user, porque el user puede tener su sesion
    /// apuntando a otro Hub (multi-Hub) y la visita siempre se factura como el
    /// Emisor de su clinica de origen.
    /// </summary>
    public int EmisorId { get; set; }
    /// <summary>
    /// Snapshot point-in-time de los datos fiscales del Emisor (Hub) tal como
    /// vinieron en el payload de SmartCare cuando se creo el prefill. El wizard
    /// FE lo usa para pintar el preview cuando el JWT del user apunta a un
    /// Emisor distinto al de la clinica de la visita (multi-Hub). Si es null
    /// (prefills pre-Plan-B), el FE cae al EmisorActual del store.
    /// </summary>
    public EmisorFiscalDto? EmisorSnapshot { get; set; }
    public int SucursalSmartixId { get; set; }
    public string TipoDte { get; set; } = "01";
    public FromSmartCareReceptorDto Receptor { get; set; } = new();
    public List<FromSmartCareInvoiceLineDto> Lineas { get; set; } = new();
    public string? Observaciones { get; set; }
    public string? FormaPagoSugerida { get; set; }
    public string? ClinicId { get; set; }
    public string? VisitId { get; set; }
    public bool Consumed { get; set; }
}

/// <summary>
/// Payload que Smartix envía a SmartCare via webhook cuando cambia el estado.
/// (Reubicado desde IncomingInvoiceDtos.cs — el archivo legacy se borra en Task 12.)
/// </summary>
/// <remarks>
/// El payload es deliberadamente correlation-only: SmartCare correlaciona por
/// <see cref="CorrelationId"/> y consulta su propia source-of-truth de la visita
/// para reconstruir lineas/montos. Por eso datos por linea (incluido
/// <c>PrecioIncluyeIva</c> del Plan C1) NO viajan aqui — viven en el row de
/// SmartCare desde antes de mandarse el prefill a Smartix.
/// </remarks>
public class SmartCareWebhookPayloadDto
{
    public string CorrelationId { get; set; } = string.Empty;
    public string? VisitId { get; set; }
    public string? ClinicId { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string CodigoGeneracion { get; set; } = string.Empty;
    public string? NumeroControl { get; set; }
    public string? SelloRecibido { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ID de la factura en Smartix. Permite a SmartCare poblar
    /// <c>visits.smartix_invoice_id</c> (huerfano hasta ahora) y habilitar el
    /// fallback de polling cuando el webhook no haya alcanzado.
    /// </summary>
    public int InvoiceId { get; set; }

    /// <summary>
    /// Deep-link al frontend de Smartix que muestra la factura en la pantalla
    /// correcta según el estado (Historial / Pendientes / Contingencia).
    /// Null si no se puede construir (estado sin pantalla o NumeroControl
    /// ausente). SmartCare lo persiste y lo usa en el botón "Ver en Smartix"
    /// para evitar reabrir el wizard de emisión y crear duplicados.
    /// </summary>
    public string? ViewUrl { get; set; }
}
