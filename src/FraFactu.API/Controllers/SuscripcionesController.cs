using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Suscripciones;
using FraFactu.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FraFactu.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SuscripcionesController : ControllerBase
{
    private readonly ISuscripcionService _suscripcionService;
    private readonly ILogger<SuscripcionesController> _logger;

    public SuscripcionesController(
        ISuscripcionService suscripcionService,
        ILogger<SuscripcionesController> logger)
    {
        _suscripcionService = suscripcionService;
        _logger = logger;
    }

    // ==========================================
    // CRUD SUSCRIPCIONES (SuperAdmin)
    // ==========================================

    /// <summary>
    /// Lista todas las suscripciones con paginación (SuperAdmin)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? orderBy = null,
        [FromQuery] string orderDirection = "asc")
    {
        var request = new PaginatedRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SearchTerm = search,
            OrderBy = orderBy,
            OrderDirection = orderDirection
        };

        var result = await _suscripcionService.GetAllAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene una suscripción por Id (SuperAdmin)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var result = await _suscripcionService.GetByIdAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Crea una suscripción para un emisor (SuperAdmin)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateSuscripcionDto dto)
    {
        try
        {
            var result = await _suscripcionService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza una suscripción (SuperAdmin)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSuscripcionDto dto)
    {
        try
        {
            var result = await _suscripcionService.UpdateAsync(id, dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Elimina (desactiva) una suscripción (SuperAdmin)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _suscripcionService.DeleteAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ==========================================
    // CONFIGURACIÓN PROVEEDOR (SuperAdmin)
    // ==========================================

    /// <summary>
    /// Obtiene la configuración del proveedor
    /// </summary>
    [HttpGet("configuracion-proveedor")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetConfiguracionProveedor()
    {
        var result = await _suscripcionService.GetConfiguracionProveedorAsync();
        return Ok(result);
    }

    /// <summary>
    /// Actualiza la configuración del proveedor (SuperAdmin)
    /// </summary>
    [HttpPut("configuracion-proveedor")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> UpdateConfiguracionProveedor([FromBody] ConfiguracionProveedorDto dto)
    {
        var result = await _suscripcionService.UpdateConfiguracionProveedorAsync(dto);
        return Ok(result);
    }

    // ==========================================
    // MI SUSCRIPCIÓN (EmisorAdmin)
    // ==========================================

    /// <summary>
    /// Obtiene la suscripción del emisor autenticado (EmisorAdmin)
    /// </summary>
    [HttpGet("mi-suscripcion")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero,Contador,Auditor")]
    public async Task<IActionResult> GetMiSuscripcion()
    {
        var emisorIdClaim = User.FindFirst("EmisorId")?.Value;
        if (string.IsNullOrEmpty(emisorIdClaim) || !int.TryParse(emisorIdClaim, out var emisorId))
            return Unauthorized(new { message = "No se pudo obtener el EmisorId del token" });

        var result = await _suscripcionService.GetMiSuscripcionAsync(emisorId);
        return Ok(result);
    }

    /// <summary>
    /// Descarga el PDF de suscripción del mes actual (EmisorAdmin)
    /// </summary>
    [HttpGet("mi-suscripcion/descargar-pdf")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero,Contador,Auditor")]
    public async Task<IActionResult> DescargarMiPdf()
    {
        var emisorIdClaim = User.FindFirst("EmisorId")?.Value;
        if (string.IsNullOrEmpty(emisorIdClaim) || !int.TryParse(emisorIdClaim, out var emisorId))
            return Unauthorized(new { message = "No se pudo obtener el EmisorId del token" });

        try
        {
            var pdfBytes = await _suscripcionService.GenerarPdfMiSuscripcionAsync(emisorId);
            var mesAnio = DateTime.UtcNow.ToString("yyyy-MM");
            return File(pdfBytes, "application/pdf", $"Suscripcion-{mesAnio}.pdf");
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ==========================================
    // REACTIVAR SUSCRIPCIÓN (SuperAdmin)
    // ==========================================

    /// <summary>
    /// Reactiva una suscripción desactivada, recalculando fechas (SuperAdmin)
    /// </summary>
    [HttpPost("{id}/reactivar")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Reactivar(int id)
    {
        try
        {
            var result = await _suscripcionService.ReactivarAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ==========================================
    // MARCAR COMO PAGADO (SuperAdmin)
    // ==========================================

    /// <summary>
    /// Marca una suscripción como pagada y avanza al siguiente período (SuperAdmin)
    /// </summary>
    [HttpPost("{id}/marcar-pagado")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> MarcarComoPagado(int id)
    {
        try
        {
            var result = await _suscripcionService.MarcarComoPagadoAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ==========================================
    // VISTA PREVIA PDF (SuperAdmin)
    // ==========================================

    /// <summary>
    /// Genera una vista previa del PDF de factura para una suscripción (SuperAdmin)
    /// </summary>
    [HttpGet("{id}/preview-pdf")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> PreviewPdf(int id)
    {
        try
        {
            var pdfBytes = await _suscripcionService.GenerarPdfPreviewAsync(id);
            return File(pdfBytes, "application/pdf", $"Preview-Suscripcion-{id}.pdf");
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ==========================================
    // ENVÍO MANUAL DE RECORDATORIO (SuperAdmin)
    // ==========================================

    /// <summary>
    /// Envía un recordatorio de pago manualmente (SuperAdmin)
    /// </summary>
    [HttpPost("{id}/enviar-recordatorio")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> EnviarRecordatorio(int id)
    {
        try
        {
            await _suscripcionService.EnviarRecordatorioManualAsync(id);
            return Ok(new { message = "Recordatorio enviado exitosamente" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando recordatorio manual para suscripción {Id}", id);
            return StatusCode(500, new { message = "Error al enviar el recordatorio" });
        }
    }

    // ==========================================
    // HISTORIAL DE FACTURAS
    // ==========================================

    /// <summary>
    /// Obtiene el historial de facturas de una suscripción (SuperAdmin)
    /// </summary>
    [HttpGet("{id}/historial")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetHistorialFacturas(int id)
    {
        var result = await _suscripcionService.GetHistorialFacturasAsync(id);
        return Ok(result);
    }
}
