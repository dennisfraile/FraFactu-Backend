using FraFactu.Application.Common.Interfaces;
using FraFactu.Domain.Entities.Catalogos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FraFactu.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class CatalogosController : ControllerBase
{
    private readonly ICatalogoService _service;

    public CatalogosController(ICatalogoService service)
    {
        _service = service;
    }

    /// <summary>
    /// Obtiene el listado de Departamentos de El Salvador.
    /// </summary>
    /// <returns>Lista de departamentos con su código MH.</returns>
    [HttpGet("departamentos")]
    public async Task<ActionResult<List<CatDepartamento>>> GetDepartamentos()
    {
        return Ok(await _service.GetCatalogoAsync<CatDepartamento>());
    }

    /// <summary>
    /// Obtiene todos los Municipios (incluye nuevos distritos y código de departamento).
    /// </summary>
    [HttpGet("municipios")]
    public async Task<ActionResult<List<CatMunicipio>>> GetMunicipios()
    {
        return Ok(await _service.GetCatalogoAsync<CatMunicipio>());
    }

    /// <summary>
    /// Obtiene todos los Distritos (CAT-008) con su código de departamento y municipio.
    /// Para la cascada Departamento → Municipio → Distrito.
    /// </summary>
    [HttpGet("distritos")]
    public async Task<ActionResult<List<CatDistrito>>> GetDistritos()
    {
        return Ok(await _service.GetCatalogoAsync<CatDistrito>());
    }

    /// <summary>
    /// Obtiene las Actividades Económicas (CAT-019).
    /// </summary>
    /// <remarks>
    /// Nota: Los registros con 'EsSeleccionable = false' son títulos de sección.
    /// </remarks>
    [HttpGet("actividades-economicas")]
    public async Task<ActionResult<List<CatActividadEconomica>>> GetActividades()
    {
        return Ok(await _service.GetCatalogoAsync<CatActividadEconomica>());
    }

    /// <summary>
    /// Obtiene los Tipos de Tributos (IVA, Renta, Fovial).
    /// </summary>
    [HttpGet("tributos")]
    public async Task<ActionResult<List<CatTributo>>> GetTributos()
    {
        return Ok(await _service.GetCatalogoAsync<CatTributo>());
    }

    /// <summary>
    /// Obtiene Unidades de Medida (Metros, Litros, Servicios).
    /// </summary>
    [HttpGet("unidades-medida")]
    public async Task<ActionResult<List<CatUnidadMedida>>> GetUnidades()
    {
        return Ok(await _service.GetCatalogoAsync<CatUnidadMedida>());
    }
    
    /// <summary>
    /// Obtiene los Tipos de Documento (Factura, CCF, Nota Crédito).
    /// </summary>
    [HttpGet("tipos-documento")]
    public async Task<ActionResult<List<CatTipoDocumento>>> GetTiposDocumento()
    {
        return Ok(await _service.GetCatalogoAsync<CatTipoDocumento>());
    }

    /// <summary>
    /// Obtiene los ambientes de destino.
    /// </summary>
    [HttpGet("ambientes-destino")]
    public async Task<ActionResult<List<CatAmbienteDestino>>> GetAmbientes()
    {
        return Ok(await _service.GetCatalogoAsync<CatAmbienteDestino>());
    }

    /// <summary>
    /// Obtiene los codigos del tipo de servicio.
    /// </summary>
    [HttpGet("tipos-servicios")]
    public async Task<ActionResult<List<CatCodigoTipoServicio>>> GetTiposServicio()
    {
        return Ok(await _service.GetCatalogoAsync<CatCodigoTipoServicio>());
    }

    /// <summary>
    /// Obtiene la condicion de operacion.
    /// </summary>
    [HttpGet("condiciones-operacione")]
    public async Task<ActionResult<List<CatCondicionOperacion>>> GetCondicionesOperacion()
    {
        return Ok(await _service.GetCatalogoAsync<CatCondicionOperacion>());
    } 

    /// <summary>
    /// Obtiene las formas de pago.
    /// </summary>
    [HttpGet("formas-pago")]
    public async Task<ActionResult<List<CatFormaPago>>> GetFormasPago()
    {
        return Ok(await _service.GetCatalogoAsync<CatFormaPago>());
    }

    /// <summary>
    /// Obtiene los modelos de facturacion.
    /// </summary>
    [HttpGet("modelos-facturacion")]
    public async Task<ActionResult<List<CatModeloFacturacion>>> GetModelosFacturacion()
    {
        return Ok(await _service.GetCatalogoAsync<CatModeloFacturacion>());
    } 

    /// <summary>
    /// Obtiene otros documentos asociados .
    /// </summary>
    [HttpGet("otros-documentos-asociados")]
    public async Task<ActionResult<List<CatOtrosDocumentosAsociados>>> GetOtrosDocumentosAsociados()
    {
        return Ok(await _service.GetCatalogoAsync<CatOtrosDocumentosAsociados>());
    }  

    /// <summary>
    /// Obtiene los plazos.
    /// </summary>
    [HttpGet("plazos")]
    public async Task<ActionResult<List<CatPlazo>>> GetPlazos()
    {
        return Ok(await _service.GetCatalogoAsync<CatPlazo>());
    }

    /// <summary>
    /// Obtiene los tipos de contingencias.
    /// </summary>
    [HttpGet("tipos-contingencias")]
    public async Task<ActionResult<List<CatTipoContingencia>>> GetTiposContingencia()
    {
        return Ok(await _service.GetCatalogoAsync<CatTipoContingencia>());
    }

    /// <summary>
    /// Obtiene los tipos de documentos de contingencia.
    /// </summary>
    [HttpGet("tipos-documento-contingencia")]
    public async Task<ActionResult<List<CatTipoDocumentoContingencia>>> GetTiposDocumentoContingencia()
    {
        return Ok(await _service.GetCatalogoAsync<CatTipoDocumentoContingencia>());
    }

    /// <summary>
    /// Obtiene los tipos de documentos de identificacion del receptor.
    /// </summary>
    [HttpGet("tipos-documento-identificacion-receptor")]
    public async Task<ActionResult<List<CatTipoDocumentoIdentificacionReceptor>>> GetTiposDocumentoIdentificacionReceptor()
    {
        return Ok(await _service.GetCatalogoAsync<CatTipoDocumentoIdentificacionReceptor>());
    }

    /// <summary>
    /// Obtiene los tipos de establecimientos.
    /// </summary>
    [HttpGet("tipos-establecimiento")]
    public async Task<ActionResult<List<CatTipoEstablecimiento>>> GetTiposEstablecimiento()
    {
        return Ok(await _service.GetCatalogoAsync<CatTipoEstablecimiento>());
    }

    /// <summary>
    /// Obtiene los tipos de generacion de documentos.
    /// </summary>
    [HttpGet("tipos-generacion-documentos")]
    public async Task<ActionResult<List<CatTipoGeneracionDocumento>>> GetTiposGeneracionDocumento()
    {
        return Ok(await _service.GetCatalogoAsync<CatTipoGeneracionDocumento>());
    }

    /// <summary>
    /// Obtiene los tipos de invalidacion.
    /// </summary>
    [HttpGet("tipos-invalidacion")]
    public async Task<ActionResult<List<CatTipoInvalidacion>>> GetTiposInvalidacion()
    {
        return Ok(await _service.GetCatalogoAsync<CatTipoInvalidacion>());
    }

    /// <summary>
    /// Obtiene los tipos de items.
    /// </summary>
    [HttpGet("tipos-items")]
    public async Task<ActionResult<List<CatTipoItem>>> GetTiposItem()
    {
        return Ok(await _service.GetCatalogoAsync<CatTipoItem>());
    }

    /// <summary>
    /// Obtiene los tipos de transmision.
    /// </summary>
    [HttpGet("tipos-transmision")]
    public async Task<ActionResult<List<CatTipoTransmision>>> GetTiposTransmision()
    {
        return Ok(await _service.GetCatalogoAsync<CatTipoTransmision>());
    }
    
}