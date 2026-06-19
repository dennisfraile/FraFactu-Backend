using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FraFactu.Application.Services;
using FraFactu.Application.Interfaces;
using FraFactu.Application.DTOs.DtesRecibidos;
using FraFactu.Application.Validators.DtesRecibidos;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace FraFactu.API.Controllers;

/// <summary>
/// Controller para gestión de DTEs recibidos desde correo electrónico
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DtesRecibidosController : ControllerBase
{
    private readonly IDteRecibidoService _dteRecibidoService;
    private readonly IEmailReaderService _emailReaderService;
    private readonly IGmailApiService _gmailApiService;
    private readonly ICurrentUserService _currentUser;
    private readonly ApplicationDbContext _context;

    public DtesRecibidosController(
        IDteRecibidoService dteRecibidoService,
        IEmailReaderService emailReaderService,
        IGmailApiService gmailApiService,
        ICurrentUserService currentUser,
        ApplicationDbContext context)
    {
        _dteRecibidoService = dteRecibidoService;
        _emailReaderService = emailReaderService;
        _gmailApiService = gmailApiService;
        _currentUser = currentUser;
        _context = context;
    }

    private int GetEmisorId()
    {
        var emisorIdClaim = User.FindFirst("EmisorId")?.Value;
        if (string.IsNullOrEmpty(emisorIdClaim) || !int.TryParse(emisorIdClaim, out var emisorId))
            throw new UnauthorizedAccessException("EmisorId no encontrado en el token");
        return emisorId;
    }

    /// <summary>
    /// Listar DTEs recibidos con filtros y paginación
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Auditor")]
    public async Task<ActionResult> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        [FromQuery] string? estado = null,
        [FromQuery] string? tipoDte = null,
        [FromQuery] string? emisorNit = null,
        [FromQuery] DateTime? fechaDesde = null,
        [FromQuery] DateTime? fechaHasta = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false)
    {
        try
        {
            var emisorId = GetEmisorId();
            var (items, total) = await _dteRecibidoService.ListarAsync(
                emisorId, pagina, tamanoPagina, estado, tipoDte, emisorNit,
                fechaDesde, fechaHasta, search, sortBy, sortDesc);

            return Ok(new
            {
                items,
                total,
                pagina,
                tamanoPagina,
                totalPaginas = (int)Math.Ceiling(total / (double)tamanoPagina)
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtener detalle de un DTE recibido con JSON completo
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Auditor")]
    public async Task<ActionResult<DteRecibidoDto>> ObtenerPorId(int id)
    {
        try
        {
            var emisorId = GetEmisorId();
            var dte = await _dteRecibidoService.ObtenerPorIdAsync(id, emisorId);
            return Ok(dte);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Encola una lectura de correos para extraer DTEs. El procesamiento es
    /// asíncrono; el cliente debe hacer polling al endpoint de estado con
    /// el <c>jobId</c> devuelto. Idempotente: si ya hay un job activo para
    /// el emisor, devuelve ese mismo en vez de crear otro.
    ///
    /// El body es opcional; si se omite, se lee el mes en curso. Para leer
    /// meses anteriores, enviar <c>{ "mesInicio": "YYYY-MM", "mesFin": "YYYY-MM" }</c>
    /// (ambos inclusivos, ambos requeridos si se manda el body).
    /// </summary>
    [HttpPost("leer-correo")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
    public async Task<ActionResult<EncolarLecturaCorreoResponseDto>> LeerCorreo(
        [FromBody] EncolarLecturaCorreoRequestDto? body,
        [FromServices] FluentValidation.IValidator<EncolarLecturaCorreoRequestDto> validator,
        CancellationToken ct)
    {
        try
        {
            var emisorId = GetEmisorId();
            var efectivo = body ?? new EncolarLecturaCorreoRequestDto();

            var validacion = await validator.ValidateAsync(efectivo, ct);
            if (!validacion.IsValid)
                return BadRequest(new { error = validacion.Errors[0].ErrorMessage });

            var (desde, hasta) = EncolarLecturaCorreoRequestDtoValidator.AMesesUtc(efectivo);

            var resultado = await _emailReaderService.EncolarLecturaAsync(
                emisorId,
                usuarioId: _currentUser.GetUsuarioId(),
                rangoDesde: desde,
                rangoHasta: hasta,
                esAutomatico: false,
                ct: ct);
            return Accepted(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Devuelve el estado del job de lectura de correo más reciente del
    /// emisor (con contadores en vivo). Pensado para polling desde la UI
    /// mientras el procesamiento esté EN_PROGRESO.
    /// </summary>
    [HttpGet("leer-correo/estado")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Auditor")]
    public async Task<ActionResult<EstadoLecturaCorreoJobDto>> EstadoLectura(CancellationToken ct)
    {
        try
        {
            var emisorId = GetEmisorId();
            var estado = await _emailReaderService.ObtenerEstadoLecturaAsync(emisorId, ct);
            return Ok(estado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Carga manual masiva de DTEs CCF en formato JSON o JWT.
    /// Acepta hasta <c>MaxArchivosPorCarga</c> archivos de <c>MaxBytesPorArchivo</c>
    /// cada uno. Cada archivo se valida individualmente (es DTE → es CCF →
    /// receptor == emisor) y se devuelve un reporte detallado por archivo.
    /// </summary>
    [HttpPost("cargar-json")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
    [RequestSizeLimit(600L * 1024 * 1024)]
    public async Task<ActionResult<CargaMasivaDtesResponseDto>> CargarJson(
        [FromForm] IFormFileCollection archivos,
        CancellationToken ct)
    {
        const int MaxArchivosPorCarga = 100;
        const long MaxBytesPorArchivo = 5L * 1024 * 1024;

        try
        {
            var emisorId = GetEmisorId();

            if (archivos == null || archivos.Count == 0)
                return BadRequest(new { error = "Debe adjuntar al menos un archivo." });

            if (archivos.Count > MaxArchivosPorCarga)
                return BadRequest(new
                {
                    error = $"Demasiados archivos: máximo {MaxArchivosPorCarga} por subida (recibidos {archivos.Count})."
                });

            var excesivos = archivos.Where(a => a.Length > MaxBytesPorArchivo)
                .Select(a => a.FileName).ToList();
            if (excesivos.Count > 0)
                return BadRequest(new
                {
                    error = $"Archivos que exceden 5 MB: {string.Join(", ", excesivos)}."
                });

            var lista = new List<ArchivoAIngestar>(archivos.Count);
            foreach (var f in archivos)
            {
                using var reader = new StreamReader(f.OpenReadStream(), Encoding.UTF8);
                var contenido = await reader.ReadToEndAsync(ct);
                lista.Add(new ArchivoAIngestar
                {
                    NombreArchivo = f.FileName,
                    Contenido = contenido
                });
            }

            var resultado = await _dteRecibidoService.CargarDtesManualmenteAsync(
                emisorId, lista, cargadoPorUsuarioId: _currentUser.GetUsuarioId(), ct);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Descartar un DTE pendiente
    /// </summary>
    [HttpPost("{id}/descartar")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
    public async Task<ActionResult> Descartar(
        int id,
        [FromBody] DescartarDteDto dto,
        [FromServices] FluentValidation.IValidator<DescartarDteDto> validator)
    {
        try
        {
            var validacion = await validator.ValidateAsync(dto);
            if (!validacion.IsValid)
                return BadRequest(new { error = validacion.Errors[0].ErrorMessage });

            var emisorId = GetEmisorId();
            await _dteRecibidoService.DescartarAsync(id, dto, emisorId);
            return Ok(new { message = "DTE descartado exitosamente" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Crear una compra pre-llenada desde un DTE recibido (legacy: todos los
    /// items van como gasto administrativo, no afectan stock). Para clasificar
    /// productos vs gastos use <see cref="MapearYCrearCompra"/>.
    /// </summary>
    [HttpPost("{id}/crear-compra")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
    public async Task<ActionResult> CrearCompra(int id, [FromQuery] int sucursalId)
    {
        try
        {
            var emisorId = GetEmisorId();
            var compraId = await _dteRecibidoService.CrearCompraDesdeAsync(id, emisorId, sucursalId);
            return Ok(new { compraId, message = "Compra creada exitosamente desde DTE" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// F1 (Plan inventario desde DTE): wizard de mapeo. Recibe la lista de
    /// items del DTE ya clasificados por el usuario (producto existente /
    /// producto nuevo / gasto) y crea la compra correspondiente con los
    /// detalles que tocan inventario.
    /// </summary>
    [HttpPost("{id}/mapear-y-crear-compra")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
    public async Task<ActionResult<MapearDteCompraResponseDto>> MapearYCrearCompra(
        int id, [FromBody] MapearDteCompraDto dto)
    {
        var validador = new MapearDteCompraDtoValidator();
        var resultado = validador.Validate(dto);
        if (!resultado.IsValid)
        {
            return BadRequest(new
            {
                error = "Datos del wizard inválidos",
                errores = resultado.Errors.Select(e => new { campo = e.PropertyName, mensaje = e.ErrorMessage })
            });
        }

        try
        {
            var emisorId = GetEmisorId();
            var respuesta = await _dteRecibidoService.MapearYCrearCompraAsync(
                id, dto, emisorId, _currentUser.GetUsuarioId());
            return Ok(respuesta);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Vincular un DTE a una compra existente
    /// </summary>
    [HttpPost("{id}/vincular/{compraId}")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
    public async Task<ActionResult> Vincular(int id, int compraId)
    {
        try
        {
            var emisorId = GetEmisorId();
            await _dteRecibidoService.VincularACompraAsync(id, compraId, emisorId);
            return Ok(new { message = "DTE vinculado a compra exitosamente" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Configurar lectura de correos
    /// </summary>
    [HttpPut("configuracion")]
    [Authorize(Roles = "EmisorAdmin")]
    public async Task<ActionResult> Configurar([FromBody] ConfiguracionLecturaCorreoDto dto)
    {
        try
        {
            var emisorId = GetEmisorId();
            await _emailReaderService.ConfigurarLecturaCorreoAsync(emisorId, dto);
            return Ok(new { message = "Configuración actualizada" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Probar conexión de Gmail para lectura de correos
    /// </summary>
    [HttpPost("probar-conexion")]
    [Authorize(Roles = "EmisorAdmin")]
    public async Task<ActionResult> ProbarConexion()
    {
        try
        {
            var emisorId = GetEmisorId();
            var (exitoso, mensaje) = await _emailReaderService.ProbarConexionGmailAsync(emisorId);
            return Ok(new { exitoso, mensaje });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtener estadísticas de DTEs recibidos. Acepta los mismos filtros
    /// opcionales que <c>GET /api/dtesrecibidos</c> para que las tarjetas
    /// queden sincronizadas con el listado. Sin filtros devuelve totales globales.
    /// </summary>
    [HttpGet("estadisticas")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Auditor")]
    public async Task<ActionResult<DteRecibidoEstadisticasDto>> ObtenerEstadisticas(
        [FromQuery] string? estado = null,
        [FromQuery] string? tipoDte = null,
        [FromQuery] string? emisorNit = null,
        [FromQuery] DateTime? fechaDesde = null,
        [FromQuery] DateTime? fechaHasta = null,
        [FromQuery] string? search = null)
    {
        try
        {
            var emisorId = GetEmisorId();
            var stats = await _dteRecibidoService.ObtenerEstadisticasAsync(
                emisorId, estado, tipoDte, emisorNit, fechaDesde, fechaHasta, search);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Enviar correos de prueba con CCF simulados al Gmail del emisor.
    /// Solo disponible en ambiente de pruebas (CatAmbienteDestinoId = 1 / código "00").
    /// </summary>
    [HttpPost("enviar-prueba")]
    [Authorize(Roles = "EmisorAdmin")]
    public async Task<ActionResult> EnviarCorreosPrueba([FromQuery] int cantidad = 3)
    {
        try
        {
            var emisorId = GetEmisorId();

            var emisor = await _context.Emisores
                .Include(e => e.AmbienteDestino)
                .FirstOrDefaultAsync(e => e.Id == emisorId);

            if (emisor == null)
                return NotFound(new { error = "Emisor no encontrado" });

            // Solo permitir en ambiente de pruebas
            if (emisor.CatAmbienteDestinoId != 1 || emisor.AmbienteDestino?.Codigo != "00")
                return BadRequest(new { error = "Esta función solo está disponible en ambiente de pruebas" });

            if (!emisor.GmailConectado || string.IsNullOrEmpty(emisor.GmailRefreshToken) || string.IsNullOrEmpty(emisor.GmailEmail))
                return BadRequest(new { error = "Gmail no está conectado. Conecte Gmail primero." });

            if (cantidad < 1 || cantidad > 10)
                cantidad = 3;

            var proveedoresPrueba = new[]
            {
                new { Nit = "06141804941035", Nrc = "1234567", Nombre = "DISTRIBUIDORA EL BUEN PRECIO S.A. DE C.V." },
                new { Nit = "06140108710123", Nrc = "9876543", Nombre = "COMERCIAL SAN JOSE S.A. DE C.V." },
                new { Nit = "06142203890456", Nrc = "5551234", Nombre = "IMPORTADORA CENTRO AMERICANA S.A." },
                new { Nit = "06140905670789", Nrc = "7773456", Nombre = "SUMINISTROS INDUSTRIALES DE EL SALVADOR" },
                new { Nit = "06141507820321", Nrc = "3339876", Nombre = "TECNOLOGIA Y SERVICIOS DIGITALES S.A. DE C.V." },
            };

            var productosPrueba = new[]
            {
                new { Codigo = "PROD-001", Desc = "Resma de papel bond carta", Precio = 4.50m, Qty = 50m },
                new { Codigo = "PROD-002", Desc = "Toner para impresora HP LaserJet", Precio = 35.00m, Qty = 5m },
                new { Codigo = "PROD-003", Desc = "Caja de lapiceros azules x12", Precio = 3.25m, Qty = 20m },
                new { Codigo = "SERV-001", Desc = "Servicio de mantenimiento de equipo", Precio = 100.00m, Qty = 1m },
                new { Codigo = "PROD-004", Desc = "Rollo de papel termico para POS", Precio = 2.50m, Qty = 100m },
                new { Codigo = "PROD-005", Desc = "Archivador de palanca oficio", Precio = 5.75m, Qty = 12m },
                new { Codigo = "SERV-002", Desc = "Servicio de limpieza mensual", Precio = 250.00m, Qty = 1m },
                new { Codigo = "PROD-006", Desc = "Cartucho de tinta color negro", Precio = 18.50m, Qty = 4m },
            };

            var random = new Random();
            var enviados = 0;

            for (int i = 0; i < cantidad; i++)
            {
                var prov = proveedoresPrueba[random.Next(proveedoresPrueba.Length)];
                var numItems = random.Next(1, 4);
                var items = new List<object>();
                decimal totalGravada = 0;

                for (int j = 0; j < numItems; j++)
                {
                    var prod = productosPrueba[random.Next(productosPrueba.Length)];
                    var qty = Math.Round(prod.Qty * (decimal)(0.5 + random.NextDouble()), 0);
                    var gravada = Math.Round(prod.Precio * qty, 2);
                    totalGravada += gravada;

                    items.Add(new
                    {
                        numItem = j + 1,
                        tipoItem = prod.Codigo.StartsWith("SERV") ? 2 : 1,
                        codigo = prod.Codigo,
                        uniMedida = 59,
                        descripcion = prod.Desc,
                        cantidad = qty,
                        precioUni = prod.Precio,
                        montoDescu = 0.00m,
                        ventaNoSuj = 0.00m,
                        ventaExenta = 0.00m,
                        ventaGravada = gravada,
                        tributos = new[] { "20" },
                        psv = 0.00m,
                        noGravado = 0.00m
                    });
                }

                var iva = Math.Round(totalGravada * 0.13m, 2);
                var total = totalGravada + iva;
                var codigoGen = Guid.NewGuid().ToString().ToUpper();
                var fechaEmision = DateTime.UtcNow.AddDays(-random.Next(0, 30));
                var numControl = $"DTE-03-0001-{random.Next(1, 999999999):D15}";

                var dteJson = new
                {
                    identificacion = new
                    {
                        version = 3,
                        ambiente = "00",
                        tipoDte = "03",
                        numeroControl = numControl,
                        codigoGeneracion = codigoGen,
                        tipoModelo = 1,
                        tipoOperacion = 1,
                        fecEmi = fechaEmision.ToString("yyyy-MM-dd"),
                        horEmi = fechaEmision.ToString("HH:mm:ss"),
                        tipoMoneda = "USD"
                    },
                    emisor = new
                    {
                        nit = prov.Nit,
                        nrc = prov.Nrc,
                        nombre = prov.Nombre,
                        codActividad = "46100",
                        descActividad = "Comercio al por mayor",
                        tipoEstablecimiento = "01",
                        direccion = new { departamento = "06", municipio = "14", complemento = "San Salvador" },
                        telefono = "22001234",
                        correo = "proveedor@prueba.com"
                    },
                    receptor = new
                    {
                        nit = emisor.Nit,
                        nrc = emisor.Nrc,
                        nombre = emisor.NombreRazonSocial,
                        correo = emisor.CorreoElectronico
                    },
                    cuerpoDocumento = items,
                    resumen = new
                    {
                        totalNoSuj = 0.00m,
                        totalExenta = 0.00m,
                        totalGravada,
                        subTotalVentas = totalGravada,
                        descuNoSuj = 0.00m,
                        descuExenta = 0.00m,
                        descuGravada = 0.00m,
                        totalDescu = 0.00m,
                        tributos = new[] { new { codigo = "20", descripcion = "Impuesto al Valor Agregado 13%", valor = iva } },
                        subTotal = totalGravada,
                        montoTotalOperacion = total,
                        totalPagar = total,
                        totalLetras = "MONTO EN LETRAS",
                        condicionOperacion = 1,
                        pagos = new[] { new { codigo = "01", montoPago = total } }
                    }
                };

                var jsonBytes = Encoding.UTF8.GetBytes(
                    JsonSerializer.Serialize(dteJson, new JsonSerializerOptions { WriteIndented = true }));

                var fileName = $"CCF_{codigoGen[..8]}.json";
                var subject = $"[PRUEBA] CCF de {prov.Nombre} - {fechaEmision:dd/MM/yyyy}";
                var body = $"<p>Comprobante de Crédito Fiscal de prueba</p><p>Proveedor: {prov.Nombre}<br/>NIT: {prov.Nit}<br/>Total: ${total:N2}</p>";

                var attachments = new List<(byte[] content, string name, string mimeType)>
                {
                    (jsonBytes, fileName, "application/json")
                };

                await _gmailApiService.EnviarEmailGmailAsync(
                    emisor.GmailRefreshToken,
                    emisor.GmailEmail,
                    emisor.GmailEmail, // Se envía a sí mismo
                    subject,
                    body,
                    attachments);

                enviados++;
            }

            return Ok(new
            {
                message = $"Se enviaron {enviados} correos de prueba con CCF al buzón {emisor.GmailEmail}",
                enviados,
                instruccion = "Ahora presione 'Leer Correo' para importar los DTEs"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Error enviando correos de prueba: {ex.Message}" });
        }
    }
}
