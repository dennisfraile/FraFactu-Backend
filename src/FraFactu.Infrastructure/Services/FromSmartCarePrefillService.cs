using System.Text.Json;
using System.Text.RegularExpressions;
using FraFactu.Application.Common.Interfaces;
using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs;
using FraFactu.Application.DTOs.FromSmartCare;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Domain.Exceptions;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FraFactu.Infrastructure.Services;

/// <summary>
/// Servicio de prefills (borradores) SmartCare → Smartix.
///
/// El flujo es:
/// 1. SmartCare hace <c>POST /api/from-smartcare/prefills</c> con datos sueltos
///    (receptor + líneas + metadata clínica).
/// 2. Este servicio persiste un <see cref="FacturaPrefill"/> y devuelve un
///    redirect URL al frontend Vue de Smartix.
/// 3. El usuario en Smartix abre el wizard pre-cargado, ajusta lo necesario y
///    emite. Al emitir, <c>FacturasController.Create</c> llama a
///    <see cref="ConsumirAsync"/> con el header <c>X-SmartCare-Prefill-Id</c>
///    para copiar los metadatos al row de la factura recién creada.
///
/// Idempotencia: si llega un <c>CorrelationId</c> ya visto y aún no consumido,
/// se devuelve el mismo prefill sin recrear.
/// </summary>
public class FromSmartCarePrefillService : IFromSmartCarePrefillService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(1);

    // Regex MH para DUI ya formateado (8 digitos + guion + 1 digito).
    // Compilamos una vez como readonly static para eficiencia.
    private static readonly Regex DuiMhPattern = new(@"^[0-9]{8}-[0-9]{1}$", RegexOptions.Compiled);

    // Snapshot fiscal: camelCase para que sea queryable desde Postgres
    // (`SnapshotFiscalJson->'emisor'->>'nit'`) y comparable con el body que recibimos
    // del HTTP (que ya viene en camelCase por default de ASP.NET).
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ApplicationDbContext _context;
    private readonly ILogger<FromSmartCarePrefillService> _logger;
    private readonly ITelemetryService _telemetry;
    private readonly SmartCareSettings _settings;
    private readonly ISmartCareWebhookService _smartCareWebhook;

    public FromSmartCarePrefillService(
        ApplicationDbContext context,
        ILogger<FromSmartCarePrefillService> logger,
        ITelemetryService telemetry,
        IOptions<SmartCareSettings> settings,
        ISmartCareWebhookService smartCareWebhook)
    {
        _context = context;
        _logger = logger;
        _telemetry = telemetry;
        _settings = settings.Value;
        _smartCareWebhook = smartCareWebhook;
    }

    /// <inheritdoc />
    public async Task<FromSmartCarePrefillResponseDto> CrearAsync(FromSmartCareInvoiceRequestDto request)
    {
        // 1. Idempotencia por CorrelationId — solo prefills no consumidos cuentan.
        //    Va primero: si el correlation ya existe, devolvemos el prefill original
        //    sin tocar nada del request entrante (incluida validacion del DUI).
        var existente = await _context.FacturaPrefills
            .FirstOrDefaultAsync(p => p.CorrelationId == request.CorrelationId
                                      && p.ConsumedAt == null);

        if (existente != null)
        {
            _logger.LogInformation(
                "Prefill SmartCare ya existe (idempotente). PrefillId={Id} CorrelationId={Cid}.",
                existente.Id, request.CorrelationId);

            _telemetry.TrackEvent("smartix.prefill.created",
                properties: new Dictionary<string, string>
                {
                    ["correlationId"] = request.CorrelationId,
                    ["origen"] = "smartcare",
                    ["prefillId"] = existente.Id.ToString(),
                    ["resultado"] = "idempotente"
                });

            return BuildResponse(existente);
        }

        // 2. Resolver Sucursal + Emisor. Hay dos caminos:
        //    (a) Plan B Hub-as-Emisor Fase 2: payload trae `sucursalFiscal.hubSucursalId`.
        //        Hacemos lookup/create de TBL_Sucursales por ese FK y, si viene tambien
        //        `emisorFiscal`, sincronizamos los campos fiscales identitarios del Emisor.
        //    (b) Legacy: SmartCare manda `sucursalSmartixId` directo y el admin tuvo que
        //        crear la Sucursal en Smartix manualmente. Se conserva por compat.
        int sucursalSmartixId;
        int emisorId;

        if (request.SucursalFiscal != null)
        {
            (sucursalSmartixId, emisorId) = await ResolverSucursalDesdePayloadAsync(
                request.SucursalFiscal, request.EmisorFiscal);
            // Mantener el campo legacy consistente con la sucursal resuelta para que
            // los snapshots / logs / wizard del FE no muestren un id viejo del cliente.
            request.SucursalSmartixId = sucursalSmartixId;
        }
        else
        {
            var emisorIdLegacy = await _context.Sucursales
                .Where(s => s.Id == request.SucursalSmartixId)
                .Select(s => (int?)s.EmisorId)
                .FirstOrDefaultAsync();

            if (emisorIdLegacy is null)
                throw new KeyNotFoundException(
                    $"No se encontró emisor para SucursalId={request.SucursalSmartixId}.");

            sucursalSmartixId = request.SucursalSmartixId;
            emisorId = emisorIdLegacy.Value;
        }

        // 2.a Sync de Emisor: solo campos fiscales identitarios. Mantenemos intactos
        //     Mh*, MhProd*, Smtp*, Gmail*, LogoUrl, CatAmbienteDestinoId, HubId —
        //     son secretos / configuracion Smartix-only que no viajan desde el Hub.
        if (request.EmisorFiscal != null)
            await SyncEmisorDesdePayloadAsync(emisorId, request.EmisorFiscal);

        // 2.b Normalizar DUI del receptor antes del upsert/serializacion. SmartCare
        //     ya aplica formatDui en su builder, esto es defensa en profundidad
        //     ante regresiones. Si tipoDocumento codigo MH es "13" (DUI) y el
        //     NumeroDocumento no se puede reformatear a XXXXXXXX-X (no son 9
        //     digitos), lanzamos ValidationException (400 con errorCode claro)
        //     en vez de dejar pasar al MH que rechazaria con '[096] DOCUMENTO
        //     NO CUMPLE ESQUEMA JSON; Campo #/receptor/numDocumento'.
        //     Bug observado UAT 2026-05-14: Paciente Prueba 3 con national_id
        //     '093462176' (sin guion). Va despues del check de idempotencia para
        //     que un correlation ya consumido devuelva el prefill original sin
        //     re-validar el body entrante.
        if (request.Receptor?.TipoDocumento == "13"
            && !string.IsNullOrWhiteSpace(request.Receptor.NumeroDocumento))
        {
            var normalizado = NormalizarDui(request.Receptor.NumeroDocumento);
            if (normalizado is null)
            {
                throw new ValidationException(
                    "receptor.numeroDocumento",
                    $"NumeroDocumento '{request.Receptor.NumeroDocumento}' no es un DUI valido. " +
                    "Debe contener exactamente 9 digitos (formato XXXXXXXX-X). " +
                    "Revisar el campo national_id del paciente en SmartCare.");
            }
            request.Receptor.NumeroDocumento = normalizado;
        }

        // 3. Sync del Receptor en Smartix (idempotente por EmisorId+NumeroDocumento).
        //    Pedido del producto: aunque SmartCare mande receptor inline, queremos
        //    que el cliente quede persistido en Receptores para historial y para
        //    poder referenciarlo por Id en facturas futuras. Si SmartCare ya manda
        //    un receptorId valido (caso de receptor recurrente), no tocamos nada.
        var receptorId = await UpsertReceptorAsync(emisorId, request.Receptor);
        if (receptorId.HasValue)
            request.Receptor.ReceptorId = receptorId.Value;

        // 4. Serializar snapshots.
        var receptorJson = JsonSerializer.Serialize(request.Receptor);
        var lineasJson = JsonSerializer.Serialize(request.Lineas);

        // Snapshot fiscal Plan B Hub-as-Emisor — Opcion Hibrida. Persistimos el
        // payload tal cual lo enviaron desde SmartHub para auditoria histórica.
        // Al consumir el prefill (B.2), este JSON se copiara al row de Factura.
        // CamelCase para que sea queryable desde Postgres con `->>'nit'`.
        string? snapshotFiscalJson = null;
        if (request.EmisorFiscal != null || request.SucursalFiscal != null)
        {
            snapshotFiscalJson = JsonSerializer.Serialize(new
            {
                emisor = request.EmisorFiscal,
                sucursal = request.SucursalFiscal,
                capturedAt = DateTime.UtcNow
            }, SnapshotJsonOptions);
        }

        // 4. Persistir prefill.
        var prefill = new FacturaPrefill
        {
            CorrelationId = request.CorrelationId,
            EmisorId = emisorId,
            SucursalSmartixId = request.SucursalSmartixId,
            TipoDte = request.TipoDte,
            ReceptorJson = receptorJson,
            LineasJson = lineasJson,
            Observaciones = request.Observaciones,
            FormaPagoSugerida = request.FormaPagoSugerida,
            SmartCareClinicId = request.ClinicId,
            SmartCareVisitId = request.VisitId,
            SmartCareWebhookUrl = request.WebhookUrl,
            ExpiresAt = DateTime.UtcNow.Add(DefaultTtl),
            SnapshotFiscalJson = snapshotFiscalJson
        };

        _context.FacturaPrefills.Add(prefill);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Prefill SmartCare creado. PrefillId={Id} CorrelationId={Cid} ExpiresAt={Exp}.",
            prefill.Id, request.CorrelationId, prefill.ExpiresAt);

        _telemetry.TrackEvent("smartix.prefill.created",
            properties: new Dictionary<string, string>
            {
                ["correlationId"] = request.CorrelationId,
                ["origen"] = "smartcare",
                ["prefillId"] = prefill.Id.ToString(),
                ["resultado"] = "creado",
                ["tipoDte"] = request.TipoDte,
                ["clinicId"] = request.ClinicId?.ToString() ?? string.Empty,
                ["visitId"] = request.VisitId?.ToString() ?? string.Empty
            });

        return BuildResponse(prefill);
    }

    /// <inheritdoc />
    public async Task<FromSmartCarePrefillReadDto?> ObtenerAsync(int prefillId)
    {
        var prefill = await _context.FacturaPrefills
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == prefillId);

        if (prefill is null)
            return null;

        var receptor = SafeDeserialize<FromSmartCareReceptorDto>(prefill.ReceptorJson)
                       ?? new FromSmartCareReceptorDto();
        var lineas = SafeDeserialize<List<FromSmartCareInvoiceLineDto>>(prefill.LineasJson)
                     ?? new List<FromSmartCareInvoiceLineDto>();

        // Linkeo catálogo Smartix (2026-05-29): hidratar el campo Codigo de las
        // líneas que tienen SmartixServicioId, para que el wizard del FE pueda
        // mostrar el código y preseleccionar el servicio en el dropdown del paso 3.
        // Lookup batch — una sola query para todas las líneas del prefill.
        var idsCatalogo = lineas
            .Where(l => l.SmartixServicioId.HasValue && l.SmartixServicioId.Value > 0)
            .Select(l => l.SmartixServicioId!.Value)
            .Distinct()
            .ToList();
        if (idsCatalogo.Count > 0)
        {
            var codigosPorId = await _context.ProductosServicios
                .AsNoTracking()
                .Where(p => idsCatalogo.Contains(p.Id) && p.EmisorId == prefill.EmisorId)
                .Select(p => new { p.Id, p.Codigo })
                .ToDictionaryAsync(p => p.Id, p => p.Codigo);
            foreach (var linea in lineas)
            {
                if (linea.SmartixServicioId.HasValue
                    && codigosPorId.TryGetValue(linea.SmartixServicioId.Value, out var codigo))
                {
                    linea.Codigo = codigo;
                }
            }
        }

        // Snapshot fiscal: re-deserializamos el JSON para entregarle al FE los
        // datos del Emisor con los que se armo el prefill. Esto evita que el
        // wizard pinte el Emisor del JWT (que puede ser de OTRO Hub si el user
        // es multi-Hub) en lugar del Emisor de la clinica de la visita.
        EmisorFiscalDto? emisorSnapshot = null;
        if (!string.IsNullOrWhiteSpace(prefill.SnapshotFiscalJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(prefill.SnapshotFiscalJson);
                if (doc.RootElement.TryGetProperty("emisor", out var emisorEl)
                    && emisorEl.ValueKind == JsonValueKind.Object)
                {
                    emisorSnapshot = emisorEl.Deserialize<EmisorFiscalDto>(SnapshotJsonOptions);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex,
                    "[Prefill {Id}] SnapshotFiscalJson invalido al deserializar para ReadDto — se omite el snapshot.",
                    prefill.Id);
            }
        }

        return new FromSmartCarePrefillReadDto
        {
            Id = prefill.Id,
            CorrelationId = prefill.CorrelationId,
            EmisorId = prefill.EmisorId,
            EmisorSnapshot = emisorSnapshot,
            SucursalSmartixId = prefill.SucursalSmartixId,
            TipoDte = prefill.TipoDte,
            Receptor = receptor,
            Lineas = lineas,
            Observaciones = prefill.Observaciones,
            FormaPagoSugerida = prefill.FormaPagoSugerida,
            ClinicId = prefill.SmartCareClinicId,
            VisitId = prefill.SmartCareVisitId,
            Consumed = prefill.ConsumedAt.HasValue
        };
    }

    /// <inheritdoc />
    public async Task ConsumirAsync(int prefillId, int facturaId)
    {
        var prefill = await _context.FacturaPrefills.FirstOrDefaultAsync(p => p.Id == prefillId);
        if (prefill is null)
        {
            _logger.LogWarning(
                "Prefill {PrefillId} no encontrado al consumir para factura {FacturaId} — se ignora.",
                prefillId, facturaId);
            return;
        }

        if (prefill.ConsumedAt.HasValue)
        {
            _logger.LogWarning(
                "Prefill {PrefillId} ya estaba consumido (factura previa {Prev}). Nueva factura {FacturaId} ignorada para consume.",
                prefillId, prefill.ConsumedFacturaId, facturaId);
            return;
        }

        if (prefill.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning(
                "Prefill {PrefillId} expirado al consumir (ExpiresAt={Exp}) — la factura {FacturaId} igual se emitió, pero los metadatos SmartCare no se adjuntan.",
                prefillId, prefill.ExpiresAt, facturaId);
            return;
        }

        var factura = await _context.Facturas.FirstOrDefaultAsync(f => f.Id == facturaId);
        if (factura is null)
        {
            _logger.LogWarning(
                "Factura {FacturaId} no encontrada al consumir prefill {PrefillId}.",
                facturaId, prefillId);
            return;
        }

        // Copiar metadata SmartCare al row de factura.
        factura.SmartCareCorrelationId = prefill.CorrelationId;
        factura.SmartCareWebhookUrl = prefill.SmartCareWebhookUrl;
        factura.SmartCareClinicId = prefill.SmartCareClinicId;
        factura.SmartCareVisitId = prefill.SmartCareVisitId;

        // Snapshot fiscal point-in-time (Opcion Hibrida): conservamos en la factura
        // el payload que SmartHub envio al armarse el prefill. Si despues admin
        // edita el Hub, la factura mantiene el dato original para auditoria.
        factura.SnapshotFiscalJson = prefill.SnapshotFiscalJson;

        // Si el prefill genero/encontro un Receptor persistido (ver
        // UpsertReceptorAsync en CrearAsync), vincular la factura a ese
        // Receptor para que aparezca en su historial. Solo seteamos si la
        // factura aun no tiene ReceptorId (no pisamos uno asignado por el
        // wizard si el usuario eligio otro receptor en Smartix).
        if (factura.ReceptorId is null)
        {
            var receptorSnap = SafeDeserialize<FromSmartCareReceptorDto>(prefill.ReceptorJson);
            if (receptorSnap?.ReceptorId is int rid && rid > 0)
            {
                factura.ReceptorId = rid;
            }
        }

        prefill.ConsumedAt = DateTime.UtcNow;
        prefill.ConsumedFacturaId = facturaId;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Prefill {PrefillId} consumido por factura {FacturaId} (correlationId={Cid}).",
            prefillId, facturaId, prefill.CorrelationId);

        _telemetry.TrackEvent("smartix.prefill.consumed",
            properties: new Dictionary<string, string>
            {
                ["correlationId"] = prefill.CorrelationId,
                ["prefillId"] = prefill.Id.ToString(),
                ["facturaId"] = facturaId.ToString(),
                ["clinicId"] = prefill.SmartCareClinicId?.ToString() ?? string.Empty,
                ["visitId"] = prefill.SmartCareVisitId?.ToString() ?? string.Empty
            });

        // Notificar a SmartCare ahora que la factura tiene CorrelationId+WebhookUrl.
        // El chequeo equivalente en FacturaService.CreateAsync corre antes que
        // este consume, asi que sin esta llamada el webhook nunca dispara para
        // emisiones via prefill. Fire-and-forget — fallar aqui no rompe la
        // emision; en peor caso el polling de SmartCare detectara el cambio.
        //
        // Tambien notificamos para PENDIENTE_ENVIO / PENDIENTE_LOTE: cuando el
        // usuario guarda como pendiente desde el wizard, la factura nace en uno
        // de esos estados y SmartCare necesita conocer el NumeroControl + el
        // CodigoGeneracion para mostrar la card "Factura asociada" en el paso 5
        // y bloquear que se vuelva a facturar la misma visita.
        if (factura.EstadoHacienda is "PROCESADO" or "RECHAZADO"
            or "PENDIENTE_ENVIO" or "PENDIENTE_LOTE")
        {
            try
            {
                await _smartCareWebhook.NotificarCambioEstadoAsync(factura);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[SmartCare-Webhook] Falla al notificar factura {Id} a SmartCare desde ConsumirAsync (CorrelationId={Cid}, Estado={Estado}).",
                    factura.Id, prefill.CorrelationId, factura.EstadoHacienda);
            }
        }
    }

    // =========================================================================
    // Catálogo para SmartCare BillingStep
    // =========================================================================

    /// <inheritdoc />
    public async Task<CatalogoBusquedaResponseDto?> BuscarCatalogoAsync(int sucursalSmartixId, string? search, int limit = 50, string? tipo = null)
    {
        // Resolver sucursal — debe existir y estar activa.
        // Si no existe o está inactiva, devolvemos null para que el controller
        // pueda mapear a 404 con mensaje claro (mismo patrón que
        // FromSmartCareSucursalesController.ListarServicios).
        var sucursal = await _context.Sucursales
            .AsNoTracking()
            .Where(s => s.Id == sucursalSmartixId && s.Activo)
            .Select(s => new { s.Id, s.EmisorId })
            .FirstOrDefaultAsync();

        if (sucursal == null) return null;

        // Filtro de acceso por sucursal: igual que ListarServicios.
        // Se incluyen ítems con AccesoTodasSucursales=true O con fila en la
        // tabla puente ProductosServiciosSucursales para esta sucursal.
        var query = _context.ProductosServicios
            .AsNoTracking()
            .Where(p => p.EmisorId == sucursal.EmisorId && p.Activo
                        && (p.AccesoTodasSucursales
                            || _context.ProductosServiciosSucursales.Any(ps =>
                                   ps.ProductoServicioId == p.Id && ps.SucursalId == sucursalSmartixId)));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p =>
                p.Codigo.ToLower().Contains(term) ||
                p.Nombre.ToLower().Contains(term));
        }

        // Filtro opcional por tipo: "producto" → CatTipoItemId=1, "servicio" → CatTipoItemId=2.
        // Null o cualquier otro valor → sin filtro (devuelve ambos).
        if (tipo == "producto")
            query = query.Where(p => p.CatTipoItemId == 1);
        else if (tipo == "servicio")
            query = query.Where(p => p.CatTipoItemId == 2);

        var total = await query.CountAsync();

        var clampedLimit = Math.Clamp(limit, 1, 200);

        // Bug #1 IVA (2026-05-29): SmartCare consume PrecioUnitario como "precio al
        // cliente final" (con IVA incluido). Smartix almacena PrecioVenta como BASE
        // (sin IVA). Aplicamos la misma fórmula que ListarServicios: Gravado con
        // PorcentajeIVA conocido → base × (1 + pct/100); Exento/NoSujeto → base tal cual.
        // La conversión se hace IN MEMORY (ToListAsync primero) porque EF no puede
        // traducir Math.Round con MidpointRounding a SQL.
        // PrecioIncluyeIva = true para TODOS los ítems: el precio devuelto siempre
        // es el "precio al cliente final" (con IVA incluido para Gravado).
        var raw = await query
            .OrderBy(p => p.Nombre)
            .Take(clampedLimit)
            .Select(p => new
            {
                p.Id,
                p.Codigo,
                p.Nombre,
                p.PrecioVenta,
                p.TipoImpuesto,
                p.PorcentajeIVA,
                p.CatTipoItemId,
                UnidadMedidaValor = p.UnidadMedida != null ? p.UnidadMedida.Valor : null,
                // Stock por sucursal solo aplica a Bien/Producto (CatTipoItemId=1).
                // Para servicios devolvemos null (no aplica gestión de stock).
                // Para productos: SUM(CantidadDisponible) en todas las bodegas
                // asociadas a la sucursal del lookup; si no hay filas, queda 0.
                // SmartCare-FE usa este valor para validar el extra antes de
                // facturar (ver BillingStep "Productos adicionales").
                StockSucursal = p.CatTipoItemId == 1
                    ? (decimal?)_context.StocksBodega
                        .Where(sb => sb.ProductoId == p.Id
                                  && sb.Bodega.SucursalId == sucursalSmartixId)
                        .Sum(sb => sb.CantidadDisponible)
                    : null,
            })
            .ToListAsync();

        var items = raw.Select(p => new CatalogoItemDto
        {
            ProductoId = p.Id,
            Codigo = p.Codigo,
            Descripcion = p.Nombre,
            PrecioUnitario = p.TipoImpuesto == TipoImpuesto.Gravado && p.PorcentajeIVA.HasValue
                ? Math.Round(p.PrecioVenta * (1m + p.PorcentajeIVA.Value / 100m), 2, MidpointRounding.AwayFromZero)
                : p.PrecioVenta,
            PrecioIncluyeIva = true,
            UnidadMedida = p.UnidadMedidaValor,
            // CatTipoItemId=2 es Servicio; 1 es Bien/Producto
            EsServicio = p.CatTipoItemId == 2,
            StockDisponible = p.StockSucursal,
            // Régimen del impuesto: SmartCare lo propaga por línea al armar
            // el prefill para que el DTE salga con la distribución correcta
            // (VentaExenta vs VentaGravada). Sin este campo el wizard Smartix
            // asume Gravado para todo extra del catálogo. Feature TipoImpuesto+IVA
            // 2026-06-09 — cierre del flujo SC→DTE 2026-06-12.
            TipoImpuesto = (int)p.TipoImpuesto,
            PorcentajeIVA = p.TipoImpuesto == TipoImpuesto.Gravado ? p.PorcentajeIVA : null,
        }).ToList();

        return new CatalogoBusquedaResponseDto { Items = items, Total = total };
    }

    /// <inheritdoc />
    public async Task<int?> ResolverEmisorPorSucursalAsync(int sucursalSmartixId)
    {
        return await _context.Sucursales
            .AsNoTracking()
            .Where(s => s.Id == sucursalSmartixId)
            .Select(s => (int?)s.EmisorId)
            .FirstOrDefaultAsync();
    }

    // =========================================================================
    // Helpers privados
    // =========================================================================

    /// <summary>
    /// Crea o actualiza un Receptor para el emisor segun los datos del prefill.
    /// Idempotente por (EmisorId, NumeroDocumento). Si el caller ya mando un
    /// receptorId valido y el receptor existe, lo usa sin tocar nada. Si los
    /// datos no permiten identificar al receptor (sin documento o sin nombre),
    /// retorna null y el flujo cae al receptor inline original.
    /// </summary>
    private async Task<int?> UpsertReceptorAsync(int emisorId, FromSmartCareReceptorDto dto)
    {
        // 1. Atajo: caller ya conoce el ReceptorId. Validamos que pertenezca al
        //    emisor para no aceptar IDs cruzados.
        if (dto.ReceptorId.HasValue && dto.ReceptorId.Value > 0)
        {
            var existe = await _context.Receptores
                .AnyAsync(r => r.Id == dto.ReceptorId.Value && r.EmisorId == emisorId);
            if (existe) return dto.ReceptorId.Value;
        }

        // 2. Sin documento no podemos identificar al receptor para upsert.
        //    El flujo sigue con receptor inline; no es un error.
        if (string.IsNullOrWhiteSpace(dto.NumeroDocumento)) return null;
        if (string.IsNullOrWhiteSpace(dto.Nombre)) return null;

        // 3. Resolver Id del tipo de documento. El DTO trae codigo MH (36/13/02/03/37).
        var tipoDocCodigo = string.IsNullOrWhiteSpace(dto.TipoDocumento) ? "37" : dto.TipoDocumento;
        var tipoDocId = await _context.CatDocsIdentidadReceptor
            .Where(t => t.Codigo == tipoDocCodigo)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync();
        if (tipoDocId is null)
        {
            _logger.LogWarning(
                "Upsert Receptor: TipoDocumento codigo {Codigo} no existe en catalogo. EmisorId={EmisorId}. Skip.",
                tipoDocCodigo, emisorId);
            return null;
        }

        // 4. Lookup por natural key (EmisorId + NumeroDocumento). Sin filtro de
        //    Activo: si esta inactivo, lo reactivamos en el update — el user
        //    pidio que receptores se persistan al facturar.
        var existing = await _context.Receptores
            .FirstOrDefaultAsync(r => r.EmisorId == emisorId
                                       && r.NumeroDocumento == dto.NumeroDocumento);

        // Tetrada territorial "todos o ninguno" (depto + muni + distrito + direccion):
        // MH (FC v2 / CCF v4) exige que receptor.direccion viaje completa o se omita
        // entera. Persistir parciales en BD lleva al payload builder a armar un
        // receptor inconsistente que MH rechaza con [096] "direccion contiene un
        // valor invalido". Reglas:
        //  - Tetrada completa entrante → actualizar los 4 atomicamente (caso comun
        //    post-fix SC-FE tetrada).
        //  - Tetrada vacia entrante (0 de 4) → no tocar ninguno: preserva backfill
        //    aplicado en Demo y permite deploy progresivo de SC viejos que aun no
        //    incluyen los 4 campos en el payload.
        //  - Tetrada parcial (1-3 de 4) → NO deberia llegar (SC-BE valida tetrada
        //    en patientsController). Defensa: log warning y no tocar nada (mejor
        //    que dejar la BD en estado inconsistente).
        var enviaDepto = dto.DepartamentoId.HasValue;
        var enviaMuni = dto.MunicipioId.HasValue;
        var enviaDistrito = dto.DistritoId.HasValue;
        var enviaDireccion = !string.IsNullOrWhiteSpace(dto.Direccion);
        var tetradaCompleta = enviaDepto && enviaMuni && enviaDistrito && enviaDireccion;
        var tetradaVacia = !enviaDepto && !enviaMuni && !enviaDistrito && !enviaDireccion;

        if (existing != null)
        {
            existing.NombreRazonSocial = dto.Nombre;
            existing.CatTipoDocumentoIdentificacionReceptorId = tipoDocId.Value;
            existing.Nrc = dto.Nrc;
            existing.CodigoActividad = dto.CodigoActividad;
            existing.DescripcionActividad = dto.DescripcionActividad;
            if (tetradaCompleta)
            {
                existing.CatDepartamentoId = dto.DepartamentoId!.Value;
                existing.CatMunicipioId = dto.MunicipioId!.Value;
                existing.CatDistritoId = dto.DistritoId!.Value;
                existing.Direccion = dto.Direccion!;
            }
            else if (!tetradaVacia)
            {
                _logger.LogWarning(
                    "Upsert Receptor: tetrada territorial parcial entrante ignorada. " +
                    "EmisorId={EmisorId} ReceptorId={ReceptorId} depto={Depto} muni={Muni} distrito={Distrito} direccion={Direccion}",
                    emisorId, existing.Id, enviaDepto, enviaMuni, enviaDistrito, enviaDireccion);
            }
            // tetradaVacia → no-clobber: preserva lo que ya hay en BD.
            existing.CorreoElectronico = dto.Correo ?? string.Empty;
            existing.Telefono = dto.Telefono ?? string.Empty;
            existing.Activo = true;
            await _context.SaveChangesAsync();
            return existing.Id;
        }

        // Receptor nuevo: si la tetrada esta parcial, logueamos y persistimos todos
        // null/vacio para evitar inconsistencia. Si esta completa, persistimos los
        // 4. Si esta vacia, todos null/vacio (CF puro sin direccion).
        if (!tetradaCompleta && !tetradaVacia)
        {
            _logger.LogWarning(
                "Upsert Receptor (nuevo): tetrada territorial parcial entrante ignorada. " +
                "EmisorId={EmisorId} NumeroDocumento={NumeroDocumento} depto={Depto} muni={Muni} distrito={Distrito} direccion={Direccion}",
                emisorId, dto.NumeroDocumento, enviaDepto, enviaMuni, enviaDistrito, enviaDireccion);
        }
        var nuevo = new Receptor
        {
            EmisorId = emisorId,
            CatTipoDocumentoIdentificacionReceptorId = tipoDocId.Value,
            NumeroDocumento = dto.NumeroDocumento,
            NombreRazonSocial = dto.Nombre,
            Nrc = dto.Nrc,
            CodigoActividad = dto.CodigoActividad,
            DescripcionActividad = dto.DescripcionActividad,
            CatDepartamentoId = tetradaCompleta ? dto.DepartamentoId : null,
            CatMunicipioId = tetradaCompleta ? dto.MunicipioId : null,
            CatDistritoId = tetradaCompleta ? dto.DistritoId : null,
            Direccion = tetradaCompleta ? dto.Direccion! : string.Empty,
            CorreoElectronico = dto.Correo ?? string.Empty,
            Telefono = dto.Telefono ?? string.Empty,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _context.Receptores.Add(nuevo);
        await _context.SaveChangesAsync();
        return nuevo.Id;
    }

    /// <summary>
    /// Normaliza un DUI al formato <c>XXXXXXXX-X</c> exigido por el schema MH.
    /// Acepta cualquier mezcla de digitos y separadores, extrae los 9 digitos
    /// numericos y devuelve <c>XXXXXXXX-X</c>. Si no hay 9 digitos retorna null
    /// para que el caller pueda rechazar el prefill con mensaje explicito (es
    /// preferible un 422 ahora a que el MH rechace con codigo [096]).
    /// </summary>
    private static string? NormalizarDui(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        // Si ya viene formateado correctamente, devolverlo tal cual (idempotente).
        if (DuiMhPattern.IsMatch(raw)) return raw;

        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length != 9) return null;

        return $"{digits[..8]}-{digits[8]}";
    }

    private FromSmartCarePrefillResponseDto BuildResponse(FacturaPrefill prefill)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_settings.SmartixFrontendBaseUrl)
            ? "http://localhost:5180"
            : _settings.SmartixFrontendBaseUrl.TrimEnd('/');

        // 01 = Factura CF -> /emision/factura ; 03 = CCF -> /emision/ccf.
        // Cada wizard (Vue) tiene su propio componente y campos especificos.
        var rutaWizard = prefill.TipoDte == "03" ? "ccf" : "factura";

        return new FromSmartCarePrefillResponseDto
        {
            PrefillId = prefill.Id,
            CorrelationId = prefill.CorrelationId,
            RedirectUrl = $"{baseUrl}/emision/{rutaWizard}?prefillId={prefill.Id}",
            ExpiresAt = prefill.ExpiresAt
        };
    }

    /// <summary>
    /// Plan B Hub-as-Emisor (D14). Resuelve la Sucursal a partir del payload del Hub:
    /// busca por <c>HubSucursalId</c> y la crea automáticamente si no existe (con los
    /// datos del payload), evitando que admin tenga que mantener sucursales en Smartix.
    /// Si la encuentra, sincroniza sus campos fiscales con el payload.
    /// </summary>
    private async Task<(int sucursalId, int emisorId)> ResolverSucursalDesdePayloadAsync(
        SucursalFiscalDto sucursalPayload, EmisorFiscalDto? emisorPayload)
    {
        // 1. Lookup por FK al Hub.
        var sucursal = await _context.Sucursales
            .FirstOrDefaultAsync(s => s.HubSucursalId == sucursalPayload.HubSucursalId);

        if (sucursal != null)
        {
            // Sincronizar todos los campos fiscales (Sucursal de Smartix no tiene
            // campos Smartix-only; todo lo que tiene proviene del Hub).
            await SyncSucursalDesdePayloadAsync(sucursal, sucursalPayload);
            return (sucursal.Id, sucursal.EmisorId);
        }

        // 2. No existe — necesitamos un Emisor padre para crearla. La forma robusta
        //    es resolver por HubId (Emisores.HubId mapea 1:1 con SmartHub.Hub).
        if (emisorPayload is null)
        {
            throw new ValidationException(
                "sucursalFiscal",
                "Payload incluye sucursalFiscal pero no emisorFiscal: no se puede auto-crear " +
                $"la Sucursal Smartix sin un Emisor padre identificable. HubSucursalId={sucursalPayload.HubSucursalId}. " +
                "Agregar `emisorFiscal` al payload o crear la Sucursal manualmente en Smartix.");
        }

        var emisor = await _context.Emisores
            .FirstOrDefaultAsync(e => e.HubId == emisorPayload.HubId);
        if (emisor is null)
        {
            throw new ValidationException(
                "emisorFiscal.hubId",
                $"No existe Emisor en Smartix vinculado al Hub {emisorPayload.HubId}. " +
                "Vincular primero el Hub con un Emisor desde la UI admin de SmartHub.");
        }

        // 3. Resolver FKs de catalogos del payload. La Sucursal SI requiere
        //    codTipoEstablecimiento (lo usa el DTE), distinto del Emisor que lo
        //    tiene opcional. Si viene null/empty, rechazamos con mensaje accionable.
        var (catDepId, catMunId, catTipoIdOpt) = await ResolverCatalogosFiscalesAsync(
            sucursalPayload.CodDepartamento, sucursalPayload.CodMunicipio, sucursalPayload.CodTipoEstablecimiento);
        var catTipoId = catTipoIdOpt
            ?? throw new ValidationException("sucursalFiscal.codTipoEstablecimiento",
                "La Sucursal requiere codTipoEstablecimiento (el DTE lo necesita). " +
                $"HubSucursalId={sucursalPayload.HubSucursalId}. Configurar en SmartHub.");

        var nueva = new Sucursal
        {
            EmisorId = emisor.Id,
            HubSucursalId = sucursalPayload.HubSucursalId,
            Nombre = sucursalPayload.Nombre,
            // Codigo interno Smartix: usamos un sentinel `HUB-{HubSucursalId}` (unique
            // gracias al unique index filtered sobre HubSucursalId en migration de
            // PR #76). NO usar el CodigoEstablecimientoMH porque MH permite que dos
            // sucursales tengan el mismo establecimiento. Admin puede renombrarlo
            // despues en la UI de Smartix si quiere algo mas amigable como "VEN-01".
            Codigo = $"HUB-{sucursalPayload.HubSucursalId}",
            CodigoEstablecimiento = sucursalPayload.CodigoEstablecimientoMH,
            CatDepartamentoId = catDepId,
            CatMunicipioId = catMunId,
            CatTipoEstablecimientoId = catTipoId,
            Direccion = sucursalPayload.DireccionComplemento,
            Telefono = sucursalPayload.TelefonoSucursal,
            CorreoElectronico = sucursalPayload.CorreoSucursal,
            ContingenciaNombreResponsable = sucursalPayload.Contingencia?.NombreResponsable,
            ContingenciaTipoDocResponsable = sucursalPayload.Contingencia?.TipoDocResponsable,
            ContingenciaNumeroDocResponsable = sucursalPayload.Contingencia?.NumeroDocResponsable,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _context.Sucursales.Add(nueva);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "[FISCAL-SYNC] Sucursal auto-creada por payload. SucursalId={Id} HubSucursalId={Hub} EmisorId={Emisor}.",
            nueva.Id, sucursalPayload.HubSucursalId, emisor.Id);

        return (nueva.Id, emisor.Id);
    }

    /// <summary>
    /// Plan B Hub-as-Emisor — Opcion Hibrida. Refresca el cache local del Emisor con
    /// los datos identitarios del payload. Intencionalmente NO toca <c>Mh*</c>,
    /// <c>Smtp*</c>, <c>Gmail*</c>, <c>LogoUrl</c>, <c>CatAmbienteDestinoId</c>
    /// ni <c>HubId</c> — esos viven solo en Smartix.
    /// </summary>
    private async Task SyncEmisorDesdePayloadAsync(int emisorId, EmisorFiscalDto payload)
    {
        var emisor = await _context.Emisores.FirstOrDefaultAsync(e => e.Id == emisorId);
        if (emisor is null) return;

        var (catDepId, catMunId, catTipoId) = await ResolverCatalogosFiscalesAsync(
            payload.CodDepartamento, payload.CodMunicipio, payload.CodTipoEstablecimiento);

        emisor.Nit = payload.Nit;
        emisor.Nrc = payload.Nrc;
        emisor.NombreRazonSocial = payload.NombreRazonSocial;
        emisor.NombreComercial = payload.NombreComercial;
        emisor.CodigoActividad = payload.CodActividadEconomica;
        emisor.DescripcionActividad = payload.DescActividadEconomica;
        emisor.CatDepartamentoId = catDepId;
        emisor.CatMunicipioId = catMunId;
        emisor.CatTipoEstablecimientoId = catTipoId;
        emisor.Direccion = payload.DireccionComplemento;
        // Telefono / Correo del Hub son opcionales — solo pisar si vienen poblados.
        if (!string.IsNullOrWhiteSpace(payload.TelefonoFiscal))
            emisor.Telefono = payload.TelefonoFiscal;
        if (!string.IsNullOrWhiteSpace(payload.CorreoFiscal))
            emisor.CorreoElectronico = payload.CorreoFiscal;

        await _context.SaveChangesAsync();
        _logger.LogInformation(
            "[FISCAL-SYNC] Emisor refrescado desde payload. EmisorId={Id} HubId={Hub}.",
            emisor.Id, payload.HubId);
    }

    private async Task SyncSucursalDesdePayloadAsync(Sucursal sucursal, SucursalFiscalDto payload)
    {
        // La Sucursal SI requiere codTipoEstablecimiento (lo usa el DTE), distinto
        // del Emisor que lo tiene opcional — ver ResolverCatalogosFiscalesAsync.
        var (catDepId, catMunId, catTipoIdOpt) = await ResolverCatalogosFiscalesAsync(
            payload.CodDepartamento, payload.CodMunicipio, payload.CodTipoEstablecimiento);
        var catTipoId = catTipoIdOpt
            ?? throw new ValidationException("sucursalFiscal.codTipoEstablecimiento",
                "La Sucursal requiere codTipoEstablecimiento (el DTE lo necesita). " +
                $"HubSucursalId={payload.HubSucursalId}. Configurar en SmartHub.");

        sucursal.Nombre = payload.Nombre;
        sucursal.CodigoEstablecimiento = payload.CodigoEstablecimientoMH;
        sucursal.CatDepartamentoId = catDepId;
        sucursal.CatMunicipioId = catMunId;
        sucursal.CatTipoEstablecimientoId = catTipoId;
        sucursal.Direccion = payload.DireccionComplemento;
        // Telefono / Correo / Contingencia son opcionales. Solo pisar si vienen
        // poblados en el payload (consistente con SyncEmisorDesdePayloadAsync).
        // Si vienen null, conservamos lo que tenia el cache.
        if (!string.IsNullOrWhiteSpace(payload.TelefonoSucursal))
            sucursal.Telefono = payload.TelefonoSucursal;
        if (!string.IsNullOrWhiteSpace(payload.CorreoSucursal))
            sucursal.CorreoElectronico = payload.CorreoSucursal;
        if (payload.Contingencia != null)
        {
            sucursal.ContingenciaNombreResponsable = payload.Contingencia.NombreResponsable;
            sucursal.ContingenciaTipoDocResponsable = payload.Contingencia.TipoDocResponsable;
            sucursal.ContingenciaNumeroDocResponsable = payload.Contingencia.NumeroDocResponsable;
        }
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Resuelve los Ids de catalogo MH para depto/municipio/tipoEstablecimiento.
    /// codTipo es opcional: el Emisor en Smartix no lo exige (es nullable en la
    /// entidad). La Sucursal si lo necesita para el DTE — el caller debe lanzar
    /// si recibe null en ese contexto.
    /// Lanza ValidationException (no KeyNotFoundException) para que el
    /// GlobalExceptionMiddleware mapee a 400 con errorCode VALIDATION_ERROR.
    /// </summary>
    private async Task<(int catDepartamentoId, int catMunicipioId, int? catTipoEstablecimientoId)>
        ResolverCatalogosFiscalesAsync(string codDep, string codMun, string? codTipo)
    {
        var catDepId = await _context.CatDepartamentos
            .Where(d => d.Codigo == codDep)
            .Select(d => (int?)d.Id)
            .FirstOrDefaultAsync()
            ?? throw new ValidationException("fiscal.codDepartamento",
                $"Codigo de departamento '{codDep}' no existe en catalogo MH.");

        var catMunId = await _context.CatMunicipios
            .Where(m => m.CodigoDepartamento == codDep && m.Codigo == codMun)
            .Select(m => (int?)m.Id)
            .FirstOrDefaultAsync()
            ?? throw new ValidationException("fiscal.codMunicipio",
                $"Codigo de municipio '{codMun}' (depto '{codDep}') no existe en catalogo MH.");

        int? catTipoId = null;
        if (!string.IsNullOrWhiteSpace(codTipo))
        {
            catTipoId = await _context.CatTiposEstablecimiento
                .Where(t => t.Codigo == codTipo)
                .Select(t => (int?)t.Id)
                .FirstOrDefaultAsync()
                ?? throw new ValidationException("fiscal.codTipoEstablecimiento",
                    $"Codigo de tipo de establecimiento '{codTipo}' no existe en catalogo MH.");
        }

        return (catDepId, catMunId, catTipoId);
    }

    private static T? SafeDeserialize<T>(string json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<T>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task<FromSmartCareFacturaEstadoDto?> ObtenerEstadoFacturaPorCorrelationAsync(string correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId)) return null;

        var factura = await _context.Facturas
            .AsNoTracking()
            .Where(f => f.SmartCareCorrelationId == correlationId)
            .OrderByDescending(f => f.Id)
            .Select(f => new FromSmartCareFacturaEstadoDto
            {
                Id = f.Id,
                CorrelationId = f.SmartCareCorrelationId!,
                EstadoHacienda = f.EstadoHacienda ?? string.Empty,
                NumeroControl = f.NumeroControl,
                CodigoGeneracion = f.CodigoGeneracion,
                ObservacionesRechazo = f.DetalleErrorEnvio ?? f.Observaciones,
                FechaEmision = f.FechaEmision,
                FechaActualizacion = f.FechaTransmision ?? f.FechaEmision
            })
            .FirstOrDefaultAsync();

        return factura;
    }
}
