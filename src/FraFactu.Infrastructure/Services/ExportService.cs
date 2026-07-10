using ClosedXML.Excel;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using FraFactu.Application.Interfaces;
using FraFactu.Infrastructure.Helpers;

namespace FraFactu.Infrastructure.Services;

public class ExportService : IExportService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;

    public ExportService(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<byte[]> ExportarFacturasAExcelAsync(DateTime? fechaInicio, DateTime? fechaFin)
    {
        fechaInicio = FechaHelper.ToUtc(fechaInicio ?? DateTime.UtcNow.AddMonths(-1));
        fechaFin = FechaHelper.ToUtc(fechaFin ?? DateTime.UtcNow);

        var facturas = await _context.Facturas
            .AsNoTracking()
            .Include(f => f.Receptor)
            .Where(f => f.FechaEmision >= fechaInicio && f.FechaEmision <= fechaFin)
            .OrderByDescending(f => f.FechaEmision)
            .Select(f => new
            {
                f.Id,
                f.CodigoGeneracion,
                f.FechaEmision,
                Cliente = f.Receptor!.NombreRazonSocial,
                Subtotal = f.SubTotal,
                IVA = f.TotalIva,
                Total = f.TotalPagar,
                Estado = f.EstadoHacienda
            })
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Facturas");

        // Headers
        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "Código Generación";
        worksheet.Cell(1, 3).Value = "Fecha";
        worksheet.Cell(1, 4).Value = "Cliente";
        worksheet.Cell(1, 5).Value = "Subtotal";
        worksheet.Cell(1, 6).Value = "IVA";
        worksheet.Cell(1, 7).Value = "Total";
        worksheet.Cell(1, 8).Value = "Estado";

        // Estilo de headers
        var headerRange = worksheet.Range("A1:H1");
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

        // Datos
        int row = 2;
        foreach (var factura in facturas)
        {
            worksheet.Cell(row, 1).Value = factura.Id;
            worksheet.Cell(row, 2).Value = factura.CodigoGeneracion;
            worksheet.Cell(row, 3).Value = factura.FechaEmision;
            worksheet.Cell(row, 4).Value = factura.Cliente;
            worksheet.Cell(row, 5).Value = factura.Subtotal;
            worksheet.Cell(row, 6).Value = factura.IVA;
            worksheet.Cell(row, 7).Value = factura.Total;
            worksheet.Cell(row, 8).Value = factura.Estado;
            row++;
        }

        // Autoajustar columnas
        worksheet.Columns().AdjustToContents();

        // Convertir a byte array
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportarInventarioAExcelAsync()
    {
        var inventario = await _context.StocksBodega
            .AsNoTracking()
            .Include(s => s.Producto)
            .Include(s => s.Bodega)
            .Select(s => new
            {
                Producto = s.Producto.Nombre,
                Codigo = s.Producto.Codigo,
                Bodega = s.Bodega.Nombre,
                s.CantidadDisponible,
                s.CantidadReservada,
                s.CostoPromedio,
                s.ValorInventario
            })
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Inventario");

        // Headers
        worksheet.Cell(1, 1).Value = "Producto";
        worksheet.Cell(1, 2).Value = "Código";
        worksheet.Cell(1, 3).Value = "Bodega";
        worksheet.Cell(1, 4).Value = "Disponible";
        worksheet.Cell(1, 5).Value = "Reservado";
        worksheet.Cell(1, 6).Value = "Costo Promedio";
        worksheet.Cell(1, 7).Value = "Valor Total";

        var headerRange = worksheet.Range("A1:G1");
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen;

        // Datos
        int row = 2;
        foreach (var item in inventario)
        {
            worksheet.Cell(row, 1).Value = item.Producto;
            worksheet.Cell(row, 2).Value = item.Codigo;
            worksheet.Cell(row, 3).Value = item.Bodega;
            worksheet.Cell(row, 4).Value = item.CantidadDisponible;
            worksheet.Cell(row, 5).Value = item.CantidadReservada;
            worksheet.Cell(row, 6).Value = item.CostoPromedio;
            worksheet.Cell(row, 7).Value = item.ValorInventario;
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportarVentasAExcelAsync(DateTime fechaInicio, DateTime fechaFin)
    {
        fechaInicio = FechaHelper.ToUtc(fechaInicio);
        fechaFin = FechaHelper.ToUtc(fechaFin);

        var ventas = await _context.FacturaDetalles
            .AsNoTracking()
            .Include(d => d.Factura)
            .Include(d => d.Producto)
            .Where(d => d.Factura.FechaEmision >= fechaInicio && d.Factura.FechaEmision <= fechaFin)
            .Select(d => new
            {
                Fecha = d.Factura.FechaEmision,
                Factura = d.Factura.CodigoGeneracion,
                Producto = d.Producto != null ? d.Producto.Nombre : "N/A",
                d.Cantidad,
                d.PrecioUnitario,
                Total = d.VentaGravada + d.VentaExenta + d.VentaNoSujeta
            })
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Ventas");

        worksheet.Cell(1, 1).Value = "Fecha";
        worksheet.Cell(1, 2).Value = "Factura";
        worksheet.Cell(1, 3).Value = "Producto";
        worksheet.Cell(1, 4).Value = "Cantidad";
        worksheet.Cell(1, 5).Value = "Precio Unit.";
        worksheet.Cell(1, 6).Value = "Total";

        var headerRange = worksheet.Range("A1:F1");
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightYellow;

        int row = 2;
        foreach (var venta in ventas)
        {
            worksheet.Cell(row, 1).Value = venta.Fecha;
            worksheet.Cell(row, 2).Value = venta.Factura;
            worksheet.Cell(row, 3).Value = venta.Producto;
            worksheet.Cell(row, 4).Value = venta.Cantidad;
            worksheet.Cell(row, 5).Value = venta.PrecioUnitario;
            worksheet.Cell(row, 6).Value = venta.Total;
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // ===================================================================
    // Plantillas Excel para carga masiva
    // ===================================================================

    public async Task<byte[]> GenerarPlantillaProductosExcelAsync(int emisorId)
    {
        var emisor = await _context.Emisores
            .AsNoTracking()
            .Include(e => e.Departamento)
            .Include(e => e.Municipio)
            .FirstOrDefaultAsync(e => e.Id == emisorId)
            ?? throw new InvalidOperationException("Emisor no encontrado");

        var categorias = await _context.Categorias
            .AsNoTracking()
            .Where(c => c.EmisorId == emisorId && c.Activo)
            .Select(c => c.Nombre)
            .OrderBy(n => n)
            .ToListAsync();

        var marcas = await _context.Marcas
            .AsNoTracking()
            .Where(m => m.EmisorId == emisorId && m.Activa)
            .Select(m => m.Nombre)
            .OrderBy(n => n)
            .ToListAsync();

        var unidades = await _context.CatUnidadesMedida
            .AsNoTracking()
            .OrderBy(u => u.Codigo)
            .Select(u => u.Valor)
            .ToListAsync();

        var sucursal = await _context.Sucursales
            .AsNoTracking()
            .Include(s => s.Departamento)
            .Include(s => s.Municipio)
            .Where(s => s.EmisorId == emisorId && s.Activo)
            .FirstOrDefaultAsync();

        byte[]? logoBytes = null;
        if (!string.IsNullOrEmpty(emisor.LogoUrl))
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                logoBytes = await client.GetByteArrayAsync(emisor.LogoUrl);
            }
            catch { /* Si falla la descarga del logo, continuar sin él */ }
        }

        using var workbook = new XLWorkbook();

        // ---- HOJA PRODUCTOS ----
        var ws = workbook.Worksheets.Add("Productos");

        var direccionCompleta = sucursal?.Direccion ?? emisor.Direccion;
        var telefono = sucursal?.Telefono ?? emisor.Telefono;
        var correo = sucursal?.CorreoElectronico ?? emisor.CorreoElectronico;

        // Total de columnas (incrementado 2026-06-03: nueva columna "Precio
        // Incluye IVA" para que el import sepa si el valor del Excel ya trae IVA
        // o es base. Hasta hoy el import asumia implicitamente "siempre base"
        // pero la instruccion del template decia lo contrario — sobrefacturacion
        // silenciosa del 13%. Ver ImportService.ImportarProductosDesdeExcelAsync.
        const int totalCols = 16;
        int headerEnd = AplicarEncabezadoEmisor(ws, workbook, emisor, direccionCompleta, telefono, correo, logoBytes, totalCols);

        // Título
        int titleRow = headerEnd;
        ws.Range(titleRow, 1, titleRow, totalCols).Merge();
        ws.Cell(titleRow, 1).Value = "FORMULARIO DE CARGA MASIVA DE PRODUCTOS";
        ws.Cell(titleRow, 1).Style.Font.Bold = true;
        ws.Cell(titleRow, 1).Style.Font.FontSize = 16;
        ws.Cell(titleRow, 1).Style.Font.FontColor = XLColor.FromHtml("#1B3A5C");
        ws.Cell(titleRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(titleRow, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#D6E4F0");
        ws.Row(titleRow).Height = 30;

        // Instrucciones
        int instrRow = titleRow + 1;
        ws.Range(instrRow, 1, instrRow, totalCols).Merge();
        ws.Cell(instrRow, 1).Value = "Complete los campos a partir de la siguiente fila. Los campos marcados con (*) son obligatorios. En \"Precio Incluye IVA\" indique si el precio que ingresa ya trae el IVA (Sí) o es el precio base sin IVA (No). Si lo deja vacío se asume \"Sí\". Para productos Exento/No Sujeto la columna se ignora (no llevan IVA).";
        ws.Cell(instrRow, 1).Style.Font.Italic = true;
        ws.Cell(instrRow, 1).Style.Font.FontSize = 9;
        ws.Cell(instrRow, 1).Style.Font.FontColor = XLColor.FromHtml("#666666");
        ws.Cell(instrRow, 1).Style.Alignment.WrapText = true;
        ws.Cell(instrRow, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2EFDA");
        ws.Row(instrRow).Height = 40;

        // Headers de columna (16 — nueva "Precio Incluye IVA" insertada despues
        // de "Precio Venta", indices subsecuentes desplazados +1).
        int hRow = instrRow + 1;
        string[] headers = {
            "Código", "Nombre *", "Descripción", "Categoría", "Marca",
            "Precio Costo ($)", "Precio Venta ($) *", "Precio Incluye\nIVA (Sí/No)",
            "Unidad de Medida *",
            "Tipo Impuesto *", "% IVA", "Código Barras / SKU",
            "Stock Mínimo", "Stock Máximo", "Punto de Reorden", "Permite Venta\nSin Stock"
        };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(hRow, i + 1).Value = headers[i];
        }
        var headerRange = ws.Range(hRow, 1, hRow, totalCols);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Font.FontSize = 11;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B3A5C");
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        headerRange.Style.Alignment.WrapText = true;
        ws.Row(hRow).Height = 42;

        // Anchos de columna (16). El nuevo col 8 = Precio Incluye IVA.
        double[] widths = { 16, 30, 35, 18, 16, 16, 18, 14, 20, 16, 12, 16, 13, 13, 14, 18 };
        for (int i = 0; i < widths.Length; i++)
            ws.Column(i + 1).Width = widths[i];

        // Data validation y estilos para 200 filas de datos
        int dataStart = hRow + 1;
        int dataEnd = dataStart + 199;

        var unidadesStr = string.Join(",", unidades);
        var categoriasStr = categorias.Count > 0 ? string.Join(",", categorias) : "";
        var marcasStr = marcas.Count > 0 ? string.Join(",", marcas) : "";

        // Data validations por rango de columna (indices ajustados +1 desde
        // la columna 9 en adelante porque insertamos "Precio Incluye IVA" en col 8).
        AgregarValidacionLista(ws, dataStart, dataEnd, 8, "Sí,No");                  // Precio Incluye IVA (NEW)
        AgregarValidacionLista(ws, dataStart, dataEnd, 10, "Gravado,Exento,No Sujeto"); // Tipo Impuesto (was 9)
        AgregarValidacionLista(ws, dataStart, dataEnd, 16, "No,Sí");                 // Permite Venta Sin Stock (was 15)
        if (unidadesStr.Length <= 255)
            AgregarValidacionLista(ws, dataStart, dataEnd, 9, unidadesStr);          // Unidad (was 8)
        if (!string.IsNullOrEmpty(categoriasStr) && categoriasStr.Length <= 255)
            AgregarValidacionLista(ws, dataStart, dataEnd, 4, categoriasStr);
        if (!string.IsNullOrEmpty(marcasStr) && marcasStr.Length <= 255)
            AgregarValidacionLista(ws, dataStart, dataEnd, 5, marcasStr);

        for (int r = dataStart; r <= dataEnd; r++)
        {
            // Estilos alternados
            var bgColor = (r - dataStart) % 2 == 0 ? XLColor.FromHtml("#F2F2F2") : XLColor.White;
            ws.Range(r, 1, r, totalCols).Style.Fill.BackgroundColor = bgColor;
            ws.Range(r, 1, r, totalCols).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(r, 1, r, totalCols).Style.Border.OutsideBorderColor = XLColor.FromHtml("#D9D9D9");
        }

        // ---- HOJA INSTRUCCIONES ----
        AgregarHojaInstruccionesProductos(workbook);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> GenerarPlantillaServiciosExcelAsync(int emisorId, List<int>? sucursalIds = null)
    {
        var emisor = await _context.Emisores
            .AsNoTracking()
            .Include(e => e.Departamento)
            .Include(e => e.Municipio)
            .FirstOrDefaultAsync(e => e.Id == emisorId)
            ?? throw new InvalidOperationException("Emisor no encontrado");

        var categorias = await _context.Categorias
            .AsNoTracking()
            .Where(c => c.EmisorId == emisorId && c.Activo)
            .Select(c => c.Nombre)
            .OrderBy(n => n)
            .ToListAsync();

        // Cargar sucursales: si sucursalIds es null (EmisorAdmin), todas; si tiene valores, solo las del usuario
        var sucursalesQuery = _context.Sucursales
            .AsNoTracking()
            .Where(s => s.EmisorId == emisorId && s.Activo);
        if (sucursalIds != null && sucursalIds.Count > 0)
            sucursalesQuery = sucursalesQuery.Where(s => sucursalIds.Contains(s.Id));

        var sucursales = await sucursalesQuery
            .OrderBy(s => s.Nombre)
            .Select(s => s.Nombre)
            .ToListAsync();

        var sucursalEncabezado = await _context.Sucursales
            .AsNoTracking()
            .Include(s => s.Departamento)
            .Include(s => s.Municipio)
            .Where(s => s.EmisorId == emisorId && s.Activo)
            .FirstOrDefaultAsync();

        byte[]? logoBytes = null;
        if (!string.IsNullOrEmpty(emisor.LogoUrl))
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                logoBytes = await client.GetByteArrayAsync(emisor.LogoUrl);
            }
            catch { }
        }

        // Total cols incrementado 2026-06-03 (+1 por nueva "Precio Incluye IVA"
        // en col 6, indices >=6 desplazados +1). Mismo fix que productos: ver
        // ImportService.ImportarServiciosDesdeExcelAsync.
        int totalCols = 9;
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Servicios");

        var direccionCompleta = sucursalEncabezado?.Direccion ?? emisor.Direccion;
        var telefono = sucursalEncabezado?.Telefono ?? emisor.Telefono;
        var correo = sucursalEncabezado?.CorreoElectronico ?? emisor.CorreoElectronico;

        int headerEnd = AplicarEncabezadoEmisor(ws, workbook, emisor, direccionCompleta, telefono, correo, logoBytes, totalCols);

        // Título
        int titleRow = headerEnd;
        ws.Range(titleRow, 1, titleRow, totalCols).Merge();
        ws.Cell(titleRow, 1).Value = "FORMULARIO DE CARGA MASIVA DE SERVICIOS";
        ws.Cell(titleRow, 1).Style.Font.Bold = true;
        ws.Cell(titleRow, 1).Style.Font.FontSize = 16;
        ws.Cell(titleRow, 1).Style.Font.FontColor = XLColor.FromHtml("#1B3A5C");
        ws.Cell(titleRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(titleRow, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#D6E4F0");
        ws.Row(titleRow).Height = 30;

        // Instrucciones
        int instrRow = titleRow + 1;
        ws.Range(instrRow, 1, instrRow, totalCols).Merge();
        ws.Cell(instrRow, 1).Value = "Complete los campos a partir de la siguiente fila. Los campos marcados con (*) son obligatorios. En \"Precio Incluye IVA\" indique si el precio que ingresa ya trae el IVA (Sí) o es el precio base sin IVA (No). Si lo deja vacío se asume \"Sí\". Para servicios Exento/No Sujeto la columna se ignora (no llevan IVA — servicios médicos generalmente son Exentos).";
        ws.Cell(instrRow, 1).Style.Font.Italic = true;
        ws.Cell(instrRow, 1).Style.Font.FontSize = 9;
        ws.Cell(instrRow, 1).Style.Font.FontColor = XLColor.FromHtml("#666666");
        ws.Cell(instrRow, 1).Style.Alignment.WrapText = true;
        ws.Cell(instrRow, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2EFDA");
        ws.Row(instrRow).Height = 40;

        // Headers (9 — nueva "Precio Incluye IVA" en col 6).
        int hRow = instrRow + 1;
        string[] headers = {
            "Código", "Nombre *", "Descripción", "Categoría",
            "Precio Venta ($) *", "Precio Incluye\nIVA (Sí/No)",
            "Tipo Impuesto *", "% IVA",
            "Sucursales\nasignadas"
        };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(hRow, i + 1).Value = headers[i];

        var headerRangeS = ws.Range(hRow, 1, hRow, totalCols);
        headerRangeS.Style.Font.Bold = true;
        headerRangeS.Style.Font.FontColor = XLColor.White;
        headerRangeS.Style.Font.FontSize = 11;
        headerRangeS.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B3A5C");
        headerRangeS.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRangeS.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        headerRangeS.Style.Alignment.WrapText = true;
        ws.Row(hRow).Height = 42;

        // Anchos (9). Col 6 = Precio Incluye IVA (estrecho, dropdown Sí/No).
        double[] widths = { 16, 35, 45, 22, 22, 14, 18, 12, 24 };
        for (int i = 0; i < widths.Length; i++)
            ws.Column(i + 1).Width = widths[i];

        int dataStart = hRow + 1;
        int dataEnd = dataStart + 199;

        var categoriasStr = categorias.Count > 0 ? string.Join(",", categorias) : "";
        var sucursalesStr = sucursales.Count > 0 ? string.Join(",", sucursales) : "";

        // Data validations por rango de columna (Tipo Impuesto era col 6 → 7;
        // Sucursales era col 8 → 9; NUEVA col 6 = Precio Incluye IVA).
        AgregarValidacionLista(ws, dataStart, dataEnd, 6, "Sí,No");                  // Precio Incluye IVA (NEW)
        AgregarValidacionLista(ws, dataStart, dataEnd, 7, "Gravado,Exento,No Sujeto"); // Tipo Impuesto (was 6)
        if (!string.IsNullOrEmpty(categoriasStr) && categoriasStr.Length <= 255)
            AgregarValidacionLista(ws, dataStart, dataEnd, 4, categoriasStr);
        if (!string.IsNullOrEmpty(sucursalesStr) && sucursalesStr.Length <= 255)
            AgregarValidacionLista(ws, dataStart, dataEnd, 9, sucursalesStr);        // Sucursales (was 8)

        for (int r = dataStart; r <= dataEnd; r++)
        {
            var bgColor = (r - dataStart) % 2 == 0 ? XLColor.FromHtml("#F2F2F2") : XLColor.White;
            ws.Range(r, 1, r, totalCols).Style.Fill.BackgroundColor = bgColor;
            ws.Range(r, 1, r, totalCols).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(r, 1, r, totalCols).Style.Border.OutsideBorderColor = XLColor.FromHtml("#D9D9D9");
        }

        AgregarHojaInstruccionesServicios(workbook);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // ===================================================================
    // Helpers privados para plantillas
    // ===================================================================

    private static void AgregarValidacionLista(IXLWorksheet ws, int rowStart, int rowEnd, int col, string valores)
    {
        var range = ws.Range(rowStart, col, rowEnd, col);
        var dv = range.CreateDataValidation();
        dv.AllowedValues = XLAllowedValues.List;
        dv.InCellDropdown = true;
        dv.Value = $"\"{valores}\"";
    }

    private static int AplicarEncabezadoEmisor(
        IXLWorksheet ws, XLWorkbook workbook,
        Domain.Entities.Emisor emisor,
        string direccion, string telefono, string correo,
        byte[]? logoBytes, int maxCol)
    {
        var azulOscuro = XLColor.FromHtml("#1B3A5C");
        var azulMedio = XLColor.FromHtml("#2E75B6");
        var gris = XLColor.FromHtml("#555555");

        // Filas del encabezado
        string[] filas = {
            emisor.NombreComercial ?? emisor.NombreRazonSocial,
            emisor.DescripcionActividad,
            $"NIT: {emisor.Nit}  |  NRC: {emisor.Nrc}",
            $"Dirección: {direccion}",
            $"Correo: {correo}  |  Teléfono: {telefono}",
        };

        for (int i = 0; i < filas.Length; i++)
        {
            int r = i + 1;
            ws.Range(r, 1, r, maxCol).Merge();
            ws.Cell(r, 1).Value = filas[i];
            ws.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(r, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Range(r, 1, r, maxCol).Style.Fill.BackgroundColor = XLColor.White;

            if (i == 0)
            {
                ws.Cell(r, 1).Style.Font.Bold = true;
                ws.Cell(r, 1).Style.Font.FontSize = 18;
                ws.Cell(r, 1).Style.Font.FontColor = azulOscuro;
                ws.Row(r).Height = 30;
            }
            else if (i == 1)
            {
                ws.Cell(r, 1).Style.Font.Italic = true;
                ws.Cell(r, 1).Style.Font.FontSize = 12;
                ws.Cell(r, 1).Style.Font.FontColor = azulMedio;
                ws.Row(r).Height = 20;
            }
            else
            {
                ws.Cell(r, 1).Style.Font.FontSize = 10;
                ws.Cell(r, 1).Style.Font.FontColor = gris;
                ws.Row(r).Height = 16;
            }
        }

        // Línea separadora
        int sepRow = filas.Length + 1;
        ws.Row(sepRow).Height = 5;
        ws.Range(sepRow, 1, sepRow, maxCol).Style.Fill.BackgroundColor = azulMedio;

        // Logo
        if (logoBytes != null && logoBytes.Length > 0)
        {
            try
            {
                using var logoStream = new MemoryStream(logoBytes);
                var picture = ws.AddPicture(logoStream);
                picture.MoveTo(ws.Cell(1, 1));
                picture.WithSize(100, 100);
            }
            catch { }
        }

        return sepRow + 1;
    }

    private static void AgregarHojaInstruccionesProductos(XLWorkbook workbook)
    {
        var ws = workbook.Worksheets.Add("Instrucciones");
        ws.Column(1).Width = 5;
        ws.Column(2).Width = 30;
        ws.Column(3).Width = 100;

        var azulOscuro = XLColor.FromHtml("#1B3A5C");

        ws.Range("A1:C1").Merge();
        ws.Cell("A1").Value = "INSTRUCCIONES PARA LLENAR EL FORMULARIO DE PRODUCTOS";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 14;
        ws.Cell("A1").Style.Font.FontColor = azulOscuro;
        ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(1).Height = 35;

        var instrucciones = new (string num, string campo, string desc, double height)[]
        {
            ("1", "Código", "Código interno del producto. Si se deja vacío, el sistema lo generará automáticamente (formato PROD-00001). Máximo 25 caracteres.", 25),
            ("2", "Nombre *", "Nombre del producto. OBLIGATORIO. Máximo 1000 caracteres.", 25),
            ("3", "Descripción", "Descripción detallada del producto. Opcional. Máximo 1000 caracteres.", 25),
            ("4", "Categoría", "Categoría del producto. Si no existe en el sistema, se creará automáticamente. Cuando descargue desde el sistema, será una lista desplegable con las categorías registradas.", 40),
            ("5", "Marca", "Marca del producto. Si no existe, se creará automáticamente. Cuando descargue desde el sistema, será una lista desplegable con las marcas registradas.", 40),
            ("6", "Precio Costo ($)", "Precio de compra/costo del producto SIN IVA. Usar punto como separador decimal. Opcional.", 25),
            ("7", "Precio Venta ($) *", "OBLIGATORIO. Precio de venta del producto. Indique en la siguiente columna si el valor ya trae IVA o no.", 35),
            ("8", "Precio Incluye IVA (Sí/No)", "OPCIONAL. \"Sí\" = el Precio Venta de la columna anterior YA trae el IVA incluido (ej: $113 = $100 base + 13% IVA), Smartix lo guardará como base ($100) y agregará el IVA al facturar. \"No\" = el Precio Venta es el precio base SIN IVA (Smartix lo guarda tal cual). Si lo deja vacío se asume \"Sí\". Para Exento/No Sujeto se ignora.", 55),
            ("9", "Unidad de Medida *", "OBLIGATORIO. Seleccione de la lista desplegable (catálogo oficial: Unidad, Kilogramo, Litro, Metro, Docena, etc.).", 30),
            ("10", "Tipo Impuesto *", "OBLIGATORIO. Gravado (con IVA, 13% por defecto), Exento (sin IVA) o No Sujeto (sin IVA).", 25),
            ("11", "% IVA", "Se asigna según Tipo Impuesto: Gravado = 13% por defecto; Exento/No Sujeto = 0%.", 25),
            ("12", "Código Barras / SKU", "Código de barras o SKU para control de inventario. Opcional.", 25),
            ("13", "Stock Mínimo", "Cantidad mínima en inventario. El sistema genera alerta al llegar a este nivel. Opcional.", 25),
            ("14", "Stock Máximo", "Cantidad máxima permitida en inventario. Opcional.", 25),
            ("15", "Punto de Reorden", "Nivel de inventario en el que debe hacer un nuevo pedido al proveedor. Cuando el stock llega a esta cantidad, el sistema notifica que es momento de reabastecer. Ej: si tarda 3 días en recibir producto y su mínimo es 5, su punto de reorden podría ser 15.", 65),
            ("16", "Permite Venta Sin Stock", "Si se permite vender sin existencias. Por defecto \"No\" para proteger contra sobreventa.", 30),
        };

        // Header tabla
        ws.Cell(3, 1).Value = "#";
        ws.Cell(3, 2).Value = "Campo";
        ws.Cell(3, 3).Value = "Descripción";
        var hRange = ws.Range("A3:C3");
        hRange.Style.Font.Bold = true;
        hRange.Style.Font.FontColor = XLColor.White;
        hRange.Style.Fill.BackgroundColor = azulOscuro;

        for (int i = 0; i < instrucciones.Length; i++)
        {
            int r = i + 4;
            var (num, campo, desc, height) = instrucciones[i];
            ws.Cell(r, 1).Value = num;
            ws.Cell(r, 2).Value = campo;
            ws.Cell(r, 3).Value = desc;
            ws.Cell(r, 2).Style.Font.Bold = true;
            ws.Cell(r, 3).Style.Alignment.WrapText = true;
            ws.Row(r).Height = height;
            if (i % 2 == 1)
                ws.Range(r, 1, r, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
        }

        // Notas importantes
        int notaStart = 4 + instrucciones.Length + 1;
        string[] notas = {
            "NOTAS IMPORTANTES:",
            "1. Los campos marcados con (*) son obligatorios.",
            "2. Borre las filas de ejemplo antes de ingresar sus datos reales.",
            "3. Las categorías y marcas que no existan se crearán automáticamente.",
            "4. Smartix guarda el precio internamente como BASE (sin IVA). Use la columna \"Precio Incluye IVA\" para decirle al sistema si el valor del Excel ya viene con IVA (Sí, default) o si es base (No).",
            "5. \"Permite Venta Sin Stock\" es \"No\" por defecto para proteger contra sobreventa.",
            "6. El Punto de Reorden le ayuda a reabastecer a tiempo.",
            "7. Puede agregar hasta 200 productos por archivo.",
        };

        for (int i = 0; i < notas.Length; i++)
        {
            int r = notaStart + i;
            ws.Range(r, 2, r, 3).Merge();
            ws.Cell(r, 2).Value = notas[i];
            ws.Cell(r, 2).Style.Font.Bold = i == 0;
            if (i % 2 == 1)
                ws.Range(r, 1, r, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
        }
    }

    private static void AgregarHojaInstruccionesServicios(XLWorkbook workbook)
    {
        var ws = workbook.Worksheets.Add("Instrucciones");
        ws.Column(1).Width = 5;
        ws.Column(2).Width = 30;
        ws.Column(3).Width = 100;

        var azulOscuro = XLColor.FromHtml("#1B3A5C");

        ws.Range("A1:C1").Merge();
        ws.Cell("A1").Value = "INSTRUCCIONES PARA LLENAR EL FORMULARIO DE SERVICIOS";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 14;
        ws.Cell("A1").Style.Font.FontColor = azulOscuro;
        ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(1).Height = 35;

        var instrucciones = new (string num, string campo, string desc, double height)[]
        {
            ("1", "Código", "Código interno del servicio. Si se deja vacío, el sistema lo generará automáticamente (formato SERV-00001). Máximo 25 caracteres.", 25),
            ("2", "Nombre *", "Nombre del servicio. OBLIGATORIO. Máximo 1000 caracteres.", 25),
            ("3", "Descripción", "Descripción detallada del servicio. Opcional. Máximo 1000 caracteres.", 25),
            ("4", "Categoría", "Categoría del servicio. Si no existe, se creará automáticamente. Cuando descargue desde el sistema, será una lista desplegable con las categorías registradas.", 40),
            ("5", "Precio Venta ($) *", "OBLIGATORIO. Precio de venta del servicio. Indique en la siguiente columna si el valor ya trae IVA o no.", 35),
            ("6", "Precio Incluye IVA (Sí/No)", "OPCIONAL. \"Sí\" = el Precio Venta YA trae el IVA incluido (Smartix lo guardará como base y agregará el IVA al facturar). \"No\" = el Precio Venta es base SIN IVA. Si lo deja vacío se asume \"Sí\". Para Exento/No Sujeto se ignora (servicios médicos generalmente son Exentos).", 55),
            ("7", "Tipo Impuesto *", "OBLIGATORIO. Gravado (con IVA, 13% por defecto), Exento (sin IVA) o No Sujeto (sin IVA).", 25),
            ("8", "% IVA", "Se asigna según Tipo Impuesto: Gravado = 13% por defecto; Exento/No Sujeto = 0%.", 25),
            ("9", "Sucursales asignadas", "Seleccione la sucursal donde se prestará este servicio. Solo se muestran las sucursales a las que usted tiene acceso. Si se deja vacío, el servicio tendrá acceso a todas las sucursales.", 40),
        };

        ws.Cell(3, 1).Value = "#";
        ws.Cell(3, 2).Value = "Campo";
        ws.Cell(3, 3).Value = "Descripción";
        var hRange = ws.Range("A3:C3");
        hRange.Style.Font.Bold = true;
        hRange.Style.Font.FontColor = XLColor.White;
        hRange.Style.Fill.BackgroundColor = azulOscuro;

        for (int i = 0; i < instrucciones.Length; i++)
        {
            int r = i + 4;
            var (num, campo, desc, height) = instrucciones[i];
            ws.Cell(r, 1).Value = num;
            ws.Cell(r, 2).Value = campo;
            ws.Cell(r, 3).Value = desc;
            ws.Cell(r, 2).Style.Font.Bold = true;
            ws.Cell(r, 3).Style.Alignment.WrapText = true;
            ws.Row(r).Height = height;
            if (i % 2 == 1)
                ws.Range(r, 1, r, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
        }

        int notaStart = 4 + instrucciones.Length + 1;
        string[] notas = {
            "NOTAS IMPORTANTES:",
            "1. Los campos marcados con (*) son obligatorios.",
            "2. Borre las filas de ejemplo antes de ingresar sus datos reales.",
            "3. Las categorías que no existan se crearán automáticamente.",
            "4. Smartix guarda el precio internamente como BASE (sin IVA). Use la columna \"Precio Incluye IVA\" para decirle al sistema si el valor del Excel ya viene con IVA (Sí, default) o si es base (No).",
            "5. Los servicios médicos (consultas, cirugías) generalmente son Exentos de IVA — para Exento/No Sujeto la columna \"Precio Incluye IVA\" se ignora.",
            "6. Si selecciona una sucursal, el servicio se asignará solo a esa sucursal. Si la deja vacía, tendrá acceso a todas.",
            "7. Puede agregar hasta 200 servicios por archivo.",
        };

        for (int i = 0; i < notas.Length; i++)
        {
            int r = notaStart + i;
            ws.Range(r, 2, r, 3).Merge();
            ws.Cell(r, 2).Value = notas[i];
            ws.Cell(r, 2).Style.Font.Bold = i == 0;
            if (i % 2 == 1)
                ws.Range(r, 1, r, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
        }
    }
}
