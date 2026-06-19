using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Facturas;
using FraFactu.Application.Interfaces;
using FraFactu.API.Helpers;

namespace FraFactu.API.Controllers
{
    /// <summary>
    /// Controller para gestionar Facturas Electrónicas (DTEs)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requiere autenticación JWT
    public class FacturasController : ControllerBase
    {
        private readonly IFacturaService _facturaService;
        private readonly IValidator<CreateFacturaElectronicaDto> _validator;
        private readonly ILogger<FacturasController> _logger;

        public FacturasController(
            IFacturaService facturaService,
            IValidator<CreateFacturaElectronicaDto> validator,
            ILogger<FacturasController> logger)
        {
            _facturaService = facturaService;
            _validator = validator;
            _logger = logger;
        }

        /// <summary>
        /// Crear una nueva Factura Electrónica
        /// </summary>
        /// <param name="dto">Datos de la factura</param>
        /// <returns>Factura creada con código de generación y número de control</returns>
        /// <response code="201">Factura creada exitosamente</response>
        /// <response code="400">Datos de entrada inválidos</response>
        /// <response code="401">No autenticado</response>
        [HttpPost]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero")]
        [ProducesResponseType(typeof(FacturaElectronicaResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateFacturaElectronicaDto dto)
        {
            // 1. Validar con FluentValidation
            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );

                return BadRequest(new ValidationProblemDetails(errors)
                {
                    Title = "Errores de validación en la factura",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Validar restricción de sucursal
            // Si el usuario tiene restricción, solo puede crear en su sucursal
            if (!ScopeHelper.ValidarAccesoSucursal(User, dto.SucursalId))
            {
                return StatusCode(403, new { error = "No tiene permiso para crear facturas en esta sucursal" });
            }

            // 2. Resolver EmisorId desde el JWT.
            var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
                ?? throw new UnauthorizedAccessException("EmisorId no encontrado en el token"));

            try
            {
                // 3. Crear factura
                var factura = await _facturaService.CreateAsync(dto, emisorId);

                // 4. Retornar 201 Created con Location header
                return CreatedAtAction(
                    nameof(GetByCodigoGeneracion),
                    new { codigo = factura.CodigoGeneracion },
                    factura
                );
            }
            catch (InvalidOperationException ex)
            {
                var facturaId = ex.Data.Contains("FacturaId") ? (int?)ex.Data["FacturaId"] : null;
                return BadRequest(new { error = ex.Message, facturaId });
            }
        }

        /// <summary>
        /// Obtener factura por ID
        /// </summary>
        /// <param name="id">ID de la factura</param>
        /// <returns>Detalle completo de la factura</returns>
        /// <response code="200">Factura encontrada</response>
        /// <response code="404">Factura no encontrada</response>
        [HttpGet("{id}")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero,Contador,Auditor")]
        [ProducesResponseType(typeof(FacturaElectronicaResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
                ?? throw new UnauthorizedAccessException("EmisorId no encontrado"));

            var factura = await _facturaService.GetByIdAsync(id, emisorId);

            if (factura == null)
                return NotFound(new { error = $"Factura con ID {id} no encontrada" });

            // Validar acceso si el usuario tiene restricción y la factura pertenece a una sucursal específica
            if (factura.SucursalId.HasValue && !ScopeHelper.ValidarAccesoSucursal(User, factura.SucursalId.Value))
            {
                return StatusCode(403, new { error = "No tiene permiso para ver esta factura (pertenece a otra sucursal)" });
            }

            return Ok(factura);
        }

        /// <summary>
        /// Obtener factura por Código de Generación (GUID)
        /// </summary>
        /// <param name="codigo">Código de generación de la factura</param>
        /// <returns>Detalle completo de la factura</returns>
        /// <response code="200">Factura encontrada</response>
        /// <response code="404">Factura no encontrada</response>
        [HttpGet("codigo/{codigo}")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero,Contador,Auditor")]
        [ProducesResponseType(typeof(FacturaElectronicaResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByCodigoGeneracion(string codigo)
        {
            var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
                ?? throw new UnauthorizedAccessException("EmisorId no encontrado"));

            var factura = await _facturaService.GetByCodigoGeneracionAsync(codigo, emisorId);

            if (factura == null)
                return NotFound(new { error = $"Factura con código {codigo} no encontrada" });

            // Validar restricción de sucursal
            if (factura.SucursalId.HasValue && !ScopeHelper.ValidarAccesoSucursal(User, factura.SucursalId.Value))
            {
                return StatusCode(403, new { error = "No tiene permiso para ver esta factura" });
            }

            return Ok(factura);
        }

        /// <summary>
        /// Listar facturas con paginación
        /// </summary>
        /// <param name="pageNumber">Número de página (default: 1)</param>
        /// <param name="pageSize">Tamaño de página (default: 10, max: 100)</param>
        /// <param name="sucursalId">ID de la sucursal para filtrar (opcional)</param>
        /// <param name="search">Término de búsqueda (número control, código, receptor)</param>
        /// <param name="fechaDesde">Fecha desde para filtrar (opcional)</param>
        /// <param name="fechaHasta">Fecha hasta para filtrar (opcional)</param>
        /// <param name="vendedorId">ID del vendedor para filtrar (opcional)</param>
        /// <param name="tipoDte">Tipo de DTE para filtrar (opcional)</param>
        /// <param name="estadoHacienda">Estado en Hacienda para filtrar (opcional)</param>
        /// <param name="catTipoTransmisionId">Tipo de transmisión para filtrar (opcional)</param>
        /// <param name="ambiente">Ambiente para filtrar: "00"=Pruebas, "01"=Producción (opcional)</param>
        /// <returns>Lista paginada de facturas</returns>
        /// <response code="200">Lista de facturas</response>
        [HttpGet]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero,Contador,Auditor")]
        [ProducesResponseType(typeof(PaginatedResponse<FacturaListDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? sucursalId = null,
            [FromQuery] string? search = null,
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null,
            [FromQuery] int? vendedorId = null,
            [FromQuery] string? tipoDte = null,
            [FromQuery] string? estadoHacienda = null,
            [FromQuery] int? catTipoTransmisionId = null,
            [FromQuery] string? ambiente = null)
        {
            var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
                ?? throw new UnauthorizedAccessException("EmisorId no encontrado"));

            var rol = ScopeHelper.GetRolFromClaims(User);

            // Determinar filtro de sucursal
            int? sucursalIdFinal = null;
            List<int>? sucursalIdsFinal = null;
            if (ScopeHelper.RequiereRestriccionSucursal(rol) && !ScopeHelper.GetAccesoTodasSucursales(User))
            {
                var userSucursalIds = ScopeHelper.GetSucursalIdsFromClaims(User);
                if (userSucursalIds.Count == 0)
                    return BadRequest(new { message = "Usuario con rol restringido no tiene sucursal asignada" });

                // Si pide una sucursal específica, validar que tenga acceso
                if (sucursalId.HasValue)
                {
                    if (!userSucursalIds.Contains(sucursalId.Value))
                        return Forbid();
                    sucursalIdFinal = sucursalId.Value;
                }
                else if (userSucursalIds.Count == 1)
                {
                    sucursalIdFinal = userSucursalIds[0];
                }
                else
                {
                    sucursalIdsFinal = userSucursalIds;
                }
            }
            else if (sucursalId.HasValue)
            {
                sucursalIdFinal = sucursalId.Value;
            }

            // Para Cajero, filtrar solo sus facturas
            int? usuarioIdFiltro = null;
            if (rol == "Cajero")
            {
                var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out var uid))
                    usuarioIdFiltro = uid;
            }

            // Validar parámetros
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var request = new PaginatedRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var facturas = await _facturaService.GetAllAsync(request, emisorId, sucursalIdFinal, search, fechaDesde, fechaHasta, vendedorId, usuarioIdFiltro, tipoDte, estadoHacienda, sucursalIdsFinal, catTipoTransmisionId, ambiente);

            return Ok(facturas);
        }

        /// <summary>
        /// Buscar facturas por criterios
        /// </summary>
        /// <param name="q">Término de búsqueda (número control, código, nombre receptor, etc.)</param>
        /// <param name="ambiente">Ambiente para filtrar: "00"=Pruebas, "01"=Producción (opcional)</param>
        /// <returns>Lista de facturas que coinciden con la búsqueda</returns>
        /// <response code="200">Resultados de la búsqueda</response>
        [HttpGet("search")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero,Contador,Auditor")]
        [ProducesResponseType(typeof(List<FacturaListDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] string? ambiente = null)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { error = "Debe proporcionar un término de búsqueda (parámetro 'q')" });

            var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
                ?? throw new UnauthorizedAccessException("EmisorId no encontrado"));

            var facturas = await _facturaService.SearchAsync(q, emisorId, ambiente);

            return Ok(facturas);
        }

        /// <summary>
        /// Obtener JSON del DTE según formato de Hacienda
        /// </summary>
        /// <param name="id">ID de la factura</param>
        /// <returns>JSON del DTE en formato oficial</returns>
        /// <response code="200">JSON generado</response>
        /// <response code="404">Factura no encontrada</response>
        [HttpGet("{id}/json-dte")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero,Contador,Auditor")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetJsonDte(int id)
        {
            var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
                ?? throw new UnauthorizedAccessException("EmisorId no encontrado"));

            // Validar acceso antes de entregar JSON
            var factura = await _facturaService.GetByIdAsync(id, emisorId);
            if (factura == null) return NotFound(new { error = "Factura no encontrada" });

            if (factura.SucursalId.HasValue && !ScopeHelper.ValidarAccesoSucursal(User, factura.SucursalId.Value))
                return StatusCode(403, new { error = "Acceso denegado a esta sucursal" });

            try
            {
                var jsonDte = await _facturaService.GenerateJsonDteAsync(id, emisorId);

                return Content(jsonDte, "application/json");
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Anular una factura
        /// </summary>
        /// <param name="id">ID de la factura</param>
        /// <param name="request">Motivo</param>
        /// <returns>Confirmación de anulación</returns>
        /// <response code="200">Factura anulada</response>
        /// <response code="404">Factura no encontrada</response>
        [HttpPost("{id}/anular")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Anular(int id, [FromBody] AnularFacturaRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Motivo))
                return BadRequest(new { error = "Debe proporcionar un motivo de anulación" });

            var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
                ?? throw new UnauthorizedAccessException("EmisorId no encontrado"));

            // Validar propiedad de la factura
            var factura = await _facturaService.GetByIdAsync(id, emisorId);
            if (factura == null) return NotFound(new { error = "Factura no encontrada" });

            if (factura.SucursalId.HasValue && !ScopeHelper.ValidarAccesoSucursal(User, factura.SucursalId.Value))
                return StatusCode(403, new { error = "No tiene permiso para anular facturas de esta sucursal" });

            var resultado = await _facturaService.AnularAsync(id, emisorId, request.Motivo);

            if (!resultado)
                return NotFound(new { error = $"Factura con ID {id} no encontrada" });

            return Ok(new { message = "Factura anulada exitosamente", facturaId = id });
        }

        /// <summary>
        /// Actualizar estado de Hacienda
        /// </summary>
        /// <param name="id">ID de la factura</param>
        /// <param name="request">Datos del estado</param>
        /// <returns>Confirmación de actualización</returns>
        /// <response code="200">Estado actualizado</response>
        /// <response code="404">Factura no encontrada</response>
        [HttpPut("{id}/estado-hacienda")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ActualizarEstadoHacienda(
            int id,
            [FromBody] ActualizarEstadoHaciendaRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Estado))
                return BadRequest(new { error = "Debe proporcionar el nuevo estado" });

            var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
                ?? throw new UnauthorizedAccessException("EmisorId no encontrado"));

            // Validar propiedad
            var factura = await _facturaService.GetByIdAsync(id, emisorId);
            if (factura == null) return NotFound(new { error = "Factura no encontrada" });

            // Nota: Quizás actualizar estado desde Hacienda (webhook o proceso background) no debería restringirse 
            // por sucursal del usuario actual si es el sistema quien lo hace, pero si es manual sí.
            if (factura.SucursalId.HasValue && !ScopeHelper.ValidarAccesoSucursal(User, factura.SucursalId.Value))
                return StatusCode(403, new { error = "Acceso denegado a esta sucursal" });

            var resultado = await _facturaService.ActualizarEstadoHaciendaAsync(
                id,
                emisorId,
                request.Estado,
                request.SelloRecepcion);

            if (!resultado)
                return NotFound(new { error = $"Factura con ID {id} no encontrada" });

            return Ok(new { message = "Estado actualizado exitosamente", facturaId = id, nuevoEstado = request.Estado });
        }

        /// <summary>
        /// Verifica si una factura puede ser invalidada (plazo de 72 horas)
        /// </summary>
        [HttpGet("{id}/puede-invalidar")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> PuedeInvalidar(int id)
        {
            var emisorIdClaim = User.FindFirst("EmisorId")?.Value;
            if (!int.TryParse(emisorIdClaim, out int emisorId))
                return Unauthorized();

            var factura = await _facturaService.GetByIdAsync(id, emisorId);
            if (factura == null)
                return NotFound();

            // Lógica de Plazos (Sincronizada con FacturaService)
            // Grupo 2 (3 Meses): 01, 11, 14
            // Grupo 1 (1 Día): Resto (03, 05, etc)

            DateTime fechaReferencia = factura.FechaTransmision ?? factura.FechaEmision;
            TimeSpan horaReferencia = factura.HoraTransmision ?? factura.HoraEmision;
            DateTime fechaHoraTransmision = fechaReferencia.Date.Add(horaReferencia);

            string codigoTipoDte = factura.Identificacion.TipoDte; // "01", "03", etc.
            var tiposTresMeses = new[] { "01", "11", "14" };

            DateTime fechaLimite;
            string descripcionPlazo;

            if (tiposTresMeses.Contains(codigoTipoDte))
            {
                fechaLimite = fechaHoraTransmision.AddMonths(3);
                descripcionPlazo = "3 meses";
            }
            else
            {
                // Hasta 23:59:59 del día siguiente
                fechaLimite = fechaHoraTransmision.Date.AddDays(1).Add(new TimeSpan(23, 59, 59));
                descripcionPlazo = "1 día (hasta el día siguiente)";
            }

            TimeSpan tiempoRestante = fechaLimite - DateTime.UtcNow;
            bool puedeInvalidar = DateTime.UtcNow <= fechaLimite;

            // Si ya tiene sello de anulación o estado anulado, no puede invalidar
            if (factura.Estado == "ANULADO" || factura.Estado == "RECHAZADO")
                puedeInvalidar = false;

            double horasRestantes = Math.Max(0, tiempoRestante.TotalHours);

            return Ok(new
            {
                puedeInvalidar,
                horasTranscurridas = (DateTime.UtcNow - fechaHoraTransmision).TotalHours,
                horasRestantes,
                fechaLimite,
                mensaje = puedeInvalidar
                    ? $"Puede invalidar. Quedan {Math.Floor(tiempoRestante.TotalDays)} días y {tiempoRestante.Hours} horas (Plazo: {descripcionPlazo})."
                    : $"Plazo expirado. El límite para {codigoTipoDte} es de {descripcionPlazo}."
            });
        }

        /// <summary>
        /// Invalidar una factura mediante evento oficial de MH
        /// </summary>
        [HttpPost("{id}/invalidar")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
        [ProducesResponseType(typeof(FraFactu.Application.DTOs.Invalidacion.InvalidacionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> InvalidarFactura(int id, [FromBody] FraFactu.Application.DTOs.Invalidacion.AnularFacturaDto dto)
        {
            try
            {
                var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
                    ?? throw new UnauthorizedAccessException("EmisorId no encontrado"));

                // Validar propiedad
                var factura = await _facturaService.GetByIdAsync(id, emisorId);
                if (factura == null) return NotFound(new { error = "Factura no encontrada" });

                if (factura.SucursalId.HasValue && !ScopeHelper.ValidarAccesoSucursal(User, factura.SucursalId.Value))
                    return StatusCode(403, new { error = "Acceso denegado a esta sucursal" });

                var resultado = await _facturaService.InvalidarFacturaAsync(id, dto, emisorId);
                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================
        // ENDPOINTS - ENVÍO POR LOTES
        // ==========================================

        /// <summary>
        /// Obtener facturas pendientes de envío (EstadoHacienda = 'PENDIENTE_ENVIO')
        /// </summary>
        /// <param name="fechaDesde">Fecha desde (opcional)</param>
        /// <param name="fechaHasta">Fecha hasta (opcional)</param>
        /// <param name="pageNumber">Número de página</param>
        /// <param name="pageSize">Tamaño de página</param>
        /// <param name="sucursalId">Filtrar por sucursal (opcional)</param>
        /// <param name="search">Búsqueda por texto (opcional)</param>
        /// <param name="ambiente">Ambiente para filtrar: "00"=Pruebas, "01"=Producción (opcional)</param>
        [HttpGet("pendientes")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero,Auditor,Contador")]
        [ProducesResponseType(typeof(PaginatedResponse<FacturaListDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerFacturasPendientes(
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? sucursalId = null,
            [FromQuery] string? search = null,
            [FromQuery] string? ambiente = null)
        {
            var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
                ?? throw new UnauthorizedAccessException("EmisorId no encontrado"));

            var rol = ScopeHelper.GetRolFromClaims(User);
            int? sucursalIdFinal = null;
            if (ScopeHelper.RequiereRestriccionSucursal(rol) && !ScopeHelper.GetAccesoTodasSucursales(User))
            {
                var userSucursalIds = ScopeHelper.GetSucursalIdsFromClaims(User);
                if (userSucursalIds.Count == 0)
                    return BadRequest(new { message = "Usuario con rol restringido no tiene sucursal asignada" });

                if (sucursalId.HasValue)
                {
                    if (!userSucursalIds.Contains(sucursalId.Value))
                        return Forbid();
                    sucursalIdFinal = sucursalId.Value;
                }
                else if (userSucursalIds.Count == 1)
                {
                    sucursalIdFinal = userSucursalIds[0];
                }
            }
            else if (sucursalId.HasValue)
            {
                sucursalIdFinal = sucursalId.Value;
            }

            var resultado = await _facturaService.ObtenerFacturasPendientesAsync(
                emisorId,
                fechaDesde,
                fechaHasta,
                pageNumber,
                pageSize,
                sucursalIdFinal,
                search,
                ambiente);

            return Ok(resultado);
        }

        /// <summary>
        /// Obtener facturas diferidas (PENDIENTE_LOTE, contingencia, transmision diferida)
        /// </summary>
        [HttpGet("diferidas")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero,Auditor")]
        [ProducesResponseType(typeof(PaginatedResponse<FacturaListDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerFacturasDiferidas(
            [FromQuery] PaginatedRequest request,
            [FromQuery] int? sucursalId = null,
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null,
            [FromQuery] string? ambiente = null)
        {
            var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
                ?? throw new UnauthorizedAccessException("EmisorId no encontrado"));

            var rol = ScopeHelper.GetRolFromClaims(User);
            List<int>? sucursalIds = null;
            int? usuarioId = null;

            if (ScopeHelper.RequiereRestriccionSucursal(rol) && !ScopeHelper.GetAccesoTodasSucursales(User))
            {
                var userSucursalIds = ScopeHelper.GetSucursalIdsFromClaims(User);
                if (userSucursalIds.Count == 0)
                    return BadRequest(new { message = "Usuario con rol restringido no tiene sucursal asignada" });

                if (sucursalId.HasValue)
                {
                    if (!userSucursalIds.Contains(sucursalId.Value))
                        return Forbid();
                    sucursalIds = new List<int> { sucursalId.Value };
                }
                else
                {
                    sucursalIds = userSucursalIds;
                }
            }
            else if (sucursalId.HasValue)
            {
                sucursalIds = new List<int> { sucursalId.Value };
            }

            if (rol == "Cajero")
            {
                var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out var uid))
                    usuarioId = uid;
            }

            var resultado = await _facturaService.ObtenerFacturasDiferidasAsync(
                request, emisorId, sucursalIds, fechaDesde, fechaHasta, usuarioId, ambiente);

            return Ok(resultado);
        }

        /// <summary>
        /// Guardar factura como pendiente SIN enviar a Hacienda
        /// (Botón "Guardar como Pendiente")
        /// </summary>
        [HttpPost("guardar-pendiente")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero")]
        [ProducesResponseType(typeof(FacturaElectronicaResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GuardarComoPendiente([FromBody] CreateFacturaElectronicaDto dto)
        {
            // Validar con FluentValidation
            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );

                return BadRequest(new ValidationProblemDetails(errors)
                {
                    Title = "Errores de validación en la factura",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Validar restricción de sucursal
            if (!ScopeHelper.ValidarAccesoSucursal(User, dto.SucursalId))
            {
                return StatusCode(403, new { error = "No tiene permiso para crear facturas en esta sucursal" });
            }

            var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
                ?? throw new UnauthorizedAccessException("EmisorId no encontrado"));

            try
            {
                var factura = await _facturaService.GuardarComoPendienteAsync(emisorId, dto);

                return CreatedAtAction(
                    nameof(GetByCodigoGeneracion),
                    new { codigo = factura.CodigoGeneracion },
                    factura
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Enviar factura individual a Hacienda (desde vista de pendientes)
        /// </summary>
        /// <param name="id">ID de la factura pendiente</param>
        [HttpPost("{id}/enviar-individual")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero")]
        [ProducesResponseType(typeof(FacturaElectronicaResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EnviarFacturaIndividual(int id)
        {
            var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
                ?? throw new UnauthorizedAccessException("EmisorId no encontrado"));

            try
            {
                var factura = await _facturaService.EnviarFacturaIndividualAsync(id);
                return Ok(factura);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Calcular tiempo restante antes de que venza el período de envío
        /// </summary>
        /// <param name="id">ID de la factura</param>
        [HttpGet("{id}/tiempo-restante")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero,Auditor")]
        [ProducesResponseType(typeof(TiempoRestanteDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerTiempoRestante(int id)
        {
            try
            {
                var resultado = await _facturaService.ObtenerTiempoRestanteAsync(id);
                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Reenviar correo electrónico con el DTE al receptor
        /// </summary>
        /// <param name="id">ID de la factura</param>
        [HttpPost("{id}/reenviar-correo")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReenviarCorreo(int id)
        {
            try
            {
                var resultado = await _facturaService.ReenviarCorreoAsync(id);
                if (resultado)
                {
                    return Ok(new { message = "Correo reenviado exitosamente", facturaId = id });
                }
                return NotFound(new { error = "Factura no encontrada" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ==========================================
        // ENDPOINTS - NOTA DE CRÉDITO
        // ==========================================

        /// <summary>
        /// Buscar DTEs procesados para referenciar en una Nota de Crédito
        /// </summary>
        /// <param name="search">Término de búsqueda (código generación, número control, receptor)</param>
        [HttpGet("buscar-para-nc")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero")]
        [ProducesResponseType(typeof(List<BuscarParaNcResultDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> BuscarParaNotaCredito([FromQuery] string search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return BadRequest(new { error = "Debe proporcionar un término de búsqueda" });

            var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
                ?? throw new UnauthorizedAccessException("EmisorId no encontrado"));

            var resultados = await _facturaService.BuscarParaNotaCreditoAsync(search, emisorId);
            return Ok(resultados);
        }

        /// <summary>
        /// Obtener detalle completo de un DTE para pre-cargar en una Nota de Crédito
        /// </summary>
        /// <param name="id">ID de la factura</param>
        [HttpGet("{id}/detalle-para-nc")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero")]
        [ProducesResponseType(typeof(DetalleParaNcDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObtenerDetalleParaNotaCredito(int id)
        {
            var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
                ?? throw new UnauthorizedAccessException("EmisorId no encontrado"));

            var detalle = await _facturaService.ObtenerDetalleParaNotaCreditoAsync(id, emisorId);
            if (detalle == null)
                return NotFound(new { error = "DTE no encontrado, no es tipo permitido para NC, o no está PROCESADO" });

            return Ok(detalle);
        }

        /// <summary>
        /// Buscar DTEs procesados para referenciar en una Nota de Débito
        /// </summary>
        [HttpGet("buscar-para-nd")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero")]
        [ProducesResponseType(typeof(List<BuscarParaNcResultDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> BuscarParaNotaDebito([FromQuery] string search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return BadRequest(new { error = "Debe proporcionar un término de búsqueda" });

            var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
                ?? throw new UnauthorizedAccessException("EmisorId no encontrado"));

            var resultados = await _facturaService.BuscarParaNotaDebitoAsync(search, emisorId);
            return Ok(resultados);
        }

        /// <summary>
        /// Obtener detalle de un DTE para referenciar en una Nota de Débito
        /// </summary>
        [HttpGet("{id}/detalle-para-nd")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero")]
        [ProducesResponseType(typeof(DetalleParaNcDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObtenerDetalleParaNotaDebito(int id)
        {
            var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
                ?? throw new UnauthorizedAccessException("EmisorId no encontrado"));

            var detalle = await _facturaService.ObtenerDetalleParaNotaDebitoAsync(id, emisorId);
            if (detalle == null)
                return NotFound(new { error = "DTE no encontrado, no es tipo permitido para ND, o no está PROCESADO" });

            return Ok(detalle);
        }

        [HttpDelete("{id}/descartar")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero")]
        public async Task<IActionResult> DescartarFacturaRechazada(int id)
        {
            var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
                ?? throw new UnauthorizedAccessException("EmisorId no encontrado"));

            try
            {
                await _facturaService.DescartarFacturaRechazadaAsync(id, emisorId);
                return Ok(new { message = "Factura descartada y stock revertido" });
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
    }

    /// <summary>
    /// Request para anular factura
    /// </summary>
    public class AnularFacturaRequest
    {
        public string Motivo { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request para actualizar estado de Hacienda
    /// </summary>
    public class ActualizarEstadoHaciendaRequest
    {
        public string Estado { get; set; } = string.Empty;
        public string? SelloRecepcion { get; set; }
    }
}
