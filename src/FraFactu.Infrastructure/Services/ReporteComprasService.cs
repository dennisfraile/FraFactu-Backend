using ClosedXML.Excel;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Helpers;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Services;

/// <summary>
/// Servicio para generar reportes de compras con ClosedXML
/// </summary>
public class ReporteComprasService : IReporteComprasService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReporteComprasService> _logger;

    public ReporteComprasService(ApplicationDbContext context, ILogger<ReporteComprasService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<byte[]> GenerarLibroComprasExcelAsync(int emisorId, DateTime desde, DateTime hasta)
    {
        desde = FechaHelper.ToUtc(desde);
        hasta = FechaHelper.ToUtc(hasta);

        var compras = await _context.ComprasExternas
            .AsNoTracking()
            .Include(c => c.Proveedor)
            .Where(c => c.Proveedor.EmisorId == emisorId
                && c.Estado == "CONFIRMADA"
                && c.FechaEmision >= desde
                && c.FechaEmision <= hasta)
            .OrderBy(c => c.FechaEmision)
            .ThenBy(c => c.NumeroFactura)
            .Select(c => new
            {
                c.FechaEmision,
                c.NumeroFactura,
                c.Proveedor.NIT,
                ProveedorNombre = c.Proveedor.Nombre,
                c.Subtotal,
                c.IVA,
                c.Total,
                c.Origen,
                c.TipoDte,
                c.CodigoGeneracionDte,
                c.NumeroControlDte
            })
            .ToListAsync();

        var emisor = await _context.Emisores.FindAsync(emisorId);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Anexo de compras");

        // Colores
        var azulOscuro = XLColor.FromHtml("#1B3A5C");
        var dorado = XLColor.FromHtml("#C68A00");
        var grisClaro = XLColor.FromHtml("#F2F2F2");
        var azulClaro = XLColor.FromHtml("#E8F0FE");

        // ======================================================
        // ENCABEZADO DEL REPORTE (Filas 1-6)
        // ======================================================

        // Fila 1: Título principal
        ws.Range("B1:V1").Merge();
        ws.Cell("B1").Value = "LIBRO DE COMPRAS (ANEXO DE COMPRAS)";
        ws.Cell("B1").Style.Font.Bold = true;
        ws.Cell("B1").Style.Font.FontSize = 16;
        ws.Cell("B1").Style.Font.FontColor = XLColor.White;
        ws.Cell("B1").Style.Fill.BackgroundColor = azulOscuro;
        ws.Cell("B1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell("B1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Row(1).Height = 35;

        // Fila 2: Nombre del emisor
        ws.Range("B2:V2").Merge();
        ws.Cell("B2").Value = emisor?.NombreRazonSocial?.ToUpper() ?? "";
        ws.Cell("B2").Style.Font.Bold = true;
        ws.Cell("B2").Style.Font.FontSize = 13;
        ws.Cell("B2").Style.Font.FontColor = azulOscuro;
        ws.Cell("B2").Style.Fill.BackgroundColor = azulClaro;
        ws.Cell("B2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(2).Height = 25;

        // Fila 3: NIT y NRC
        ws.Range("B3:V3").Merge();
        ws.Cell("B3").Value = $"NIT: {emisor?.Nit ?? ""}    |    NRC: {emisor?.Nrc ?? ""}    |    Actividad: {emisor?.DescripcionActividad ?? ""}";
        ws.Cell("B3").Style.Font.FontSize = 10;
        ws.Cell("B3").Style.Font.FontColor = XLColor.DarkGray;
        ws.Cell("B3").Style.Fill.BackgroundColor = azulClaro;
        ws.Cell("B3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(3).Height = 20;

        // Fila 4: Período
        ws.Range("B4:V4").Merge();
        ws.Cell("B4").Value = $"Período: {desde:dd/MM/yyyy}  al  {hasta:dd/MM/yyyy}";
        ws.Cell("B4").Style.Font.Bold = true;
        ws.Cell("B4").Style.Font.FontSize = 11;
        ws.Cell("B4").Style.Font.FontColor = XLColor.White;
        ws.Cell("B4").Style.Fill.BackgroundColor = dorado;
        ws.Cell("B4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(4).Height = 22;

        // Fila 5: Info adicional
        ws.Range("B5:V5").Merge();
        ws.Cell("B5").Value = $"Total de registros: {compras.Count}    |    Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
        ws.Cell("B5").Style.Font.FontSize = 9;
        ws.Cell("B5").Style.Font.Italic = true;
        ws.Cell("B5").Style.Font.FontColor = XLColor.Gray;
        ws.Cell("B5").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(5).Height = 18;

        // Fila 6: Separador
        ws.Row(6).Height = 5;

        // ======================================================
        // HEADERS DE COLUMNAS (Fila 7)
        // ======================================================
        int hr = 7;

        string[] headers = {
            "FECHA DE EMISIÓN\nDEL DOCUMENTO",
            "CLASE DE\nDOCUMENTO",
            "TIPO DE\nDOCUMENTO",
            "NÚMERO DE\nDOCUMENTO",
            "NIT O NRC DEL\nPROVEEDOR",
            "NOMBRE DEL\nPROVEEDOR",
            "COMPRAS\nINTERNAS\nEXENTAS",
            "INTERNACI\nONES EXENTAS\nY/O NO SUJETAS",
            "IMPORTACI\nONES EXENTAS\nY/O NO SUJETAS",
            "COMPRAS\nINTERNAS\nGRAVADAS",
            "INTERNACI\nONES GRAVADAS\nDE BIENES",
            "IMPORTACI\nONES GRAVADAS\nDE BIENES",
            "IMPORTACI\nONES GRAVADAS\nDE SERVICIOS",
            "CRÉDITO\nFISCAL",
            "TOTAL DE\nCOMPRAS",
            "DUI DEL\nPROVEEDOR",
            "TIPO DE\nOPERACIÓN\n(Renta)",
            "CLASIFICACIÓN\n(Renta)",
            "SECTOR\n(Renta)",
            "TIPO DE\nCOSTO/GASTO\n(Renta)",
            "NÚMERO\nDEL ANEXO"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(hr, i + 2);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 9;
            cell.Style.Fill.BackgroundColor = dorado;
            cell.Style.Font.FontColor = XLColor.Black;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText = true;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#999999");
        }

        ws.Row(hr).Height = 55;

        // ======================================================
        // DATOS
        // ======================================================
        int row = hr + 1;
        bool alternar = false;

        foreach (var c in compras)
        {
            var rowBg = alternar ? grisClaro : XLColor.White;

            ws.Cell(row, 2).Value = c.FechaEmision;
            ws.Cell(row, 2).Style.DateFormat.Format = "dd/MM/yyyy";

            ws.Cell(row, 3).Value = c.Origen == "DTE"
                ? "4. DOCUMENTO TRIBUTARIO ELECTRONICO (DTE)"
                : "1. IMPRESO POR IMPRENTA O TIQUETES";

            ws.Cell(row, 4).Value = ObtenerTipoDocumentoDescripcion(c.TipoDte ?? "03");

            ws.Cell(row, 5).Value = c.Origen == "DTE" && !string.IsNullOrEmpty(c.CodigoGeneracionDte)
                ? c.CodigoGeneracionDte
                : c.NumeroFactura;

            ws.Cell(row, 6).Value = c.NIT;
            ws.Cell(row, 7).Value = c.ProveedorNombre;
            ws.Cell(row, 8).Value = 0.00m;
            ws.Cell(row, 9).Value = 0.00m;
            ws.Cell(row, 10).Value = 0.00m;
            ws.Cell(row, 11).Value = c.Subtotal;
            ws.Cell(row, 12).Value = 0.00m;
            ws.Cell(row, 13).Value = 0.00m;
            ws.Cell(row, 14).Value = 0.00m;
            ws.Cell(row, 15).Value = c.IVA;
            ws.Cell(row, 16).Value = c.Total;
            ws.Cell(row, 17).Value = "";
            ws.Cell(row, 18).Value = "1 Gravada";
            ws.Cell(row, 19).Value = "1 Costo";
            ws.Cell(row, 20).Value = "4 Servicios, F5 Costo Artículo";
            ws.Cell(row, 21).Value = "1 Costo";
            ws.Cell(row, 22).Value = 3;

            // Estilos de fila
            for (int col = 8; col <= 16; col++)
                ws.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";

            for (int col = 2; col <= 22; col++)
            {
                ws.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Cell(row, col).Style.Border.OutsideBorderColor = XLColor.FromHtml("#CCCCCC");
                ws.Cell(row, col).Style.Fill.BackgroundColor = rowBg;
                ws.Cell(row, col).Style.Font.FontSize = 9;
                ws.Cell(row, col).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            ws.Row(row).Height = 22;
            alternar = !alternar;
            row++;
        }

        // ======================================================
        // FILA DE TOTALES
        // ======================================================
        row++; // Fila en blanco
        var totalRow = row;

        ws.Cell(totalRow, 7).Value = "TOTALES:";
        ws.Cell(totalRow, 7).Style.Font.Bold = true;
        ws.Cell(totalRow, 7).Style.Font.FontSize = 10;
        ws.Cell(totalRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ws.Cell(totalRow, 8).Value = 0.00m;
        ws.Cell(totalRow, 9).Value = 0.00m;
        ws.Cell(totalRow, 10).Value = 0.00m;
        ws.Cell(totalRow, 11).Value = compras.Sum(c => c.Subtotal);
        ws.Cell(totalRow, 12).Value = 0.00m;
        ws.Cell(totalRow, 13).Value = 0.00m;
        ws.Cell(totalRow, 14).Value = 0.00m;
        ws.Cell(totalRow, 15).Value = compras.Sum(c => c.IVA);
        ws.Cell(totalRow, 16).Value = compras.Sum(c => c.Total);

        for (int col = 7; col <= 16; col++)
        {
            ws.Cell(totalRow, col).Style.Font.Bold = true;
            ws.Cell(totalRow, col).Style.Font.FontSize = 10;
            ws.Cell(totalRow, col).Style.Fill.BackgroundColor = azulOscuro;
            ws.Cell(totalRow, col).Style.Font.FontColor = XLColor.White;
            ws.Cell(totalRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            if (col >= 8)
                ws.Cell(totalRow, col).Style.NumberFormat.Format = "$#,##0.00";
        }
        ws.Row(totalRow).Height = 25;

        // ======================================================
        // ANCHOS DE COLUMNA
        // ======================================================
        ws.Column(1).Width = 2;   // Margen A
        ws.Column(2).Width = 14;  // Fecha
        ws.Column(3).Width = 22;  // Clase
        ws.Column(4).Width = 18;  // Tipo
        ws.Column(5).Width = 40;  // Número
        ws.Column(6).Width = 20;  // NIT
        ws.Column(7).Width = 35;  // Nombre
        for (int col = 8; col <= 16; col++)
            ws.Column(col).Width = 14;
        ws.Column(17).Width = 14; // DUI
        for (int col = 18; col <= 21; col++)
            ws.Column(col).Width = 18;
        ws.Column(22).Width = 10; // Anexo

        // Proteger hoja (solo lectura)
        ws.Protect();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> GenerarLibroComprasCsvAsync(int emisorId, DateTime desde, DateTime hasta)
    {
        desde = FechaHelper.ToUtc(desde);
        hasta = FechaHelper.ToUtc(hasta);

        var compras = await _context.ComprasExternas
            .AsNoTracking()
            .Include(c => c.Proveedor)
            .Where(c => c.Proveedor.EmisorId == emisorId
                && c.Estado == "CONFIRMADA"
                && c.FechaEmision >= desde
                && c.FechaEmision <= hasta)
            .OrderBy(c => c.FechaEmision)
            .ThenBy(c => c.NumeroFactura)
            .Select(c => new
            {
                c.FechaEmision,
                c.NumeroFactura,
                c.Proveedor.NIT,
                ProveedorNombre = c.Proveedor.Nombre,
                c.Subtotal,
                c.IVA,
                c.Total,
                c.Origen,
                c.TipoDte,
                c.CodigoGeneracionDte,
                c.NumeroControlDte
            })
            .ToListAsync();

        var sb = new System.Text.StringBuilder();

        // Header
        sb.AppendLine("FECHA DE EMISION,CLASE DE DOCUMENTO,TIPO DE DOCUMENTO,NUMERO DE DOCUMENTO,NIT O NRC DEL PROVEEDOR,NOMBRE DEL PROVEEDOR,COMPRAS INTERNAS EXENTAS,INTERNACIONES EXENTAS Y/O NO SUJETAS,IMPORTACIONES EXENTAS Y/O NO SUJETAS,COMPRAS INTERNAS GRAVADAS,INTERNACIONES GRAVADAS DE BIENES,IMPORTACIONES GRAVADAS DE BIENES,IMPORTACIONES GRAVADAS DE SERVICIOS,CREDITO FISCAL,TOTAL DE COMPRAS,DUI DEL PROVEEDOR,TIPO DE OPERACION (Renta),CLASIFICACION (Renta),SECTOR (Renta),TIPO DE COSTO/GASTO (Renta),NUMERO DEL ANEXO");

        // Datos
        foreach (var c in compras)
        {
            var clase = c.Origen == "DTE"
                ? "4. DOCUMENTO TRIBUTARIO ELECTRONICO (DTE)"
                : "1. IMPRESO POR IMPRENTA O TIQUETES";
            var tipo = ObtenerTipoDocumentoDescripcion(c.TipoDte ?? "03");
            var numero = c.Origen == "DTE" && !string.IsNullOrEmpty(c.CodigoGeneracionDte)
                ? c.CodigoGeneracionDte
                : c.NumeroFactura;

            sb.AppendLine(string.Join(",",
                c.FechaEmision.ToString("dd/MM/yyyy"),
                CsvEscape(clase),
                CsvEscape(tipo),
                CsvEscape(numero),
                CsvEscape(c.NIT),
                CsvEscape(c.ProveedorNombre),
                "0.00", "0.00", "0.00",
                c.Subtotal.ToString("F2"),
                "0.00", "0.00", "0.00",
                c.IVA.ToString("F2"),
                c.Total.ToString("F2"),
                "",
                "1 Gravada",
                "1 Costo",
                CsvEscape("4 Servicios, F5 Costo Articulo"),
                "1 Costo",
                "3"
            ));
        }

        return System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();
    }

    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    /// <summary>
    /// Convierte código de tipo DTE a la descripción del modelo de Hacienda
    /// </summary>
    private static string ObtenerTipoDocumentoDescripcion(string tipoDte)
    {
        return tipoDte switch
        {
            "01" => "01. FACTURA",
            "03" => "03. COMPROBANTE DE CRÉDITO FISCAL",
            "05" => "05. NOTA DE CRÉDITO",
            "06" => "06. NOTA DE DÉBITO",
            "07" => "07. COMPROBANTE DE RETENCIÓN",
            "11" => "11. FACTURA DE EXPORTACIÓN",
            "14" => "14. FACTURA DE SUJETO EXCLUIDO",
            _ => $"{tipoDte}. DOCUMENTO"
        };
    }

    public async Task<byte[]> GenerarResumenMensualExcelAsync(int emisorId, int mes, int anio)
    {
        var desde = new DateTime(anio, mes, 1);
        var hasta = desde.AddMonths(1).AddDays(-1);

        var resumen = await _context.ComprasExternas
            .AsNoTracking()
            .Include(c => c.Proveedor)
            .Where(c => c.Proveedor.EmisorId == emisorId
                && c.Estado == "CONFIRMADA"
                && c.FechaEmision >= desde
                && c.FechaEmision <= hasta)
            .GroupBy(c => new { c.Proveedor.NIT, c.Proveedor.Nombre })
            .Select(g => new
            {
                g.Key.NIT,
                ProveedorNombre = g.Key.Nombre,
                CantidadCompras = g.Count(),
                TotalGravado = g.Sum(c => c.Subtotal),
                TotalIVA = g.Sum(c => c.IVA),
                TotalGeneral = g.Sum(c => c.Total)
            })
            .OrderByDescending(x => x.TotalGeneral)
            .ToListAsync();

        var emisor = await _context.Emisores.FindAsync(emisorId);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Resumen Mensual");

        // Encabezado
        ws.Cell(1, 1).Value = $"RESUMEN MENSUAL DE COMPRAS - {emisor?.NombreRazonSocial ?? ""}";
        ws.Range("A1:G1").Merge().Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell(2, 1).Value = $"Mes: {desde:MMMM yyyy}";
        ws.Range("A2:G2").Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Headers
        int headerRow = 4;
        string[] headers = { "Nº", "NIT", "Proveedor", "Cantidad", "Gravado", "IVA Crédito Fiscal", "Total" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(headerRow, i + 1).Value = headers[i];

        var headerRange = ws.Range(headerRow, 1, headerRow, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#009688");
        headerRange.Style.Font.FontColor = XLColor.White;

        // Datos
        int row = headerRow + 1;
        int num = 1;

        foreach (var item in resumen)
        {
            ws.Cell(row, 1).Value = num++;
            ws.Cell(row, 2).Value = item.NIT;
            ws.Cell(row, 3).Value = item.ProveedorNombre;
            ws.Cell(row, 4).Value = item.CantidadCompras;
            ws.Cell(row, 5).Value = item.TotalGravado;
            ws.Cell(row, 6).Value = item.TotalIVA;
            ws.Cell(row, 7).Value = item.TotalGeneral;

            for (int col = 5; col <= 7; col++)
                ws.Cell(row, col).Style.NumberFormat.Format = "$#,##0.00";

            row++;
        }

        // Totales
        row++;
        ws.Cell(row, 3).Value = "TOTALES:";
        ws.Cell(row, 3).Style.Font.Bold = true;
        ws.Cell(row, 4).Value = resumen.Sum(x => x.CantidadCompras);
        ws.Cell(row, 4).Style.Font.Bold = true;
        ws.Cell(row, 5).Value = resumen.Sum(x => x.TotalGravado);
        ws.Cell(row, 6).Value = resumen.Sum(x => x.TotalIVA);
        ws.Cell(row, 7).Value = resumen.Sum(x => x.TotalGeneral);

        for (int col = 5; col <= 7; col++)
        {
            ws.Cell(row, col).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(row, col).Style.Font.Bold = true;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> GenerarDetallePorProveedorExcelAsync(int emisorId, DateTime desde, DateTime hasta, int? proveedorId = null)
    {
        desde = FechaHelper.ToUtc(desde);
        hasta = FechaHelper.ToUtc(hasta);

        var query = _context.ComprasExternas
            .AsNoTracking()
            .Include(c => c.Proveedor)
            .Where(c => c.Proveedor.EmisorId == emisorId
                && c.Estado == "CONFIRMADA"
                && c.FechaEmision >= desde
                && c.FechaEmision <= hasta);

        if (proveedorId.HasValue)
            query = query.Where(c => c.ProveedorId == proveedorId.Value);

        var compras = await query
            .OrderBy(c => c.Proveedor.Nombre)
            .ThenBy(c => c.FechaEmision)
            .Select(c => new
            {
                c.Proveedor.NIT,
                ProveedorNombre = c.Proveedor.Nombre,
                c.NumeroFactura,
                c.FechaEmision,
                c.Subtotal,
                c.IVA,
                c.Total,
                c.Origen
            })
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Detalle por Proveedor");

        // Headers
        string[] headers = { "Proveedor", "NIT", "Nº Factura", "Fecha", "Subtotal", "IVA", "Total", "Origen" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        var headerRange = ws.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#009688");
        headerRange.Style.Font.FontColor = XLColor.White;

        int row = 2;
        string currentProveedor = "";

        foreach (var c in compras)
        {
            // Separador visual por proveedor
            if (c.ProveedorNombre != currentProveedor && !string.IsNullOrEmpty(currentProveedor))
            {
                row++; // Línea en blanco
            }
            currentProveedor = c.ProveedorNombre;

            ws.Cell(row, 1).Value = c.ProveedorNombre;
            ws.Cell(row, 2).Value = c.NIT;
            ws.Cell(row, 3).Value = c.NumeroFactura;
            ws.Cell(row, 4).Value = c.FechaEmision.ToString("dd/MM/yyyy");
            ws.Cell(row, 5).Value = c.Subtotal;
            ws.Cell(row, 6).Value = c.IVA;
            ws.Cell(row, 7).Value = c.Total;
            ws.Cell(row, 8).Value = c.Origen;

            for (int col = 5; col <= 7; col++)
                ws.Cell(row, col).Style.NumberFormat.Format = "$#,##0.00";

            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> GenerarCruceComprasVsDtesExcelAsync(int emisorId, DateTime desde, DateTime hasta)
    {
        desde = FechaHelper.ToUtc(desde);
        hasta = FechaHelper.ToUtc(hasta);

        // Compras confirmadas en el período
        var compras = await _context.ComprasExternas
            .AsNoTracking()
            .Include(c => c.Proveedor)
            .Where(c => c.Proveedor.EmisorId == emisorId
                && c.Estado == "CONFIRMADA"
                && c.FechaEmision >= desde
                && c.FechaEmision <= hasta)
            .Select(c => new
            {
                c.Id,
                c.NumeroFactura,
                c.FechaEmision,
                c.Proveedor.NIT,
                ProveedorNombre = c.Proveedor.Nombre,
                c.Total,
                c.Origen,
                c.CodigoGeneracionDte,
                TieneDte = c.CodigoGeneracionDte != null
            })
            .OrderBy(c => c.FechaEmision)
            .ToListAsync();

        // DTEs recibidos en el período
        var dtes = await _context.DtesRecibidos
            .AsNoTracking()
            .Where(d => d.EmisorId == emisorId
                && d.FechaEmision >= desde
                && d.FechaEmision <= hasta)
            .Select(d => new
            {
                d.Id,
                d.CodigoGeneracion,
                d.TipoDte,
                d.FechaEmision,
                d.EmisorNit,
                d.EmisorNombre,
                d.Total,
                d.Estado,
                d.CompraExternaId,
                TieneCompra = d.CompraExternaId != null
            })
            .OrderBy(d => d.FechaEmision)
            .ToListAsync();

        using var workbook = new XLWorkbook();

        // Hoja 1: Compras sin DTE
        var ws1 = workbook.Worksheets.Add("Compras sin DTE");
        var comprasSinDte = compras.Where(c => !c.TieneDte).ToList();

        ws1.Cell(1, 1).Value = "COMPRAS SIN DTE VINCULADO";
        ws1.Range("A1:F1").Merge().Style.Font.Bold = true;
        ws1.Cell(1, 1).Style.Font.FontSize = 12;

        string[] h1 = { "Nº Factura", "Fecha", "Proveedor", "NIT", "Total", "Origen" };
        for (int i = 0; i < h1.Length; i++)
            ws1.Cell(3, i + 1).Value = h1[i];
        ws1.Range(3, 1, 3, h1.Length).Style.Font.Bold = true;
        ws1.Range(3, 1, 3, h1.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#FF9800");
        ws1.Range(3, 1, 3, h1.Length).Style.Font.FontColor = XLColor.White;

        int row1 = 4;
        foreach (var c in comprasSinDte)
        {
            ws1.Cell(row1, 1).Value = c.NumeroFactura;
            ws1.Cell(row1, 2).Value = c.FechaEmision.ToString("dd/MM/yyyy");
            ws1.Cell(row1, 3).Value = c.ProveedorNombre;
            ws1.Cell(row1, 4).Value = c.NIT;
            ws1.Cell(row1, 5).Value = c.Total;
            ws1.Cell(row1, 5).Style.NumberFormat.Format = "$#,##0.00";
            ws1.Cell(row1, 6).Value = c.Origen;
            row1++;
        }
        ws1.Columns().AdjustToContents();

        // Hoja 2: DTEs sin compra
        var ws2 = workbook.Worksheets.Add("DTEs sin Compra");
        var dtesSinCompra = dtes.Where(d => !d.TieneCompra && d.Estado != EstadoDteRecibido.DESCARTADO).ToList();

        ws2.Cell(1, 1).Value = "DTEs RECIBIDOS SIN COMPRA VINCULADA";
        ws2.Range("A1:F1").Merge().Style.Font.Bold = true;
        ws2.Cell(1, 1).Style.Font.FontSize = 12;

        string[] h2 = { "Código Generación", "Tipo DTE", "Fecha", "Emisor", "NIT", "Total", "Estado" };
        for (int i = 0; i < h2.Length; i++)
            ws2.Cell(3, i + 1).Value = h2[i];
        ws2.Range(3, 1, 3, h2.Length).Style.Font.Bold = true;
        ws2.Range(3, 1, 3, h2.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#F44336");
        ws2.Range(3, 1, 3, h2.Length).Style.Font.FontColor = XLColor.White;

        int row2 = 4;
        foreach (var d in dtesSinCompra)
        {
            ws2.Cell(row2, 1).Value = d.CodigoGeneracion;
            ws2.Cell(row2, 2).Value = d.TipoDte;
            ws2.Cell(row2, 3).Value = d.FechaEmision.ToString("dd/MM/yyyy");
            ws2.Cell(row2, 4).Value = d.EmisorNombre;
            ws2.Cell(row2, 5).Value = d.EmisorNit;
            ws2.Cell(row2, 6).Value = d.Total;
            ws2.Cell(row2, 6).Style.NumberFormat.Format = "$#,##0.00";
            ws2.Cell(row2, 7).Value = d.Estado.ToString();
            row2++;
        }
        ws2.Columns().AdjustToContents();

        // Hoja 3: Resumen
        var ws3 = workbook.Worksheets.Add("Resumen Cruce");
        ws3.Cell(1, 1).Value = $"RESUMEN DE CRUCE - {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}";
        ws3.Range("A1:B1").Merge().Style.Font.Bold = true;

        ws3.Cell(3, 1).Value = "Indicador";
        ws3.Cell(3, 2).Value = "Cantidad";
        ws3.Cell(3, 3).Value = "Monto Total";
        ws3.Range(3, 1, 3, 3).Style.Font.Bold = true;

        ws3.Cell(4, 1).Value = "Total Compras Confirmadas";
        ws3.Cell(4, 2).Value = compras.Count;
        ws3.Cell(4, 3).Value = compras.Sum(c => c.Total);

        ws3.Cell(5, 1).Value = "Compras CON DTE";
        ws3.Cell(5, 2).Value = compras.Count(c => c.TieneDte);
        ws3.Cell(5, 3).Value = compras.Where(c => c.TieneDte).Sum(c => c.Total);

        ws3.Cell(6, 1).Value = "Compras SIN DTE";
        ws3.Cell(6, 2).Value = comprasSinDte.Count;
        ws3.Cell(6, 3).Value = comprasSinDte.Sum(c => c.Total);

        ws3.Cell(7, 1).Value = "Total DTEs Recibidos";
        ws3.Cell(7, 2).Value = dtes.Count;
        ws3.Cell(7, 3).Value = dtes.Sum(d => d.Total);

        ws3.Cell(8, 1).Value = "DTEs Vinculados";
        ws3.Cell(8, 2).Value = dtes.Count(d => d.TieneCompra);
        ws3.Cell(8, 3).Value = dtes.Where(d => d.TieneCompra).Sum(d => d.Total);

        ws3.Cell(9, 1).Value = "DTEs SIN Compra (pendientes)";
        ws3.Cell(9, 2).Value = dtesSinCompra.Count;
        ws3.Cell(9, 3).Value = dtesSinCompra.Sum(d => d.Total);

        for (int r = 4; r <= 9; r++)
            ws3.Cell(r, 3).Style.NumberFormat.Format = "$#,##0.00";

        ws3.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
