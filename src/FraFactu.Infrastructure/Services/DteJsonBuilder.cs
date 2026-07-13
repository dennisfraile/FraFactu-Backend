using System.Text.Json;
using System.Text.Json.Serialization;
using FraFactu.Application.Interfaces;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Services
{
    /// <summary>
    /// Implementación de la generación del JSON DTE (Fase 2 del refactor de FacturaService).
    /// </summary>
    public class DteJsonBuilder : IDteJsonBuilder
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DteJsonBuilder> _logger;

        public DteJsonBuilder(ApplicationDbContext context, ILogger<DteJsonBuilder> logger)
        {
            _context = context;
            _logger = logger;
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
                        direccion = FacturaService.ConstruirDireccionEmisor(factura),
                        telefono = factura.Sucursal?.Telefono ?? factura.Emisor.Telefono,
                        codEstable = factura.Sucursal?.Codigo,
                        codPuntoVenta = factura.Caja?.CodPuntoVenta,
                        correo = factura.Sucursal?.CorreoElectronico ?? factura.Emisor.CorreoElectronico
                    },
                    receptor = factura.Receptor != null ? new
                    {
                        tipoDocumento = FacturaService.NormalizarTipoDocumentoReceptor(factura.Receptor.TipoDocumento?.Codigo, factura.Receptor.NumeroDocumento),
                        numDocumento = FacturaService.NormalizarNumDocumentoReceptor(factura.Receptor.TipoDocumento?.Codigo, factura.Receptor.NumeroDocumento),
                        nombre = factura.Receptor.NombreRazonSocial,
                        codActividad = factura.Receptor.CodigoActividad,
                        descActividad = factura.Receptor.DescripcionActividad,
                        // Receptor sin distrito → direccion = null (en FSE v2 la direccion es obligatoria,
                        // así que sin distrito el DTE quedará inválido a propósito).
                        direccion = FacturaService.ConstruirDireccionReceptor(factura.Receptor),
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
                        direccion = FacturaService.ConstruirDireccionEmisor(factura),
                        telefono = factura.Sucursal?.Telefono ?? factura.Emisor.Telefono,
                        correo = factura.Sucursal?.CorreoElectronico ?? factura.Emisor.CorreoElectronico
                    },
                    receptor = factura.Receptor != null ? new
                    {
                        tipoDocumento = FacturaService.NormalizarTipoDocumentoReceptor(factura.Receptor.TipoDocumento?.Codigo, factura.Receptor.NumeroDocumento),
                        numDocumento = FacturaService.NormalizarNumDocumentoReceptor(factura.Receptor.TipoDocumento?.Codigo, factura.Receptor.NumeroDocumento),
                        nrc = factura.Receptor.Nrc?.Replace("-", ""),
                        nombre = factura.Receptor.NombreRazonSocial,
                        codActividad = factura.Receptor.CodigoActividad,
                        descActividad = factura.Receptor.DescripcionActividad,
                        nombreComercial = factura.Receptor.NombreRazonSocial,
                        // Receptor sin distrito → direccion = null (en NC v4 la direccion es obligatoria,
                        // así que sin distrito el DTE quedará inválido a propósito).
                        direccion = FacturaService.ConstruirDireccionReceptor(factura.Receptor),
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
                        direccion = FacturaService.ConstruirDireccionEmisor(factura),
                        telefono = factura.Sucursal?.Telefono ?? factura.Emisor.Telefono,
                        correo = factura.Sucursal?.CorreoElectronico ?? factura.Emisor.CorreoElectronico
                    },
                    receptor = factura.Receptor != null ? new
                    {
                        tipoDocumento = FacturaService.NormalizarTipoDocumentoReceptor(factura.Receptor.TipoDocumento?.Codigo, factura.Receptor.NumeroDocumento),
                        numDocumento = FacturaService.NormalizarNumDocumentoReceptor(factura.Receptor.TipoDocumento?.Codigo, factura.Receptor.NumeroDocumento),
                        nrc = factura.Receptor.Nrc?.Replace("-", ""),
                        nombre = factura.Receptor.NombreRazonSocial,
                        codActividad = factura.Receptor.CodigoActividad,
                        descActividad = factura.Receptor.DescripcionActividad,
                        nombreComercial = factura.Receptor.NombreRazonSocial,
                        // Receptor sin distrito → direccion = null (en ND v4 la direccion es obligatoria,
                        // así que sin distrito el DTE quedará inválido a propósito).
                        direccion = FacturaService.ConstruirDireccionReceptor(factura.Receptor),
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
                    direccion = FacturaService.ConstruirDireccionEmisor(factura),
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
                            direccion = FacturaService.ConstruirDireccionReceptor(factura.Receptor),
                            telefono = string.IsNullOrEmpty(factura.Receptor.Telefono) ? null : factura.Receptor.Telefono,
                            correo = string.IsNullOrEmpty(factura.Receptor.CorreoElectronico) ? null : factura.Receptor.CorreoElectronico
                        }
                        : new // Estructura para Factura (01) y otros
                        {
                            tipoDocumento = FacturaService.NormalizarTipoDocumentoReceptor(factura.Receptor.TipoDocumento?.Codigo, factura.Receptor.NumeroDocumento),
                            numDocumento = FacturaService.NormalizarNumDocumentoReceptor(factura.Receptor.TipoDocumento?.Codigo, factura.Receptor.NumeroDocumento),
                            // NRC solo si tipoDocumento es "36" (NIT), de lo contrario null
                            nrc = factura.Receptor.TipoDocumento?.Codigo == "36"
                                ? factura.Receptor.Nrc?.Replace("-", "")
                                : null,
                            nombre = factura.Receptor.NombreRazonSocial,
                            codActividad = factura.Receptor.CodigoActividad,
                            descActividad = factura.Receptor.DescripcionActividad,
                            // Receptor sin distrito → direccion = null (el esquema FE lo permite)
                            direccion = FacturaService.ConstruirDireccionReceptor(factura.Receptor),
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
    }
}
