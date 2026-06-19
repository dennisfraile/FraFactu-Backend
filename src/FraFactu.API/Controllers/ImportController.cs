using Microsoft.AspNetCore.Mvc;
using FraFactu.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace FraFactu.API.Controllers;

/// <summary>
/// Controller para importación masiva de datos desde Excel
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
public class ImportController : ControllerBase
{
    private readonly IImportService _importService;

    public ImportController(IImportService importService)
    {
        _importService = importService;
    }

    /// <summary>
    /// Importar productos desde archivo Excel
    /// </summary>
    [HttpPost("productos")]
    public async Task<IActionResult> ImportarProductos(IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0)
            return BadRequest(new { message = "Debe seleccionar un archivo Excel." });

        if (!archivo.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "El archivo debe ser formato Excel (.xlsx)." });

        var emisorId = GetEmisorId();

        try
        {
            using var stream = archivo.OpenReadStream();
            var result = await _importService.ImportarProductosDesdeExcelAsync(stream, emisorId);
            return Ok(result);
        }
        catch (Exception)
        {
            return BadRequest(new { message = "No se pudo leer el archivo. Verifique que sea un archivo Excel (.xlsx) válido y que esté usando la plantilla descargada del sistema." });
        }
    }

    /// <summary>
    /// Importar servicios desde archivo Excel
    /// </summary>
    [HttpPost("servicios")]
    public async Task<IActionResult> ImportarServicios(IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0)
            return BadRequest(new { message = "Debe seleccionar un archivo Excel." });

        if (!archivo.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "El archivo debe ser formato Excel (.xlsx)." });

        var emisorId = GetEmisorId();

        try
        {
            using var stream = archivo.OpenReadStream();
            var result = await _importService.ImportarServiciosDesdeExcelAsync(stream, emisorId);
            return Ok(result);
        }
        catch (Exception)
        {
            return BadRequest(new { message = "No se pudo leer el archivo. Verifique que sea un archivo Excel (.xlsx) válido y que esté usando la plantilla descargada del sistema." });
        }
    }

    private int GetEmisorId()
    {
        var emisorIdClaim = User.FindFirst("EmisorId")?.Value;
        if (string.IsNullOrEmpty(emisorIdClaim) || !int.TryParse(emisorIdClaim, out var emisorId))
            throw new UnauthorizedAccessException("No se encontró el EmisorId en el token.");
        return emisorId;
    }
}
