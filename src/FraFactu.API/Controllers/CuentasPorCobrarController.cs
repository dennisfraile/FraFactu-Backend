using FraFactu.Application.DTOs.Cuotas;
using FraFactu.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FraFactu.API.Controllers
{
    [ApiController]
    [Route("api/cuentas-por-cobrar")]
    [Authorize]
    public class CuentasPorCobrarController : ControllerBase
    {
        private readonly IPlanCuotasService _service;
        private readonly IConfiguracionCuotasService _configService;
        private readonly IAgingService _agingService;
        private readonly IAgingExcelExporter _agingExcel;
        private readonly IAgingPdfExporter _agingPdf;
        private readonly IEstadoCuentaService _estadoCuentaService;
        private readonly IEstadoCuentaExcelExporter _estadoCuentaExcel;
        private readonly IEstadoCuentaPdfExporter _estadoCuentaPdf;
        private readonly ILogger<CuentasPorCobrarController> _logger;

        public CuentasPorCobrarController(
            IPlanCuotasService service,
            IConfiguracionCuotasService configService,
            IAgingService agingService,
            IAgingExcelExporter agingExcel,
            IAgingPdfExporter agingPdf,
            IEstadoCuentaService estadoCuentaService,
            IEstadoCuentaExcelExporter estadoCuentaExcel,
            IEstadoCuentaPdfExporter estadoCuentaPdf,
            ILogger<CuentasPorCobrarController> logger)
        {
            _service = service;
            _configService = configService;
            _agingService = agingService;
            _agingExcel = agingExcel;
            _agingPdf = agingPdf;
            _estadoCuentaService = estadoCuentaService;
            _estadoCuentaExcel = estadoCuentaExcel;
            _estadoCuentaPdf = estadoCuentaPdf;
            _logger = logger;
        }

        private int EmisorId => int.Parse(User.FindFirst("EmisorId")?.Value
            ?? throw new UnauthorizedAccessException("EmisorId no encontrado en el token"));

        /// <summary>Crear una venta a crédito/mixto con plan de cuotas (emite la cuota 1).</summary>
        [HttpPost]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero")]
        public async Task<IActionResult> Crear([FromBody] CrearVentaCreditoDto dto)
        {
            try
            {
                var plan = await _service.CrearVentaCreditoAsync(dto, EmisorId);
                return CreatedAtAction(nameof(GetById), new { planId = plan.Id }, plan);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>Listado de planes con saldo del emisor.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool soloConSaldo = true)
            => Ok(await _service.GetAllAsync(EmisorId, soloConSaldo));

        /// <summary>Detalle de un plan.</summary>
        [HttpGet("{planId:int}")]
        public async Task<IActionResult> GetById(int planId)
        {
            var plan = await _service.GetByIdAsync(planId, EmisorId);
            return plan is null ? NotFound() : Ok(plan);
        }

        /// <summary>Registrar el pago de una cuota (emite su DTE).</summary>
        [HttpPost("{planId:int}/cuotas/{numero:int}/pagar")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero")]
        public async Task<IActionResult> PagarCuota(
            int planId, int numero, [FromBody] RegistrarPagoCuotaDto dto)
        {
            try
            {
                var plan = await _service.PagarCuotaAsync(planId, numero, dto, EmisorId);
                return Ok(plan);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>Mora estimada de una cuota a la fecha actual (solo lectura, no emite).</summary>
        [HttpGet("{planId:int}/cuotas/{numero:int}/mora-estimada")]
        public async Task<IActionResult> EstimarMora(int planId, int numero)
        {
            try
            {
                var est = await _service.EstimarMoraCuotaAsync(planId, numero, EmisorId);
                return Ok(est);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>Obtener (o crear con valores por defecto) la configuración de mora del emisor.</summary>
        [HttpGet("configuracion-mora")]
        public async Task<IActionResult> GetConfiguracionMora()
            => Ok(await _configService.ObtenerOCrearAsync(EmisorId));

        /// <summary>Actualizar la configuración de mora del emisor.</summary>
        [HttpPut("configuracion-mora")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
        public async Task<IActionResult> ActualizarConfiguracionMora([FromBody] ActualizarConfiguracionCuotasDto dto)
            => Ok(await _configService.ActualizarAsync(EmisorId, dto));

        /// <summary>Refinanciar un plan de cuotas con saldo pendiente.</summary>
        [HttpPost("{planId:int}/refinanciar")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
        public async Task<IActionResult> Refinanciar(int planId, [FromBody] RefinanciarPlanDto dto)
        {
            try
            {
                var usuarioId = int.TryParse(
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid)
                    ? uid : (int?)null;
                var plan = await _service.RefinanciarPlanAsync(planId, dto, EmisorId, usuarioId);
                return Ok(plan);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>Reporte de antigüedad de saldos en Excel.</summary>
        [HttpGet("reportes/antiguedad/excel")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Auditor")]
        public async Task<IActionResult> AntiguedadExcel()
        {
            var rep = await _agingService.GenerarAsync(EmisorId);
            var bytes = _agingExcel.Generar(rep);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Antiguedad_Saldos_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        /// <summary>Reporte de antigüedad de saldos en PDF.</summary>
        [HttpGet("reportes/antiguedad/pdf")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Auditor")]
        public async Task<IActionResult> AntiguedadPdf()
        {
            var rep = await _agingService.GenerarAsync(EmisorId);
            var bytes = _agingPdf.Generar(rep);
            return File(bytes, "application/pdf", $"Antiguedad_Saldos_{DateTime.Now:yyyyMMdd}.pdf");
        }

        /// <summary>Estado de cuenta de un plan (PDF).</summary>
        [HttpGet("reportes/estado-cuenta/plan/{planId:int}/pdf")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero")]
        public async Task<IActionResult> EstadoCuentaPlanPdf(int planId)
        {
            var dto = await _estadoCuentaService.GenerarPlanAsync(planId, EmisorId);
            if (dto == null) return NotFound();
            return File(_estadoCuentaPdf.GenerarPlan(dto), "application/pdf",
                $"EstadoCuenta_Plan_{planId}_{DateTime.Now:yyyyMMdd}.pdf");
        }

        /// <summary>Estado de cuenta de un plan (Excel).</summary>
        [HttpGet("reportes/estado-cuenta/plan/{planId:int}/excel")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero")]
        public async Task<IActionResult> EstadoCuentaPlanExcel(int planId)
        {
            var dto = await _estadoCuentaService.GenerarPlanAsync(planId, EmisorId);
            if (dto == null) return NotFound();
            return File(_estadoCuentaExcel.GenerarPlan(dto), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"EstadoCuenta_Plan_{planId}_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        /// <summary>Estado de cuenta de un cliente (PDF).</summary>
        [HttpGet("reportes/estado-cuenta/cliente/{receptorId:int}/pdf")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero")]
        public async Task<IActionResult> EstadoCuentaClientePdf(int receptorId)
        {
            var dto = await _estadoCuentaService.GenerarClienteAsync(receptorId, EmisorId);
            if (dto == null) return NotFound();
            return File(_estadoCuentaPdf.GenerarCliente(dto), "application/pdf",
                $"EstadoCuenta_Cliente_{receptorId}_{DateTime.Now:yyyyMMdd}.pdf");
        }

        /// <summary>Estado de cuenta de un cliente (Excel).</summary>
        [HttpGet("reportes/estado-cuenta/cliente/{receptorId:int}/excel")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero")]
        public async Task<IActionResult> EstadoCuentaClienteExcel(int receptorId)
        {
            var dto = await _estadoCuentaService.GenerarClienteAsync(receptorId, EmisorId);
            if (dto == null) return NotFound();
            return File(_estadoCuentaExcel.GenerarCliente(dto), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"EstadoCuenta_Cliente_{receptorId}_{DateTime.Now:yyyyMMdd}.xlsx");
        }
    }
}
