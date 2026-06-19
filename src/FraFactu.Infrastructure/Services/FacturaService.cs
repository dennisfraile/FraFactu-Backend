using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using FraFactu.Application.Common.Interfaces;
using FraFactu.Application.Services;
using FraFactu.Infrastructure.Helpers.Pagination;
using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Facturas;
using FraFactu.Application.Interfaces;
using FraFactu.Application.Interfaces.Hacienda;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Services
{
    /// <summary>
    /// Implementación del servicio de Facturas Electrónicas
    /// </summary>
    public class FacturaService : IFacturaService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IInventarioIntegrationService _inventarioService;
        private readonly IHaciendaApiService _haciendaApiService;
        private readonly IHaciendaRetryService _retryService;
        private readonly IEventoContingenciaService _contingenciaService;
        private readonly ICurrentUserService _currentUserService;
        private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<FacturaService> _logger;
        private readonly IEmailService _emailService;
        private readonly ICrossDbCorrelativoService _crossDbService;
        private readonly ICorrelativoInicialService _correlativoInicialService;
        private readonly ISaldoDteService _saldoDteService;
        private readonly ITelemetryService _telemetry;

        public FacturaService(
            ApplicationDbContext context,
            IMapper mapper,
            IInventarioIntegrationService inventarioService,
            IHaciendaApiService haciendaApiService,
            IHaciendaRetryService retryService,
            IEventoContingenciaService contingenciaService,
            ICurrentUserService currentUserService,
            Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor,
            ILogger<FacturaService> logger,
            IEmailService emailService,
            ICrossDbCorrelativoService crossDbService,
            ICorrelativoInicialService correlativoInicialService,
            ISaldoDteService saldoDteService,
            ITelemetryService telemetry)
        {
            _context = context;
            _mapper = mapper;
            _inventarioService = inventarioService;
            _haciendaApiService = haciendaApiService;
            _retryService = retryService;
            _contingenciaService = contingenciaService;
            _currentUserService = currentUserService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _emailService = emailService;
            _crossDbService = crossDbService;
            _correlativoInicialService = correlativoInicialService;
            _saldoDteService = saldoDteService;
            _telemetry = telemetry;
        }

        /// <summary>
        /// Converter que sanitiza saltos de línea en todos los valores string del DTE.
        /// La Normativa V2.0 del MH rechaza `\r`/`\n` en los valores (ND v4 lo exige por
        /// patrón `^(?!.*[\r\n])`); los reemplaza por un espacio antes de serializar.
        /// </summary>
        private sealed class SanitizadorTextoJsonConverter : JsonConverter<string>
        {
            public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => reader.GetString();

            public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
                => writer.WriteStringValue(value.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " "));
        }

        public async Task<FacturaElectronicaResponseDto> CreateAsync(CreateFacturaElectronicaDto dto, int emisorId)
        {
            // 1. Validar que la sucursal pertenece al emisor
            // NC (05) y ND (06) pueden no tener sucursal — usar la primera del emisor como fallback
            var sucursal = dto.SucursalId > 0
                ? await _context.Sucursales
                    .Include(s => s.TipoEstablecimiento)
                    .FirstOrDefaultAsync(s => s.Id == dto.SucursalId && s.EmisorId == emisorId)
                : null;

            if (sucursal == null && (dto.Identificacion?.TipoDte == "05" || dto.Identificacion?.TipoDte == "06"))
            {
                sucursal = await _context.Sucursales
                    .Include(s => s.TipoEstablecimiento)
                    .FirstOrDefaultAsync(s => s.EmisorId == emisorId);
                if (sucursal != null) dto.SucursalId = sucursal.Id;
            }

            if (sucursal == null)
                throw new InvalidOperationException("Sucursal no encontrada o no pertenece al emisor");

            // 1b. Validar que la caja existe y pertenece a la sucursal
            // NC (05) no requiere caja obligatoriamente — es un documento de ajuste, no una venta en POS
            Caja? caja = null;
            if (dto.CajaId.HasValue)
            {
                caja = await _context.Cajas
                    .FirstOrDefaultAsync(c => c.Id == dto.CajaId && c.SucursalId == dto.SucursalId);

                if (caja == null)
                    throw new InvalidOperationException("Caja no encontrada o no pertenece a la sucursal");
            }
            else if (dto.Identificacion?.TipoDte != "05" && dto.Identificacion?.TipoDte != "06")
            {
                throw new InvalidOperationException("CajaId es obligatorio para este tipo de DTE");
            }

            // 1c. Validar Vendedor (si se especifica)
            if (dto.VendedorId.HasValue)
            {
                var vendedorExista = await _context.Vendedores
                    .AnyAsync(v => v.Id == dto.VendedorId.Value && v.Activo
                        && (v.AccesoTodasSucursales
                            || v.VendedorSucursales.Any(vs => vs.SucursalId == dto.SucursalId)));

                if (!vendedorExista)
                    throw new InvalidOperationException("Vendedor no encontrado, inactivo o no pertenece a la sucursal");
            }

            // 2. Obtener el emisor completo (con AmbienteDestino para el código de ambiente)
            var emisor = await _context.Emisores
                .Include(e => e.AmbienteDestino)
                .FirstOrDefaultAsync(e => e.Id == emisorId);
            if (emisor == null)
                throw new InvalidOperationException("Emisor no encontrado");

            // 3. Generar NumeroControl y CodigoGeneracion
            var codigoGeneracion = GenerarCodigoGeneracion();
            var identificacion = dto.Identificacion!;
            var catTipoDocumentoId = ConvertirTipoDteACatalogoId(identificacion.TipoDte);
            var codPuntoVenta = caja?.CodPuntoVentaMH ?? sucursal!.CodigoEstablecimiento;
            var ambiente = emisor.AmbienteDestino?.Codigo ?? "00";
            var anioEmision = identificacion.FechaEmision.Year;
            var correlativo = await ObtenerSiguienteCorrelativoAsync(emisorId, catTipoDocumentoId, identificacion.TipoDte, sucursal.CodigoEstablecimiento, codPuntoVenta, anioEmision, ambiente);
            var numeroControl = NumeroControlHelper.Generar(identificacion.TipoDte, sucursal.TipoEstablecimiento?.Codigo, sucursal.CodigoEstablecimiento, codPuntoVenta, correlativo);

            _logger.LogDebug("[FACTURA] Generados identificadores - CodigoGeneracion: {CodigoGeneracion}, NumeroControl: {NumeroControl}, Correlativo: {Correlativo}",
                codigoGeneracion, numeroControl, correlativo);

            // 4. Crear la entidad FacturaElectronica
            var factura = new FacturaElectronica
            {
                EmisorId = emisorId,
                SucursalId = dto.SucursalId,
                CajaId = dto.CajaId,

                // Determinar versión según tipo de DTE (según schemas de Hacienda)
                Version = ObtenerVersionSegunTipoDte(identificacion.TipoDte),
                Ambiente = emisor.AmbienteDestino?.Codigo ?? "00",
                CatTipoDocumentoId = catTipoDocumentoId,
                NumeroControl = numeroControl,
                CodigoGeneracion = codigoGeneracion,
                CatModeloFacturacionId = identificacion.TipoModelo,
                CatTipoTransmisionId = identificacion.TipoOperacion,
                CatTipoContingenciaId = identificacion.TipoContingencia,
                MotivoContingencia = identificacion.MotivoContingencia,
                FechaEmision = DateTime.SpecifyKind(identificacion.FechaEmision, DateTimeKind.Utc),
                HoraEmision = TimeSpan.Parse(identificacion.HoraEmision),

                // Receptor
                ReceptorId = dto.ReceptorId ?? 0, // Se manejará después

                // Resumen de totales
                TotalNoSujeto = dto.Resumen.TotalNoSuj,
                TotalExento = dto.Resumen.TotalExenta,
                TotalGravado = dto.Resumen.TotalGravada,
                SubTotalVentas = dto.Resumen.SubTotalVentas ?? 0,
                DescuentoNoSujeto = dto.Resumen.DescuNoSuj ?? 0,
                DescuentoExento = dto.Resumen.DescuExenta ?? 0,
                DescuentoGravado = dto.Resumen.DescuGravada ?? 0,
                PorcentajeDescuento = dto.Resumen.PorcentajeDescuento ?? 0,
                TotalDescuento = dto.Resumen.TotalDescu ?? 0,
                SubTotal = dto.Resumen.SubTotal,
                TotalIva = dto.Resumen.TotalIva,
                IvaPercibido = dto.Resumen.IvaPerci1 ?? 0,
                IvaRetenido = dto.Resumen.IvaRete1 ?? 0,
                RetencionRenta = dto.Resumen.ReteRenta ?? 0,
                MontoTotalOperacion = dto.Resumen.MontoTotalOperacion ?? 0,
                TotalNoGravado = dto.Resumen.TotalNoGravado ?? 0,
                TotalPagar = dto.Resumen.TotalPagar,
                SaldoFavor = dto.Resumen.SaldoFavor ?? 0,
                TotalLetras = dto.Resumen.TotalLetras,
                NumPagoElectronico = dto.Resumen.NumPagoElectronico,
                CatCondicionOperacionId = dto.Resumen.CondicionOperacion,

                // Vendedor (opcional)
                VendedorId = dto.VendedorId,

                // Usuario (cajero) que crea la factura
                UsuarioId = _currentUserService.GetUsuarioId(),

                // Estado inicial
                EstadoHacienda = "GENERADO",
                Observaciones = dto.Observaciones,

                FechaCreacion = DateTime.UtcNow,
                Activo = true
            };

            // FSE (tipo 14): override resumen fields — remap semántico
            if (identificacion.TipoDte == "14")
            {
                factura.TotalGravado = dto.Resumen.TotalCompras ?? dto.CuerpoDocumento.Sum(i => (i.Compra ?? 0) + (i.MontoDescuento ?? 0));
                factura.DescuentoGravado = dto.Resumen.Descu ?? 0;
                factura.TotalNoSujeto = 0;
                factura.TotalExento = 0;
                factura.SubTotalVentas = 0;
                factura.DescuentoNoSujeto = 0;
                factura.DescuentoExento = 0;
                factura.PorcentajeDescuento = 0;
                factura.TotalIva = 0;
                factura.IvaPercibido = 0;
                factura.MontoTotalOperacion = 0;
                factura.TotalNoGravado = 0;
                factura.SaldoFavor = 0;
                factura.TotalLetras = FacturaCalculosHelper.ConvertirMontoALetras(factura.TotalPagar);
            }

            // 4.1 Asignar Código de Vendedor para histórico
            if (dto.VendedorId.HasValue)
            {
                var vendedor = await _context.Vendedores.FindAsync(dto.VendedorId.Value);
                if (vendedor != null)
                {
                    factura.CodigoVendedor = vendedor.Codigo;
                }
            }

            // 5. Manejar Receptor (si viene ReceptorId, usarlo; si viene Receptor nuevo, crearlo)
            if (dto.ReceptorId.HasValue)
            {
                factura.ReceptorId = dto.ReceptorId.Value;
            }
            else if (dto.Receptor != null)
            {
                // Crear receptor temporal o buscar si ya existe
                var receptorExistente = await _context.Receptores
                    .FirstOrDefaultAsync(r => r.EmisorId == emisorId &&
                                             r.NumeroDocumento == dto.Receptor.NumDocumento);

                if (receptorExistente != null)
                {
                    factura.ReceptorId = receptorExistente.Id;
                }
                else
                {
                    var nuevoReceptor = new Receptor
                    {
                        EmisorId = emisorId,
                        // Convertir código string a ID de catálogo (null si "Sin documento" en FC tipo 01).
                        CatTipoDocumentoIdentificacionReceptorId = ConvertirTipoDocumentoACatalogoId(dto.Receptor.TipoDocumento),
                        NumeroDocumento = string.IsNullOrWhiteSpace(dto.Receptor.NumDocumento) ? null : dto.Receptor.NumDocumento,
                        Nrc = dto.Receptor.Nrc,
                        NombreRazonSocial = dto.Receptor.Nombre,
                        CodigoActividad = dto.Receptor.CodActividad,
                        DescripcionActividad = dto.Receptor.DescActividad,
                        // Ubicación (convertir códigos a IDs)
                        CatDepartamentoId = ConvertirDepartamentoACatalogoId(dto.Receptor.Direccion?.Departamento),
                        CatMunicipioId = ConvertirMunicipioACatalogoId(dto.Receptor.Direccion?.Municipio),
                        CatDistritoId = await ConvertirDistritoACatalogoId(dto.Receptor.Direccion?.Departamento, dto.Receptor.Direccion?.Municipio, dto.Receptor.Direccion?.Distrito),
                        Direccion = dto.Receptor.Direccion?.Complemento ?? "",
                        Telefono = dto.Receptor.Telefono ?? "",
                        CorreoElectronico = dto.Receptor.Correo ?? "",
                        FechaCreacion = DateTime.UtcNow,
                        Activo = true
                    };

                    _context.Receptores.Add(nuevoReceptor);
                    await _context.SaveChangesAsync();
                    factura.ReceptorId = nuevoReceptor.Id;
                }
            }

            // 5.1 Validar dirección del receptor (tetrada CAT-008 + complemento) según el tipo de DTE (Normativa V2.0)
            await ValidarDireccionReceptorAsync(identificacion.TipoDte, factura.ReceptorId);

            // 6. Agregar detalles (ítems)
            foreach (var itemDto in dto.CuerpoDocumento)
            {
                // VALIDACIÓN Y AUTO-COST DE INVENTARIO
                //
                // Bug "linkeo de servicio" (2026-05-29): antes exigíamos
                // ProductoId+BodegaId juntos SIEMPRE. Eso forzaba a los items
                // que vienen del prefill SmartCare (que son Servicios, sin
                // bodega) a quedar como ad-hoc (sin productoId), perdiendo
                // trazabilidad y reportes "ventas por servicio". Ahora
                // distinguimos por CatTipoItemId:
                //  - Bien (CatTipoItemId=1): si hay ProductoId, BodegaId es
                //    requerida (descuento de stock + auto-cost).
                //  - Servicio (CatTipoItemId=2): BodegaId opcional. Si falta,
                //    se salta el auto-cost (no hay stock que consultar).
                if (itemDto.ProductoId.HasValue)
                {
                    // Validar que el producto existe y pertenece al emisor
                    var producto = await _context.ProductosServicios
                        .FirstOrDefaultAsync(p => p.Id == itemDto.ProductoId.Value && p.EmisorId == emisorId);

                    if (producto == null)
                    {
                        throw new InvalidOperationException($"Item {itemDto.NumItem}: Producto con ID {itemDto.ProductoId} no existe o no pertenece al emisor");
                    }

                    // Bienes requieren BodegaId. Servicios no.
                    if (producto.CatTipoItemId == 1 && !itemDto.BodegaId.HasValue)
                    {
                        throw new InvalidOperationException($"Item {itemDto.NumItem}: BodegaId es requerida para productos físicos (Bien)");
                    }

                    if (itemDto.BodegaId.HasValue)
                    {
                        // Validar que la bodega existe y pertenece al emisor
                        var bodega = await _context.Bodegas
                            .Include(b => b.Sucursal)
                            .FirstOrDefaultAsync(b => b.Id == itemDto.BodegaId.Value);

                        if (bodega == null || (bodega.SucursalId.HasValue && bodega.Sucursal?.EmisorId != emisorId))
                        {
                            throw new InvalidOperationException($"Item {itemDto.NumItem}: Bodega con ID {itemDto.BodegaId} no existe o no pertenece al emisor");
                        }

                        // Obtener stock y asignar CostoUnitario automáticamente
                        var stock = await _context.StocksBodega
                            .FirstOrDefaultAsync(s => s.ProductoId == itemDto.ProductoId.Value &&
                                                     s.BodegaId == itemDto.BodegaId.Value);

                        if (stock != null)
                        {
                            // Auto-assign CostoUnitario from stock
                            itemDto.CostoUnitario = stock.CostoPromedio;

                            _logger.LogDebug("[FACTURA-DETALLE] Item {NumItem}: Costo Promedio asignado = {CostoPromedio}, Stock disponible = {StockDisponible}",
                                itemDto.NumItem, stock.CostoPromedio, stock.CantidadDisponible);
                        }
                        else
                        {
                            // Fallback: usar PrecioCosto del producto cuando no existe StockBodega
                            itemDto.CostoUnitario = producto.PrecioCosto ?? 0;
                            _logger.LogWarning("[FACTURA-DETALLE] Item {NumItem}: No se encontró stock para ProductoId={ProductoId}, BodegaId={BodegaId}. Usando PrecioCosto={PrecioCosto}",
                                itemDto.NumItem, itemDto.ProductoId, itemDto.BodegaId, itemDto.CostoUnitario);
                        }
                    }
                    else
                    {
                        // Servicio sin bodega: no hay stock que consultar, el costo queda
                        // en lo que vino del DTO (típicamente null para servicios).
                        _logger.LogDebug("[FACTURA-DETALLE] Item {NumItem}: Servicio (CatTipoItemId={Tipo}) sin BodegaId; saltando auto-cost de inventario.",
                            itemDto.NumItem, producto.CatTipoItemId);
                    }
                }
                else if (itemDto.BodegaId.HasValue)
                {
                    // BodegaId sin ProductoId no tiene sentido.
                    throw new InvalidOperationException($"Item {itemDto.NumItem}: ProductoId es requerido si se especifica BodegaId");
                }

                // FSE (tipo 14): mapear Compra -> VentaGravada, sin IVA
                bool esFSE = identificacion.TipoDte == "14";

                // Plan C2 (2026-05-19, hotfix) — Recalculo defensivo DTE-aware. Sin
                // el hotfix el recalc setea VentaGravada=base para CF y rompe la
                // validacion MH `VentaGravada == PrecioUni × Cantidad - Descuento`.
                // Ahora el helper devuelve tambien PrecioUni con la forma esperada
                // por cada tipo: gross para CF (01), base para CCF (03) y otros.
                // Si el flag NO viene (clientes legacy), respeta los valores del FE.
                decimal precioUnitarioCalc = itemDto.PrecioUni;
                decimal ventaGravadaCalc = itemDto.VentaGravada;
                decimal ivaItemCalc = itemDto.IvaItem;
                if (!esFSE)
                {
                    var recalc = FacturaCalculosHelper.RecalcularGravadoSiAplica(
                        tipoDte: identificacion.TipoDte,
                        precioUni: itemDto.PrecioUni,
                        cantidad: itemDto.Cantidad,
                        montoDescuento: itemDto.MontoDescuento ?? 0,
                        ventaNoSujeta: itemDto.VentaNoSuj,
                        ventaExenta: itemDto.VentaExenta,
                        precioIncluyeIva: itemDto.PrecioIncluyeIva);
                    if (recalc.HasValue)
                    {
                        precioUnitarioCalc = recalc.Value.precioUni;
                        ventaGravadaCalc = recalc.Value.ventaGravada;
                        ivaItemCalc = recalc.Value.ivaItem;
                    }
                }

                var detalle = new FacturaElectronicaDetalle
                {
                    NumeroItem = itemDto.NumItem,
                    CatTipoItemId = itemDto.TipoItem,
                    Cantidad = itemDto.Cantidad,
                    CatUnidadMedidaId = itemDto.UniMedida,
                    CodigoProducto = itemDto.Codigo ?? "",
                    Descripcion = itemDto.Descripcion,
                    PrecioUnitario = precioUnitarioCalc,
                    MontoDescuento = itemDto.MontoDescuento ?? 0,
                    VentaNoSujeta = esFSE ? 0 : itemDto.VentaNoSuj,
                    VentaExenta = esFSE ? 0 : itemDto.VentaExenta,
                    VentaGravada = esFSE ? (itemDto.Compra ?? 0) : ventaGravadaCalc,
                    IvaItem = esFSE ? 0 : ivaItemCalc,
                    // Integración con inventario
                    ProductoId = itemDto.ProductoId,
                    BodegaId = itemDto.BodegaId,
                    CostoUnitario = itemDto.CostoUnitario ?? 0,
                    // Nuevos campos según esquema MH
                    NumeroDocumentoRelacionado = itemDto.NumeroDocumento,
                    CodTributo = itemDto.CodTributo,
                    TributosAplicados = itemDto.Tributos != null && itemDto.Tributos.Any()
                        ? string.Join(",", itemDto.Tributos)
                        : null,
                    PrecioSugeridoVenta = itemDto.Psv,
                    NoGravado = itemDto.NoGravado ?? 0,
                    FechaCreacion = DateTime.UtcNow,
                    Activo = true
                };

                factura.Detalles.Add(detalle);
            }

            // 6.1 Red de seguridad: resincronizar el resumen con los ítems recalculados (evita [020] de MH).
            ResincronizarResumenConDetalles(factura, identificacion.TipoDte);

            // 7. Agregar formas de pago
            foreach (var pagoDto in dto.Resumen.Pagos)
            {
                var pago = new Pago
                {
                    CatFormaPagoId = pagoDto.CatFormaPagoId,
                    Monto = pagoDto.Monto,
                    Referencia = pagoDto.Referencia,
                    CatPlazoId = pagoDto.CatPlazoId,
                    Periodo = pagoDto.Periodo,
                    FechaCreacion = DateTime.UtcNow,
                    Activo = true
                };

                factura.Pagos.Add(pago);
            }

            // 8. Procesar Extensión (si viene)
            if (dto.Extension != null)
            {
                var extension = new FacturaExtencion
                {
                    NombEntrega = dto.Extension.NombEntrega,
                    DocuEntrega = dto.Extension.DocuEntrega,
                    NombRecibe = dto.Extension.NombRecibe,
                    DocuRecibe = dto.Extension.DocuRecibe,
                    PlacaVehiculo = dto.Extension.PlacaVehiculo,
                    Observaciones = dto.Extension.Observaciones,
                    FechaCreacion = DateTime.UtcNow,
                    Activo = true
                };

                factura.Extension = extension;
            }

            // 9. Procesar VentaTercero (solo CCF)
            if (dto.VentaTercero != null)
            {
                var ventaTercero = new VentaTercero
                {
                    Nit = dto.VentaTercero.Nit,
                    Nombre = dto.VentaTercero.Nombre,
                    FechaCreacion = DateTime.UtcNow,
                    Activo = true
                };
                factura.VentaTercero = ventaTercero;
            }

            // 10. Procesar OtrosDocumentos (solo CCF, máximo 10)
            if (dto.OtrosDocumentos != null && dto.OtrosDocumentos.Any())
            {
                foreach (var docDto in dto.OtrosDocumentos)
                {
                    var otroDoc = new OtroDocumento
                    {
                        CodDocAsociado = docDto.CodDocAsociado,
                        DescDocumento = docDto.DescDocumento,
                        DetalleDocumento = docDto.DetalleDocumento,
                        FechaCreacion = DateTime.UtcNow,
                        Activo = true
                    };

                    // Si CodDocAsociado = 3, agregar Medico
                    if (docDto.CodDocAsociado == 3 && docDto.Medico != null)
                    {
                        otroDoc.Medico = new MedicoServicio
                        {
                            Nombre = docDto.Medico.Nombre,
                            Nit = docDto.Medico.Nit,
                            DocIdentificacion = docDto.Medico.DocIdentificacion,
                            TipoServicio = docDto.Medico.TipoServicio,
                            FechaCreacion = DateTime.UtcNow,
                            Activo = true
                        };
                    }

                    factura.OtrosDocumentos.Add(otroDoc);
                }
            }

            // 11. Procesar Tributos del Resumen (solo CCF)
            if (dto.Resumen.Tributos != null && dto.Resumen.Tributos.Any())
            {
                foreach (var tributoDto in dto.Resumen.Tributos)
                {

                    // Mapear código del tributo a ID del catálogo
                    var tributo = new FacturaTributo
                    {
                        CatTributoId = await ConvertirTributoCodigoACatalogoIdAsync(tributoDto.Codigo),
                        CodigoAttribute = tributoDto.Codigo,
                        Descripcion = tributoDto.Descripcion,
                        Valor = tributoDto.Valor,
                        FechaCreacion = DateTime.UtcNow,
                        Activo = true
                    };
                    factura.Tributos.Add(tributo);
                }
            }

            // 11.4.1 VALIDACIONES NOTA DE CRÉDITO (DTE-05) — RN-001 a RN-004
            if (identificacion.TipoDte == "05" && dto.DocumentosRelacionados != null)
            {
                foreach (var docRel in dto.DocumentosRelacionados)
                {
                    // Buscar el DTE original referenciado por su CodigoGeneracion (RN-006: usar UUID, no NumeroControl)
                    var dteOriginal = await _context.Facturas
                        .AsNoTracking()
                        .Include(f => f.TipoDocumento)
                        .FirstOrDefaultAsync(f => f.CodigoGeneracion == docRel.NumeroDocumento && f.EmisorId == emisorId);

                    if (dteOriginal == null)
                        throw new InvalidOperationException(
                            $"DTE referenciado '{docRel.NumeroDocumento}' no encontrado en la base de datos");

                    // RN-001: Según esquema MH fe-nc-v3.json, NC solo referencia CCF (03) y CR (07)
                    var tipoDteOriginal = dteOriginal.TipoDocumento?.Codigo;
                    if (tipoDteOriginal != "03" && tipoDteOriginal != "07")
                        throw new InvalidOperationException(
                            $"DTE referenciado '{docRel.NumeroDocumento}' es tipo '{tipoDteOriginal}'. NC solo permite 03 (CCF) o 07 (Comprobante de Retención) según esquema MH");

                    // RN-002: DTE original debe estar PROCESADO
                    if (dteOriginal.EstadoHacienda != "PROCESADO")
                        throw new InvalidOperationException(
                            $"DTE referenciado '{docRel.NumeroDocumento}' tiene estado '{dteOriginal.EstadoHacienda}'. Debe estar PROCESADO");

                    // RN-003: Verificar que el DTE pertenece al mismo emisor
                    if (dteOriginal.EmisorId != emisorId)
                        throw new InvalidOperationException(
                            $"DTE referenciado '{docRel.NumeroDocumento}' no pertenece al emisor actual");

                    // RN-004: Fecha emisión NC >= Fecha emisión DTE original
                    var fechaNc = identificacion.FechaEmision;
                    if (fechaNc < dteOriginal.FechaEmision)
                        throw new InvalidOperationException(
                            $"Fecha de emisión de la NC ({fechaNc:yyyy-MM-dd}) no puede ser anterior a la del DTE original ({dteOriginal.FechaEmision:yyyy-MM-dd})");

                    // RN-003: Verificar saldo disponible
                    // Auto-inicializar saldo si el DTE fue creado antes de esta feature
                    var saldoExistente = await _saldoDteService.ObtenerSaldoPorCodigoGeneracion(docRel.NumeroDocumento, emisorId);
                    if (saldoExistente == null)
                    {
                        var montoOriginal = dteOriginal.MontoTotalOperacion > 0 ? dteOriginal.MontoTotalOperacion : dteOriginal.TotalPagar;
                        await _saldoDteService.InicializarSaldoAsync(docRel.NumeroDocumento, tipoDteOriginal!, montoOriginal, emisorId);
                        _logger.LogInformation("[SALDO-DTE] Auto-inicializado saldo para DTE existente {CodigoGeneracion}: ${Monto}",
                            docRel.NumeroDocumento, montoOriginal);
                    }

                    var montoNce = dto.Resumen.MontoTotalOperacion ?? dto.Resumen.TotalPagar;
                    var saldoDisponible = await _saldoDteService.ValidarSaldoDisponible(docRel.NumeroDocumento, montoNce, emisorId);
                    if (!saldoDisponible)
                    {
                        var saldoInfo = await _saldoDteService.ObtenerSaldoPorCodigoGeneracion(docRel.NumeroDocumento, emisorId);
                        var disponible = saldoInfo?.SaldoDisponible ?? 0;
                        throw new InvalidOperationException(
                            $"Saldo insuficiente en DTE '{docRel.NumeroDocumento}'. Disponible: ${disponible:F2}, NCE solicita: ${montoNce:F2}");
                    }
                }
            }

            // 11.4.2 VALIDACIONES NOTA DE DÉBITO (DTE-06) — RN-001, RN-002, RN-007
            if (identificacion.TipoDte == "06" && dto.DocumentosRelacionados != null)
            {
                foreach (var docRel in dto.DocumentosRelacionados)
                {
                    // Buscar el DTE original referenciado por su CodigoGeneracion (UUID)
                    var dteOriginal = await _context.Facturas
                        .AsNoTracking()
                        .Include(f => f.TipoDocumento)
                        .FirstOrDefaultAsync(f => f.CodigoGeneracion == docRel.NumeroDocumento && f.EmisorId == emisorId);

                    if (dteOriginal == null)
                        throw new InvalidOperationException(
                            $"DTE referenciado '{docRel.NumeroDocumento}' no encontrado en la base de datos");

                    // RN-001: Según esquema MH fe-nd-v3.json, ND solo referencia CCF (03) y CR (07)
                    var tipoDteOriginal = dteOriginal.TipoDocumento?.Codigo;
                    if (tipoDteOriginal != "03" && tipoDteOriginal != "07")
                        throw new InvalidOperationException(
                            $"DTE referenciado '{docRel.NumeroDocumento}' es tipo '{tipoDteOriginal}'. ND solo permite 03 (CCF) o 07 (Comprobante de Retención) según esquema MH");

                    // RN-002: DTE original debe estar PROCESADO
                    if (dteOriginal.EstadoHacienda != "PROCESADO")
                        throw new InvalidOperationException(
                            $"DTE referenciado '{docRel.NumeroDocumento}' tiene estado '{dteOriginal.EstadoHacienda}'. Debe estar PROCESADO");

                    // Verificar que el DTE pertenece al mismo emisor
                    if (dteOriginal.EmisorId != emisorId)
                        throw new InvalidOperationException(
                            $"DTE referenciado '{docRel.NumeroDocumento}' no pertenece al emisor actual");

                    // RN-007: Fecha emisión ND >= Fecha emisión DTE original
                    var fechaNd = identificacion.FechaEmision;
                    if (fechaNd < dteOriginal.FechaEmision)
                        throw new InvalidOperationException(
                            $"Fecha de emisión de la ND ({fechaNd:yyyy-MM-dd}) no puede ser anterior a la del DTE original ({dteOriginal.FechaEmision:yyyy-MM-dd})");

                    // RN-004: NDE NO tiene límite de monto — no verificar saldo
                }
            }

            // 11.5. Procesar Documentos Relacionados (opcional)
            if (dto.DocumentosRelacionados != null && dto.DocumentosRelacionados.Any())
            {
                foreach (var docDto in dto.DocumentosRelacionados)
                {
                    var docRelacionado = new FacturaDocumentoRelacionado
                    {
                        CatTipoDocumentoId = ConvertirTipoDteACatalogoId(docDto.TipoDocumento),
                        CatTipoGeneracionDocumentoId = docDto.TipoGeneracion,
                        NumeroDocumento = docDto.NumeroDocumento,
                        FechaEmision = DateTime.SpecifyKind(docDto.FechaEmision, DateTimeKind.Utc),
                        FechaCreacion = DateTime.UtcNow,
                        Activo = true
                    };
                    factura.DocumentosRelacionados.Add(docRelacionado);
                }
            }

            // 11.6. Procesar Apéndices (opcional)
            if (dto.Apendices != null && dto.Apendices.Any())
            {
                foreach (var apendiceDto in dto.Apendices)
                {
                    var apendice = new FacturaApendice
                    {
                        Campo = apendiceDto.Campo,
                        Etiqueta = apendiceDto.Etiqueta,
                        Valor = apendiceDto.Valor,
                        FechaCreacion = DateTime.UtcNow,
                        Activo = true
                    };
                    factura.Apendices.Add(apendice);
                }
            }

            // 12. TRANSACCIÓN: Guardar factura + validar + descontar stock (atómico)
            using var stockTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Facturas.Add(factura);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[FACTURA] Factura creada con ID={FacturaId}, NumeroControl={NumeroControl}, TotalPagar={TotalPagar}",
                    factura.Id, factura.NumeroControl, factura.TotalPagar);

                // 13. INTEGRACIÓN CON INVENTARIO (DESCUENTO DIRECTO - dentro de la transacción)
                // Se omite la validación y descuento de stock en:
                // - Ambiente de pruebas ("00"): para permitir emitir sin inventario
                // - FSE tipo "14": el emisor es el COMPRADOR, no el vendedor — el stock no se descuenta
                //   (la entrada de inventario por compra es un flujo separado)
                var tipoDteActual = identificacion.TipoDte;
                // NC (05) y ND (06) no descuentan stock — son documentos de ajuste, no una venta
                if (factura.Ambiente != "00" && tipoDteActual != "14" && tipoDteActual != "05" && tipoDteActual != "06")
                {
                    _logger.LogDebug("[FACTURA-INVENTARIO] Iniciando validación de stock para factura {FacturaId}", factura.Id);

                    // Validar stock disponible (usa FOR UPDATE internamente)
                    var stockDisponible = await _inventarioService.ValidarStockDisponibleAsync(factura.Id);
                    if (!stockDisponible)
                    {
                        _logger.LogWarning("[FACTURA-INVENTARIO] Stock insuficiente para factura {FacturaId}", factura.Id);
                        await stockTransaction.RollbackAsync(); // Revierte todo incluyendo la factura
                        throw new InvalidOperationException("Stock insuficiente para uno o más productos de la factura");
                    }

                    _logger.LogDebug("[FACTURA-INVENTARIO] Stock disponible validado. Procediendo a descontar stock inmediatamente para factura {FacturaId}", factura.Id);

                    // Descontar stock inmediatamente (usa FOR UPDATE internamente, reutiliza la transacción)
                    await _inventarioService.DescontarStockInmediatoAsync(factura.Id);

                    _logger.LogDebug("[FACTURA-INVENTARIO] Stock descontado exitosamente para factura {FacturaId}", factura.Id);
                }
                else
                {
                    var razon = factura.Ambiente == "00" ? "Ambiente de pruebas" : $"Tipo DTE {tipoDteActual} (compra, no venta)";
                    _logger.LogInformation("[FACTURA-INVENTARIO] {Razon}: se omite validación y descuento de stock para factura {FacturaId}", razon, factura.Id);
                }

                await stockTransaction.CommitAsync();
            }
            catch (InvalidOperationException)
            {
                // Rollback explícito por si la excepción viene de DescontarStockInmediatoAsync.
                // Si ya se hizo rollback arriba (stock insuficiente), el segundo intento es ignorado.
                try { await stockTransaction.RollbackAsync(); } catch { /* ya revertida */ }
                throw;
            }
            catch
            {
                await stockTransaction.RollbackAsync();
                throw;
            }

            // 14. ENVÍO A MH y post-procesamiento (FUERA de la transacción de stock para no bloquear filas durante timeout de API externa)
            // VALIDACIÓN: Si es transmisión diferida (2) o modelo facturación diferido (2)
            // NO enviar a MH, solo guardar localmente
            if (factura.CatTipoTransmisionId == 2 || factura.CatModeloFacturacionId == 2)
            {
                _logger.LogInformation("[FACTURA-MH] Factura {NumeroControl} diferida. Se guarda como PENDIENTE_LOTE sin enviar a MH.", factura.NumeroControl);
                factura.EstadoHacienda = "PENDIENTE_LOTE";
                await _context.SaveChangesAsync();

                // PROCESO AUTOMÁTICO DE CONTINGENCIA
                if (factura.CatTipoTransmisionId == 2)
                {
                    try
                    {
                        // Validar que vengan los datos obligatorios
                        if (identificacion.TipoContingencia.HasValue && identificacion.CrearEventoAutomatico)
                        {
                            var eventoDto = new FraFactu.Application.DTOs.Contingencia.CrearEventoContingenciaDto
                            {
                                FacturaIds = new List<int> { factura.Id },
                                FechaInicioContingencia = identificacion.FechaEmision.Date,
                                FechaFinContingencia = identificacion.FechaEmision.Date,
                                HoraInicioContingencia = TimeSpan.Parse(identificacion.HoraEmision).Subtract(TimeSpan.FromMinutes(1)),
                                HoraFinContingencia = TimeSpan.Parse(identificacion.HoraEmision),
                                TipoContingencia = identificacion.TipoContingencia.Value,
                                MotivoContingencia = identificacion.MotivoContingencia ?? "Contingencia Automática",
                                NombreResponsable = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "Sistema",
                                CatTipoDocResponsableId = 3,
                                NumeroDocResponsable = emisor.Nit
                            };

                            _logger.LogInformation("Creando evento de contingencia automático para factura {Id}", factura.Id);
                            var eventoResult = await _contingenciaService.CrearEventoAsync(eventoDto, emisorId);

                            _logger.LogInformation(
                                "[FACTURA-CONTINGENCIA] Evento creado exitosamente para factura {Id}. " +
                                "EventoId={EventoId}, Estado={Estado}",
                                factura.Id, eventoResult.Id, eventoResult.EstadoHacienda);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "[FACTURA-CONTINGENCIA] Error creando evento de contingencia automático para factura {Id}. " +
                            "TipoContingencia={Tipo}. La factura fue guardada pero el evento NO se creó.",
                            factura.Id, identificacion.TipoContingencia);
                    }
                }
            }
            else
            {
                while (true)
                {
                    try
                    {
                        // Generar JSON del DTE directamente
                        var jsonDte = await GenerateJsonDteAsync(factura.Id, emisorId);

                        // Enviar JSON directamente a Hacienda
                        var respuestaMh = await _haciendaApiService.TransmitirDteJsonAsync(
                            emisorId,
                            jsonDte,
                            factura.Version,
                            factura.TipoDocumento?.Codigo ?? "01",
                            factura.CodigoGeneracion);

                        // Procesar respuesta
                        if (respuestaMh.Estado == "PROCESADO")
                        {
                            // Si MH dice PROCESADO pero no devuelve sello, marcar como RECHAZADO
                            if (string.IsNullOrEmpty(respuestaMh.SelloRecibido))
                            {
                                _logger.LogWarning("[FACTURA-MH] Factura {NumeroControl} reportada como PROCESADO por MH pero SIN sello de recepción. Se marca como RECHAZADO.",
                                    factura.NumeroControl);

                                factura.EstadoHacienda = "RECHAZADO";
                                factura.Observaciones = "MH respondió PROCESADO pero no devolvió sello de recepción";
                                await SincronizarLoteDetalleAsync(factura, factura.Observaciones);
                                await _context.SaveChangesAsync();

                                _telemetry.TrackEvent("smartix.factura.rechazada_mh",
                                    properties: new Dictionary<string, string>
                                    {
                                        ["facturaId"] = factura.Id.ToString(),
                                        ["numeroControl"] = factura.NumeroControl ?? string.Empty,
                                        ["tipoDte"] = factura.TipoDocumento?.Codigo ?? string.Empty,
                                        ["motivo"] = "sin_sello_recepcion",
                                        ["origen"] = "create"
                                    });

                                var exSinSello = new InvalidOperationException($"Factura rechazada: MH no devolvió sello de recepción");
                                exSinSello.Data["FacturaId"] = factura.Id;
                                throw exSinSello;
                            }

                            _logger.LogInformation("[FACTURA-MH] Factura {NumeroControl} APROBADA por MH. Sello: {Sello}",
                                factura.NumeroControl, respuestaMh.SelloRecibido);

                            factura.EstadoHacienda = "PROCESADO";
                            factura.SelloRecibido = respuestaMh.SelloRecibido;
                            factura.JsonFirmado = respuestaMh.DocumentoFirmado;
                            factura.FechaTransmision = DateTime.UtcNow.Date;
                            factura.HoraTransmision = DateTime.UtcNow.TimeOfDay;
                            await SincronizarLoteDetalleAsync(factura);
                            await _context.SaveChangesAsync();

                            // SALDO DTE: Inicializar saldo para DTEs que pueden recibir NC (03, 07)
                            var tiposConSaldo = new[] { "03", "07" };
                            if (tiposConSaldo.Contains(identificacion.TipoDte))
                            {
                                try
                                {
                                    var montoTotal = dto.Resumen.MontoTotalOperacion ?? dto.Resumen.TotalPagar;
                                    await _saldoDteService.InicializarSaldoAsync(
                                        factura.CodigoGeneracion, identificacion.TipoDte, montoTotal, emisorId);
                                }
                                catch (Exception exSaldo)
                                {
                                    _logger.LogWarning(exSaldo, "[SALDO-DTE] Error inicializando saldo para DTE {CodigoGeneracion}.",
                                        factura.CodigoGeneracion);
                                }
                            }

                            // SALDO DTE: Si es NC (05), actualizar saldo del DTE original
                            if (identificacion.TipoDte == "05" && dto.DocumentosRelacionados != null)
                            {
                                var montoNce = dto.Resumen.MontoTotalOperacion ?? dto.Resumen.TotalPagar;
                                foreach (var docRel in dto.DocumentosRelacionados)
                                {
                                    await _saldoDteService.ActualizarSaldoAsync(docRel.NumeroDocumento, montoNce, emisorId);
                                    _logger.LogInformation("[SALDO-DTE] Saldo actualizado para DTE {CodigoGeneracion} tras NC {NumeroControl}",
                                        docRel.NumeroDocumento, factura.NumeroControl);
                                }
                            }

                            await IntentarEnviarEmailDteAsync(factura.Id, factura.ReceptorId == 0 ? null : (int?)factura.ReceptorId);
                            break;
                        }
                        else
                        {
                            // Si es NumeroControl duplicado → regenerar y reintentar automáticamente
                            if (await RegenerarSiNumeroControlDuplicadoInternalAsync(factura, respuestaMh.CodigoMsg, respuestaMh.DescripcionMsg))
                                continue;

                            var detallesMh = new List<string>();
                            if (!string.IsNullOrEmpty(respuestaMh.DescripcionMsg))
                                detallesMh.Add(respuestaMh.DescripcionMsg);
                            if (respuestaMh.Observaciones != null && respuestaMh.Observaciones.Any())
                                detallesMh.AddRange(respuestaMh.Observaciones);
                            var observaciones = detallesMh.Any()
                                ? $"[{respuestaMh.CodigoMsg}] {string.Join("; ", detallesMh)}"
                                : "Rechazado por MH";

                            _logger.LogWarning("[FACTURA-MH] Factura {NumeroControl} RECHAZADA por MH. Motivo: {Motivo}",
                                factura.NumeroControl, observaciones);

                            factura.EstadoHacienda = "RECHAZADO";
                            factura.Observaciones = observaciones;
                            await SincronizarLoteDetalleAsync(factura, observaciones);
                            await _context.SaveChangesAsync();

                            _telemetry.TrackEvent("smartix.factura.rechazada_mh",
                                properties: new Dictionary<string, string>
                                {
                                    ["facturaId"] = factura.Id.ToString(),
                                    ["numeroControl"] = factura.NumeroControl ?? string.Empty,
                                    ["tipoDte"] = factura.TipoDocumento?.Codigo ?? string.Empty,
                                    ["motivo"] = "rechazado_mh",
                                    ["codigoMsg"] = respuestaMh.CodigoMsg ?? string.Empty,
                                    ["descripcion"] = observaciones,
                                    ["origen"] = "create"
                                });

                            var exRech = new InvalidOperationException($"Factura rechazada por MH: {observaciones}");
                            exRech.Data["FacturaId"] = factura.Id;
                            throw exRech;
                        }
                    }
                    catch (InvalidOperationException) when (factura.EstadoHacienda == "RECHAZADO")
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[FACTURA-MH] Error al comunicarse con MH para factura {NumeroControl}", factura.NumeroControl);

                        factura.EstadoHacienda = "ERROR";
                        factura.Observaciones = $"Error al enviar a MH: {ex.Message}";
                        await _context.SaveChangesAsync();

                        var exComm = new InvalidOperationException($"Error al comunicarse con Ministerio de Hacienda: {ex.Message}", ex);
                        exComm.Data["FacturaId"] = factura.Id;
                        throw exComm;
                    }
                }
            }

            // 15. Recargar factura con navigation properties para el mapeo
            var facturaParaMapeo = await _context.Facturas
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.Departamento)
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.Municipio)
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.TipoEstablecimiento)
                .Include(f => f.Receptor!)
                    .ThenInclude(r => r.TipoDocumento)
                .Include(f => f.Detalles)
                .Include(f => f.Pagos)
                .Include(f => f.DocumentosRelacionados)
                    .ThenInclude(d => d.TipoDocumento)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.TipoEstablecimiento)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.Departamento)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.Municipio)
                .Include(f => f.Caja)
                .Include(f => f.Vendedor)
                .FirstOrDefaultAsync(f => f.Id == factura.Id);

            var response = _mapper.Map<FacturaElectronicaResponseDto>(facturaParaMapeo);

            // F7: si llegamos aqui es porque MH PROCESADO con sello (los demas paths arrojan).
            _telemetry.TrackEvent("smartix.factura.emitida",
                properties: new Dictionary<string, string>
                {
                    ["facturaId"] = factura.Id.ToString(),
                    ["numeroControl"] = factura.NumeroControl ?? string.Empty,
                    ["tipoDte"] = factura.TipoDocumento?.Codigo ?? string.Empty,
                    ["selloRecepcion"] = factura.SelloRecibido ?? string.Empty,
                    ["origen"] = "create"
                },
                measurements: new Dictionary<string, double>
                {
                    ["totalPagar"] = (double)(factura.TotalPagar)
                });

            return response;
        }

        public async Task<FacturaElectronicaResponseDto?> GetByIdAsync(int id, int emisorId)
        {
            var factura = await _context.Facturas
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.Departamento)
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.Municipio)
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.TipoEstablecimiento)
                .Include(f => f.Receptor!)
                    .ThenInclude(r => r.TipoDocumento)
                .Include(f => f.Receptor!)
                    .ThenInclude(r => r.Departamento)
                .Include(f => f.Receptor!)
                    .ThenInclude(r => r.Municipio)
                .Include(f => f.Detalles)
                .Include(f => f.Pagos)
                .Include(f => f.TipoDocumento)
                .Include(f => f.TipoContingencia)
                .Include(f => f.DocumentosRelacionados)
                    .ThenInclude(d => d.TipoDocumento)
                .Include(f => f.Apendices)
                .Include(f => f.Extension)
                .Include(f => f.VentaTercero)
                .Include(f => f.OtrosDocumentos)
                    .ThenInclude(o => o.Medico)
                .Include(f => f.Vendedor)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.TipoEstablecimiento)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.Departamento)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.Municipio)
                .Include(f => f.Caja)
                .Include(f => f.Tributos)
                .FirstOrDefaultAsync(f => f.Id == id && f.EmisorId == emisorId);

            if (factura == null)
                return null;

            return _mapper.Map<FacturaElectronicaResponseDto>(factura);
        }

        public async Task<FacturaElectronicaResponseDto?> GetByCodigoGeneracionAsync(string codigoGeneracion, int emisorId)
        {
            var factura = await _context.Facturas
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.Departamento)
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.Municipio)
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.TipoEstablecimiento)
                .Include(f => f.Receptor!)
                    .ThenInclude(r => r.TipoDocumento)
                .Include(f => f.Receptor!)
                    .ThenInclude(r => r.Departamento)
                .Include(f => f.Receptor!)
                    .ThenInclude(r => r.Municipio)
                .Include(f => f.Detalles)
                .Include(f => f.Pagos)
                .Include(f => f.TipoDocumento)
                .Include(f => f.TipoContingencia)
                .Include(f => f.DocumentosRelacionados)
                    .ThenInclude(d => d.TipoDocumento)
                .Include(f => f.VentaTercero)
                .Include(f => f.OtrosDocumentos)
                    .ThenInclude(o => o.Medico)
                .Include(f => f.Vendedor)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.TipoEstablecimiento)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.Departamento)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.Municipio)
                .Include(f => f.Caja)
                .FirstOrDefaultAsync(f => f.CodigoGeneracion == codigoGeneracion && f.EmisorId == emisorId);

            if (factura == null)
                return null;

            return _mapper.Map<FacturaElectronicaResponseDto>(factura);
        }

        public async Task<PaginatedResponse<FacturaListDto>> GetAllAsync(PaginatedRequest request, int emisorId, int? sucursalId = null, string? search = null, DateTime? fechaDesde = null, DateTime? fechaHasta = null, int? vendedorId = null, int? usuarioId = null, string? tipoDte = null, string? estadoHacienda = null, List<int>? sucursalIds = null, int? catTipoTransmisionId = null, string? ambiente = null)
        {
            var query = _context.Facturas
                .AsNoTracking()
                .Where(f => f.EmisorId == emisorId);

            if (!string.IsNullOrEmpty(ambiente))
                query = query.Where(f => f.Ambiente == ambiente);

            // Filtrar por sucursal(es) si se proporciona (para roles restringidos)
            if (sucursalIds != null && sucursalIds.Any())
            {
                query = query.Where(f => f.SucursalId.HasValue && sucursalIds.Contains(f.SucursalId.Value));
            }
            else if (sucursalId.HasValue)
            {
                query = query.Where(f => f.SucursalId == sucursalId.Value);
            }

            // Filtrar por usuario (para Cajero)
            if (usuarioId.HasValue)
                query = query.Where(f => f.UsuarioId == usuarioId.Value);

            // Filtrar por tipo de DTE (código: "01" = Factura, "03" = CCF, etc.)
            if (!string.IsNullOrEmpty(tipoDte))
                query = query.Where(f => f.TipoDocumento.Codigo == tipoDte);

            // Filtrar por estado en Hacienda (PROCESADO, RECHAZADO, PENDIENTE_ENVIO, etc.)
            if (!string.IsNullOrEmpty(estadoHacienda))
                query = query.Where(f => f.EstadoHacienda == estadoHacienda);
            else if (string.IsNullOrEmpty(search) && !catTipoTransmisionId.HasValue)
                query = query.Where(f => f.EstadoHacienda != "PENDIENTE_ENVIO" && f.EstadoHacienda != "PENDIENTE_LOTE");

            // Filtrar por tipo de transmisión (1=Normal, 2=Contingencia/Diferido)
            // Si se pide diferidos (tipo 2), incluir también PENDIENTE_LOTE de transmisión normal
            // (facturas vencidas que necesitan reportarse en contingencia)
            if (catTipoTransmisionId.HasValue && catTipoTransmisionId.Value == 2)
                query = query.Where(f => f.CatTipoTransmisionId == 2 || f.EstadoHacienda == "PENDIENTE_LOTE");
            else if (catTipoTransmisionId.HasValue)
                query = query.Where(f => f.CatTipoTransmisionId == catTipoTransmisionId.Value);

            if (fechaDesde.HasValue)
            {
                var fechaDesdeUtc = DateTime.SpecifyKind(fechaDesde.Value.Date, DateTimeKind.Utc);
                query = query.Where(f => f.FechaEmision >= fechaDesdeUtc);
            }

            if (fechaHasta.HasValue)
            {
                // Agregar un día para incluir todo el día final
                var fechaHastaUtc = DateTime.SpecifyKind(fechaHasta.Value.Date.AddDays(1), DateTimeKind.Utc);
                query = query.Where(f => f.FechaEmision < fechaHastaUtc);
            }

            if (vendedorId.HasValue)
                query = query.Where(f => f.VendedorId == vendedorId.Value);

            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(f =>
                    f.NumeroControl.ToLower().Contains(searchLower) ||
                    f.CodigoGeneracion.ToLower().Contains(searchLower) ||
                    (f.Receptor != null && f.Receptor.NombreRazonSocial.ToLower().Contains(searchLower)) ||
                    (f.Receptor != null && f.Receptor.NumeroDocumento != null && f.Receptor.NumeroDocumento.ToLower().Contains(searchLower)));
            }

            query = query.OrderByDescending(f => f.FechaCreacion)
                .AsQueryable();

            var pagedData = await query
                .Select(f => new FacturaListDto
                {
                    Id = f.Id,
                    CodigoGeneracion = f.CodigoGeneracion,
                    NumeroControl = f.NumeroControl,
                    FechaEmision = f.FechaEmision,
                    HoraEmision = f.HoraEmision,
                    ReceptorNombre = f.Receptor != null ? f.Receptor.NombreRazonSocial : "Consumidor Final",
                    ReceptorNumeroDocumento = f.Receptor != null ? (f.Receptor.NumeroDocumento ?? string.Empty) : string.Empty,
                    TotalPagar = f.TotalPagar,
                    EstadoHacienda = f.EstadoHacienda,
                    FechaCreacion = f.FechaCreacion,
                    Activo = f.Activo,
                    SelloRecepcion = f.SelloRecibido,
                    FechaTransmision = f.FechaTransmision,
                    HoraTransmision = f.HoraTransmision,
                    TipoDte = f.TipoDocumento.Codigo,
                    VendedorId = f.VendedorId,
                    VendedorNombre = f.Vendedor != null ? f.Vendedor.Nombre : null,
                    VendedorCodigo = f.Vendedor != null ? f.Vendedor.Codigo : null,
                    CajaCodigo = f.Caja != null ? f.Caja.Codigo : null,
                    UsuarioId = f.UsuarioId,
                    CatTipoTransmisionId = f.CatTipoTransmisionId,
                    LoteId = f.LoteId,
                    SucursalId = f.SucursalId,
                    EventoContingenciaId = f.EventoContingenciaId,
                    Ambiente = f.Ambiente
                })
                .ToPaginatedListAsync(request.PageNumber, request.PageSize);

            return new PaginatedResponse<FacturaListDto>
            {
                Items = pagedData,
                TotalCount = pagedData.TotalCount,
                CurrentPage = pagedData.CurrentPage,
                PageSize = pagedData.PageSize,
                TotalPages = pagedData.TotalPages
            };
        }

        public async Task<List<FacturaListDto>> SearchAsync(string searchTerm, int emisorId, string? ambiente = null)
        {
            var searchLower = searchTerm.ToLower();
            var query = _context.Facturas
                .AsNoTracking()
                .Where(f => f.EmisorId == emisorId &&
                           (f.NumeroControl.ToLower().Contains(searchLower) ||
                             f.CodigoGeneracion.ToLower().Contains(searchLower) ||
                            (f.Receptor != null &&
                             (f.Receptor.NombreRazonSocial.ToLower().Contains(searchLower) ||
                              (f.Receptor.NumeroDocumento != null && f.Receptor.NumeroDocumento.ToLower().Contains(searchLower))))));

            if (!string.IsNullOrEmpty(ambiente))
                query = query.Where(f => f.Ambiente == ambiente);

            query = query
                .OrderByDescending(f => f.FechaCreacion)
                .Take(50);

            return await query
                .Select(f => new FacturaListDto
                {
                    Id = f.Id,
                    CodigoGeneracion = f.CodigoGeneracion,
                    NumeroControl = f.NumeroControl,
                    FechaEmision = f.FechaEmision,
                    HoraEmision = f.HoraEmision,
                    ReceptorNombre = f.Receptor != null ? f.Receptor.NombreRazonSocial : "Consumidor Final",
                    ReceptorNumeroDocumento = f.Receptor != null ? (f.Receptor.NumeroDocumento ?? string.Empty) : string.Empty,
                    TotalPagar = f.TotalPagar,
                    EstadoHacienda = f.EstadoHacienda,
                    FechaCreacion = f.FechaCreacion,
                    Activo = f.Activo,
                    SelloRecepcion = f.SelloRecibido,
                    FechaTransmision = f.FechaTransmision,
                    HoraTransmision = f.HoraTransmision,
                    TipoDte = f.TipoDocumento.Codigo,
                    VendedorId = f.VendedorId,
                    VendedorNombre = f.Vendedor != null ? f.Vendedor.Nombre : null,
                    VendedorCodigo = f.Vendedor != null ? f.Vendedor.Codigo : null,
                    CajaCodigo = f.Caja != null ? f.Caja.Codigo : null,
                    UsuarioId = f.UsuarioId,
                    CatTipoTransmisionId = f.CatTipoTransmisionId,
                    LoteId = f.LoteId,
                    SucursalId = f.SucursalId,
                    EventoContingenciaId = f.EventoContingenciaId,
                    Ambiente = f.Ambiente
                })
                .ToListAsync();
        }

        public async Task<string> GenerateJsonDteAsync(int facturaId, int emisorId)
        {
            var factura = await _context.Facturas
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.AmbienteDestino)
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.Departamento)
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.Municipio)
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.Distrito)
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.TipoEstablecimiento)
                .Include(f => f.Receptor!)
                    .ThenInclude(r => r.TipoDocumento)
                .Include(f => f.Receptor!)
                    .ThenInclude(r => r.Departamento)
                .Include(f => f.Receptor!)
                    .ThenInclude(r => r.Municipio)
                .Include(f => f.Receptor!)
                    .ThenInclude(r => r.Distrito)
                .Include(f => f.Detalles)
                    .ThenInclude(d => d.UnidadMedida)
                .Include(f => f.Pagos)
                    .ThenInclude(p => p.FormaPago)
                .Include(f => f.Pagos)
                    .ThenInclude(p => p.Plazo)
                .Include(f => f.CondicionOperacion)
                .Include(f => f.TipoDocumento)
                .Include(f => f.TipoContingencia)
                .Include(f => f.DocumentosRelacionados)
                    .ThenInclude(d => d.TipoDocumento)
                .Include(f => f.OtrosDocumentos)
                    .ThenInclude(o => o.Medico)
                .Include(f => f.VentaTercero)
                .Include(f => f.Extension)
                .Include(f => f.Apendices)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.TipoEstablecimiento)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.Departamento)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.Municipio)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.Distrito)
                .Include(f => f.Caja)
                .Include(f => f.Tributos)
                .FirstOrDefaultAsync(f => f.Id == facturaId && f.EmisorId == emisorId);

            if (factura == null)
                throw new InvalidOperationException("Factura no encontrada");

            if (factura.Sucursal == null)
                throw new InvalidOperationException($"La factura {facturaId} no tiene sucursal asignada");
            // NC (05) y ND (06) pueden no tener caja asignada — usar sucursal como fallback
            var tipoDteCodeCheck = factura.TipoDocumento?.Codigo;
            if (factura.Caja == null && tipoDteCodeCheck != "05" && tipoDteCodeCheck != "06")
                throw new InvalidOperationException($"La factura {facturaId} no tiene caja asignada");
            if (factura.Emisor.AmbienteDestino == null)
                throw new InvalidOperationException($"El emisor no tiene ambiente de destino configurado");

            // Usar la versión del schema oficial almacenada en la factura
            // (Normativa V2.0: FE(01)=2 fe-f-v2.json, CCF(03)=4 fe-ccf-v4.json).
            var tipoDteCode = factura.TipoDocumento?.Codigo ?? "01";
            var schemaVersion = factura.Version;

            // Obtener código de moneda
            var tipoMoneda = await _context.CatMonedas
                .Where(m => m.Id == factura.CatMonedaId)
                .Select(m => m.Codigo)
                .FirstOrDefaultAsync() ?? "USD";

            // ===================================================================
            // FSE (tipo 14): Estructura JSON completamente diferente
            // ===================================================================
            if (tipoDteCode == "14")
            {
                var fseDteJson = new
                {
                    identificacion = new
                    {
                        version = schemaVersion,
                        ambiente = factura.Emisor.AmbienteDestino?.Codigo ?? "00",
                        tipoDte = tipoDteCode,
                        numeroControl = factura.NumeroControl,
                        codigoGeneracion = factura.CodigoGeneracion,
                        tipoModelo = factura.CatModeloFacturacionId,
                        tipoOperacion = factura.CatTipoTransmisionId,
                        tipoContingencia = factura.TipoContingencia != null
                            ? int.Parse(factura.TipoContingencia.Codigo)
                            : (int?)null,
                        motivoContin = factura.MotivoContingencia,
                        fecEmi = factura.FechaEmision.ToString("yyyy-MM-dd"),
                        horEmi = factura.HoraEmision.ToString(@"hh\:mm\:ss"),
                        tipoMoneda = tipoMoneda
                    },
                    emisor = new
                    {
                        nit = factura.Emisor.Nit,
                        nrc = factura.Emisor.Nrc,
                        nombre = factura.Emisor.NombreRazonSocial,
                        codActividad = factura.Emisor.CodigoActividad,
                        descActividad = factura.Emisor.DescripcionActividad,
                        direccion = ConstruirDireccionEmisor(factura),
                        telefono = factura.Sucursal?.Telefono ?? factura.Emisor.Telefono,
                        codEstable = factura.Sucursal?.Codigo,
                        codPuntoVenta = factura.Caja?.CodPuntoVenta,
                        correo = factura.Sucursal?.CorreoElectronico ?? factura.Emisor.CorreoElectronico
                    },
                    receptor = factura.Receptor != null ? new
                    {
                        tipoDocumento = NormalizarTipoDocumentoReceptor(factura.Receptor.TipoDocumento?.Codigo, factura.Receptor.NumeroDocumento),
                        numDocumento = NormalizarNumDocumentoReceptor(factura.Receptor.TipoDocumento?.Codigo, factura.Receptor.NumeroDocumento),
                        nombre = factura.Receptor.NombreRazonSocial,
                        codActividad = factura.Receptor.CodigoActividad,
                        descActividad = factura.Receptor.DescripcionActividad,
                        // Receptor sin distrito → direccion = null (en FSE v2 la direccion es obligatoria,
                        // así que sin distrito el DTE quedará inválido a propósito).
                        direccion = ConstruirDireccionReceptor(factura.Receptor),
                        telefono = string.IsNullOrEmpty(factura.Receptor.Telefono) ? null : factura.Receptor.Telefono,
                        correo = string.IsNullOrEmpty(factura.Receptor.CorreoElectronico) ? null : factura.Receptor.CorreoElectronico
                    } : null,
                    cuerpoDocumento = factura.Detalles.Select(d => new
                    {
                        numItem = d.NumeroItem,
                        tipoItem = d.CatTipoItemId,
                        cantidad = Math.Round(d.Cantidad, 8, MidpointRounding.AwayFromZero),
                        codigo = string.IsNullOrEmpty(d.CodigoProducto) ? null : d.CodigoProducto,
                        uniMedida = d.UnidadMedida?.Codigo != null ? int.Parse(d.UnidadMedida.Codigo) : d.CatUnidadMedidaId,
                        descripcion = d.Descripcion,
                        precioUni = Math.Round(d.PrecioUnitario, 8, MidpointRounding.AwayFromZero),
                        montoDescu = Math.Round(d.MontoDescuento, 8, MidpointRounding.AwayFromZero),
                        compra = Math.Round(d.VentaGravada, 8, MidpointRounding.AwayFromZero)
                    }).ToList(),
                    resumen = new
                    {
                        totalCompra = Math.Round(factura.TotalGravado, 2, MidpointRounding.AwayFromZero),
                        descu = Math.Round(factura.DescuentoGravado, 2, MidpointRounding.AwayFromZero),
                        totalDescu = Math.Round(factura.TotalDescuento, 2, MidpointRounding.AwayFromZero),
                        subTotal = Math.Round(factura.SubTotal, 2, MidpointRounding.AwayFromZero),
                        reteRenta = Math.Round(factura.RetencionRenta, 2, MidpointRounding.AwayFromZero),
                        totalPagar = Math.Round(factura.TotalPagar, 2, MidpointRounding.AwayFromZero),
                        totalLetras = factura.TotalLetras,
                        condicionOperacion = factura.CondicionOperacion?.Codigo != null
                            ? int.Parse(factura.CondicionOperacion.Codigo)
                            : factura.CatCondicionOperacionId,
                        pagos = factura.Pagos.Select(p => new
                        {
                            codigo = p.FormaPago?.Codigo ?? p.CatFormaPagoId.ToString("D2"),
                            montoPago = p.Monto,
                            referencia = p.Referencia,
                            plazo = p.Plazo?.Codigo,
                            periodo = p.Periodo
                        }).ToList(),
                        observaciones = factura.Observaciones
                    },
                    apendice = factura.Apendices.Any() ? factura.Apendices.Select(a => new
                    {
                        campo = a.Campo,
                        etiqueta = a.Etiqueta,
                        valor = a.Valor
                    }).ToList() : null
                };

                var fseOptions = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new SanitizadorTextoJsonConverter() }
                };

                var fseJsonString = JsonSerializer.Serialize(fseDteJson, fseOptions);
                _logger.LogInformation("[DTE-JSON] Schema version: {Version}, TipoDte: {TipoDte} (FSE)", schemaVersion, tipoDteCode);

                if (!string.IsNullOrWhiteSpace(factura.JsonFirmado))
                {
                    var jsonConFirma = JsonSerializer.Deserialize<Dictionary<string, object>>(fseJsonString) ?? new Dictionary<string, object>();
                    jsonConFirma["firmaElectronica"] = factura.JsonFirmado;
                    return JsonSerializer.Serialize(jsonConFirma, new JsonSerializerOptions { WriteIndented = true });
                }

                return fseJsonString;
            }

            // ===================================================================
            // NC (tipo 05): Estructura JSON específica para Nota de Crédito
            // Schema: fe-nc-v3.json — campos diferentes a FCF/CCF
            // ===================================================================
            if (tipoDteCode == "05")
            {
                var ncDteJson = new
                {
                    identificacion = new
                    {
                        version = schemaVersion,
                        ambiente = factura.Emisor.AmbienteDestino?.Codigo ?? "00",
                        tipoDte = tipoDteCode,
                        numeroControl = factura.NumeroControl,
                        codigoGeneracion = factura.CodigoGeneracion,
                        tipoModelo = factura.CatModeloFacturacionId,
                        tipoOperacion = factura.CatTipoTransmisionId,
                        tipoContingencia = factura.TipoContingencia != null
                            ? int.Parse(factura.TipoContingencia.Codigo)
                            : (int?)null,
                        motivoContin = factura.MotivoContingencia,
                        fecEmi = factura.FechaEmision.ToString("yyyy-MM-dd"),
                        horEmi = factura.HoraEmision.ToString(@"hh\:mm\:ss"),
                        tipoMoneda = tipoMoneda,
                        fusion = (string?)null
                    },
                    documentoRelacionado = factura.DocumentosRelacionados.Any() ? factura.DocumentosRelacionados.Select(d => new
                    {
                        tipoDocumento = d.TipoDocumento?.Codigo,
                        tipoGeneracion = d.CatTipoGeneracionDocumentoId,
                        numeroDocumento = d.NumeroDocumento,
                        fechaEmision = d.FechaEmision.ToString("yyyy-MM-dd")
                    }).ToList() : null,
                    emisor = new
                    {
                        nit = factura.Emisor.Nit,
                        nrc = factura.Emisor.Nrc,
                        nombre = factura.Emisor.NombreRazonSocial,
                        codActividad = factura.Emisor.CodigoActividad,
                        descActividad = factura.Emisor.DescripcionActividad,
                        nombreComercial = factura.Emisor.NombreComercial,
                        direccion = ConstruirDireccionEmisor(factura),
                        telefono = factura.Sucursal?.Telefono ?? factura.Emisor.Telefono,
                        correo = factura.Sucursal?.CorreoElectronico ?? factura.Emisor.CorreoElectronico
                    },
                    receptor = factura.Receptor != null ? new
                    {
                        tipoDocumento = NormalizarTipoDocumentoReceptor(factura.Receptor.TipoDocumento?.Codigo, factura.Receptor.NumeroDocumento),
                        numDocumento = NormalizarNumDocumentoReceptor(factura.Receptor.TipoDocumento?.Codigo, factura.Receptor.NumeroDocumento),
                        nrc = factura.Receptor.Nrc?.Replace("-", ""),
                        nombre = factura.Receptor.NombreRazonSocial,
                        codActividad = factura.Receptor.CodigoActividad,
                        descActividad = factura.Receptor.DescripcionActividad,
                        nombreComercial = factura.Receptor.NombreRazonSocial,
                        // Receptor sin distrito → direccion = null (en NC v4 la direccion es obligatoria,
                        // así que sin distrito el DTE quedará inválido a propósito).
                        direccion = ConstruirDireccionReceptor(factura.Receptor),
                        telefono = string.IsNullOrEmpty(factura.Receptor.Telefono) ? null : factura.Receptor.Telefono,
                        correo = string.IsNullOrEmpty(factura.Receptor.CorreoElectronico) ? null : factura.Receptor.CorreoElectronico
                    } : null,
                    ventaTercero = factura.VentaTercero != null ? new
                    {
                        nit = factura.VentaTercero.Nit,
                        nombre = factura.VentaTercero.Nombre,
                        codDomiciliado = (int?)null
                    } : null,
                    // Regla 7.2 MH: el cuerpo del documento admite hasta 8 decimales.
                    cuerpoDocumento = factura.Detalles.Select(d => new
                    {
                        numItem = d.NumeroItem,
                        tipoItem = d.CatTipoItemId,
                        numeroDocumento = d.NumeroDocumentoRelacionado,
                        cantidad = Math.Round(d.Cantidad, 8, MidpointRounding.AwayFromZero),
                        codigo = string.IsNullOrEmpty(d.CodigoProducto) ? null : d.CodigoProducto,
                        codTributo = d.CodTributo,
                        uniMedida = d.UnidadMedida?.Codigo != null ? int.Parse(d.UnidadMedida.Codigo) : d.CatUnidadMedidaId,
                        descripcion = d.Descripcion,
                        precioUni = Math.Round(d.PrecioUnitario, 8, MidpointRounding.AwayFromZero),
                        montoDescu = Math.Round(d.MontoDescuento, 8, MidpointRounding.AwayFromZero),
                        ventaNoSuj = Math.Round(d.VentaNoSujeta, 8, MidpointRounding.AwayFromZero),
                        ventaExenta = Math.Round(d.VentaExenta, 8, MidpointRounding.AwayFromZero),
                        ventaGravada = Math.Round(d.VentaGravada, 8, MidpointRounding.AwayFromZero),
                        tributos = !string.IsNullOrEmpty(d.TributosAplicados)
                            ? d.TributosAplicados.Split(',').Select(t => t.Trim()).ToList()
                            : d.VentaGravada > 0
                                ? new List<string> { "20" }
                                : null,
                        noGravado = Math.Round(d.NoGravado, 8, MidpointRounding.AwayFromZero),
                        ivaPerci = 0m,
                        totalIva = Math.Round(d.IvaItem, 8, MidpointRounding.AwayFromZero),
                        ivaRete = 0m
                    }).ToList(),
                    // Regla 7.2 MH: el resumen del documento admite hasta 2 decimales.
                    resumen = new
                    {
                        totalNoSuj = Math.Round(factura.TotalNoSujeto, 2, MidpointRounding.AwayFromZero),
                        totalExenta = Math.Round(factura.TotalExento, 2, MidpointRounding.AwayFromZero),
                        totalGravada = Math.Round(factura.TotalGravado, 2, MidpointRounding.AwayFromZero),
                        subTotalVentas = Math.Round(factura.SubTotalVentas, 2, MidpointRounding.AwayFromZero),
                        totalDescu = Math.Round(factura.TotalDescuento, 2, MidpointRounding.AwayFromZero),
                        tributos = factura.Tributos.Any()
                            ? factura.Tributos.Select(t => new
                            {
                                codigo = t.CodigoAttribute,
                                descripcion = t.Descripcion,
                                valor = Math.Round(t.Valor, 2, MidpointRounding.AwayFromZero)
                            }).ToList() is var tribNc && tribNc.Any() ? tribNc : null
                            : null,
                        montoTotalOperacion = Math.Round(factura.MontoTotalOperacion, 2, MidpointRounding.AwayFromZero),
                        ivaPerci = Math.Round(factura.IvaPercibido, 2, MidpointRounding.AwayFromZero),
                        totalIva = Math.Round(factura.TotalIva, 2, MidpointRounding.AwayFromZero),
                        ivaRete = Math.Round(factura.IvaRetenido, 2, MidpointRounding.AwayFromZero),
                        totalNoGravado = Math.Round(factura.TotalNoGravado, 2, MidpointRounding.AwayFromZero),
                        totalPagar = Math.Round(factura.TotalPagar, 2, MidpointRounding.AwayFromZero),
                        totalLetras = factura.TotalLetras,
                        condicionOperacion = factura.CondicionOperacion?.Codigo != null
                            ? int.Parse(factura.CondicionOperacion.Codigo)
                            : factura.CatCondicionOperacionId,
                        observaciones = string.IsNullOrEmpty(factura.Observaciones) ? null : factura.Observaciones,
                        codigoRetencionMH = (string?)null
                    },
                    apendice = factura.Apendices.Any() ? factura.Apendices.Select(a => new
                    {
                        campo = a.Campo,
                        etiqueta = a.Etiqueta,
                        valor = a.Valor
                    }).ToList() : null
                };

                var ncOptions = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new SanitizadorTextoJsonConverter() }
                };

                var ncJsonString = JsonSerializer.Serialize(ncDteJson, ncOptions);
                _logger.LogInformation("[DTE-JSON] Schema version: {Version}, TipoDte: {TipoDte} (NC)", schemaVersion, tipoDteCode);

                if (!string.IsNullOrWhiteSpace(factura.JsonFirmado))
                {
                    var jsonConFirma = JsonSerializer.Deserialize<Dictionary<string, object>>(ncJsonString) ?? new Dictionary<string, object>();
                    jsonConFirma["firmaElectronica"] = factura.JsonFirmado;
                    return JsonSerializer.Serialize(jsonConFirma, new JsonSerializerOptions { WriteIndented = true });
                }

                return ncJsonString;
            }

            // ===================================================================
            // ND (tipo 06): Estructura JSON específica para Nota de Débito
            // Schema: fe-nd-v3.json — similar a NC pero con campos adicionales
            // ===================================================================
            if (tipoDteCode == "06")
            {
                var ndDteJson = new
                {
                    identificacion = new
                    {
                        version = schemaVersion,
                        ambiente = factura.Emisor.AmbienteDestino?.Codigo ?? "00",
                        tipoDte = tipoDteCode,
                        numeroControl = factura.NumeroControl,
                        codigoGeneracion = factura.CodigoGeneracion,
                        tipoModelo = factura.CatModeloFacturacionId,
                        tipoOperacion = factura.CatTipoTransmisionId,
                        tipoContingencia = factura.TipoContingencia != null
                            ? int.Parse(factura.TipoContingencia.Codigo)
                            : (int?)null,
                        motivoContin = factura.MotivoContingencia,
                        fecEmi = factura.FechaEmision.ToString("yyyy-MM-dd"),
                        horEmi = factura.HoraEmision.ToString(@"hh\:mm\:ss"),
                        tipoMoneda = tipoMoneda,
                        fusion = (string?)null
                    },
                    documentoRelacionado = factura.DocumentosRelacionados.Any() ? factura.DocumentosRelacionados.Select(d => new
                    {
                        tipoDocumento = d.TipoDocumento?.Codigo,
                        tipoGeneracion = d.CatTipoGeneracionDocumentoId,
                        numeroDocumento = d.NumeroDocumento,
                        fechaEmision = d.FechaEmision.ToString("yyyy-MM-dd")
                    }).ToList() : null,
                    emisor = new
                    {
                        nit = factura.Emisor.Nit,
                        nrc = factura.Emisor.Nrc,
                        nombre = factura.Emisor.NombreRazonSocial,
                        codActividad = factura.Emisor.CodigoActividad,
                        descActividad = factura.Emisor.DescripcionActividad,
                        nombreComercial = factura.Emisor.NombreComercial,
                        direccion = ConstruirDireccionEmisor(factura),
                        telefono = factura.Sucursal?.Telefono ?? factura.Emisor.Telefono,
                        correo = factura.Sucursal?.CorreoElectronico ?? factura.Emisor.CorreoElectronico
                    },
                    receptor = factura.Receptor != null ? new
                    {
                        tipoDocumento = NormalizarTipoDocumentoReceptor(factura.Receptor.TipoDocumento?.Codigo, factura.Receptor.NumeroDocumento),
                        numDocumento = NormalizarNumDocumentoReceptor(factura.Receptor.TipoDocumento?.Codigo, factura.Receptor.NumeroDocumento),
                        nrc = factura.Receptor.Nrc?.Replace("-", ""),
                        nombre = factura.Receptor.NombreRazonSocial,
                        codActividad = factura.Receptor.CodigoActividad,
                        descActividad = factura.Receptor.DescripcionActividad,
                        nombreComercial = factura.Receptor.NombreRazonSocial,
                        // Receptor sin distrito → direccion = null (en ND v4 la direccion es obligatoria,
                        // así que sin distrito el DTE quedará inválido a propósito).
                        direccion = ConstruirDireccionReceptor(factura.Receptor),
                        telefono = string.IsNullOrEmpty(factura.Receptor.Telefono) ? null : factura.Receptor.Telefono,
                        correo = string.IsNullOrEmpty(factura.Receptor.CorreoElectronico) ? null : factura.Receptor.CorreoElectronico
                    } : null,
                    ventaTercero = factura.VentaTercero != null ? new
                    {
                        nit = factura.VentaTercero.Nit,
                        nombre = factura.VentaTercero.Nombre,
                        codDomiciliado = (int?)null
                    } : (object?)null,
                    // Regla 7.2 MH: el cuerpo del documento admite hasta 8 decimales.
                    cuerpoDocumento = factura.Detalles.Select(d => new
                    {
                        numItem = d.NumeroItem,
                        tipoItem = d.CatTipoItemId,
                        numeroDocumento = d.NumeroDocumentoRelacionado,
                        cantidad = Math.Round(d.Cantidad, 8, MidpointRounding.AwayFromZero),
                        codigo = string.IsNullOrEmpty(d.CodigoProducto) ? null : d.CodigoProducto,
                        codTributo = d.CodTributo,
                        uniMedida = d.UnidadMedida?.Codigo != null ? int.Parse(d.UnidadMedida.Codigo) : d.CatUnidadMedidaId,
                        descripcion = d.Descripcion,
                        precioUni = Math.Round(d.PrecioUnitario, 8, MidpointRounding.AwayFromZero),
                        montoDescu = Math.Round(d.MontoDescuento, 8, MidpointRounding.AwayFromZero),
                        ventaNoSuj = Math.Round(d.VentaNoSujeta, 8, MidpointRounding.AwayFromZero),
                        ventaExenta = Math.Round(d.VentaExenta, 8, MidpointRounding.AwayFromZero),
                        ventaGravada = Math.Round(d.VentaGravada, 8, MidpointRounding.AwayFromZero),
                        tributos = !string.IsNullOrEmpty(d.TributosAplicados)
                            ? d.TributosAplicados.Split(',').Select(t => t.Trim()).ToList()
                            : d.VentaGravada > 0
                                ? new List<string> { "20" }
                                : null,
                        noGravado = Math.Round(d.NoGravado, 8, MidpointRounding.AwayFromZero),
                        ivaPerci = 0m,
                        totalIva = Math.Round(d.IvaItem, 8, MidpointRounding.AwayFromZero),
                        ivaRete = 0m
                    }).ToList(),
                    // Regla 7.2 MH: el resumen del documento admite hasta 2 decimales.
                    resumen = new
                    {
                        totalNoSuj = Math.Round(factura.TotalNoSujeto, 2, MidpointRounding.AwayFromZero),
                        totalExenta = Math.Round(factura.TotalExento, 2, MidpointRounding.AwayFromZero),
                        totalGravada = Math.Round(factura.TotalGravado, 2, MidpointRounding.AwayFromZero),
                        subTotalVentas = Math.Round(factura.SubTotalVentas, 2, MidpointRounding.AwayFromZero),
                        totalDescu = Math.Round(factura.TotalDescuento, 2, MidpointRounding.AwayFromZero),
                        tributos = factura.Tributos.Any()
                            ? factura.Tributos.Select(t => new
                            {
                                codigo = t.CodigoAttribute,
                                descripcion = t.Descripcion,
                                valor = Math.Round(t.Valor, 2, MidpointRounding.AwayFromZero)
                            }).ToList() is var tribNd && tribNd.Any() ? tribNd : null
                            : null,
                        ivaPerci = Math.Round(factura.IvaPercibido, 2, MidpointRounding.AwayFromZero),
                        totalIva = Math.Round(factura.TotalIva, 2, MidpointRounding.AwayFromZero),
                        ivaRete = Math.Round(factura.IvaRetenido, 2, MidpointRounding.AwayFromZero),
                        montoTotalOperacion = Math.Round(factura.MontoTotalOperacion, 2, MidpointRounding.AwayFromZero),
                        totalNoGravado = Math.Round(factura.TotalNoGravado, 2, MidpointRounding.AwayFromZero),
                        totalPagar = Math.Round(factura.TotalPagar, 2, MidpointRounding.AwayFromZero),
                        totalLetras = factura.TotalLetras,
                        condicionOperacion = factura.CondicionOperacion?.Codigo != null
                            ? int.Parse(factura.CondicionOperacion.Codigo)
                            : factura.CatCondicionOperacionId,
                        numPagoElectronico = (string?)null,
                        observaciones = string.IsNullOrEmpty(factura.Observaciones) ? null : factura.Observaciones,
                        codigoRetencionMH = (string?)null
                    },
                    apendice = factura.Apendices.Any() ? factura.Apendices.Select(a => new
                    {
                        campo = a.Campo,
                        etiqueta = a.Etiqueta,
                        valor = a.Valor
                    }).ToList() : null
                };

                var ndOptions = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new SanitizadorTextoJsonConverter() }
                };

                var ndJsonString = JsonSerializer.Serialize(ndDteJson, ndOptions);
                _logger.LogInformation("[DTE-JSON] Schema version: {Version}, TipoDte: {TipoDte} (ND)", schemaVersion, tipoDteCode);

                if (!string.IsNullOrWhiteSpace(factura.JsonFirmado))
                {
                    var jsonConFirma = JsonSerializer.Deserialize<Dictionary<string, object>>(ndJsonString) ?? new Dictionary<string, object>();
                    jsonConFirma["firmaElectronica"] = factura.JsonFirmado;
                    return JsonSerializer.Serialize(jsonConFirma, new JsonSerializerOptions { WriteIndented = true });
                }

                return ndJsonString;
            }

            // Construir objeto JSON según estándar MH (FE v2 / CCF v4).
            // IMPORTANTE: El orden de campos debe coincidir con el schema oficial de MH:
            // 1.identificacion, 2.documentoRelacionado, 3.emisor, 4.receptor,
            // 5.otrosDocumentos, 6.ventaTercero, 7.cuerpoDocumento, 8.resumen, 9.apendice
            // (V2.0 elimina la sección "extension" en FE/CCF)
            var dteJson = new
            {
                identificacion = new
                {
                    version = schemaVersion,
                    ambiente = factura.Emisor.AmbienteDestino?.Codigo ?? "00",
                    tipoDte = tipoDteCode,
                    numeroControl = factura.NumeroControl,
                    codigoGeneracion = factura.CodigoGeneracion,
                    tipoModelo = factura.CatModeloFacturacionId,
                    tipoOperacion = factura.CatTipoTransmisionId,
                    tipoContingencia = factura.TipoContingencia != null
                        ? int.Parse(factura.TipoContingencia.Codigo)
                        : (int?)null,
                    motivoContin = factura.MotivoContingencia,
                    fecEmi = factura.FechaEmision.ToString("yyyy-MM-dd"),
                    horEmi = factura.HoraEmision.ToString(@"hh\:mm\:ss"),
                    tipoMoneda = tipoMoneda
                },
                documentoRelacionado = factura.DocumentosRelacionados.Any() ? factura.DocumentosRelacionados.Select(d => new
                {
                    tipoDocumento = d.TipoDocumento?.Codigo,
                    tipoGeneracion = d.CatTipoGeneracionDocumentoId,
                    numeroDocumento = d.NumeroDocumento,
                    fechaEmision = d.FechaEmision.ToString("yyyy-MM-dd")
                }).ToList() : null,
                emisor = new
                {
                    nit = factura.Emisor.Nit,
                    nrc = factura.Emisor.Nrc,
                    nombre = factura.Emisor.NombreRazonSocial,
                    codActividad = factura.Emisor.CodigoActividad,
                    descActividad = factura.Emisor.DescripcionActividad,
                    nombreComercial = factura.Emisor.NombreComercial,
                    direccion = ConstruirDireccionEmisor(factura),
                    telefono = factura.Sucursal?.Telefono ?? factura.Emisor.Telefono,
                    correo = factura.Sucursal?.CorreoElectronico ?? factura.Emisor.CorreoElectronico,
                    codEstable = factura.Sucursal?.Codigo,
                    codPuntoVenta = factura.Caja?.CodPuntoVenta
                },
                receptor = factura.Receptor != null
                    ? (object)(tipoDteCode == "03" // CCF usa estructura diferente
                        ? new // Estructura para CCF (03)
                        {
                            nit = factura.Receptor.NumeroDocumento?.Replace("-", ""), // NIT sin guiones
                            nrc = factura.Receptor.Nrc?.Replace("-", ""),  // NRC sin guión
                            nombre = factura.Receptor.NombreRazonSocial,
                            nombreComercial = factura.Receptor.NombreRazonSocial,
                            codActividad = factura.Receptor.CodigoActividad,
                            descActividad = factura.Receptor.DescripcionActividad,
                            direccion = ConstruirDireccionReceptor(factura.Receptor),
                            telefono = string.IsNullOrEmpty(factura.Receptor.Telefono) ? null : factura.Receptor.Telefono,
                            correo = string.IsNullOrEmpty(factura.Receptor.CorreoElectronico) ? null : factura.Receptor.CorreoElectronico
                        }
                        : new // Estructura para Factura (01) y otros
                        {
                            tipoDocumento = NormalizarTipoDocumentoReceptor(factura.Receptor.TipoDocumento?.Codigo, factura.Receptor.NumeroDocumento),
                            numDocumento = NormalizarNumDocumentoReceptor(factura.Receptor.TipoDocumento?.Codigo, factura.Receptor.NumeroDocumento),
                            // NRC solo si tipoDocumento es "36" (NIT), de lo contrario null
                            nrc = factura.Receptor.TipoDocumento?.Codigo == "36"
                                ? factura.Receptor.Nrc?.Replace("-", "")
                                : null,
                            nombre = factura.Receptor.NombreRazonSocial,
                            codActividad = factura.Receptor.CodigoActividad,
                            descActividad = factura.Receptor.DescripcionActividad,
                            // Receptor sin distrito → direccion = null (el esquema FE lo permite)
                            direccion = ConstruirDireccionReceptor(factura.Receptor),
                            telefono = string.IsNullOrEmpty(factura.Receptor.Telefono) ? null : factura.Receptor.Telefono,
                            correo = string.IsNullOrEmpty(factura.Receptor.CorreoElectronico) ? null : factura.Receptor.CorreoElectronico
                        })
                    : null,
                otrosDocumentos = factura.OtrosDocumentos.Any() ? factura.OtrosDocumentos.Select(o => new
                {
                    codDocAsociado = o.CodDocAsociado,
                    descDocumento = o.DescDocumento,
                    detalleDocumento = o.DetalleDocumento,
                    medico = o.Medico != null ? new
                    {
                        nombre = o.Medico.Nombre,
                        nit = o.Medico.Nit,
                        docIdentificacion = o.Medico.DocIdentificacion,
                        tipoServicio = o.Medico.TipoServicio
                    } : null
                }).ToList() : null,
                ventaTercero = factura.VentaTercero != null ? new
                {
                    nit = factura.VentaTercero.Nit,
                    nombre = factura.VentaTercero.Nombre,
                    codDomiciliado = (int?)null
                } : null,
                cuerpoDocumento = factura.Detalles.Select(d =>
                {
                    // Campos base (comunes para todos los tipos de DTE)
                    // Regla 7.2 MH: el cuerpo del documento admite hasta 8 decimales.
                    var baseItem = new
                    {
                        numItem = d.NumeroItem,
                        tipoItem = d.CatTipoItemId,
                        numeroDocumento = d.NumeroDocumentoRelacionado,
                        cantidad = Math.Round(d.Cantidad, 8, MidpointRounding.AwayFromZero),
                        codigo = string.IsNullOrEmpty(d.CodigoProducto) ? null : d.CodigoProducto,
                        codTributo = d.CodTributo,
                        uniMedida = d.UnidadMedida?.Codigo != null ? int.Parse(d.UnidadMedida.Codigo) : d.CatUnidadMedidaId,
                        descripcion = d.Descripcion,
                        precioUni = Math.Round(d.PrecioUnitario, 8, MidpointRounding.AwayFromZero),
                        montoDescu = Math.Round(d.MontoDescuento, 8, MidpointRounding.AwayFromZero),
                        ventaNoSuj = Math.Round(d.VentaNoSujeta, 8, MidpointRounding.AwayFromZero),
                        ventaExenta = Math.Round(d.VentaExenta, 8, MidpointRounding.AwayFromZero),
                        ventaGravada = Math.Round(d.VentaGravada, 8, MidpointRounding.AwayFromZero),
                        tributos = !string.IsNullOrEmpty(d.TributosAplicados)
                            ? d.TributosAplicados.Split(',').Select(t => t.Trim()).ToList()
                            : tipoDteCode == "03" && d.VentaGravada > 0
                                ? new List<string> { "20" }  // Fallback: IVA solo para CCF (03) con venta gravada
                                : null,
                        psv = Math.Round(d.PrecioSugeridoVenta ?? 0.00m, 8, MidpointRounding.AwayFromZero),  // Hacienda rechaza null
                        noGravado = Math.Round(d.NoGravado, 8, MidpointRounding.AwayFromZero)
                    };

                    // CCF (03) NO incluye ivaItem, otros tipos SÍ
                    return tipoDteCode == "03"
                        ? (object)baseItem
                        : new
                        {
                            baseItem.numItem,
                            baseItem.tipoItem,
                            baseItem.numeroDocumento,
                            baseItem.cantidad,
                            baseItem.codigo,
                            baseItem.codTributo,
                            baseItem.uniMedida,
                            baseItem.descripcion,
                            baseItem.precioUni,
                            baseItem.montoDescu,
                            baseItem.ventaNoSuj,
                            baseItem.ventaExenta,
                            baseItem.ventaGravada,
                            baseItem.tributos,
                            baseItem.psv,
                            baseItem.noGravado,
                            ivaItem = Math.Round(d.IvaItem, 8, MidpointRounding.AwayFromZero)  // Solo para Factura (01) y otros
                        };
                }).ToList(),
                resumen = tipoDteCode == "03" // CCF usa estructura diferente
                    // Regla 7.2 MH: el resumen del documento admite hasta 2 decimales.
                    ? (object)new // Estructura para CCF (03)
                    {
                        totalNoSuj = Math.Round(factura.TotalNoSujeto, 2, MidpointRounding.AwayFromZero),
                        totalExenta = Math.Round(factura.TotalExento, 2, MidpointRounding.AwayFromZero),
                        totalGravada = Math.Round(factura.TotalGravado, 2, MidpointRounding.AwayFromZero),
                        subTotalVentas = Math.Round(factura.SubTotalVentas, 2, MidpointRounding.AwayFromZero),
                        descuNoSuj = Math.Round(factura.DescuentoNoSujeto, 2, MidpointRounding.AwayFromZero),
                        descuExenta = Math.Round(factura.DescuentoExento, 2, MidpointRounding.AwayFromZero),
                        descuGravada = Math.Round(factura.DescuentoGravado, 2, MidpointRounding.AwayFromZero),
                        porcentajeDescuento = Math.Round(factura.PorcentajeDescuento, 2, MidpointRounding.AwayFromZero),
                        totalDescu = Math.Round(factura.TotalDescuento, 2, MidpointRounding.AwayFromZero),
                        tributos = factura.Tributos.Any()
                            ? factura.Tributos
                                .Where(t => tipoDteCode != "01" || t.CodigoAttribute != "D4")
                                .Select(t => new
                                {
                                    codigo = t.CodigoAttribute,
                                    descripcion = t.Descripcion,
                                    valor = Math.Round(t.Valor, 2, MidpointRounding.AwayFromZero)
                                }).ToList() is var tribCcf && tribCcf.Any() ? tribCcf : null
                            : null,
                        subTotal = Math.Round(factura.SubTotal, 2, MidpointRounding.AwayFromZero),
                        ivaPerci = Math.Round(factura.IvaPercibido, 2, MidpointRounding.AwayFromZero),  // IVA Percibido (retención), no IVA del 13%
                        ivaRete = Math.Round(factura.IvaRetenido, 2, MidpointRounding.AwayFromZero),
                        montoTotalOperacion = Math.Round(factura.MontoTotalOperacion, 2, MidpointRounding.AwayFromZero),
                        totalNoGravado = Math.Round(factura.TotalNoGravado, 2, MidpointRounding.AwayFromZero),
                        totalPagar = Math.Round(factura.TotalPagar, 2, MidpointRounding.AwayFromZero),
                        totalLetras = factura.TotalLetras,
                        saldoFavor = Math.Round(factura.SaldoFavor, 2, MidpointRounding.AwayFromZero),
                        condicionOperacion = factura.CondicionOperacion?.Codigo != null ? int.Parse(factura.CondicionOperacion.Codigo) : factura.CatCondicionOperacionId,
                        pagos = factura.Pagos.Select(p => new
                        {
                            codigo = p.FormaPago?.Codigo ?? p.CatFormaPagoId.ToString("D2"),
                            montoPago = Math.Round(p.Monto, 2, MidpointRounding.AwayFromZero),
                            referencia = p.Referencia,
                            plazo = p.Plazo?.Codigo,
                            periodo = p.Periodo
                        }).ToList(),
                        numPagoElectronico = factura.NumPagoElectronico,
                        observaciones = string.IsNullOrEmpty(factura.Observaciones) ? null : factura.Observaciones
                    }
                    // Regla 7.2 MH: el resumen del documento admite hasta 2 decimales.
                    : new // Estructura para Factura (01) y otros
                    {
                        totalNoSuj = Math.Round(factura.TotalNoSujeto, 2, MidpointRounding.AwayFromZero),
                        totalExenta = Math.Round(factura.TotalExento, 2, MidpointRounding.AwayFromZero),
                        totalGravada = Math.Round(factura.TotalGravado, 2, MidpointRounding.AwayFromZero),
                        subTotalVentas = Math.Round(factura.SubTotalVentas, 2, MidpointRounding.AwayFromZero),
                        descuNoSuj = Math.Round(factura.DescuentoNoSujeto, 2, MidpointRounding.AwayFromZero),
                        descuExenta = Math.Round(factura.DescuentoExento, 2, MidpointRounding.AwayFromZero),
                        descuGravada = Math.Round(factura.DescuentoGravado, 2, MidpointRounding.AwayFromZero),
                        porcentajeDescuento = Math.Round(factura.PorcentajeDescuento, 2, MidpointRounding.AwayFromZero),
                        totalDescu = Math.Round(factura.TotalDescuento, 2, MidpointRounding.AwayFromZero),
                        tributos = factura.Tributos.Any()
                            ? factura.Tributos
                                .Where(t => tipoDteCode != "01" || t.CodigoAttribute != "D4")
                                .Select(t => new
                                {
                                    codigo = t.CodigoAttribute,
                                    descripcion = t.Descripcion,
                                    valor = Math.Round(t.Valor, 2, MidpointRounding.AwayFromZero)
                                }).ToList() is var tribFac && tribFac.Any() ? tribFac : null
                            : null,
                        subTotal = Math.Round(factura.SubTotal, 2, MidpointRounding.AwayFromZero),
                        ivaRete = Math.Round(factura.IvaRetenido, 2, MidpointRounding.AwayFromZero),
                        montoTotalOperacion = Math.Round(factura.MontoTotalOperacion, 2, MidpointRounding.AwayFromZero),
                        totalNoGravado = Math.Round(factura.TotalNoGravado, 2, MidpointRounding.AwayFromZero),
                        totalPagar = Math.Round(factura.TotalPagar, 2, MidpointRounding.AwayFromZero),
                        totalLetras = factura.TotalLetras,
                        totalIva = Math.Round(factura.TotalIva, 2, MidpointRounding.AwayFromZero),  // Factura y otros usan totalIva
                        saldoFavor = Math.Round(factura.SaldoFavor, 2, MidpointRounding.AwayFromZero),
                        condicionOperacion = factura.CondicionOperacion?.Codigo != null ? int.Parse(factura.CondicionOperacion.Codigo) : factura.CatCondicionOperacionId,
                        pagos = factura.Pagos.Select(p => new
                        {
                            codigo = p.FormaPago?.Codigo ?? p.CatFormaPagoId.ToString("D2"),
                            montoPago = Math.Round(p.Monto, 2, MidpointRounding.AwayFromZero),
                            referencia = p.Referencia,
                            plazo = p.Plazo?.Codigo,
                            periodo = p.Periodo
                        }).ToList(),
                        numPagoElectronico = factura.NumPagoElectronico,
                        observaciones = string.IsNullOrEmpty(factura.Observaciones) ? null : factura.Observaciones
                    },
                apendice = factura.Apendices.Any() ? factura.Apendices.Select(a => new
                {
                    campo = a.Campo,
                    etiqueta = a.Etiqueta,
                    valor = a.Valor
                }).ToList() : null
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                // NO usar WhenWritingNull - MH requiere que campos estén presentes con null
                Converters = { new SanitizadorTextoJsonConverter() }
            };

            var jsonString = JsonSerializer.Serialize(dteJson, options);

            _logger.LogInformation("[DTE-JSON] Schema version: {Version}, TipoDte: {TipoDte}", schemaVersion, tipoDteCode);

            // Si la factura ya fue firmada, incluir la firma electrónica en el JSON
            if (!string.IsNullOrWhiteSpace(factura.JsonFirmado))
            {
                var jsonConFirma = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString) ?? new Dictionary<string, object>();
                jsonConFirma["firmaElectronica"] = factura.JsonFirmado;
                return JsonSerializer.Serialize(jsonConFirma, new JsonSerializerOptions { WriteIndented = true });
            }

            return jsonString;
        }

        public async Task<bool> AnularAsync(int facturaId, int emisorId, string motivo)
        {
            var factura = await _context.Facturas
                .FirstOrDefaultAsync(f => f.Id == facturaId && f.EmisorId == emisorId);

            if (factura == null)
                return false;

            factura.EstadoHacienda = "ANULADO";
            factura.Observaciones = $"ANULADO: {motivo}";
            factura.Activo = false;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ActualizarEstadoHaciendaAsync(int facturaId, int emisorId, string estado, string? selloRecepcion = null)
        {
            var factura = await _context.Facturas
                .FirstOrDefaultAsync(f => f.Id == facturaId && f.EmisorId == emisorId);

            if (factura == null)
                return false;

            var estadoAnterior = factura.EstadoHacienda;
            factura.EstadoHacienda = estado;
            if (selloRecepcion != null)
                factura.SelloRecibido = selloRecepcion;

            await _context.SaveChangesAsync();

            // Inicializar saldo cuando un DTE pasa a PROCESADO (para soporte de NC futuras)
            if (estado == "PROCESADO" && estadoAnterior != "PROCESADO")
            {
                var tiposConSaldo = new[] { "01", "03", "14" };
                var tipoDteCodigo = factura.TipoDocumento?.Codigo
                    ?? (await _context.Facturas.Include(f => f.TipoDocumento)
                        .FirstAsync(f => f.Id == facturaId)).TipoDocumento?.Codigo;

                if (tipoDteCodigo != null && tiposConSaldo.Contains(tipoDteCodigo))
                {
                    try
                    {
                        var montoTotal = factura.MontoTotalOperacion > 0 ? factura.MontoTotalOperacion : factura.TotalPagar;
                        await _saldoDteService.InicializarSaldoAsync(
                            factura.CodigoGeneracion, tipoDteCodigo, montoTotal, emisorId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[SALDO-DTE] Error inicializando saldo en ActualizarEstado para DTE {CodigoGeneracion}",
                            factura.CodigoGeneracion);
                    }
                }
            }

            return true;
        }

        // ==========================================
        // MÉTODOS PRIVADOS - LÓGICA DE NEGOCIO
        // ==========================================

        /// <summary>
        /// Genera un GUID para el Código de Generación del DTE
        /// </summary>
        private string GenerarCodigoGeneracion()
        {
            return IdentificacionHelper.GenerarCodigoGeneracion();
        }

        /// <summary>
        /// Genera el Número de Control según formato de Hacienda
        /// Formato: DTE-{tipoDte}-{codEstable4codPuntoVenta4}-{correlativo15}
        /// Total: 31 caracteres base. Patrón: ^DTE-\d{2}-[A-Z0-9]{8}-[0-9]{15}$
        /// Según esquema oficial de Hacienda
        /// </summary>
        private int ExtraerCorrelativo(string numeroControl)
        {
            var partes = numeroControl.Split('-');
            if (partes.Length == 4 && int.TryParse(partes[3], out int correlativo))
                return correlativo;
            return 1;
        }

        /// <summary>
        /// Detecta si MH rechazó por NumeroControl duplicado (código 004) y si es así,
        /// incrementa el correlativo y regenera CodigoGeneracion en la factura.
        /// Retorna true si se regeneró (el caller debe reintentar el envío).
        /// </summary>
        public async Task<bool> RegenerarSiNumeroControlDuplicadoAsync(int facturaId, string? codigoMsg, string? descripcionMsg)
        {
            var factura = await _context.Facturas.FindAsync(facturaId);
            if (factura == null) return false;
            return await RegenerarSiNumeroControlDuplicadoInternalAsync(factura, codigoMsg, descripcionMsg);
        }

        private async Task<bool> RegenerarSiNumeroControlDuplicadoInternalAsync(
            FacturaElectronica factura, string? codigoMsg, string? descripcionMsg)
        {
            if (codigoMsg != "004"
                || descripcionMsg?.Contains("numeroControl", StringComparison.OrdinalIgnoreCase) != true)
                return false;

            var correlativoActual = ExtraerCorrelativo(factura.NumeroControl);
            var nuevoCorrelativo = correlativoActual + 1;
            var anteriorNc = factura.NumeroControl;

            // El bloque de establecimiento/punto de venta no cambia al resolver un
            // duplicado: solo se renueva el correlativo (último segmento), preservando
            // el formato actual del numeroControl.
            var partes = factura.NumeroControl.Split('-');
            if (partes.Length >= 4)
            {
                partes[^1] = nuevoCorrelativo.ToString("D15");
                factura.NumeroControl = string.Join('-', partes);
            }
            factura.CodigoGeneracion = GenerarCodigoGeneracion();
            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "[FACTURA-MH] NumeroControl duplicado en MH. Regenerado: {Anterior} → {Nuevo}, CodigoGeneracion={CodigoGen}",
                anteriorNc, factura.NumeroControl, factura.CodigoGeneracion);

            return true;
        }

        /// <summary>
        /// Obtiene el siguiente correlativo para el emisor, por tipo de DTE y año actual.
        /// El correlativo se reinicia cada año y es independiente por tipo de documento.
        /// </summary>
        private async Task<int> ObtenerSiguienteCorrelativoAsync(int emisorId, int catTipoDocumentoId, string tipoDte, string? codEstablecimiento, string? codPuntoVenta, int anio, string ambiente)
        {
            // 1. Consultar máximo correlativo GLOBAL en TODAS las BDs
            //    Filtra por emisorId + tipoDte + año + ambiente
            var maxCrossDb = await _crossDbService.ObtenerMaxCorrelativoAsync(emisorId, tipoDte, anio, ambiente);

            // 2. Consultar máximo correlativo GLOBAL en la BD local
            //    Filtra por emisorId + tipoDte + año + ambiente
            var prefijoDte = $"DTE-{tipoDte}-%";
            var maxLocalResult = await _context.Database
                .SqlQueryRaw<int?>(
                    @"SELECT MAX(CAST(SPLIT_PART(""NumeroControl"", '-', 4) AS INTEGER)) AS ""Value""
                      FROM ""Facturas""
                      WHERE ""EmisorId"" = {0}
                        AND ""NumeroControl"" LIKE {1}
                        AND CAST(EXTRACT(YEAR FROM ""FechaEmision"") AS INTEGER) = {2}
                        AND ""Ambiente"" = {3}",
                    emisorId, prefijoDte, anio, ambiente)
                .FirstOrDefaultAsync();

            var maxLocalCorrelativo = maxLocalResult ?? 0;

            // 2.b Correlativo inicial declarado por el emisor (migración desde otro sistema)
            var correlativoInicial = await _correlativoInicialService
                .ObtenerUltimoCorrelativoAsync(emisorId, tipoDte, anio, ambiente);

            _logger.LogInformation(
                "[CORRELATIVO] EmisorId={EmisorId}, TipoDte='{TipoDte}', Anio={Anio}, Ambiente='{Ambiente}' => LocalMax={LocalMax}, CrossDbMax={CrossDbMax}, CorrelativoInicial={CorrelativoInicial}",
                emisorId, tipoDte, anio, ambiente, maxLocalCorrelativo, maxCrossDb, correlativoInicial);

            // 3. Usar el mayor de los tres + 1
            var maxGlobal = Math.Max(maxCrossDb, Math.Max(maxLocalCorrelativo, correlativoInicial));

            if (maxGlobal == 0)
                return 1;

            if (maxCrossDb > maxLocalCorrelativo)
            {
                _logger.LogWarning(
                    "[CROSS-DB] Correlativo local ({Local}) menor que cross-DB ({CrossDb}) para EmisorId={EmisorId}, TipoDte='{TipoDte}', Anio={Anio}, Ambiente='{Ambiente}'. Usando cross-DB.",
                    maxLocalCorrelativo, maxCrossDb, emisorId, tipoDte, anio, ambiente);
            }

            return maxGlobal + 1;
        }

        // Métodos helper para convertir códigos string de MH a IDs de catálogo.
        // Retorna null cuando el receptor va "Sin documento" (FC tipo 01 admite tipoDocumento+numDocumento null).
        // Antes hacía fallback silencioso a 1 (NIT) — perdía la intención y violaba la integridad del payload MH.
        private int? ConvertirTipoDocumentoACatalogoId(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return null;
            return codigo switch
            {
                "36" => 1, // NIT
                "13" => 2, // DUI
                "02" => 3, // Carnet de residente
                "03" => 4, // Pasaporte
                "37" => 5, // Otro
                _ => null  // Código desconocido → null (no enmascarar como NIT).
            };
        }

        /// <summary>
        /// Convierte código de tipo de DTE de Hacienda a ID de catálogo CatTiposDocumento
        /// </summary>
        private int ConvertirTipoDteACatalogoId(string? codigo)
        {
            // Mapeo de códigos MH a IDs de catálogo según CatTiposDocumento
            return codigo switch
            {
                "01" => 1, // Factura
                "03" => 2, // Comprobante de Crédito Fiscal (CCF)
                "04" => 3, // Nota de Remisión
                "05" => 4, // Nota de Crédito
                "06" => 5, // Nota de Débito
                "07" => 6, // Comprobante de Retención
                "08" => 7, // Comprobante de Liquidación
                "09" => 8, // Documento Contable de Liquidación
                "11" => 9, // Factura de Exportación
                "14" => 10, // Factura de Sujeto Excluido
                "15" => 11, // Comprobante de Donación
                _ => 1 // Default: Factura
            };
        }

        /// <summary>
        /// Obtiene la versión del schema según el tipo de DTE (según documentación MH)
        /// </summary>
        private int ObtenerVersionSegunTipoDte(string? tipoDte)
        {
            // Mapeo de tipo de DTE a versión de schema según Hacienda
            return tipoDte switch
            {
                "01" => 2, // Factura → fe-f-v2.json (Normativa V2.0)
                "03" => 4, // CCF → fe-ccf-v4.json (Normativa V2.0)
                "04" => 1, // Nota de Remisión → fe-nr-v1.json
                "05" => 4, // Nota de Crédito → fe-nc-v4.json (Normativa V2.0)
                "06" => 4, // Nota de Débito → fe-nd-v4.json (Normativa V2.0)
                "07" => 1, // Comprobante de Retención → fe-cr-v1.json
                "08" => 1, // Comprobante de Liquidación → fe-cl-v1.json
                "09" => 1, // Documento Contable de Liquidación
                "11" => 1, // Factura de Exportación → fe-fex-v1.json
                "14" => 2, // Factura de Sujeto Excluido → fe-fse-v2.json (Normativa V2.0)
                "15" => 1, // Comprobante de Donación
                _ => 1 // Default: version 1
            };
        }

        /// <summary>
        /// Obtiene el código MH (ej "36") dado un ID de catálogo
        /// </summary>
        private async Task<string> ObtenerCodigoTipoDocumentoIdentificacion(int id)
        {
            var tipo = await _context.CatDocsIdentidadReceptor.FindAsync(id);
            return tipo?.Codigo ?? "36"; // Default NIT
        }


        private int? ConvertirDepartamentoACatalogoId(string? codigo)
        {
            if (string.IsNullOrEmpty(codigo)) return null;

            // El código ya es numérico "01"-"14", convertir a int
            if (int.TryParse(codigo, out int id) && id >= 1 && id <= 14)
            {
                return id;
            }
            return null;
        }

        private int? ConvertirMunicipioACatalogoId(string? codigo)
        {
            if (string.IsNullOrEmpty(codigo)) return null;

            // El código ya es numérico, convertir a int
            if (int.TryParse(codigo, out int id) && id >= 1 && id <= 262)
            {
                return id;
            }
            return null;
        }

        /// <summary>
        /// Construye el objeto receptor.direccion para el JSON DTE. Devuelve null si la
        /// tetrada (departamento + municipio + distrito + complemento texto) NO está
        /// completa. Devuelve el objeto con los códigos MH si la tetrada es completa.
        /// Esta regla evita que Smartix mande a MH una direccion parcial que rebote con
        /// [096] DOCUMENTO NO CUMPLE CON NORMATIVA. Smoke empírico 2026-06-11 confirma
        /// MH requiere los 4 campos cuando direccion no es null.
        ///
        /// Reemplaza la guarda inline `Receptor.Distrito != null ? new {...} : null` que
        /// estaba en los builders de FC/CCF/NC/ND/FSE.
        /// </summary>
        internal static object? ConstruirDireccionReceptor(Receptor receptor)
        {
            if (receptor.Departamento == null
             || receptor.Municipio    == null
             || receptor.Distrito     == null
             || string.IsNullOrWhiteSpace(receptor.Direccion))
            {
                return null;
            }

            return new
            {
                departamento = receptor.Departamento.Codigo,
                municipio    = receptor.Municipio.Codigo,
                distrito     = receptor.Distrito.Codigo,
                complemento  = receptor.Direccion
            };
        }

        /// <summary>
        /// Construye la dirección del emisor (departamento/municipio/distrito/complemento)
        /// tomándola de forma ATÓMICA de una sola fuente: sucursal si su triada catálogo
        /// está completa, sino fallback al emisor entero. Preserva el fallback de complemento
        /// sucursal→emisor cuando sucursal.Direccion está vacía.
        ///
        /// Devuelve null cuando ni sucursal ni emisor tienen tetrada completa. MH rechazará
        /// el DTE con [096] "/emisor/direccion/{campo}: required/no cumple esquema" — eso es
        /// intencional: dejamos que MH sea la única fuente de verdad sobre validez de la
        /// dirección del emisor, y el FE mapea [096] /emisor/direccion/* a un mensaje
        /// humano. Antes lanzábamos InvalidOperationException pre-MH; eso dejaba la factura
        /// en estado PENDIENTE_ENVIO sin poder descartarse desde el endpoint /descartar
        /// (solo acepta RECHAZADO/ERROR).
        ///
        /// Nunca se mezcla la triada entre fuentes: el código de distrito NO es único (se
        /// reinicia por municipio), así que combinar el departamento/municipio de la sucursal
        /// con el distrito del emisor produce un valor de distrito inválido y MH rechaza el
        /// DTE con "#/emisor/direccion/distrito contiene un valor inválido".
        /// </summary>
        internal static object? ConstruirDireccionEmisor(FacturaElectronica factura)
        {
            var s = factura.Sucursal;
            var sucursalTriadaCompleta = s != null
                && s.Departamento != null
                && s.Municipio    != null
                && s.Distrito     != null;

            if (sucursalTriadaCompleta)
            {
                var complemento = string.IsNullOrWhiteSpace(s!.Direccion)
                    ? factura.Emisor.Direccion
                    : s.Direccion;

                if (string.IsNullOrWhiteSpace(complemento))
                {
                    return null;
                }

                return new
                {
                    departamento = s.Departamento!.Codigo,
                    municipio    = s.Municipio!.Codigo,
                    distrito     = s.Distrito!.Codigo,
                    complemento
                };
            }

            var e = factura.Emisor;
            var emisorTetradaCompleta = e.Departamento != null
                && e.Municipio    != null
                && e.Distrito     != null
                && !string.IsNullOrWhiteSpace(e.Direccion);

            if (emisorTetradaCompleta)
            {
                return new
                {
                    departamento = e.Departamento!.Codigo,
                    municipio    = e.Municipio!.Codigo,
                    distrito     = e.Distrito!.Codigo,
                    complemento  = e.Direccion
                };
            }

            return null;
        }

        /// <summary>
        /// Normaliza el número de documento del receptor al formato exigido por MH según el
        /// tipo de documento (CAT-002): NIT (36) → solo dígitos (sin guiones); DUI (13) →
        /// solo dígitos (sin guion). Hasta v1 del esquema DTE MH aceptaba el DUI con guión
        /// (########-#), pero el promote a DTE v2 (2026-06-05) endureció el cross-validation
        /// del backend MH y ahora rechaza el guion con "no cumple el formato requerido". El
        /// resto (Carnet 02, Pasaporte 03, Otro 37) se envía tal cual.
        /// </summary>
        internal static string? NormalizarNumDocumentoReceptor(string? tipoDocumento, string? numDocumento)
        {
            // null o whitespace → null (NO string vacio). MH fe-f-v2 declara
            // numDocumento como ["string", "null"] con minLength=1, asi que
            // "" rebota con [096] "no cumple el tamaño minimo permitido".
            // El receptor sin documento (FC anonimo) debe ir como null entero.
            if (string.IsNullOrWhiteSpace(numDocumento))
                return null;

            var limpio = numDocumento.Trim();

            return tipoDocumento switch
            {
                // NIT: 14 dígitos sin guiones ni espacios
                "36" => new string(limpio.Where(char.IsDigit).ToArray()),
                // DUI: 9 dígitos sin guion. MH v2 rechaza con guion. Solo normalizamos si
                // tenemos exactamente 9 dígitos; si no, lo dejamos como viene para que MH
                // detalle el error en vez de enviar algo a medio formatear.
                "13" => NormalizarDuiSoloDigitos(limpio),
                // Carnet (02), Pasaporte (03), Otro (37): alfanumérico
                _ => limpio
            };
        }

        /// <summary>
        /// Empareja tipoDocumento con numDocumento al construir el payload MH.
        /// Si el numero queda null (receptor sin documento / FC anonimo), el tipo
        /// tambien va null — MH rechaza si solo uno de los 2 esta presente, y
        /// no hay sentido semantico en "DUI sin numero".
        /// </summary>
        internal static string? NormalizarTipoDocumentoReceptor(string? tipoDocumento, string? numDocumento)
        {
            if (string.IsNullOrWhiteSpace(numDocumento))
                return null;
            return string.IsNullOrWhiteSpace(tipoDocumento) ? null : tipoDocumento;
        }

        private static string NormalizarDuiSoloDigitos(string dui)
        {
            var soloDigitos = new string(dui.Where(char.IsDigit).ToArray());
            return soloDigitos.Length == 9 ? soloDigitos : dui;
        }

        /// <summary>
        /// Resuelve el Id de CatDistrito (CAT-008) a partir de la triada de códigos MH
        /// departamento → municipio → distrito. El código de distrito NO es único por sí
        /// solo (se reinicia por municipio), por eso se busca por la triada completa.
        /// Devuelve null si no se puede resolver; en ese caso la validación dura rechazará
        /// la emisión de los documentos que exigen distrito (CCF/NC/ND/FSE).
        /// </summary>
        internal async Task<int?> ConvertirDistritoACatalogoId(string? departamento, string? municipio, string? distrito)
        {
            if (string.IsNullOrEmpty(departamento) || string.IsNullOrEmpty(municipio) || string.IsNullOrEmpty(distrito))
                return null;

            var entidad = await _context.CatDistritos
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.CodigoDepartamento == departamento
                                       && d.CodigoMunicipio == municipio
                                       && d.Codigo == distrito);

            return entidad?.Id;
        }

        /// <summary>
        /// Valida que el receptor tenga dirección completa (departamento + municipio + distrito
        /// + complemento texto no vacío) cuando el tipo de DTE lo exige. La Normativa DTE V2.0
        /// requiere dirección en CCF (03), Nota de Remisión (04), Nota de Crédito (05), Nota
        /// de Débito (06) y Factura de Sujeto Excluido (14). La Factura (01) está exenta porque
        /// el schema FC v2 permite receptor.direccion = null.
        ///
        /// El mensaje de error cita exactamente cuáles campos faltan para que el admin sepa qué
        /// completar antes de re-emitir.
        ///
        /// Smoke empírico 2026-06-11 contra MH UAT confirma que MH reporta cada campo required
        /// nulo individualmente, por eso este validador hace lo mismo para mensajes precisos.
        /// </summary>
        internal async Task ValidarDireccionReceptorAsync(string? tipoDte, int? receptorId)
        {
            var requiereDireccion = tipoDte is "03" or "04" or "05" or "06" or "14";
            if (!requiereDireccion)
                return;

            if (receptorId is null or <= 0)
                throw new InvalidOperationException(
                    $"El documento tipo {tipoDte} requiere un receptor con dirección completa " +
                    "(departamento, municipio, distrito, complemento).");

            var r = await _context.Receptores
                .AsNoTracking()
                .Where(x => x.Id == receptorId.Value)
                .Select(x => new
                {
                    x.CatDepartamentoId,
                    x.CatMunicipioId,
                    x.CatDistritoId,
                    x.Direccion
                })
                .FirstOrDefaultAsync();

            var faltantes = new List<string>();
            if (r?.CatDepartamentoId is null)             faltantes.Add("departamento");
            if (r?.CatMunicipioId   is null)              faltantes.Add("municipio");
            if (r?.CatDistritoId    is null)              faltantes.Add("distrito");
            if (string.IsNullOrWhiteSpace(r?.Direccion))  faltantes.Add("complemento");

            if (faltantes.Count > 0)
                throw new InvalidOperationException(
                    $"El receptor del documento tipo {tipoDte} debe tener dirección completa. " +
                    $"Falta: {string.Join(", ", faltantes)}. Actualice el cliente antes de emitir.");
        }

        /// <summary>
        /// Convierte código de tributo de Hacienda a ID de catálogo interno
        /// </summary>
        private async Task<int> ConvertirTributoCodigoACatalogoIdAsync(string? codigo)
        {
            if (string.IsNullOrEmpty(codigo))
                return 1; // Default: IVA

            // Buscar tributo por código en el catálogo
            var tributo = await _context.CatTributos
                .FirstOrDefaultAsync(t => t.Codigo == codigo);

            // Si se encuentra, retornar su ID; si no, retornar 1 (IVA por defecto)
            return tributo?.Id ?? 1;
        }


        /// <summary>
        /// Invalida una factura electrónica con integración de inventario y envío a Hacienda
        /// </summary>
        public async Task<FraFactu.Application.DTOs.Invalidacion.InvalidacionDto> InvalidarFacturaAsync(
            int facturaId,
            FraFactu.Application.DTOs.Invalidacion.AnularFacturaDto dto,
            int emisorId)
        {
            var factura = await _context.Facturas
                .Include(f => f.Receptor!)
                    .ThenInclude(r => r.TipoDocumento)
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.AmbienteDestino)
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.TipoEstablecimiento) // Importante para construir DTO
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.TipoEstablecimiento)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.Departamento)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.Municipio)
                .Include(f => f.Caja)
                .FirstOrDefaultAsync(f => f.Id == facturaId && f.EmisorId == emisorId);

            if (factura == null)
                throw new InvalidOperationException("Factura no encontrada");

            if (string.IsNullOrEmpty(factura.SelloRecibido))
                throw new InvalidOperationException("Solo se pueden invalidar facturas con sello de recepción");

            var yaInvalidada = await _context.Invalidaciones
                .AnyAsync(i => i.FacturaElectronicaId == facturaId);

            if (yaInvalidada)
                throw new InvalidOperationException("La factura ya está invalidada");

            // =================================================================================
            // 1. VALIDACIÓN DE PLAZOS (CORREGIDO SEGÚN MANUAL HACIENDA)
            // =================================================================================
            // Grupo 1: CCF (03), NRE (04), NCE (05), NDE (06), CRE (07), CLE (08), DCLE (09), CDE (15)
            // Plazo: 1 día (hasta las 23:59:59 del día siguiente a la fecha de transmisión)
            // 
            // Grupo 2: FE (01), FEXE (11), FSEE (14)
            // Plazo: 3 meses desde la fecha de transmisión

            // Obtener tipo de documento
            var tipoDocumento = await _context.CatTiposDocumento
                .FirstOrDefaultAsync(t => t.Id == factura.CatTipoDocumentoId);
            string codigoTipoDte = tipoDocumento?.Codigo ?? "01";

            DateTime fechaTransmision = factura.FechaTransmision ?? factura.FechaEmision;
            if (factura.HoraTransmision.HasValue)
                fechaTransmision = fechaTransmision.Date.Add(factura.HoraTransmision.Value);

            DateTime fechaLimite;
            string descripcionPlazo;

            // Tipos con plazo de 3 meses (Grupo 2)
            var tiposTresMeses = new[] { "01", "11", "14" };

            if (tiposTresMeses.Contains(codigoTipoDte))
            {
                fechaLimite = fechaTransmision.AddMonths(3);
                descripcionPlazo = "3 meses";
            }
            else
            {
                // Grupo 1: Hasta 23:59:59 del día siguiente
                fechaLimite = fechaTransmision.Date.AddDays(1).Add(new TimeSpan(23, 59, 59));
                descripcionPlazo = "1 día (hasta el día siguiente)";
            }

            if (DateTime.UtcNow > fechaLimite)
            {
                throw new InvalidOperationException(
                    $"El plazo para invalidar ha expirado. " +
                    $"Tipo DTE: {codigoTipoDte}. Plazo máximo: {descripcionPlazo}. " +
                    $"Fecha límite: {fechaLimite:dd/MM/yyyy HH:mm:ss} UTC");
            }

            // =================================================================================
            // 2. VALIDACIONES DE LOGICA DE NEGOCIO (REEMPLAZOS Y RECEPTORES)
            // =================================================================================

            // Validación de Factura de Reemplazo para tipos 1 (Error) y 3 (Otro)
            // Excepción: Nota de Crédito (05), Nota de Débito (06) y Comp. Liquidación (08) NO requieren reemplazo
            bool requiereReemplazo = (dto.TipoAnulacion == 1 || dto.TipoAnulacion == 3)
                                     && codigoTipoDte != "05" && codigoTipoDte != "06" && codigoTipoDte != "08";

            FacturaElectronica? facturaReemplazo = null;

            if (requiereReemplazo)
            {
                if (!dto.FacturaReemplazoId.HasValue)
                    throw new InvalidOperationException("Para este tipo de anulación debe especificar la factura de reemplazo/nuevo documento.");

                facturaReemplazo = await _context.Facturas
                    .FirstOrDefaultAsync(f => f.Id == dto.FacturaReemplazoId.Value && f.EmisorId == emisorId);

                if (facturaReemplazo == null)
                    throw new InvalidOperationException("La factura de reemplazo especificada no existe.");

                if (string.IsNullOrEmpty(facturaReemplazo.SelloRecibido))
                    throw new InvalidOperationException("La factura de reemplazo debe estar PROCESADA y tener Sello de Recepción antes de invalidar la original.");
            }

            // Para tipo 2 (Rescisión), el reemplazo debe ser nulo
            if (dto.TipoAnulacion == 2)
            {
                dto.FacturaReemplazoId = null;
                facturaReemplazo = null;
            }

            // Validación de fecha de anulación (fecAnula)
            // Hacienda espera fecha y hora en zona horaria de El Salvador (UTC-6)
            var zonaElSalvador = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
            DateTime fechaAnulacion = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaElSalvador);

            // =================================================================================
            // 3. CONSTRUCCIÓN DEL EVENTO JSON (EventoInvalidacionDto)
            // =================================================================================

            // Obtener códigos de catálogos
            string tipDocResponsable = await ObtenerCodigoTipoDocumentoIdentificacion(dto.CatTipoDocResponsableId);
            string tipDocSolicita = await ObtenerCodigoTipoDocumentoIdentificacion(dto.CatTipoDocSolicitaId);

            // Manejo de Receptor NULL (para facturas Consumidor Final sin datos)
            // Según manual: si no hay receptor, enviar cadena literal "null"
            bool receptorEsNull = factura.Receptor == null;
            string receptorTipoDoc = receptorEsNull ? "null" : (factura.Receptor?.TipoDocumento?.Codigo ?? "36");
            string receptorNumDoc = receptorEsNull ? "null" : (factura.Receptor?.NumeroDocumento ?? "null");
            string receptorNombre = receptorEsNull ? "null" : (factura.Receptor?.NombreRazonSocial ?? "null");
            string? receptorTelefono = receptorEsNull ? null : (string.IsNullOrEmpty(factura.Receptor?.Telefono) ? null : factura.Receptor?.Telefono);
            string? receptorCorreo = receptorEsNull ? null : factura.Receptor?.CorreoElectronico;

            // Construir DTO para Hacienda
            var eventoDto = new FraFactu.Application.DTOs.Hacienda.EventoInvalidacionDto
            {
                Identificacion = new FraFactu.Application.DTOs.Hacienda.IdentificacionInvalidacionDto
                {
                    Version = 3,
                    Ambiente = factura.Emisor.AmbienteDestino?.Codigo ?? "00", // "00": Pruebas, "01": Prod
                    CodigoGeneracion = Guid.NewGuid().ToString().ToUpper(), // Nuevo GUID para el evento
                    FecEmi = fechaAnulacion.ToString("yyyy-MM-dd"),
                    HorEmi = fechaAnulacion.ToString("HH:mm:ss"),
                    Fusion = null // Solo aplica en eventos de fusión de contribuyentes
                },
                Emisor = new FraFactu.Application.DTOs.Hacienda.EmisorInvalidacionDto
                {
                    Nit = factura.Emisor.Nit?.Replace("-", "") ?? "",
                    Nombre = factura.Emisor.NombreRazonSocial,
                    CodEstableMH = factura.Sucursal!.CodigoEstablecimiento,
                    CodEstable = factura.Sucursal!.Codigo ?? "",
                    CodPuntoVentaMH = factura.Caja!.CodPuntoVentaMH,
                    CodPuntoVenta = factura.Caja!.CodPuntoVenta,
                    Telefono = (factura.Sucursal?.Telefono ?? factura.Emisor.Telefono)?.Replace("-", ""),
                    Correo = factura.Sucursal?.CorreoElectronico ?? factura.Emisor.CorreoElectronico
                },
                Documento = new FraFactu.Application.DTOs.Hacienda.DocumentoInvalidacionDto
                {
                    TipoDte = codigoTipoDte,
                    CodigoGeneracion = factura.CodigoGeneracion,
                    SelloRecibido = factura.SelloRecibido,
                    NumeroControl = factura.NumeroControl,
                    FecEmi = factura.FechaEmision.ToString("yyyy-MM-dd"),
                    CodigoGeneracionR = facturaReemplazo?.CodigoGeneracion, // NULL si no aplica
                    TipoDocumento = receptorTipoDoc,
                    NumDocumento = receptorNumDoc,
                    Nombre = receptorNombre,
                    Telefono = receptorTelefono?.Replace("-", ""),
                    Correo = receptorCorreo
                },
                Motivo = new FraFactu.Application.DTOs.Hacienda.MotivoInvalidacionDto
                {
                    TipoAnulacion = dto.TipoAnulacion,
                    MotivoAnulacion = dto.MotivoAnulacion,
                    NombreResponsable = dto.NombreResponsable,
                    TipDocResponsable = tipDocResponsable,
                    NumDocResponsable = dto.NumDocResponsable,
                    NombreSolicita = dto.NombreSolicita,
                    TipDocSolicita = tipDocSolicita,
                    NumDocSolicita = dto.NumDocSolicita
                }
            };

            // =================================================================================
            // 4. TRANSMISIÓN A HACIENDA
            // =================================================================================

            // Llamar al servicio API (AnularDteAsync ya existe e implementa firma y envío)
            var responseHacienda = await _haciendaApiService.AnularDteAsync(emisorId, eventoDto);

            // Verificar respuesta
            if (responseHacienda.Estado == "RECHAZADO")
            {
                // Caso especial: Hacienda dice que el documento ya fue invalidado previamente
                // (puede pasar si un intento anterior llegó a Hacienda pero falló localmente)
                bool yaInvalidadoEnHacienda = responseHacienda.DescripcionMsg?.Contains("SE ENCUENTRA INVALIDADO", StringComparison.OrdinalIgnoreCase) == true;

                if (yaInvalidadoEnHacienda)
                {
                    _logger.LogWarning("Hacienda indica que el DTE ya está invalidado. Sincronizando estado local.");
                    factura.EstadoHacienda = "ANULADO";
                    factura.Observaciones = $"ANULADO (sincronizado): {dto.MotivoAnulacion}";
                    factura.Activo = false;

                    // Crear registro de invalidación local si no existe
                    var existeLocal = await _context.Invalidaciones.AnyAsync(i => i.FacturaElectronicaId == facturaId);
                    if (!existeLocal)
                    {
                        _context.Invalidaciones.Add(new Invalidacion
                        {
                            EmisorId = emisorId,
                            FacturaElectronicaId = facturaId,
                            FacturaReemplazoId = dto.FacturaReemplazoId,
                            CodigoGeneracion = eventoDto.Identificacion.CodigoGeneracion,
                            FechaAnulacion = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc),
                            HoraAnulacion = fechaAnulacion.TimeOfDay,
                            Ambiente = eventoDto.Identificacion.Ambiente,
                            TipoAnulacion = dto.TipoAnulacion,
                            MotivoAnulacion = dto.MotivoAnulacion,
                            NombreResponsable = dto.NombreResponsable,
                            CatTipoDocResponsableId = dto.CatTipoDocResponsableId,
                            NumDocResponsable = dto.NumDocResponsable,
                            NombreSolicita = dto.NombreSolicita,
                            CatTipoDocSolicitaId = dto.CatTipoDocSolicitaId,
                            NumDocSolicita = dto.NumDocSolicita,
                            CatTipoDocReceptorId = factura.Receptor?.CatTipoDocumentoIdentificacionReceptorId ?? 1,
                            NumDocReceptor = factura.Receptor?.NumeroDocumento ?? "",
                            NombreReceptor = factura.Receptor?.NombreRazonSocial ?? "",
                            MontoIva = factura.TotalIva,
                            CorreoReceptor = factura.Receptor?.CorreoElectronico,
                            TelefonoReceptor = receptorTelefono,
                            JsonEvento = JsonSerializer.Serialize(eventoDto),
                            JsonRespuesta = JsonSerializer.Serialize(responseHacienda),
                            EstadoHacienda = "ANULADO",
                            FechaTransmision = DateTime.UtcNow,
                            TipoInvalidacion = dto.TipoInvalidacion ?? "ERROR_FACTURA",
                            RevirtiInventario = dto.RevirtiInventario
                        });
                    }

                    await _context.SaveChangesAsync();

                    return new FraFactu.Application.DTOs.Invalidacion.InvalidacionDto
                    {
                        Identificacion = new FraFactu.Application.DTOs.Invalidacion.IdentificacionInvalidacionDto
                        {
                            CodigoGeneracion = eventoDto.Identificacion.CodigoGeneracion,
                            FecAnula = fechaAnulacion.ToString("yyyy-MM-dd"),
                            HorAnula = fechaAnulacion.ToString("HH:mm:ss")
                        },
                        Estado = "PROCESADO",
                        SelloRecibido = null
                    };
                }

                // Rechazo real: lanzar excepción con detalle
                string errorMsg = $"Hacienda rechazó la invalidación. Código: {responseHacienda.CodigoMsg}. Mensaje: {responseHacienda.DescripcionMsg}";
                if (responseHacienda.Observaciones != null && responseHacienda.Observaciones.Any())
                    errorMsg += ". Observaciones: " + string.Join("; ", responseHacienda.Observaciones);

                throw new InvalidOperationException(errorMsg);
            }

            // Si el estado no es procesado (y no es rechazado explícito), verificar
            if (responseHacienda.Estado != "PROCESADO")
            {
                // Si devuelve otro estado (ej: EN_COLA), advertir o manejar
                // Por ahora asumimos que si no es RECHAZADO es éxito
            }

            // =================================================================================
            // 5. PERSISTENCIA LOCAL
            // =================================================================================

            // Crear entidad de invalidación local
            var invalidacion = new Invalidacion
            {
                EmisorId = emisorId,
                FacturaElectronicaId = facturaId,
                FacturaReemplazoId = dto.FacturaReemplazoId,
                CodigoGeneracion = eventoDto.Identificacion.CodigoGeneracion, // GUID del evento
                FechaAnulacion = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc),
                HoraAnulacion = fechaAnulacion.TimeOfDay,
                Ambiente = eventoDto.Identificacion.Ambiente,
                TipoAnulacion = dto.TipoAnulacion,
                MotivoAnulacion = dto.MotivoAnulacion,
                NombreResponsable = dto.NombreResponsable,
                CatTipoDocResponsableId = dto.CatTipoDocResponsableId,
                NumDocResponsable = dto.NumDocResponsable,
                NombreSolicita = dto.NombreSolicita,
                CatTipoDocSolicitaId = dto.CatTipoDocSolicitaId,
                NumDocSolicita = dto.NumDocSolicita,
                CatTipoDocReceptorId = factura.Receptor?.CatTipoDocumentoIdentificacionReceptorId ?? 1, // Default NIT si null
                NumDocReceptor = factura.Receptor?.NumeroDocumento ?? "",
                NombreReceptor = factura.Receptor?.NombreRazonSocial ?? "",
                MontoIva = factura.TotalIva,
                CorreoReceptor = factura.Receptor?.CorreoElectronico,
                TelefonoReceptor = receptorTelefono,

                // GUARDAR DATOS DE RESPUESTA REAL
                SelloRecibido = responseHacienda.SelloRecibido,
                EstadoHacienda = responseHacienda.Estado, // Debería ser "PROCESADO"
                FechaTransmision = DateTime.UtcNow,
                JsonEvento = JsonSerializer.Serialize(eventoDto),
                JsonRespuesta = JsonSerializer.Serialize(responseHacienda),

                // CAMPOS DE INVENTARIO
                TipoInvalidacion = dto.TipoInvalidacion ?? "ERROR_FACTURA",
                RevirtiInventario = dto.RevirtiInventario
            };

            _context.Invalidaciones.Add(invalidacion);

            // ACTUALIZAR ESTADO DE LA FACTURA ORIGINAL
            // Solo marcar como ANULADO si Hacienda dijo PROCESADO
            if (responseHacienda.Estado == "PROCESADO")
            {
                factura.EstadoHacienda = "ANULADO";
                factura.Observaciones = $"ANULADO: {dto.MotivoAnulacion}";
                factura.Activo = false;
            }

            await _context.SaveChangesAsync();

            // Enviar notificación de invalidación al receptor (no bloqueante)
            if (responseHacienda.Estado == "PROCESADO")
            {
                await IntentarEnviarEmailInvalidacionAsync(factura.Id, factura.ReceptorId == 0 ? null : (int?)factura.ReceptorId, dto.MotivoAnulacion ?? "Sin motivo especificado");
            }

            // INTEGRACIÓN CON INVENTARIO
            if (invalidacion.RevirtiInventario && responseHacienda.Estado == "PROCESADO")
            {
                try
                {
                    await _inventarioService.RevertirVentaAsync(invalidacion.Id);
                }
                catch (Exception ex)
                {
                    // Log error pero no fallar la transacción de invalidación
                    _logger.LogError(ex, "Error al revertir inventario para invalidación {InvalidacionId}", invalidacion.Id);
                    // TODO: Implementar mecanismo de reintento o alerta
                }
            }

            // Mapear respuesta
            return new FraFactu.Application.DTOs.Invalidacion.InvalidacionDto
            {
                Identificacion = new FraFactu.Application.DTOs.Invalidacion.IdentificacionInvalidacionDto
                {
                    CodigoGeneracion = invalidacion.CodigoGeneracion,
                    FecAnula = invalidacion.FechaAnulacion.ToString("yyyy-MM-dd"),
                    HorAnula = invalidacion.HoraAnulacion.ToString(@"hh\:mm\:ss")
                },
                Estado = responseHacienda.Estado,
                SelloRecibido = responseHacienda.SelloRecibido
            };
        }



        // ==========================================
        // NUEVOS MÉTODOS - ENVÍO POR LOTES
        // ==========================================

        /// <summary>
        /// Obtiene facturas con EstadoHacienda = 'PENDIENTE_ENVIO'
        /// </summary>
        public async Task<PaginatedResponse<FacturaListDto>> ObtenerFacturasPendientesAsync(
            int emisorId,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null,
            int pageNumber = 1,
            int pageSize = 10,
            int? sucursalId = null,
            string? search = null,
            string? ambiente = null)
        {
            var query = _context.Facturas
                .Where(f => f.EmisorId == emisorId && f.EstadoHacienda == "PENDIENTE_ENVIO");

            if (!string.IsNullOrEmpty(ambiente))
                query = query.Where(f => f.Ambiente == ambiente);

            if (sucursalId.HasValue)
                query = query.Where(f => f.SucursalId == sucursalId.Value);

            // Filtros de fecha
            if (fechaDesde.HasValue)
            {
                var fechaDesdeUtc = DateTime.SpecifyKind(fechaDesde.Value.Date, DateTimeKind.Utc);
                query = query.Where(f => f.FechaEmision >= fechaDesdeUtc);
            }

            if (fechaHasta.HasValue)
            {
                var fechaHastaUtc = DateTime.SpecifyKind(fechaHasta.Value.Date.AddDays(1), DateTimeKind.Utc);
                query = query.Where(f => f.FechaEmision < fechaHastaUtc);
            }

            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(f =>
                    (f.NumeroControl != null && f.NumeroControl.ToLower().Contains(searchLower)) ||
                    (f.CodigoGeneracion != null && f.CodigoGeneracion.ToLower().Contains(searchLower)) ||
                    (f.Receptor != null && f.Receptor.NombreRazonSocial.ToLower().Contains(searchLower)));
            }

            query = query.OrderByDescending(f => f.FechaEmision).AsQueryable();

            var pagedData = await query
                .Select(f => new FacturaListDto
                {
                    Id = f.Id,
                    CodigoGeneracion = f.CodigoGeneracion,
                    NumeroControl = f.NumeroControl,
                    FechaEmision = f.FechaEmision,
                    HoraEmision = f.HoraEmision,
                    ReceptorNombre = f.Receptor != null ? f.Receptor.NombreRazonSocial : "Consumidor Final",
                    ReceptorNumeroDocumento = f.Receptor != null ? (f.Receptor.NumeroDocumento ?? string.Empty) : string.Empty,
                    TotalPagar = f.TotalPagar,
                    EstadoHacienda = f.EstadoHacienda,
                    FechaCreacion = f.FechaCreacion,
                    Activo = f.Activo,
                    SelloRecepcion = f.SelloRecibido,
                    FechaTransmision = f.FechaTransmision,
                    HoraTransmision = f.HoraTransmision,
                    TipoDte = f.TipoDocumento != null ? f.TipoDocumento.Codigo : string.Empty,
                    VendedorId = f.VendedorId,
                    VendedorNombre = f.Vendedor != null ? f.Vendedor.Nombre : null,
                    VendedorCodigo = f.Vendedor != null ? f.Vendedor.Codigo : null,
                    CajaCodigo = f.Caja != null ? f.Caja.Codigo : null,
                    UsuarioId = f.UsuarioId,
                    CatTipoTransmisionId = f.CatTipoTransmisionId,
                    LoteId = f.LoteId,
                    SucursalId = f.SucursalId,
                    EventoContingenciaId = f.EventoContingenciaId,
                    Ambiente = f.Ambiente
                })
                .ToPaginatedListAsync(pageNumber, pageSize);

            return new PaginatedResponse<FacturaListDto>
            {
                Items = pagedData,
                TotalCount = pagedData.TotalCount,
                CurrentPage = pagedData.CurrentPage,
                PageSize = pagedData.PageSize,
                TotalPages = pagedData.TotalPages
            };
        }

        public async Task<PaginatedResponse<FacturaListDto>> ObtenerFacturasDiferidasAsync(
            PaginatedRequest request,
            int emisorId,
            List<int>? sucursalIds = null,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null,
            int? usuarioId = null,
            string? ambiente = null)
        {
            var query = _context.Facturas
                .Where(f => f.EmisorId == emisorId && (
                    f.EstadoHacienda == "PENDIENTE_LOTE"
                    || f.EventoContingenciaId != null
                    || f.CatTipoTransmisionId == 2
                    || f.LoteId != null
                ));

            if (!string.IsNullOrEmpty(ambiente))
                query = query.Where(f => f.Ambiente == ambiente);

            if (sucursalIds != null && sucursalIds.Count > 0)
            {
                query = query.Where(f => f.SucursalId.HasValue && sucursalIds.Contains(f.SucursalId.Value));
            }

            if (usuarioId.HasValue)
            {
                query = query.Where(f => f.UsuarioId == usuarioId.Value);
            }

            if (fechaDesde.HasValue)
            {
                var fechaDesdeUtc = DateTime.SpecifyKind(fechaDesde.Value.Date, DateTimeKind.Utc);
                query = query.Where(f => f.FechaEmision >= fechaDesdeUtc);
            }

            if (fechaHasta.HasValue)
            {
                var fechaHastaUtc = DateTime.SpecifyKind(fechaHasta.Value.Date.AddDays(1), DateTimeKind.Utc);
                query = query.Where(f => f.FechaEmision < fechaHastaUtc);
            }

            query = query.OrderByDescending(f => f.FechaEmision).AsQueryable();

            var pagedData = await query
                .Select(f => new FacturaListDto
                {
                    Id = f.Id,
                    CodigoGeneracion = f.CodigoGeneracion,
                    NumeroControl = f.NumeroControl,
                    FechaEmision = f.FechaEmision,
                    HoraEmision = f.HoraEmision,
                    ReceptorNombre = f.Receptor != null ? f.Receptor.NombreRazonSocial : "Consumidor Final",
                    ReceptorNumeroDocumento = f.Receptor != null ? (f.Receptor.NumeroDocumento ?? string.Empty) : string.Empty,
                    TotalPagar = f.TotalPagar,
                    EstadoHacienda = f.EstadoHacienda,
                    FechaCreacion = f.FechaCreacion,
                    Activo = f.Activo,
                    SelloRecepcion = f.SelloRecibido,
                    FechaTransmision = f.FechaTransmision,
                    HoraTransmision = f.HoraTransmision,
                    TipoDte = f.TipoDocumento != null ? f.TipoDocumento.Codigo : string.Empty,
                    VendedorId = f.VendedorId,
                    VendedorNombre = f.Vendedor != null ? f.Vendedor.Nombre : null,
                    VendedorCodigo = f.Vendedor != null ? f.Vendedor.Codigo : null,
                    CajaCodigo = f.Caja != null ? f.Caja.Codigo : null,
                    UsuarioId = f.UsuarioId,
                    CatTipoTransmisionId = f.CatTipoTransmisionId,
                    LoteId = f.LoteId,
                    SucursalId = f.SucursalId,
                    EventoContingenciaId = f.EventoContingenciaId,
                    Ambiente = f.Ambiente
                })
                .ToPaginatedListAsync(request.PageNumber, request.PageSize);

            return new PaginatedResponse<FacturaListDto>
            {
                Items = pagedData,
                TotalCount = pagedData.TotalCount,
                CurrentPage = pagedData.CurrentPage,
                PageSize = pagedData.PageSize,
                TotalPages = pagedData.TotalPages
            };
        }

        /// <summary>
        /// Calcula el tiempo restante antes de que venza el período de envío
        /// </summary>
        public async Task<TiempoRestanteDto> ObtenerTiempoRestanteAsync(int facturaId)
        {
            var factura = await _context.Facturas
                .FirstOrDefaultAsync(f => f.Id == facturaId);

            if (factura == null)
                throw new InvalidOperationException("Factura no encontrada");

            var fechaEmisionConHora = factura.FechaEmision.Date.Add(factura.HoraEmision);
            var ahora = DateTime.UtcNow;

            // Determinación de plazos corregida:
            // Group 2 (3 Meses): 01, 11, 14
            // Group 1 (1 Día): Resto (03, 05, etc)

            var tipoDocumento = await _context.CatTiposDocumento.FindAsync(factura.CatTipoDocumentoId);
            string codigoTipo = tipoDocumento?.Codigo ?? "01";
            var tiposTresMeses = new[] { "01", "11", "14" };

            DateTime fechaLimite;
            // string mensajePlazo; // unused

            if (tiposTresMeses.Contains(codigoTipo))
            {
                // 3 meses
                fechaLimite = fechaEmisionConHora.AddMonths(3);
                // mensajePlazo = "3 meses";
            }
            else
            {
                // 1 día (hasta 23:59:59 del día siguiente)
                fechaLimite = fechaEmisionConHora.Date.AddDays(1).Add(new TimeSpan(23, 59, 59));
                // mensajePlazo = "1 día";
            }

            var tiempoRestante = fechaLimite - ahora;
            var segundosRestantes = (long)Math.Max(0, tiempoRestante.TotalSeconds);

            string mensaje;
            if (segundosRestantes == 0)
            {
                mensaje = "El plazo de transmisión ha expirado";
            }
            else if (tiempoRestante.TotalHours > 24)
            {
                mensaje = $"Quedan {(int)tiempoRestante.TotalDays} días para transmitir";
            }
            else
            {
                mensaje = $"Quedan {(int)tiempoRestante.TotalHours}h {tiempoRestante.Minutes}m para transmitir";
            }

            return new TiempoRestanteDto
            {
                FacturaId = factura.Id,
                NumeroControl = factura.NumeroControl,
                FechaEmision = factura.FechaEmision,
                FechaLimiteEnvio = fechaLimite,
                SegundosRestantes = segundosRestantes,
                EsUltimoDiaMes = false, // Deprecado con nueva logica
                Mensaje = mensaje,
                EstaPorVencer = tiempoRestante.TotalHours < 24 // Alerta si queda menos de un día
            };
        }

        /// <summary>
        /// Reenvía el correo con el DTE al receptor
        /// </summary>
        public async Task<bool> ReenviarCorreoAsync(int facturaId)
        {
            var factura = await _context.Facturas
                .Include(f => f.Receptor)
                .FirstOrDefaultAsync(f => f.Id == facturaId);

            if (factura == null)
                return false;

            if (factura.EstadoHacienda != "PROCESADO")
                throw new InvalidOperationException("Solo se pueden reenviar correos de facturas procesadas");

            var emailReceptor = factura.Receptor?.CorreoElectronico;
            if (string.IsNullOrWhiteSpace(emailReceptor))
                throw new InvalidOperationException("El receptor no tiene correo electrónico configurado");

            await _emailService.ReenviarDteAsync(facturaId);

            return true;
        }

        /// <summary>
        /// Guarda una factura sin firmar en estado PENDIENTE_ENVIO
        /// </summary>
        public async Task<FacturaElectronicaResponseDto> GuardarComoPendienteAsync(
            int emisorId,
            CreateFacturaElectronicaDto facturaDto)
        {
            // Reutilizar la lógica de CreateAsync pero sin enviar a Hacienda
            // Crear factura de forma similar a CreateAsync pero establecer EstadoHacienda = "PENDIENTE_ENVIO"

            _logger.LogInformation("[FACTURA-PENDIENTE] Guardando factura como pendiente para emisor {EmisorId}", emisorId);

            // Obtener emisor
            var emisor = await _context.Emisores
                .Include(e => e.AmbienteDestino)
                .FirstOrDefaultAsync(e => e.Id == emisorId);

            if (emisor == null)
                throw new InvalidOperationException("Emisor no encontrado");

            // Validar que la sucursal y caja existen
            var sucursal = await _context.Sucursales
                .Include(s => s.TipoEstablecimiento)
                .FirstOrDefaultAsync(s => s.Id == facturaDto.SucursalId && s.EmisorId == emisorId);

            if (sucursal == null)
                throw new InvalidOperationException("Sucursal no encontrada o no pertenece al emisor");

            var caja = await _context.Cajas
                .FirstOrDefaultAsync(c => c.Id == facturaDto.CajaId && c.SucursalId == facturaDto.SucursalId);

            if (caja == null)
                throw new InvalidOperationException("Caja no encontrada o no pertenece a la sucursal");

            // Contingencia (TipoOperacion=2) o diferido (TipoModelo=2): el documento se
            // entrega al cliente en la venta con su número, por lo que se reserva el
            // correlativo al crear. Modelo normal: NO se reserva; el número se genera al
            // enviar (EnviarFacturaIndividualAsync), así toda factura emitida sigue el
            // correlativo de la última emitida, sin huecos.
            bool esContingenciaODiferido = facturaDto.Identificacion.TipoOperacion == 2
                || facturaDto.Identificacion.TipoModelo == 2;

            string numeroControl = esContingenciaODiferido
                ? NumeroControlHelper.Generar(
                    facturaDto.Identificacion.TipoDte,
                    sucursal.TipoEstablecimiento?.Codigo,
                    sucursal.CodigoEstablecimiento,
                    caja.CodPuntoVentaMH,
                    await ObtenerSiguienteCorrelativoAsync(
                        emisorId,
                        ConvertirTipoDteACatalogoId(facturaDto.Identificacion.TipoDte),
                        facturaDto.Identificacion.TipoDte,
                        sucursal.CodigoEstablecimiento,
                        caja.CodPuntoVentaMH,
                        facturaDto.Identificacion.FechaEmision.Year,
                        emisor.AmbienteDestino?.Codigo ?? "00"))
                : string.Empty;

            // Crear la factura (sin enviar a Hacienda)
            var factura = new FacturaElectronica
            {
                EmisorId = emisorId,
                SucursalId = facturaDto.SucursalId,
                CajaId = facturaDto.CajaId,
                VendedorId = facturaDto.VendedorId,
                UsuarioId = _currentUserService.GetUsuarioId(),
                CodigoGeneracion = GenerarCodigoGeneracion(),
                NumeroControl = numeroControl,
                FechaEmision = DateTime.SpecifyKind(facturaDto.Identificacion.FechaEmision, DateTimeKind.Utc),
                HoraEmision = TimeSpan.Parse(facturaDto.Identificacion.HoraEmision),
                Version = ObtenerVersionSegunTipoDte(facturaDto.Identificacion.TipoDte),
                Ambiente = emisor.AmbienteDestino?.Codigo ?? "00",
                CatTipoDocumentoId = ConvertirTipoDteACatalogoId(facturaDto.Identificacion.TipoDte),
                CatModeloFacturacionId = facturaDto.Identificacion.TipoModelo,
                CatTipoTransmisionId = facturaDto.Identificacion.TipoOperacion,
                // CatMonedaId usa el valor por defecto (1 = USD) definido en la entidad
                // Contingencia/Diferido → PENDIENTE_LOTE (requiere evento de contingencia antes de enviar)
                // Normal → PENDIENTE_ENVIO (se envía directamente por el background service)
                EstadoHacienda = esContingenciaODiferido ? "PENDIENTE_LOTE" : "PENDIENTE_ENVIO",
                FechaCreacion = DateTime.UtcNow,
                Activo = true
            };

            // Asignar Código de Vendedor para histórico (como en CreateAsync)
            if (facturaDto.VendedorId.HasValue)
            {
                var vendedor = await _context.Vendedores.FindAsync(facturaDto.VendedorId.Value);
                if (vendedor != null)
                {
                    factura.CodigoVendedor = vendedor.Codigo;
                }
            }

            // Asignar receptor (misma lógica que CreateAsync)
            if (facturaDto.ReceptorId.HasValue)
            {
                factura.ReceptorId = facturaDto.ReceptorId.Value;
            }
            else if (facturaDto.Receptor != null)
            {
                var receptorExistente = await _context.Receptores
                    .FirstOrDefaultAsync(r => r.NumeroDocumento == facturaDto.Receptor.NumDocumento
                                            && r.EmisorId == emisorId);
                if (receptorExistente != null)
                {
                    factura.ReceptorId = receptorExistente.Id;
                }
                else
                {
                    var nuevoReceptor = new Receptor
                    {
                        EmisorId = emisorId,
                        CatTipoDocumentoIdentificacionReceptorId = ConvertirTipoDocumentoACatalogoId(facturaDto.Receptor.TipoDocumento),
                        NumeroDocumento = string.IsNullOrWhiteSpace(facturaDto.Receptor.NumDocumento) ? null : facturaDto.Receptor.NumDocumento,
                        Nrc = facturaDto.Receptor.Nrc,
                        NombreRazonSocial = facturaDto.Receptor.Nombre,
                        CodigoActividad = facturaDto.Receptor.CodActividad,
                        DescripcionActividad = facturaDto.Receptor.DescActividad,
                        CatDepartamentoId = ConvertirDepartamentoACatalogoId(facturaDto.Receptor.Direccion?.Departamento),
                        CatMunicipioId = ConvertirMunicipioACatalogoId(facturaDto.Receptor.Direccion?.Municipio),
                        CatDistritoId = await ConvertirDistritoACatalogoId(facturaDto.Receptor.Direccion?.Departamento, facturaDto.Receptor.Direccion?.Municipio, facturaDto.Receptor.Direccion?.Distrito),
                        Direccion = facturaDto.Receptor.Direccion?.Complemento ?? "",
                        Telefono = facturaDto.Receptor.Telefono ?? "",
                        CorreoElectronico = facturaDto.Receptor.Correo ?? "",
                        FechaCreacion = DateTime.UtcNow,
                        Activo = true
                    };

                    _context.Receptores.Add(nuevoReceptor);
                    await _context.SaveChangesAsync();
                    factura.ReceptorId = nuevoReceptor.Id;
                }
            }

            // Validar dirección del receptor (tetrada CAT-008 + complemento) según el tipo de DTE (Normativa V2.0)
            await ValidarDireccionReceptorAsync(facturaDto.Identificacion.TipoDte, factura.ReceptorId);

            // Copiar totales del resumen
            factura.TotalNoSujeto = facturaDto.Resumen.TotalNoSuj;
            factura.TotalExento = facturaDto.Resumen.TotalExenta;
            factura.TotalGravado = facturaDto.Resumen.TotalGravada;
            factura.SubTotalVentas = facturaDto.Resumen.SubTotalVentas ?? 0;
            factura.TotalPagar = facturaDto.Resumen.TotalPagar;
            factura.TotalLetras = facturaDto.Resumen.TotalLetras;
            factura.CatCondicionOperacionId = facturaDto.Resumen.CondicionOperacion;

            // Campos adicionales del resumen (para que coincida con CreateAsync)
            factura.SubTotal = facturaDto.Resumen.SubTotal;
            factura.TotalIva = facturaDto.Resumen.TotalIva;
            factura.MontoTotalOperacion = facturaDto.Resumen.MontoTotalOperacion ?? 0;
            factura.TotalDescuento = facturaDto.Resumen.TotalDescu ?? 0;
            factura.DescuentoNoSujeto = facturaDto.Resumen.DescuNoSuj ?? 0;
            factura.DescuentoExento = facturaDto.Resumen.DescuExenta ?? 0;
            factura.DescuentoGravado = facturaDto.Resumen.DescuGravada ?? 0;
            factura.PorcentajeDescuento = facturaDto.Resumen.PorcentajeDescuento ?? 0;
            factura.IvaPercibido = facturaDto.Resumen.IvaPerci1 ?? 0;
            factura.IvaRetenido = facturaDto.Resumen.IvaRete1 ?? 0;
            factura.RetencionRenta = facturaDto.Resumen.ReteRenta ?? 0;
            factura.TotalNoGravado = facturaDto.Resumen.TotalNoGravado ?? 0;
            factura.SaldoFavor = facturaDto.Resumen.SaldoFavor ?? 0;
            factura.NumPagoElectronico = facturaDto.Resumen.NumPagoElectronico;
            factura.Observaciones = facturaDto.Observaciones;

            // FSE (tipo 14): override resumen fields — remap semántico
            if (facturaDto.Identificacion.TipoDte == "14")
            {
                factura.TotalGravado = facturaDto.Resumen.TotalCompras ?? facturaDto.CuerpoDocumento.Sum(i => (i.Compra ?? 0) + (i.MontoDescuento ?? 0));
                factura.DescuentoGravado = facturaDto.Resumen.Descu ?? 0;
                factura.TotalNoSujeto = 0;
                factura.TotalExento = 0;
                factura.SubTotalVentas = 0;
                factura.DescuentoNoSujeto = 0;
                factura.DescuentoExento = 0;
                factura.PorcentajeDescuento = 0;
                factura.TotalIva = 0;
                factura.IvaPercibido = 0;
                factura.MontoTotalOperacion = 0;
                factura.TotalNoGravado = 0;
                factura.SaldoFavor = 0;
                factura.TotalLetras = FacturaCalculosHelper.ConvertirMontoALetras(factura.TotalPagar);
            }

            // Agregar detalles (items)
            foreach (var itemDto in facturaDto.CuerpoDocumento)
            {
                // Obtener stock y asignar CostoUnitario automáticamente
                if (itemDto.ProductoId.HasValue && itemDto.BodegaId.HasValue)
                {
                    var stock = await _context.StocksBodega
                        .FirstOrDefaultAsync(s => s.ProductoId == itemDto.ProductoId.Value &&
                                                   s.BodegaId == itemDto.BodegaId.Value);

                    if (stock != null)
                    {
                        itemDto.CostoUnitario = stock.CostoPromedio;

                        _logger.LogDebug("[FACTURA-PENDIENTE-DETALLE] Item {NumItem}: Costo Promedio asignado = {CostoPromedio}",
                            itemDto.NumItem, stock.CostoPromedio);
                    }
                    else
                    {
                        // Fallback: usar PrecioCosto del producto cuando no existe StockBodega
                        var producto = await _context.ProductosServicios
                            .FirstOrDefaultAsync(p => p.Id == itemDto.ProductoId.Value);
                        itemDto.CostoUnitario = producto?.PrecioCosto ?? 0;
                        _logger.LogWarning("[FACTURA-PENDIENTE-DETALLE] Item {NumItem}: No se encontró stock para ProductoId={ProductoId}, BodegaId={BodegaId}. Usando PrecioCosto={PrecioCosto}",
                            itemDto.NumItem, itemDto.ProductoId, itemDto.BodegaId, itemDto.CostoUnitario);
                    }
                }

                // FSE (tipo 14): mapear Compra -> VentaGravada, sin IVA
                bool esFSEPendiente = facturaDto.Identificacion.TipoDte == "14";

                var detalle = new FacturaElectronicaDetalle
                {
                    NumeroItem = itemDto.NumItem,
                    CatTipoItemId = itemDto.TipoItem,
                    CodigoProducto = itemDto.Codigo ?? string.Empty,
                    Descripcion = itemDto.Descripcion,
                    Cantidad = itemDto.Cantidad,
                    CatUnidadMedidaId = itemDto.UniMedida,
                    PrecioUnitario = itemDto.PrecioUni,
                    MontoDescuento = itemDto.MontoDescuento ?? 0,
                    VentaNoSujeta = esFSEPendiente ? 0 : itemDto.VentaNoSuj,
                    VentaExenta = esFSEPendiente ? 0 : itemDto.VentaExenta,
                    VentaGravada = esFSEPendiente ? (itemDto.Compra ?? 0) : itemDto.VentaGravada,
                    IvaItem = esFSEPendiente ? 0 : itemDto.IvaItem,
                    ProductoId = itemDto.ProductoId,
                    BodegaId = itemDto.BodegaId,
                    CostoUnitario = itemDto.CostoUnitario ?? 0,
                    // Campos MH que faltaban (consistente con CreateAsync)
                    NumeroDocumentoRelacionado = itemDto.NumeroDocumento,
                    CodTributo = itemDto.CodTributo,
                    TributosAplicados = itemDto.Tributos != null && itemDto.Tributos.Any()
                        ? string.Join(",", itemDto.Tributos)
                        : null,
                    PrecioSugeridoVenta = itemDto.Psv,
                    NoGravado = itemDto.NoGravado ?? 0,
                    FechaCreacion = DateTime.UtcNow,
                    Activo = true
                };
                factura.Detalles.Add(detalle);
            }

            // Red de seguridad: resincronizar el resumen con los ítems (evita [020] de MH).
            ResincronizarResumenConDetalles(factura, facturaDto.Identificacion.TipoDte);

            // Agregar formas de pago - FALTA ESTO
            foreach (var pagoDto in facturaDto.Resumen.Pagos)
            {
                var pago = new Pago
                {
                    CatFormaPagoId = pagoDto.CatFormaPagoId,
                    Monto = pagoDto.Monto,
                    Referencia = pagoDto.Referencia,
                    CatPlazoId = pagoDto.CatPlazoId,
                    Periodo = pagoDto.Periodo,
                    FechaCreacion = DateTime.UtcNow,
                    Activo = true
                };
                factura.Pagos.Add(pago);
            }

            // Agregar tributos del resumen (para CCF)
            if (facturaDto.Resumen.Tributos != null && facturaDto.Resumen.Tributos.Any())
            {
                foreach (var tributoDto in facturaDto.Resumen.Tributos)
                {
                    var tributo = new FacturaTributo
                    {
                        CatTributoId = await ConvertirTributoCodigoACatalogoIdAsync(tributoDto.Codigo),
                        CodigoAttribute = tributoDto.Codigo,
                        Descripcion = tributoDto.Descripcion,
                        Valor = tributoDto.Valor,
                        FechaCreacion = DateTime.UtcNow,
                        Activo = true
                    };
                    factura.Tributos.Add(tributo);
                }
            }

            // Procesar Extensión (si viene)
            if (facturaDto.Extension != null)
            {
                var extension = new FacturaExtencion
                {
                    NombEntrega = facturaDto.Extension.NombEntrega,
                    DocuEntrega = facturaDto.Extension.DocuEntrega,
                    NombRecibe = facturaDto.Extension.NombRecibe,
                    DocuRecibe = facturaDto.Extension.DocuRecibe,
                    PlacaVehiculo = facturaDto.Extension.PlacaVehiculo,
                    Observaciones = facturaDto.Extension.Observaciones,
                    FechaCreacion = DateTime.UtcNow,
                    Activo = true
                };
                factura.Extension = extension;
            }

            // Procesar VentaTercero (solo CCF)
            if (facturaDto.VentaTercero != null)
            {
                var ventaTercero = new VentaTercero
                {
                    Nit = facturaDto.VentaTercero.Nit,
                    Nombre = facturaDto.VentaTercero.Nombre,
                    FechaCreacion = DateTime.UtcNow,
                    Activo = true
                };
                factura.VentaTercero = ventaTercero;
            }

            // Procesar OtrosDocumentos (solo CCF, máximo 10)
            if (facturaDto.OtrosDocumentos != null && facturaDto.OtrosDocumentos.Any())
            {
                foreach (var docDto in facturaDto.OtrosDocumentos)
                {
                    var otroDoc = new OtroDocumento
                    {
                        CodDocAsociado = docDto.CodDocAsociado,
                        DescDocumento = docDto.DescDocumento,
                        DetalleDocumento = docDto.DetalleDocumento,
                        FechaCreacion = DateTime.UtcNow,
                        Activo = true
                    };

                    if (docDto.CodDocAsociado == 3 && docDto.Medico != null)
                    {
                        otroDoc.Medico = new MedicoServicio
                        {
                            Nombre = docDto.Medico.Nombre,
                            Nit = docDto.Medico.Nit,
                            DocIdentificacion = docDto.Medico.DocIdentificacion,
                            TipoServicio = docDto.Medico.TipoServicio,
                            FechaCreacion = DateTime.UtcNow,
                            Activo = true
                        };
                    }

                    factura.OtrosDocumentos.Add(otroDoc);
                }
            }

            // Procesar Documentos Relacionados (opcional)
            if (facturaDto.DocumentosRelacionados != null && facturaDto.DocumentosRelacionados.Any())
            {
                foreach (var docDto in facturaDto.DocumentosRelacionados)
                {
                    var docRelacionado = new FacturaDocumentoRelacionado
                    {
                        CatTipoDocumentoId = ConvertirTipoDteACatalogoId(docDto.TipoDocumento),
                        CatTipoGeneracionDocumentoId = docDto.TipoGeneracion,
                        NumeroDocumento = docDto.NumeroDocumento,
                        FechaEmision = DateTime.SpecifyKind(docDto.FechaEmision, DateTimeKind.Utc),
                        FechaCreacion = DateTime.UtcNow,
                        Activo = true
                    };
                    factura.DocumentosRelacionados.Add(docRelacionado);
                }
            }

            // Procesar Apéndices (opcional)
            if (facturaDto.Apendices != null && facturaDto.Apendices.Any())
            {
                foreach (var apendiceDto in facturaDto.Apendices)
                {
                    var apendice = new FacturaApendice
                    {
                        Campo = apendiceDto.Campo,
                        Etiqueta = apendiceDto.Etiqueta,
                        Valor = apendiceDto.Valor,
                        FechaCreacion = DateTime.UtcNow,
                        Activo = true
                    };
                    factura.Apendices.Add(apendice);
                }
            }

            _context.Facturas.Add(factura);
            await _context.SaveChangesAsync();

            // INTEGRACIÓN CON INVENTARIO (RESERVA DE STOCK)
            // Se omite la validación y reserva de stock en:
            // - Ambiente de pruebas ("00")
            // - FSE tipo "14": el emisor es el COMPRADOR, no descuenta stock
            try
            {
                if (factura.Ambiente != "00" && facturaDto.Identificacion.TipoDte != "14")
                {
                    _logger.LogDebug("[FACTURA-PENDIENTE] Validando y reservando stock para factura {FacturaId}", factura.Id);

                    // 1. Validar stock disponible
                    var stockDisponible = await _inventarioService.ValidarStockDisponibleAsync(factura.Id);
                    if (!stockDisponible)
                    {
                        _logger.LogWarning("[FACTURA-PENDIENTE] Stock insuficiente para factura {FacturaId}", factura.Id);

                        // Eliminar factura si no hay stock
                        _context.Facturas.Remove(factura);
                        await _context.SaveChangesAsync();
                        throw new InvalidOperationException("Stock insuficiente para uno o más productos de la factura");
                    }

                    // 2. Reservar stock (Mueve de Disponible a Reservado)
                    await _inventarioService.ReservarStockAsync(factura.Id);

                    _logger.LogInformation("[FACTURA-PENDIENTE] Stock reservado exitosamente para factura {FacturaId}", factura.Id);
                }
                else
                {
                    var razonPendiente = factura.Ambiente == "00" ? "Ambiente de pruebas" : "Tipo DTE 14 (compra, no venta)";
                    _logger.LogInformation("[FACTURA-PENDIENTE] {Razon}: se omite validación y reserva de stock para factura {FacturaId}", razonPendiente, factura.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FACTURA-PENDIENTE] Error al reservar stock para factura {FacturaId}", factura.Id);

                // Si la factura se guardó pero falló la reserva, deberíamos revertir la factura
                // para no dejar inconsistencias (factura sin stock reservado)
                if (factura.Id > 0)
                {
                    try
                    {
                        _context.Facturas.Remove(factura);
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogError(cleanupEx, "Error al limpiar factura fallida {FacturaId}", factura.Id);
                    }
                }

                // Re-lanzar excepción original o una amigable
                if (ex is InvalidOperationException) throw;
                throw new InvalidOperationException($"Error al reservar inventario: {ex.Message}", ex);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("[FACTURA-PENDIENTE] Factura guardada como pendiente con ID={FacturaId}, Estado={Estado}",
                factura.Id, factura.EstadoHacienda);

            // Recargar la factura con todas las navegaciones necesarias para el mapeo
            var facturaParaMapeo = await _context.Facturas
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.TipoEstablecimiento)
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.Departamento)
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.Municipio)
                .Include(f => f.TipoDocumento)
                .Include(f => f.Receptor!)
                    .ThenInclude(r => r.TipoDocumento)
                .Include(f => f.Receptor!)
                    .ThenInclude(r => r.Departamento)
                .Include(f => f.Receptor!)
                    .ThenInclude(r => r.Municipio)
                .Include(f => f.Detalles)
                .Include(f => f.Pagos)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.TipoEstablecimiento)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.Departamento)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.Municipio)
                .Include(f => f.Caja)
                .Include(f => f.Vendedor)
                .Include(f => f.VentaTercero)
                .Include(f => f.OtrosDocumentos)
                .Include(f => f.DocumentosRelacionados)
                    .ThenInclude(d => d.TipoDocumento)
                .Include(f => f.Extension)
                .Include(f => f.Apendices)
                .Include(f => f.Tributos)
                .Include(f => f.CondicionOperacion)
                .FirstOrDefaultAsync(f => f.Id == factura.Id);

            _telemetry.TrackEvent("smartix.factura.creada",
                properties: new Dictionary<string, string>
                {
                    ["facturaId"] = factura.Id.ToString(),
                    ["numeroControl"] = factura.NumeroControl ?? string.Empty,
                    ["tipoDte"] = factura.TipoDocumento?.Codigo ?? string.Empty,
                    ["estado"] = factura.EstadoHacienda ?? string.Empty,
                    ["origen"] = "guardar_como_pendiente"
                },
                measurements: new Dictionary<string, double>
                {
                    ["totalPagar"] = (double)factura.TotalPagar
                });

            return _mapper.Map<FacturaElectronicaResponseDto>(facturaParaMapeo);
        }

        /// <summary>
        /// Envía una factura individual (firma y transmite a Hacienda)
        /// </summary>
        public async Task<FacturaElectronicaResponseDto> EnviarFacturaIndividualAsync(int facturaId)
        {
            var factura = await _context.Facturas
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.AmbienteDestino)
                .Include(f => f.TipoDocumento)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.TipoEstablecimiento)
                .Include(f => f.Caja)
                .FirstOrDefaultAsync(f => f.Id == facturaId);

            if (factura == null)
                throw new InvalidOperationException("Factura no encontrada");

            if (factura.EstadoHacienda != "PENDIENTE_ENVIO")
                throw new InvalidOperationException("Solo se pueden enviar facturas en estado PENDIENTE_ENVIO");

            _logger.LogInformation("[FACTURA-INDIVIDUAL] Enviando factura individual ID={FacturaId}, NumeroControl={NumeroControl}",
                facturaId, factura.NumeroControl);

            // Número de control diferido: las pendientes del modelo normal se guardan
            // SIN número (Task 1). Se genera aquí, justo antes de transmitir, para que
            // tome el correlativo siguiente a la última factura EMITIDA. Las facturas
            // que ya tienen número (p. ej. reintentos desde ERROR) no se regeneran.
            if (string.IsNullOrEmpty(factura.NumeroControl))
            {
                var anioNc = factura.FechaEmision.Year;
                var ambienteNc = factura.Emisor?.AmbienteDestino?.Codigo ?? "00";
                var correlativoNc = await ObtenerSiguienteCorrelativoAsync(
                    factura.EmisorId,
                    factura.CatTipoDocumentoId,
                    factura.TipoDocumento?.Codigo ?? "01",
                    factura.Sucursal?.CodigoEstablecimiento,
                    factura.Caja?.CodPuntoVentaMH,
                    anioNc,
                    ambienteNc);

                factura.NumeroControl = NumeroControlHelper.Generar(
                    factura.TipoDocumento?.Codigo ?? "01",
                    factura.Sucursal?.TipoEstablecimiento?.Codigo,
                    factura.Sucursal?.CodigoEstablecimiento,
                    factura.Caja?.CodPuntoVentaMH,
                    correlativoNc);

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "[FACTURA-INDIVIDUAL] Numero de control generado al enviar: {NumeroControl} (FacturaId={FacturaId})",
                    factura.NumeroControl, factura.Id);
            }

            while (true)
            {
                try
                {
                    // Generar JSON del DTE (se regenera en cada intento para reflejar nuevo NumeroControl/CodigoGeneracion)
                    var jsonDte = await GenerateJsonDteAsync(factura.Id, factura.EmisorId);

                    // Enviar con estrategia de reintentos
                    var ambiente = factura.Emisor?.AmbienteDestino?.Codigo ?? "00";
                    var tipoDteCode = factura.TipoDocumento?.Codigo ?? "01";

                    var resultado = await _retryService.EnviarConReintentosAsync(
                        factura.Id,
                        factura.EmisorId,
                        jsonDte,
                        factura.Version,
                        tipoDteCode,
                        factura.CodigoGeneracion,
                        ambiente);

                    if (resultado.EsContingencia)
                    {
                        _logger.LogWarning("[FACTURA-CONTINGENCIA] Factura {NumeroControl} entra en contingencia. Motivo: {Motivo}",
                            factura.NumeroControl, resultado.Diagnostico?.Razon);

                        factura.EstadoHacienda = "PENDIENTE_LOTE";
                        factura.CatModeloFacturacionId = 2;
                        factura.CatTipoTransmisionId = 2;
                        factura.TipoContingenciaSugerido = resultado.Diagnostico?.TipoContingenciaSugerido;
                        factura.DetalleErrorEnvio = resultado.Diagnostico?.Razon;
                        factura.FechaErrorEnvio = DateTime.UtcNow;
                        factura.IntentosEnvio = 4;

                        try
                        {
                            var tipoContingencia = resultado.Diagnostico?.TipoContingenciaSugerido ?? 1;
                            var motivoContingencia = resultado.Diagnostico?.Razon ?? "Sistema del Ministerio de Hacienda no disponible";
                            var eventoId = await _contingenciaService.ObtenerOCrearEventoContingenciaAutomaticoAsync(
                                factura.EmisorId, tipoContingencia, motivoContingencia);
                            factura.EventoContingenciaId = eventoId;
                        }
                        catch (Exception exEvento)
                        {
                            _logger.LogWarning(exEvento,
                                "[FACTURA-CONTINGENCIA] No se pudo asociar evento automático para factura {FacturaId}",
                                factura.Id);
                        }

                        await _context.SaveChangesAsync();

                        break;
                    }
                    else if (resultado.Exitoso)
                    {
                        // Si MH dice PROCESADO pero no devuelve sello, marcar como RECHAZADO
                        if (string.IsNullOrEmpty(resultado.SelloRecibido))
                        {
                            _logger.LogWarning("[FACTURA-MH] Factura {NumeroControl} reportada como PROCESADO por MH pero SIN sello de recepción. Se marca como RECHAZADO.",
                                factura.NumeroControl);

                            factura.EstadoHacienda = "RECHAZADO";
                            factura.Observaciones = "MH respondió PROCESADO pero no devolvió sello de recepción";
                            await SincronizarLoteDetalleAsync(factura, factura.Observaciones);
                            await _context.SaveChangesAsync();
                            break;
                        }

                        _logger.LogInformation("[FACTURA-MH] Factura {NumeroControl} APROBADA por MH. Sello: {Sello}",
                            factura.NumeroControl, resultado.SelloRecibido);

                        if (factura.Ambiente != "00")
                        {
                            await _inventarioService.ConfirmarVentaAsync(factura.Id);
                        }

                        factura.EstadoHacienda = "PROCESADO";
                        factura.SelloRecibido = resultado.SelloRecibido;
                        factura.JsonFirmado = resultado.DocumentoFirmado;
                        factura.FechaTransmision = DateTime.UtcNow.Date;
                        factura.HoraTransmision = DateTime.UtcNow.TimeOfDay;
                        await SincronizarLoteDetalleAsync(factura);
                        await _context.SaveChangesAsync();

                        await IntentarEnviarEmailDteAsync(factura.Id, factura.ReceptorId == 0 ? null : (int?)factura.ReceptorId);
                        break;
                    }
                    else
                    {
                        // Si es NumeroControl duplicado → regenerar y reintentar automáticamente
                        if (await RegenerarSiNumeroControlDuplicadoInternalAsync(factura, resultado.CodigoMsg, resultado.DescripcionMsg))
                            continue;

                        var detallesMh = new List<string>();
                        if (!string.IsNullOrEmpty(resultado.MensajeError))
                            detallesMh.Add(resultado.MensajeError);
                        if (resultado.Observaciones != null && resultado.Observaciones.Any())
                            detallesMh.AddRange(resultado.Observaciones);
                        var motivo = detallesMh.Any()
                            ? string.Join("; ", detallesMh)
                            : "Rechazado por MH";

                        _logger.LogWarning("[FACTURA-MH] Factura {NumeroControl} RECHAZADA/FALLIDA. Motivo: {Motivo}",
                           factura.NumeroControl, motivo);

                        await _inventarioService.LiberarReservasAsync(factura.Id);

                        factura.EstadoHacienda = "RECHAZADO";
                        factura.Observaciones = motivo;
                        await SincronizarLoteDetalleAsync(factura, motivo);
                        await _context.SaveChangesAsync();

                        throw new InvalidOperationException($"Factura rechazada o fallida: {motivo}");
                    }
                }
                catch (Exception ex) when (ex is not InvalidOperationException)
                {
                    _logger.LogError(ex, "[FACTURA-INDIVIDUAL] Error al comunicarse con MH para factura {NumeroControl}", factura.NumeroControl);

                    factura.EstadoHacienda = "ERROR";
                    factura.Observaciones = $"Error al enviar a MH: {ex.Message}";
                    await _context.SaveChangesAsync();

                    throw new InvalidOperationException($"Error al comunicarse con Ministerio de Hacienda: {ex.Message}", ex);
                }
            }

            return _mapper.Map<FacturaElectronicaResponseDto>(factura);
        }

        // Tope de reintentos automáticos de transmisión antes de escalar a contingencia (regla 13.2.1).
        private const int MaxIntentosReintentoError = 3;
        // Separación mínima entre reintentos automáticos de una misma factura en ERROR.
        private static readonly TimeSpan VentanaReintentoError = TimeSpan.FromMinutes(15);

        public async Task<ReintentoErrorResultadoDto> ReintentarEnviosEnErrorAsync(CancellationToken cancellationToken = default)
        {
            var resultado = new ReintentoErrorResultadoDto();
            var limite = DateTime.UtcNow - VentanaReintentoError;

            // Facturas en ERROR cuyo último error ocurrió hace ≥ 15 min (o sin marca de fecha).
            var candidatas = await _context.Facturas
                .Where(f => f.EstadoHacienda == "ERROR"
                            && (f.FechaErrorEnvio == null || f.FechaErrorEnvio <= limite))
                .OrderBy(f => f.FechaErrorEnvio)
                .ToListAsync(cancellationToken);

            resultado.Candidatas = candidatas.Count;

            foreach (var factura in candidatas)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Tope agotado → escalar a contingencia en vez de reintentar.
                if (factura.IntentosEnvio >= MaxIntentosReintentoError)
                {
                    await EscalarErrorAContingenciaAsync(factura);
                    resultado.Escaladas++;
                    continue;
                }

                // Registrar el intento ANTES de reenviar: así la ventana de 15 min se respeta
                // aunque el reenvío vuelva a fallar (EnviarFacturaIndividualAsync deja la
                // factura en ERROR sin tocar IntentosEnvio/FechaErrorEnvio).
                factura.IntentosEnvio++;
                factura.FechaErrorEnvio = DateTime.UtcNow;
                factura.EstadoHacienda = "PENDIENTE_ENVIO";
                await _context.SaveChangesAsync(cancellationToken);

                try
                {
                    await EnviarFacturaIndividualAsync(factura.Id);
                    resultado.Reintentadas++;
                }
                catch (Exception ex)
                {
                    // EnviarFacturaIndividualAsync ya dejó la factura en su estado final
                    // (ERROR si fue fallo de comunicación, RECHAZADO si MH la rechazó).
                    _logger.LogWarning(ex,
                        "[REINTENTO-ERROR] Reintento {Intento}/{Max} fallido para factura {FacturaId} ({NumeroControl})",
                        factura.IntentosEnvio, MaxIntentosReintentoError, factura.Id, factura.NumeroControl);
                    resultado.Fallidas++;
                }
            }

            if (resultado.Candidatas > 0)
            {
                _logger.LogInformation(
                    "[REINTENTO-ERROR] Pasada completada: {Candidatas} candidatas, {Reintentadas} reintentadas, {Escaladas} escaladas a contingencia, {Fallidas} fallidas",
                    resultado.Candidatas, resultado.Reintentadas, resultado.Escaladas, resultado.Fallidas);
            }

            return resultado;
        }

        // Escala una factura en ERROR (con reintentos agotados) a contingencia diferida:
        // mismo tratamiento que cuando EnviarFacturaIndividualAsync detecta MH no disponible.
        private async Task EscalarErrorAContingenciaAsync(FacturaElectronica factura)
        {
            factura.EstadoHacienda = "PENDIENTE_LOTE";
            factura.CatModeloFacturacionId = 2;
            factura.CatTipoTransmisionId = 2;
            factura.TipoContingenciaSugerido ??= 1;
            if (string.IsNullOrEmpty(factura.DetalleErrorEnvio))
                factura.DetalleErrorEnvio = "Reintentos automáticos de transmisión agotados";
            factura.FechaErrorEnvio = DateTime.UtcNow;

            try
            {
                var eventoId = await _contingenciaService.ObtenerOCrearEventoContingenciaAutomaticoAsync(
                    factura.EmisorId,
                    factura.TipoContingenciaSugerido ?? 1,
                    factura.DetalleErrorEnvio ?? "Reintentos automáticos de transmisión agotados");
                factura.EventoContingenciaId = eventoId;
            }
            catch (Exception exEvento)
            {
                _logger.LogWarning(exEvento,
                    "[REINTENTO-ERROR] No se pudo asociar evento de contingencia automático a la factura {FacturaId} tras agotar reintentos",
                    factura.Id);
            }

            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "[REINTENTO-ERROR] Factura {FacturaId} ({NumeroControl}) escalada a contingencia (PENDIENTE_LOTE) tras agotar {Max} reintentos",
                factura.Id, factura.NumeroControl, MaxIntentosReintentoError);
        }

        // Red de seguridad (hardening): tras recalcular los ítems (RecalcularGravadoSiAplica), el
        // total gravado del resumen —cargado tal cual del DTO del frontend— puede no cuadrar con la
        // suma de las ventas gravadas de los ítems ya recalculados; MH rechaza ese descuadre con [020]
        // (`[resumen.totalGravada] CALCULO INCORRECTO`). Para FE (01), donde los montos van CON IVA
        // incluido, el desfase del gravado se refleja 1:1 en subtotal/monto/total, así que se
        // resincroniza el resumen. Para CCF/NC/ND/FSE la propagación del IVA es distinta y
        // auto-corregir sería arriesgado: solo se alerta para revisión.
        internal void ResincronizarResumenConDetalles(FacturaElectronica factura, string? tipoDte)
        {
            if (factura.Detalles == null || factura.Detalles.Count == 0)
                return;

            var sumaGravadaItems = decimal.Round(factura.Detalles.Sum(d => d.VentaGravada), 2);
            if (Math.Abs(sumaGravadaItems - factura.TotalGravado) <= 0.01m)
                return; // ya cuadra: nada que resincronizar

            var delta = sumaGravadaItems - factura.TotalGravado;

            if (tipoDte == "01")
            {
                _logger.LogWarning(
                    "[RESYNC-RESUMEN] FE {NumeroControl}: el total gravado del resumen ({Resumen}) no cuadra con la suma de ítems ({Items}); se resincroniza (delta {Delta}).",
                    factura.NumeroControl, factura.TotalGravado, sumaGravadaItems, delta);

                factura.TotalGravado = sumaGravadaItems;
                factura.SubTotalVentas = decimal.Round(factura.SubTotalVentas + delta, 2);
                factura.SubTotal = decimal.Round(factura.SubTotal + delta, 2);
                factura.MontoTotalOperacion = decimal.Round(factura.MontoTotalOperacion + delta, 2);
                factura.TotalPagar = decimal.Round(factura.TotalPagar + delta, 2);
                // En FE el TotalIva es informativo (IVA embebido): tomar el de los ítems recalculados.
                factura.TotalIva = decimal.Round(factura.Detalles.Sum(d => d.IvaItem), 2);

                _telemetry.TrackEvent("smartix.factura.resumen_resincronizado",
                    properties: new Dictionary<string, string>
                    {
                        ["numeroControl"] = factura.NumeroControl ?? string.Empty,
                        ["tipoDte"] = tipoDte,
                        ["delta"] = delta.ToString("F2")
                    });
            }
            else
            {
                _logger.LogError(
                    "[RESYNC-RESUMEN] {TipoDte} {NumeroControl}: descuadre entre el total gravado del resumen ({Resumen}) y la suma de ítems ({Items}). NO se auto-corrige; revisar antes de enviar a MH.",
                    tipoDte, factura.NumeroControl, factura.TotalGravado, sumaGravadaItems);

                _telemetry.TrackEvent("smartix.factura.descuadre_resumen_no_corregido",
                    properties: new Dictionary<string, string>
                    {
                        ["numeroControl"] = factura.NumeroControl ?? string.Empty,
                        ["tipoDte"] = tipoDte ?? string.Empty,
                        ["totalGravadoResumen"] = factura.TotalGravado.ToString("F2"),
                        ["sumaItems"] = sumaGravadaItems.ToString("F2")
                    });
            }
        }

        // Plazo máximo (horas) para transmitir un DTE en contingencia desde el sello del Evento
        // de Contingencia (Normativa DTE, Cuadro 4 y regla 13.2.2: 72 h desde el Sello de Recepción
        // del Evento de Contingencia).
        private const int PlazoTransmisionHoras = 72;
        // Antelación con la que se empieza a avisar que el plazo de 72 h está por vencer.
        private static readonly TimeSpan VentanaAvisoPlazo72h = TimeSpan.FromHours(12);
        // Marca idempotente que se deja en Observaciones para no re-marcar/avisar en cada pasada.
        internal const string MarcaPlazo72hVencido = "[PLAZO-72H-VENCIDO]";
        // Días de contingencia consecutivos que obligan a presentar el Informe Técnico (regla 13.2.1.1).
        private const int DiasContingenciaInformeTecnico = 3;

        public async Task<ControlPlazo72hResultadoDto> MarcarDiferidosPorVencer72hAsync(CancellationToken cancellationToken = default)
        {
            var ahora = DateTime.UtcNow;
            var resultado = new ControlPlazo72hResultadoDto();

            // Facturas en contingencia (PENDIENTE_LOTE) asociadas a un Evento de Contingencia.
            var facturas = await _context.Facturas
                .Where(f => f.EstadoHacienda == "PENDIENTE_LOTE" && f.EventoContingenciaId != null)
                .ToListAsync(cancellationToken);

            if (facturas.Count == 0)
                return resultado;

            // El plazo de 72 h corre desde el sello del Evento de Contingencia (FechaTransmisionMH).
            var eventoIds = facturas.Select(f => f.EventoContingenciaId!.Value).Distinct().ToList();
            var eventos = await _context.EventosContingencia
                .Where(e => eventoIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, cancellationToken);

            foreach (var factura in facturas)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (!eventos.TryGetValue(factura.EventoContingenciaId!.Value, out var evento))
                    continue;

                // El plazo de 72 h arranca con el sello del evento; sin sello aún no corre.
                if (evento.FechaTransmisionMH == null)
                    continue;

                var limite = evento.FechaTransmisionMH.Value.AddHours(PlazoTransmisionHoras);
                var restante = limite - ahora;

                if (restante <= TimeSpan.Zero)
                {
                    resultado.Vencidas++;

                    // Marcar una sola vez (idempotente por el texto en Observaciones).
                    var yaMarcada = factura.Observaciones != null
                        && factura.Observaciones.Contains(MarcaPlazo72hVencido);
                    if (!yaMarcada)
                    {
                        resultado.NuevasVencidas++;
                        var aviso = $"{MarcaPlazo72hVencido} El plazo de 72 h para transmitir este DTE en contingencia " +
                                    $"venció el {limite:yyyy-MM-dd HH:mm} UTC (sello del evento: {evento.FechaTransmisionMH:yyyy-MM-dd HH:mm} UTC). " +
                                    "Debe invalidarse y reemitirse.";
                        factura.Observaciones = string.IsNullOrEmpty(factura.Observaciones)
                            ? aviso
                            : $"{factura.Observaciones} {aviso}";

                        _logger.LogWarning(
                            "[PLAZO-72H] Factura {FacturaId} ({NumeroControl}) superó el plazo de 72 h desde el sello del evento {EventoId} (selló {Sello:u}, venció {Limite:u})",
                            factura.Id, factura.NumeroControl, evento.Id, evento.FechaTransmisionMH, limite);

                        _telemetry.TrackEvent("smartix.contingencia.plazo72h_vencido",
                            properties: new Dictionary<string, string>
                            {
                                ["facturaId"] = factura.Id.ToString(),
                                ["numeroControl"] = factura.NumeroControl ?? string.Empty,
                                ["emisorId"] = factura.EmisorId.ToString(),
                                ["eventoContingenciaId"] = evento.Id.ToString()
                            });
                    }
                }
                else if (restante <= VentanaAvisoPlazo72h)
                {
                    resultado.PorVencer++;
                    _logger.LogWarning(
                        "[PLAZO-72H] Factura {FacturaId} ({NumeroControl}) por vencer el plazo de 72 h en {Horas:F1} h (sello del evento {EventoId})",
                        factura.Id, factura.NumeroControl, restante.TotalHours, evento.Id);
                }
            }

            if (resultado.NuevasVencidas > 0)
                await _context.SaveChangesAsync(cancellationToken);

            if (resultado.Vencidas > 0 || resultado.PorVencer > 0)
            {
                _logger.LogInformation(
                    "[PLAZO-72H] Control de plazo: {Vencidas} vencidas ({Nuevas} nuevas), {PorVencer} por vencer",
                    resultado.Vencidas, resultado.NuevasVencidas, resultado.PorVencer);
            }

            return resultado;
        }

        public async Task<InformeTecnicoContingenciaResultadoDto> DetectarContingenciasParaInformeTecnicoAsync(CancellationToken cancellationToken = default)
        {
            var ahora = DateTime.UtcNow;
            var resultado = new InformeTecnicoContingenciaResultadoDto();
            var umbral = ahora.AddDays(-DiasContingenciaInformeTecnico);

            // Contingencia en curso por sujeto pasivo (emisor): facturas aún en PENDIENTE_LOTE
            // sin transmitir. Si la contingencia del emisor (desde la factura más antigua que entró
            // en contingencia) persiste > 3 días, debe presentar el Informe Técnico a MH antes de
            // transmitir el Evento de Contingencia (regla 13.2.1.1).
            var porEmisor = await _context.Facturas
                .Where(f => f.EstadoHacienda == "PENDIENTE_LOTE" && f.FechaErrorEnvio != null)
                .GroupBy(f => f.EmisorId)
                .Select(g => new
                {
                    EmisorId = g.Key,
                    Inicio = g.Min(f => f.FechaErrorEnvio),
                    Cantidad = g.Count()
                })
                .ToListAsync(cancellationToken);

            foreach (var grupo in porEmisor)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (grupo.Inicio == null || grupo.Inicio.Value > umbral)
                    continue;

                resultado.EmisoresQueRequierenInforme++;
                var dias = (ahora - grupo.Inicio.Value).TotalDays;

                _logger.LogWarning(
                    "[INFORME-TECNICO] El emisor {EmisorId} tiene una contingencia que persiste {Dias:F1} días (> {Umbral}) con {Cantidad} DTE en PENDIENTE_LOTE. " +
                    "Debe presentar el Informe Técnico de Contingencia a Hacienda antes de transmitir el Evento de Contingencia (regla 13.2.1.1).",
                    grupo.EmisorId, dias, DiasContingenciaInformeTecnico, grupo.Cantidad);

                _telemetry.TrackEvent("smartix.contingencia.informe_tecnico_requerido",
                    properties: new Dictionary<string, string>
                    {
                        ["emisorId"] = grupo.EmisorId.ToString(),
                        ["diasContingencia"] = dias.ToString("F1"),
                        ["dtePendientes"] = grupo.Cantidad.ToString()
                    });
            }

            return resultado;
        }


        public async Task DescartarFacturaRechazadaAsync(int facturaId, int emisorId)
        {
            var factura = await _context.Facturas
                .FirstOrDefaultAsync(f => f.Id == facturaId && f.EmisorId == emisorId);

            if (factura == null)
                throw new KeyNotFoundException("Factura no encontrada");

            if (factura.EstadoHacienda != "RECHAZADO" && factura.EstadoHacienda != "ERROR")
                throw new InvalidOperationException("Solo se pueden descartar facturas en estado RECHAZADO o ERROR");

            // Otras FKs con RESTRICT que bloquearian el delete: pre-chequear y dar mensaje
            // claro en vez del DATABASE_ERROR generico del middleware global.
            // - LoteDetalles.FacturaElectronicaId: la factura quedo en un lote (raro para
            //   RECHAZADO pero defensivo).
            // - ContingenciaDetalles.FacturaElectronicaId: la factura es parte de un
            //   evento de contingencia.
            // - Invalidaciones.{FacturaElectronicaId,FacturaReemplazoId}: la factura es
            //   origen o reemplazo de una invalidacion (no aplica normalmente a RECHAZADO).
            var enLote = await _context.LoteDetalles.AnyAsync(d => d.FacturaElectronicaId == facturaId);
            if (enLote)
                throw new InvalidOperationException(
                    "No se puede descartar: la factura forma parte de un lote enviado. " +
                    "Para anular este DTE use el flujo de Invalidacion, no Descartar.");

            var enContingencia = await _context.ContingenciaDetalles.AnyAsync(d => d.FacturaElectronicaId == facturaId);
            if (enContingencia)
                throw new InvalidOperationException(
                    "No se puede descartar: la factura esta vinculada a un evento de contingencia. " +
                    "Elimine primero el detalle del evento o use Invalidacion.");

            var tieneInvalidacion = await _context.Invalidaciones.AnyAsync(i =>
                i.FacturaElectronicaId == facturaId || i.FacturaReemplazoId == facturaId);
            if (tieneInvalidacion)
                throw new InvalidOperationException(
                    "No se puede descartar: existe una invalidacion que referencia esta factura " +
                    "(como origen o reemplazo). Revise el flujo de invalidaciones primero.");

            // Revertir stock (solo si no es FSE tipo 14, que nunca descontó stock)
            var tipoDocFact = await _context.CatTiposDocumento
                .Where(t => t.Id == factura.CatTipoDocumentoId)
                .Select(t => t.Codigo)
                .FirstOrDefaultAsync();

            if (tipoDocFact != "14")
            {
                await _inventarioService.RevertirStockFacturaAsync(facturaId);
            }

            // Eliminar la factura (FacturaApendices, FacturaDocumentosRelacionados,
            // FacturaExtensiones, FacturaPagos, FacturaTributos, factura_detalles,
            // OtrosDocumentos, VentaTerceros cascadean por FK CASCADE).
            _context.Facturas.Remove(factura);
            await _context.SaveChangesAsync();
        }

        // ==========================================
        // HELPERS - SINCRONIZACIÓN DE LOTE
        // ==========================================

        /// <summary>
        /// Sincroniza el LoteDetalle y Lote cuando una factura cambia de estado
        /// fuera del flujo normal de lote (ej: envío individual).
        /// NO llama SaveChangesAsync — el caller debe guardar.
        /// </summary>
        private async Task SincronizarLoteDetalleAsync(FacturaElectronica factura, string? motivoRechazo = null)
        {
            if (factura.LoteId == null) return;

            var loteId = factura.LoteId.Value;
            var loteDetalle = await _context.LoteDetalles
                .FirstOrDefaultAsync(d => d.FacturaElectronicaId == factura.Id && d.LoteId == loteId);

            if (loteDetalle == null) return;

            if (factura.EstadoHacienda == "PROCESADO")
            {
                loteDetalle.EstadoDte = "PROCESADO";
                loteDetalle.SelloRecibido = factura.SelloRecibido;
                loteDetalle.FechaConsulta = DateTime.UtcNow;
            }
            else if (factura.EstadoHacienda == "RECHAZADO")
            {
                loteDetalle.EstadoDte = "RECHAZADO";
                loteDetalle.FechaConsulta = DateTime.UtcNow;
                loteDetalle.ObservacionesRechazo = motivoRechazo;
                factura.LoteId = null;
            }

            var lote = await _context.Lotes
                .Include(l => l.Detalles)
                .FirstOrDefaultAsync(l => l.Id == loteId);

            if (lote != null)
            {
                lote.TotalAprobados = lote.Detalles.Count(d => d.SelloRecibido != null);
                lote.TotalRechazados = lote.Detalles.Count(d => d.EstadoDte == "RECHAZADO");
                lote.TotalPendientes = lote.TotalDtes - lote.TotalAprobados - lote.TotalRechazados;
                lote.FechaUltimaConsulta = DateTime.UtcNow;
                lote.FechaModificacion = DateTime.UtcNow;

                if (lote.TotalPendientes <= 0 && lote.Estado != "Procesado")
                {
                    lote.Estado = "Procesado";
                }
            }

            _logger.LogInformation(
                "[LOTE-SYNC] Sincronizado LoteDetalle para factura {FacturaId} en lote {LoteId}: EstadoDte={Estado}",
                factura.Id, loteId, loteDetalle.EstadoDte);
        }

        // ==========================================
        // HELPERS - ENVÍO DE EMAIL NO BLOQUEANTE
        // ==========================================

        /// <summary>
        /// Envía email de DTE al receptor de forma no bloqueante.
        /// Si falla, registra el error pero NO interrumpe el flujo principal.
        /// </summary>
        private async Task IntentarEnviarEmailDteAsync(int facturaId, int? receptorId)
        {
            try
            {
                // Verificar si ya se envió el correo (evita reenvíos en reprocesos)
                var factura = await _context.Facturas.FindAsync(facturaId);
                if (factura?.CorreoEnviado == true)
                {
                    _logger.LogDebug("[EMAIL-AUTO] Factura {FacturaId} ya tiene correo enviado, omitiendo", facturaId);
                    return;
                }

                if (!receptorId.HasValue || receptorId.Value == 0)
                {
                    _logger.LogDebug("[EMAIL-AUTO] Factura {FacturaId} sin receptor asignado, omitiendo email", facturaId);
                    return;
                }

                var receptor = await _context.Receptores.FindAsync(receptorId.Value);
                var emailReceptor = receptor?.CorreoElectronico;

                if (string.IsNullOrWhiteSpace(emailReceptor))
                {
                    _logger.LogDebug("[EMAIL-AUTO] Receptor {ReceptorId} sin correo electrónico, omitiendo email para factura {FacturaId}",
                        receptorId.Value, facturaId);
                    return;
                }

                await _emailService.EnviarDteAsync(facturaId, emailReceptor);
                _logger.LogInformation("[EMAIL-AUTO] Email DTE enviado para factura {FacturaId} a {Email}", facturaId, emailReceptor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EMAIL-AUTO] Error al enviar email DTE para factura {FacturaId}. El error no afecta la transaccion principal.", facturaId);
            }
        }

        /// <summary>
        /// Envía email de notificación de invalidación de forma no bloqueante.
        /// </summary>
        private async Task IntentarEnviarEmailInvalidacionAsync(int facturaId, int? receptorId, string motivoAnulacion)
        {
            try
            {
                if (!receptorId.HasValue || receptorId.Value == 0)
                {
                    _logger.LogDebug("[EMAIL-AUTO] Factura {FacturaId} sin receptor asignado, omitiendo email de invalidación", facturaId);
                    return;
                }

                var receptor = await _context.Receptores.FindAsync(receptorId.Value);
                var emailReceptor = receptor?.CorreoElectronico;

                if (string.IsNullOrWhiteSpace(emailReceptor))
                {
                    _logger.LogDebug("[EMAIL-AUTO] Receptor {ReceptorId} sin correo, omitiendo email de invalidación para factura {FacturaId}",
                        receptorId.Value, facturaId);
                    return;
                }

                await _emailService.EnviarNotificacionInvalidacionAsync(facturaId, emailReceptor, motivoAnulacion);
                _logger.LogInformation("[EMAIL-AUTO] Email de invalidación enviado para factura {FacturaId} a {Email}", facturaId, emailReceptor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EMAIL-AUTO] Error al enviar email de invalidación para factura {FacturaId}. El error no afecta la transaccion principal.", facturaId);
            }
        }

        /// <summary>
        /// Remueve acentos y diacríticos de un string para búsqueda flexible
        /// </summary>
        private static string RemoverAcentos(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return texto;
            var normalized = texto.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var c in normalized)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        // ==========================================
        // NOTA DE CRÉDITO — BÚSQUEDA DE DTEs
        // ==========================================

        public async Task<List<BuscarParaNcResultDto>> BuscarParaNotaCreditoAsync(string searchTerm, int emisorId)
        {
            var tiposPermitidos = new[] { "03", "07" };
            var search = RemoverAcentos(searchTerm.Trim().ToLower());

            // Primero filtrar por emisor, estado y tipo (en BD)
            var facturasCandidatas = await _context.Facturas
                .AsNoTracking()
                .Include(f => f.TipoDocumento)
                .Include(f => f.Receptor)
                .Where(f => f.EmisorId == emisorId
                    && f.EstadoHacienda == "PROCESADO"
                    && f.TipoDocumento != null
                    && tiposPermitidos.Contains(f.TipoDocumento.Codigo))
                .OrderByDescending(f => f.FechaEmision)
                .Take(200) // Limitar candidatos para filtro en memoria
                .ToListAsync();

            // Filtrar en memoria con búsqueda sin acentos
            var facturas = facturasCandidatas.Where(f =>
                f.CodigoGeneracion.ToLower().Contains(search)
                || f.NumeroControl.ToLower().Contains(search)
                || (f.Receptor != null && RemoverAcentos(f.Receptor.NombreRazonSocial.ToLower()).Contains(search))
                || (f.Receptor != null && f.Receptor.NumeroDocumento != null && f.Receptor.NumeroDocumento.ToLower().Contains(search)))
                .Take(20)
                .ToList();

            var result = new List<BuscarParaNcResultDto>();
            foreach (var f in facturas)
            {
                var saldo = await _saldoDteService.ObtenerSaldoPorCodigoGeneracion(f.CodigoGeneracion, emisorId);

                result.Add(new BuscarParaNcResultDto
                {
                    Id = f.Id,
                    NumeroControl = f.NumeroControl,
                    CodigoGeneracion = f.CodigoGeneracion,
                    TipoDte = f.TipoDocumento?.Codigo ?? "",
                    TipoDocumentoNombre = f.TipoDocumento?.Valor ?? "",
                    FechaEmision = f.FechaEmision,
                    ReceptorNombre = f.Receptor?.NombreRazonSocial ?? "Consumidor Final",
                    ReceptorNumDocumento = f.Receptor?.NumeroDocumento ?? "",
                    MontoTotalOperacion = f.MontoTotalOperacion,
                    TotalPagar = f.TotalPagar,
                    EstadoHacienda = f.EstadoHacienda ?? "",
                    SaldoDisponible = saldo?.SaldoDisponible ?? f.MontoTotalOperacion,
                    MontoAcreditado = saldo?.MontoAcreditado ?? 0,
                    NceCount = saldo?.NceCount ?? 0
                });
            }

            return result;
        }

        public async Task<DetalleParaNcDto?> ObtenerDetalleParaNotaCreditoAsync(int facturaId, int emisorId)
        {
            var factura = await _context.Facturas
                .AsNoTracking()
                .Include(f => f.TipoDocumento)
                .Include(f => f.Receptor)
                .Include(f => f.Detalles)
                .FirstOrDefaultAsync(f => f.Id == facturaId && f.EmisorId == emisorId);

            if (factura == null) return null;

            var tipoDte = factura.TipoDocumento?.Codigo;
            var tiposPermitidos = new[] { "03", "07" };
            if (tipoDte == null || !tiposPermitidos.Contains(tipoDte))
                return null;

            if (factura.EstadoHacienda != "PROCESADO")
                return null;

            var saldo = await _saldoDteService.ObtenerSaldoPorCodigoGeneracion(factura.CodigoGeneracion, emisorId);

            return new DetalleParaNcDto
            {
                Id = factura.Id,
                NumeroControl = factura.NumeroControl,
                CodigoGeneracion = factura.CodigoGeneracion,
                TipoDte = tipoDte,
                FechaEmision = factura.FechaEmision,
                CondicionOperacion = factura.CatCondicionOperacionId,
                ReceptorId = factura.ReceptorId,
                ReceptorNombre = factura.Receptor?.NombreRazonSocial ?? "Consumidor Final",
                ReceptorNumDocumento = factura.Receptor?.NumeroDocumento ?? "",
                ReceptorNit = factura.Receptor?.Nrc, // En SV, NIT del receptor se almacena en Nrc o NumeroDocumento
                ReceptorNrc = factura.Receptor?.Nrc,
                ReceptorCorreo = factura.Receptor?.CorreoElectronico,
                ReceptorTelefono = factura.Receptor?.Telefono,
                TotalGravada = factura.TotalGravado,
                TotalExenta = factura.TotalExento,
                TotalNoSuj = factura.TotalNoSujeto,
                SubTotal = factura.SubTotal,
                TotalIva = factura.TotalIva,
                MontoTotalOperacion = factura.MontoTotalOperacion,
                TotalPagar = factura.TotalPagar,
                SaldoDisponible = saldo?.SaldoDisponible ?? factura.MontoTotalOperacion,
                MontoAcreditado = saldo?.MontoAcreditado ?? 0,
                NceCount = saldo?.NceCount ?? 0,
                Items = factura.Detalles?.Select(d => new DetalleItemParaNcDto
                {
                    NumItem = d.NumeroItem,
                    TipoItem = d.CatTipoItemId,
                    Codigo = d.CodigoProducto,
                    Descripcion = d.Descripcion,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    VentaGravada = d.VentaGravada,
                    VentaExenta = d.VentaExenta,
                    VentaNoSuj = d.VentaNoSujeta,
                    MontoDescuento = d.MontoDescuento,
                    IvaItem = d.IvaItem,
                    UnidadMedida = d.CatUnidadMedidaId,
                    NumeroDocumentoRelacionado = d.NumeroDocumentoRelacionado
                }).OrderBy(d => d.NumItem).ToList() ?? new()
            };
        }

        // ==========================================
        // NOTA DE DÉBITO (DTE-06) — Búsqueda y Detalle
        // ==========================================

        public async Task<List<BuscarParaNcResultDto>> BuscarParaNotaDebitoAsync(string searchTerm, int emisorId)
        {
            var tiposPermitidos = new[] { "03", "07" };
            var search = RemoverAcentos(searchTerm.Trim().ToLower());

            var facturasCandidatas = await _context.Facturas
                .AsNoTracking()
                .Include(f => f.TipoDocumento)
                .Include(f => f.Receptor)
                .Where(f => f.EmisorId == emisorId
                    && f.EstadoHacienda == "PROCESADO"
                    && f.TipoDocumento != null
                    && tiposPermitidos.Contains(f.TipoDocumento.Codigo))
                .OrderByDescending(f => f.FechaEmision)
                .Take(200)
                .ToListAsync();

            var facturas = facturasCandidatas.Where(f =>
                f.CodigoGeneracion.ToLower().Contains(search)
                || f.NumeroControl.ToLower().Contains(search)
                || (f.Receptor != null && RemoverAcentos(f.Receptor.NombreRazonSocial.ToLower()).Contains(search))
                || (f.Receptor != null && f.Receptor.NumeroDocumento != null && f.Receptor.NumeroDocumento.ToLower().Contains(search)))
                .Take(20)
                .ToList();

            return facturas.Select(f => new BuscarParaNcResultDto
            {
                Id = f.Id,
                NumeroControl = f.NumeroControl,
                CodigoGeneracion = f.CodigoGeneracion,
                TipoDte = f.TipoDocumento?.Codigo ?? "",
                TipoDocumentoNombre = f.TipoDocumento?.Valor ?? "",
                FechaEmision = f.FechaEmision,
                ReceptorNombre = f.Receptor?.NombreRazonSocial ?? "Consumidor Final",
                ReceptorNumDocumento = f.Receptor?.NumeroDocumento ?? "",
                MontoTotalOperacion = f.MontoTotalOperacion,
                TotalPagar = f.TotalPagar,
                EstadoHacienda = f.EstadoHacienda ?? "",
                SaldoDisponible = 0, // ND no tiene límite de monto — no aplica saldo
                MontoAcreditado = 0,
                NceCount = 0
            }).ToList();
        }

        public async Task<DetalleParaNcDto?> ObtenerDetalleParaNotaDebitoAsync(int facturaId, int emisorId)
        {
            var factura = await _context.Facturas
                .AsNoTracking()
                .Include(f => f.TipoDocumento)
                .Include(f => f.Receptor)
                .Include(f => f.Detalles)
                .FirstOrDefaultAsync(f => f.Id == facturaId && f.EmisorId == emisorId);

            if (factura == null) return null;

            var tipoDte = factura.TipoDocumento?.Codigo;
            var tiposPermitidos = new[] { "03", "07" };
            if (tipoDte == null || !tiposPermitidos.Contains(tipoDte))
                return null;

            if (factura.EstadoHacienda != "PROCESADO")
                return null;

            return new DetalleParaNcDto
            {
                Id = factura.Id,
                NumeroControl = factura.NumeroControl,
                CodigoGeneracion = factura.CodigoGeneracion,
                TipoDte = tipoDte,
                FechaEmision = factura.FechaEmision,
                CondicionOperacion = factura.CatCondicionOperacionId,
                ReceptorId = factura.ReceptorId,
                ReceptorNombre = factura.Receptor?.NombreRazonSocial ?? "Consumidor Final",
                ReceptorNumDocumento = factura.Receptor?.NumeroDocumento ?? "",
                ReceptorNit = factura.Receptor?.Nrc,
                ReceptorNrc = factura.Receptor?.Nrc,
                ReceptorCorreo = factura.Receptor?.CorreoElectronico,
                ReceptorTelefono = factura.Receptor?.Telefono,
                TotalGravada = factura.TotalGravado,
                TotalExenta = factura.TotalExento,
                TotalNoSuj = factura.TotalNoSujeto,
                SubTotal = factura.SubTotal,
                TotalIva = factura.TotalIva,
                MontoTotalOperacion = factura.MontoTotalOperacion,
                TotalPagar = factura.TotalPagar,
                SaldoDisponible = 0, // ND no tiene límite de monto
                MontoAcreditado = 0,
                NceCount = 0,
                Items = factura.Detalles?.Select(d => new DetalleItemParaNcDto
                {
                    NumItem = d.NumeroItem,
                    TipoItem = d.CatTipoItemId,
                    Codigo = d.CodigoProducto,
                    Descripcion = d.Descripcion,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    VentaGravada = d.VentaGravada,
                    VentaExenta = d.VentaExenta,
                    VentaNoSuj = d.VentaNoSujeta,
                    MontoDescuento = d.MontoDescuento,
                    IvaItem = d.IvaItem,
                    UnidadMedida = d.CatUnidadMedidaId,
                    NumeroDocumentoRelacionado = d.NumeroDocumentoRelacionado
                }).OrderBy(d => d.NumItem).ToList() ?? new()
            };
        }
    }
}
