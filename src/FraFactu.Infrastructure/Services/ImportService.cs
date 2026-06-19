using ClosedXML.Excel;
using FraFactu.Application.DTOs.Import;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FraFactu.Infrastructure.Services;

public class ImportService : IImportService
{
    private readonly ApplicationDbContext _context;

    public ImportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ImportResultDto> ImportarProductosDesdeExcelAsync(Stream excelStream, int emisorId)
    {
        var result = new ImportResultDto();

        using var workbook = new XLWorkbook(excelStream);
        var ws = workbook.Worksheets.First();

        // Buscar la fila de headers dinámicamente (buscar "Nombre *" en alguna celda)
        int headerRow = EncontrarFilaHeaders(ws, "Nombre *");
        if (headerRow == -1)
        {
            result.Errores.Add(new ImportErrorDto
            {
                Fila = 0,
                Campo = "General",
                Mensaje = "No se encontró la fila de encabezados. Asegúrese de usar la plantilla descargada del sistema."
            });
            return result;
        }

        int dataStartRow = headerRow + 1;

        // Cargar datos de referencia
        var categorias = await _context.Categorias
            .Where(c => c.EmisorId == emisorId && c.Activo)
            .ToListAsync();

        var marcas = await _context.Marcas
            .Where(m => m.EmisorId == emisorId && m.Activa)
            .ToListAsync();

        var unidadesMedida = await _context.Set<Domain.Entities.Catalogos.CatUnidadMedida>()
            .ToListAsync();

        var codigosExistentes = await _context.ProductosServicios
            .Where(p => p.EmisorId == emisorId && p.Activo)
            .Select(p => p.Codigo)
            .ToListAsync();

        // Obtener último código auto-generado
        var ultimoCodigo = codigosExistentes
            .Where(c => c.StartsWith("PROD-"))
            .Select(c => int.TryParse(c.Replace("PROD-", ""), out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();
        int secuencia = ultimoCodigo + 1;

        // ============================================================
        // FASE 1: Validar todas las filas SIN guardar nada
        // ============================================================
        int lastRow = ws.LastRowUsed()?.RowNumber() ?? dataStartRow;
        var filasValidadas = new List<FilaProductoValidada>();
        // Categorías y marcas nuevas detectadas (para crear solo si todo es válido)
        var categoriasNuevas = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // nombre -> código
        var marcasNuevas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var codigosEnArchivo = new HashSet<string>(); // Para detectar duplicados dentro del mismo archivo

        for (int row = dataStartRow; row <= lastRow; row++)
        {
            var nombre = ws.Cell(row, 2).GetString().Trim();
            if (string.IsNullOrWhiteSpace(nombre))
                continue; // Fila vacía, saltar

            result.TotalFilas++;
            var erroresFila = new List<ImportErrorDto>();

            // Col 1: Código
            var codigo = ws.Cell(row, 1).GetString().Trim();
            bool codigoAutoGenerado = false;
            if (string.IsNullOrWhiteSpace(codigo))
            {
                codigo = $"PROD-{secuencia:D5}";
                secuencia++;
                codigoAutoGenerado = true;
            }
            else if (codigo.Length > 50)
            {
                erroresFila.Add(new ImportErrorDto { Fila = row, Campo = "Código", Mensaje = "El código excede 50 caracteres." });
            }

            if (codigosExistentes.Contains(codigo))
            {
                erroresFila.Add(new ImportErrorDto { Fila = row, Campo = "Código", Mensaje = $"El código '{codigo}' ya existe para este emisor." });
            }
            else if (!codigoAutoGenerado && codigosEnArchivo.Contains(codigo))
            {
                erroresFila.Add(new ImportErrorDto { Fila = row, Campo = "Código", Mensaje = $"El código '{codigo}' está duplicado en el archivo." });
            }
            codigosEnArchivo.Add(codigo);

            // Col 2: Nombre (ya leído)
            if (nombre.Length < 3 || nombre.Length > 200)
            {
                erroresFila.Add(new ImportErrorDto { Fila = row, Campo = "Nombre", Mensaje = "El nombre debe tener entre 3 y 200 caracteres." });
            }

            // Col 3: Descripción
            var descripcion = ws.Cell(row, 3).GetString().Trim();
            if (descripcion.Length > 500) descripcion = descripcion[..500];

            // Col 4: Categoría
            var categoriaNombre = ws.Cell(row, 4).GetString().Trim();
            if (!string.IsNullOrWhiteSpace(categoriaNombre))
            {
                var catExiste = categorias.Any(c => c.Nombre.Equals(categoriaNombre, StringComparison.OrdinalIgnoreCase));
                if (!catExiste && !categoriasNuevas.ContainsKey(categoriaNombre))
                {
                    categoriasNuevas[categoriaNombre] = $"CAT-{categorias.Count + categoriasNuevas.Count + 1:D3}";
                }
            }

            // Col 5: Marca
            var marcaNombre = ws.Cell(row, 5).GetString().Trim();
            if (!string.IsNullOrWhiteSpace(marcaNombre))
            {
                var marcaExiste = marcas.Any(m => m.Nombre.Equals(marcaNombre, StringComparison.OrdinalIgnoreCase));
                if (!marcaExiste)
                    marcasNuevas.Add(marcaNombre);
            }

            // Col 6: Precio Costo
            decimal? precioCosto = null;
            var precioCostoStr = ws.Cell(row, 6).GetString().Trim();
            if (!string.IsNullOrWhiteSpace(precioCostoStr))
            {
                if (!decimal.TryParse(precioCostoStr, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var pc))
                    erroresFila.Add(new ImportErrorDto { Fila = row, Campo = "Precio Costo", Mensaje = "Valor numérico inválido." });
                else
                    precioCosto = pc;
            }

            // Col 7: Precio Venta (obligatorio). Smartix guarda PrecioVenta como
            // BASE (sin IVA) — la conversion se hace mas abajo cuando ya
            // conocemos tipoImpuesto y porcentajeIVA, segun la nueva col 8.
            decimal precioVenta = 0;
            var precioVentaStr = ws.Cell(row, 7).GetString().Trim();
            if (string.IsNullOrWhiteSpace(precioVentaStr))
            {
                erroresFila.Add(new ImportErrorDto { Fila = row, Campo = "Precio Venta", Mensaje = "El precio de venta es obligatorio." });
            }
            else if (!decimal.TryParse(precioVentaStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out precioVenta) || precioVenta <= 0)
            {
                erroresFila.Add(new ImportErrorDto { Fila = row, Campo = "Precio Venta", Mensaje = "Debe ser un número mayor a 0." });
            }

            // Col 8: Precio Incluye IVA (Sí/No). Si vacio asumimos "Sí" (la mayoria
            // de admins piensa en precios al cliente). La conversion /1.13 se aplica
            // solo si Gravado; para Exento/NoSujeto se ignora la celda.
            // Vease tambien ProductosView.vue manual form que tiene el mismo toggle.
            bool precioIncluyeIva = true;  // default
            var incluyeIvaStr = ws.Cell(row, 8).GetString().Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(incluyeIvaStr))
            {
                if (incluyeIvaStr == "sí" || incluyeIvaStr == "si" || incluyeIvaStr == "yes" || incluyeIvaStr == "true")
                    precioIncluyeIva = true;
                else if (incluyeIvaStr == "no" || incluyeIvaStr == "false")
                    precioIncluyeIva = false;
                else
                    erroresFila.Add(new ImportErrorDto { Fila = row, Campo = "Precio Incluye IVA", Mensaje = $"Valor '{incluyeIvaStr}' no reconocido. Opciones: Sí, No (o vacío = Sí)." });
            }

            // Col 9: Unidad de Medida (obligatorio)
            int catUnidadMedidaId = 0;
            var unidadStr = ws.Cell(row, 9).GetString().Trim();
            if (string.IsNullOrWhiteSpace(unidadStr))
            {
                erroresFila.Add(new ImportErrorDto { Fila = row, Campo = "Unidad de Medida", Mensaje = "La unidad de medida es obligatoria." });
            }
            else
            {
                var unidad = unidadesMedida.FirstOrDefault(u =>
                    u.Valor.Equals(unidadStr, StringComparison.OrdinalIgnoreCase) ||
                    u.Codigo.Equals(unidadStr, StringComparison.OrdinalIgnoreCase));
                if (unidad == null)
                    erroresFila.Add(new ImportErrorDto { Fila = row, Campo = "Unidad de Medida", Mensaje = $"Unidad '{unidadStr}' no encontrada en el catálogo." });
                else
                    catUnidadMedidaId = unidad.Id;
            }

            // Col 10: Tipo Impuesto (obligatorio, debe ser valor reconocido)
            TipoImpuesto tipoImpuesto = TipoImpuesto.Gravado;
            var tipoImpStr = ws.Cell(row, 10).GetString().Trim();
            if (string.IsNullOrWhiteSpace(tipoImpStr))
            {
                erroresFila.Add(new ImportErrorDto { Fila = row, Campo = "Tipo Impuesto", Mensaje = "El tipo de impuesto es obligatorio. Opciones: Gravado, Exento, No Sujeto." });
            }
            else
            {
                var tipoLower = tipoImpStr.ToLower();
                if (tipoLower == "gravado") tipoImpuesto = TipoImpuesto.Gravado;
                else if (tipoLower == "exento") tipoImpuesto = TipoImpuesto.Exento;
                else if (tipoLower == "no sujeto") tipoImpuesto = TipoImpuesto.NoSujeto;
                else erroresFila.Add(new ImportErrorDto { Fila = row, Campo = "Tipo Impuesto", Mensaje = $"Valor '{tipoImpStr}' no reconocido. Opciones: Gravado, Exento, No Sujeto." });
            }

            // Col 11: % IVA
            decimal? porcentajeIVA = tipoImpuesto == TipoImpuesto.Gravado ? 13m : null;
            var ivaStr = ws.Cell(row, 11).GetString().Trim();
            if (!string.IsNullOrWhiteSpace(ivaStr) && decimal.TryParse(ivaStr,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var ivaVal))
            {
                porcentajeIVA = ivaVal;
            }

            // Conversion a base: si precioIncluyeIva && Gravado && porcentajeIVA > 0,
            // dividir el precioVenta para guardarlo como BASE (consistente con el
            // contrato del BD y con el form manual de ProductosView.vue).
            // Exento/NoSujeto se guardan tal cual (no llevan IVA).
            if (precioIncluyeIva
                && tipoImpuesto == TipoImpuesto.Gravado
                && porcentajeIVA.HasValue && porcentajeIVA.Value > 0
                && precioVenta > 0)
            {
                precioVenta = Math.Round(
                    precioVenta / (1m + (porcentajeIVA.Value / 100m)),
                    2,
                    MidpointRounding.AwayFromZero);
            }

            // Col 12: Código Barras / SKU
            var codigoBarras = ws.Cell(row, 12).GetString().Trim();
            if (string.IsNullOrWhiteSpace(codigoBarras)) codigoBarras = null;

            // Col 13-15: Stock
            decimal? stockMinimo = ParseDecimalOptional(ws.Cell(row, 13).GetString());
            decimal? stockMaximo = ParseDecimalOptional(ws.Cell(row, 14).GetString());
            decimal? puntoReorden = ParseDecimalOptional(ws.Cell(row, 15).GetString());

            // Col 16: Permite Venta Sin Stock
            bool? permiteVentaSinStock = null;
            var permiteStr = ws.Cell(row, 16).GetString().Trim().ToLower();
            if (permiteStr == "sí" || permiteStr == "si" || permiteStr == "yes" || permiteStr == "true")
                permiteVentaSinStock = true;
            else if (permiteStr == "no" || permiteStr == "false")
                permiteVentaSinStock = false;

            if (erroresFila.Count > 0)
            {
                result.Fallidos++;
                result.Errores.AddRange(erroresFila);
            }
            else
            {
                filasValidadas.Add(new FilaProductoValidada
                {
                    Codigo = codigo,
                    Nombre = nombre,
                    Descripcion = descripcion,
                    CategoriaNombre = categoriaNombre,
                    MarcaNombre = marcaNombre,
                    PrecioCosto = precioCosto,
                    PrecioVenta = precioVenta,
                    CatUnidadMedidaId = catUnidadMedidaId,
                    TipoImpuesto = tipoImpuesto,
                    PorcentajeIVA = porcentajeIVA,
                    CodigoBarras = codigoBarras,
                    StockMinimo = stockMinimo,
                    StockMaximo = stockMaximo,
                    PuntoReorden = puntoReorden,
                    PermiteVentaSinStock = permiteVentaSinStock,
                    PrecioIncluyeIva = precioIncluyeIva,
                });
            }
        }

        // ============================================================
        // FASE 2: Si hay errores, NO guardar nada
        // ============================================================
        if (result.Errores.Count > 0)
        {
            result.Exitosos = 0;
            return result;
        }

        // ============================================================
        // FASE 3: Todo válido → crear categorías, marcas y productos
        // ============================================================
        // Crear categorías nuevas
        foreach (var (nombre, codigoCat) in categoriasNuevas)
        {
            var cat = new Categoria
            {
                Nombre = nombre,
                Codigo = codigoCat,
                EmisorId = emisorId,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };
            _context.Categorias.Add(cat);
            categorias.Add(cat);
            result.CategoriasCreadas++;
        }

        // Crear marcas nuevas
        foreach (var marcaNombre in marcasNuevas)
        {
            var marca = new Marca
            {
                Nombre = marcaNombre,
                EmisorId = emisorId,
                Activa = true,
                FechaCreacion = DateTime.UtcNow
            };
            _context.Marcas.Add(marca);
            marcas.Add(marca);
            result.MarcasCreadas++;
        }

        if (categoriasNuevas.Count > 0 || marcasNuevas.Count > 0)
            await _context.SaveChangesAsync();

        // Crear productos
        foreach (var fila in filasValidadas)
        {
            int? categoriaId = null;
            if (!string.IsNullOrWhiteSpace(fila.CategoriaNombre))
            {
                categoriaId = categorias.First(c =>
                    c.Nombre.Equals(fila.CategoriaNombre, StringComparison.OrdinalIgnoreCase)).Id;
            }

            int? marcaId = null;
            if (!string.IsNullOrWhiteSpace(fila.MarcaNombre))
            {
                marcaId = marcas.First(m =>
                    m.Nombre.Equals(fila.MarcaNombre, StringComparison.OrdinalIgnoreCase)).Id;
            }

            var producto = new ProductoServicio
            {
                Codigo = fila.Codigo,
                Nombre = fila.Nombre,
                Descripcion = string.IsNullOrWhiteSpace(fila.Descripcion) ? null : fila.Descripcion,
                CategoriaId = categoriaId,
                MarcaId = marcaId,
                PrecioCosto = fila.PrecioCosto,
                PrecioVenta = fila.PrecioVenta,
                CatUnidadMedidaId = fila.CatUnidadMedidaId,
                CatTipoItemId = 1, // Producto
                TipoImpuesto = fila.TipoImpuesto,
                PorcentajeIVA = fila.PorcentajeIVA,
                CodigoBarras = fila.CodigoBarras,
                StockMinimo = fila.StockMinimo,
                StockMaximo = fila.StockMaximo,
                PuntoReorden = fila.PuntoReorden,
                PermiteVentaSinStock = fila.PermiteVentaSinStock,
                PrecioIncluyeIva = fila.PrecioIncluyeIva,
                EmisorId = emisorId,
                AccesoTodasSucursales = true,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            _context.ProductosServicios.Add(producto);
            result.Exitosos++;
        }

        if (result.Exitosos > 0)
            await _context.SaveChangesAsync();

        return result;
    }

    public async Task<ImportResultDto> ImportarServiciosDesdeExcelAsync(Stream excelStream, int emisorId)
    {
        var result = new ImportResultDto();

        using var workbook = new XLWorkbook(excelStream);
        var ws = workbook.Worksheets.First();

        int headerRow = EncontrarFilaHeaders(ws, "Nombre *");
        if (headerRow == -1)
        {
            result.Errores.Add(new ImportErrorDto
            {
                Fila = 0,
                Campo = "General",
                Mensaje = "No se encontró la fila de encabezados. Asegúrese de usar la plantilla descargada del sistema."
            });
            return result;
        }

        int dataStartRow = headerRow + 1;

        var categorias = await _context.Categorias
            .Where(c => c.EmisorId == emisorId && c.Activo)
            .ToListAsync();

        var sucursalesEmisor = await _context.Sucursales
            .Where(s => s.EmisorId == emisorId && s.Activo)
            .ToListAsync();

        var codigosExistentes = await _context.ProductosServicios
            .Where(p => p.EmisorId == emisorId && p.Activo)
            .Select(p => p.Codigo)
            .ToListAsync();

        var ultimoCodigo = codigosExistentes
            .Where(c => c.StartsWith("SERV-"))
            .Select(c => int.TryParse(c.Replace("SERV-", ""), out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();
        int secuencia = ultimoCodigo + 1;

        // Determinar unidad de medida por defecto para servicios
        var unidadesMedida = await _context.Set<Domain.Entities.Catalogos.CatUnidadMedida>().ToListAsync();
        var unidadServicio = unidadesMedida.FirstOrDefault(u => u.Valor == "Otro (especificar)")
            ?? unidadesMedida.FirstOrDefault(u => u.Valor.Contains("Servicio"))
            ?? unidadesMedida.FirstOrDefault(u => u.Codigo == "99")
            ?? unidadesMedida.First();

        int lastRow = ws.LastRowUsed()?.RowNumber() ?? dataStartRow;

        // ============================================================
        // FASE 1: Validar todas las filas SIN guardar nada
        // ============================================================
        var filasValidadas = new List<FilaServicioValidada>();
        var categoriasNuevas = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var codigosEnArchivo = new HashSet<string>();

        for (int row = dataStartRow; row <= lastRow; row++)
        {
            var nombre = ws.Cell(row, 2).GetString().Trim();
            if (string.IsNullOrWhiteSpace(nombre))
                continue;

            result.TotalFilas++;
            var erroresFila = new List<ImportErrorDto>();

            // Col 1: Código
            var codigo = ws.Cell(row, 1).GetString().Trim();
            bool codigoAutoGenerado = false;
            if (string.IsNullOrWhiteSpace(codigo))
            {
                codigo = $"SERV-{secuencia:D5}";
                secuencia++;
                codigoAutoGenerado = true;
            }
            else if (codigo.Length > 50)
            {
                erroresFila.Add(new ImportErrorDto { Fila = row, Campo = "Código", Mensaje = "El código excede 50 caracteres." });
            }

            if (codigosExistentes.Contains(codigo))
            {
                erroresFila.Add(new ImportErrorDto { Fila = row, Campo = "Código", Mensaje = $"El código '{codigo}' ya existe para este emisor." });
            }
            else if (!codigoAutoGenerado && codigosEnArchivo.Contains(codigo))
            {
                erroresFila.Add(new ImportErrorDto { Fila = row, Campo = "Código", Mensaje = $"El código '{codigo}' está duplicado en el archivo." });
            }
            codigosEnArchivo.Add(codigo);

            // Col 2: Nombre
            if (nombre.Length < 3 || nombre.Length > 200)
            {
                erroresFila.Add(new ImportErrorDto { Fila = row, Campo = "Nombre", Mensaje = "El nombre debe tener entre 3 y 200 caracteres." });
            }

            // Col 3: Descripción
            var descripcion = ws.Cell(row, 3).GetString().Trim();
            if (descripcion.Length > 500) descripcion = descripcion[..500];

            // Col 4: Categoría
            var categoriaNombre = ws.Cell(row, 4).GetString().Trim();
            if (!string.IsNullOrWhiteSpace(categoriaNombre))
            {
                var catExiste = categorias.Any(c => c.Nombre.Equals(categoriaNombre, StringComparison.OrdinalIgnoreCase));
                if (!catExiste && !categoriasNuevas.ContainsKey(categoriaNombre))
                {
                    categoriasNuevas[categoriaNombre] = $"CAT-{categorias.Count + categoriasNuevas.Count + 1:D3}";
                }
            }

            // Col 5: Precio Venta (obligatorio). Smartix guarda PrecioVenta como
            // BASE (sin IVA) — la conversion se hace mas abajo segun la nueva col 6.
            decimal precioVenta = 0;
            var precioVentaStr = ws.Cell(row, 5).GetString().Trim();
            if (string.IsNullOrWhiteSpace(precioVentaStr))
            {
                erroresFila.Add(new ImportErrorDto { Fila = row, Campo = "Precio Venta", Mensaje = "El precio de venta es obligatorio." });
            }
            else if (!decimal.TryParse(precioVentaStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out precioVenta) || precioVenta <= 0)
            {
                erroresFila.Add(new ImportErrorDto { Fila = row, Campo = "Precio Venta", Mensaje = "Debe ser un número mayor a 0." });
            }

            // Col 6: Precio Incluye IVA (Sí/No). Default "Sí" si vacio.
            // Mismo patron que productos — ver comentario en ImportarProductosDesdeExcelAsync.
            bool precioIncluyeIva = true;
            var incluyeIvaStr = ws.Cell(row, 6).GetString().Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(incluyeIvaStr))
            {
                if (incluyeIvaStr == "sí" || incluyeIvaStr == "si" || incluyeIvaStr == "yes" || incluyeIvaStr == "true")
                    precioIncluyeIva = true;
                else if (incluyeIvaStr == "no" || incluyeIvaStr == "false")
                    precioIncluyeIva = false;
                else
                    erroresFila.Add(new ImportErrorDto { Fila = row, Campo = "Precio Incluye IVA", Mensaje = $"Valor '{incluyeIvaStr}' no reconocido. Opciones: Sí, No (o vacío = Sí)." });
            }

            // Col 7: Tipo Impuesto (obligatorio, debe ser valor reconocido)
            TipoImpuesto tipoImpuesto = TipoImpuesto.Gravado;
            var tipoImpStr = ws.Cell(row, 7).GetString().Trim();
            if (string.IsNullOrWhiteSpace(tipoImpStr))
            {
                erroresFila.Add(new ImportErrorDto { Fila = row, Campo = "Tipo Impuesto", Mensaje = "El tipo de impuesto es obligatorio. Opciones: Gravado, Exento, No Sujeto." });
            }
            else
            {
                var tipoLower = tipoImpStr.ToLower();
                if (tipoLower == "gravado") tipoImpuesto = TipoImpuesto.Gravado;
                else if (tipoLower == "exento") tipoImpuesto = TipoImpuesto.Exento;
                else if (tipoLower == "no sujeto") tipoImpuesto = TipoImpuesto.NoSujeto;
                else erroresFila.Add(new ImportErrorDto { Fila = row, Campo = "Tipo Impuesto", Mensaje = $"Valor '{tipoImpStr}' no reconocido. Opciones: Gravado, Exento, No Sujeto." });
            }

            // Col 8: % IVA
            decimal? porcentajeIVA = tipoImpuesto == TipoImpuesto.Gravado ? 13m : null;
            var ivaStr = ws.Cell(row, 8).GetString().Trim();
            if (!string.IsNullOrWhiteSpace(ivaStr) && decimal.TryParse(ivaStr,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var ivaVal))
            {
                porcentajeIVA = ivaVal;
            }

            // Conversion a base: si precioIncluyeIva && Gravado && porcentajeIVA > 0,
            // dividir el precioVenta para guardarlo como BASE (consistente con el
            // contrato del BD y con el form manual ServiciosView.vue).
            if (precioIncluyeIva
                && tipoImpuesto == TipoImpuesto.Gravado
                && porcentajeIVA.HasValue && porcentajeIVA.Value > 0
                && precioVenta > 0)
            {
                precioVenta = Math.Round(
                    precioVenta / (1m + (porcentajeIVA.Value / 100m)),
                    2,
                    MidpointRounding.AwayFromZero);
            }

            // Col 9: Sucursales asignadas
            var sucursalNombre = ws.Cell(row, 9).GetString().Trim();
            var sucursalIdsParaAsignar = new List<int>();
            bool accesoTodas = true;

            if (!string.IsNullOrWhiteSpace(sucursalNombre))
            {
                var suc = sucursalesEmisor.FirstOrDefault(s =>
                    s.Nombre.Equals(sucursalNombre, StringComparison.OrdinalIgnoreCase));
                if (suc == null)
                {
                    erroresFila.Add(new ImportErrorDto { Fila = row, Campo = "Sucursales", Mensaje = $"La sucursal '{sucursalNombre}' no existe para este emisor." });
                }
                else
                {
                    sucursalIdsParaAsignar.Add(suc.Id);
                    accesoTodas = false;
                }
            }

            if (erroresFila.Count > 0)
            {
                result.Fallidos++;
                result.Errores.AddRange(erroresFila);
            }
            else
            {
                filasValidadas.Add(new FilaServicioValidada
                {
                    Codigo = codigo,
                    Nombre = nombre,
                    Descripcion = descripcion,
                    CategoriaNombre = categoriaNombre,
                    PrecioVenta = precioVenta,
                    TipoImpuesto = tipoImpuesto,
                    PorcentajeIVA = porcentajeIVA,
                    SucursalIds = sucursalIdsParaAsignar,
                    AccesoTodas = accesoTodas,
                    UnidadMedidaId = unidadServicio.Id,
                    PrecioIncluyeIva = precioIncluyeIva,
                });
            }
        }

        // ============================================================
        // FASE 2: Si hay errores, NO guardar nada
        // ============================================================
        if (result.Errores.Count > 0)
        {
            result.Exitosos = 0;
            return result;
        }

        // ============================================================
        // FASE 3: Todo válido → crear categorías y servicios
        // ============================================================
        foreach (var (nombre, codigoCat) in categoriasNuevas)
        {
            var cat = new Categoria
            {
                Nombre = nombre,
                Codigo = codigoCat,
                EmisorId = emisorId,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };
            _context.Categorias.Add(cat);
            categorias.Add(cat);
            result.CategoriasCreadas++;
        }

        if (categoriasNuevas.Count > 0)
            await _context.SaveChangesAsync();

        var asignacionesSucursales = new List<(ProductoServicio servicio, List<int> sucursalIds)>();

        foreach (var fila in filasValidadas)
        {
            int? categoriaId = null;
            if (!string.IsNullOrWhiteSpace(fila.CategoriaNombre))
            {
                categoriaId = categorias.First(c =>
                    c.Nombre.Equals(fila.CategoriaNombre, StringComparison.OrdinalIgnoreCase)).Id;
            }

            var servicio = new ProductoServicio
            {
                Codigo = fila.Codigo,
                Nombre = fila.Nombre,
                Descripcion = string.IsNullOrWhiteSpace(fila.Descripcion) ? null : fila.Descripcion,
                CategoriaId = categoriaId,
                MarcaId = null,
                PrecioCosto = null,
                PrecioVenta = fila.PrecioVenta,
                CatUnidadMedidaId = fila.UnidadMedidaId,
                CatTipoItemId = 2, // Servicio
                TipoImpuesto = fila.TipoImpuesto,
                PorcentajeIVA = fila.PorcentajeIVA,
                CodigoBarras = null,
                StockMinimo = null,
                StockMaximo = null,
                PuntoReorden = null,
                PermiteVentaSinStock = null,
                PrecioIncluyeIva = fila.PrecioIncluyeIva,
                EmisorId = emisorId,
                AccesoTodasSucursales = fila.AccesoTodas,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            _context.ProductosServicios.Add(servicio);
            result.Exitosos++;

            if (fila.SucursalIds.Count > 0)
                asignacionesSucursales.Add((servicio, fila.SucursalIds));
        }

        if (result.Exitosos > 0)
        {
            await _context.SaveChangesAsync();

            // Crear las relaciones ProductoServicioSucursal
            foreach (var (servicio, sucIds) in asignacionesSucursales)
            {
                foreach (var sucId in sucIds)
                {
                    _context.ProductosServiciosSucursales.Add(new ProductoServicioSucursal
                    {
                        ProductoServicioId = servicio.Id,
                        SucursalId = sucId
                    });
                }
            }

            if (asignacionesSucursales.Count > 0)
                await _context.SaveChangesAsync();
        }

        return result;
    }

    /// <summary>
    /// Busca la fila que contiene los headers buscando un texto específico en las primeras 20 filas
    /// </summary>
    private static int EncontrarFilaHeaders(IXLWorksheet ws, string headerText)
    {
        for (int row = 1; row <= 20; row++)
        {
            for (int col = 1; col <= 15; col++)
            {
                var cellValue = ws.Cell(row, col).GetString().Trim();
                if (cellValue.Contains(headerText, StringComparison.OrdinalIgnoreCase))
                    return row;
            }
        }
        return -1;
    }

    private static decimal? ParseDecimalOptional(string value)
    {
        value = value?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(value)) return null;
        return decimal.TryParse(value, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    // Clases internas para la validación en 2 fases
    private class FilaProductoValidada
    {
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string CategoriaNombre { get; set; } = "";
        public string MarcaNombre { get; set; } = "";
        public decimal? PrecioCosto { get; set; }
        public decimal PrecioVenta { get; set; }
        public int CatUnidadMedidaId { get; set; }
        public TipoImpuesto TipoImpuesto { get; set; }
        public decimal? PorcentajeIVA { get; set; }
        public string? CodigoBarras { get; set; }
        public decimal? StockMinimo { get; set; }
        public decimal? StockMaximo { get; set; }
        public decimal? PuntoReorden { get; set; }
        public bool? PermiteVentaSinStock { get; set; }
        public bool PrecioIncluyeIva { get; set; }
    }

    private class FilaServicioValidada
    {
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string CategoriaNombre { get; set; } = "";
        public decimal PrecioVenta { get; set; }
        public TipoImpuesto TipoImpuesto { get; set; }
        public decimal? PorcentajeIVA { get; set; }
        public List<int> SucursalIds { get; set; } = new();
        public bool AccesoTodas { get; set; }
        public int UnidadMedidaId { get; set; }
        public bool PrecioIncluyeIva { get; set; }
    }
}
