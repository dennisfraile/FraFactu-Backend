using AutoMapper;
using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Facturas;
using FraFactu.Application.Interfaces;
using FraFactu.Application.Services;
using FraFactu.Infrastructure.Helpers.Pagination;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FraFactu.Infrastructure.Services
{
    /// <summary>
    /// Implementación de lectura/consulta de Facturas Electrónicas (Fase 1 del refactor).
    /// </summary>
    public class FacturaQueryService : IFacturaQueryService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ISaldoDteService _saldoDteService;

        public FacturaQueryService(
            ApplicationDbContext context,
            IMapper mapper,
            ISaldoDteService saldoDteService)
        {
            _context = context;
            _mapper = mapper;
            _saldoDteService = saldoDteService;
        }

        public async Task<FacturaElectronicaResponseDto?> GetByIdAsync(int id, int emisorId)
        {
            var factura = await _context.Facturas
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.Departamento)
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.Municipio)
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.TipoEstablecimiento)
                .Include(f => f.Receptor!)
                    .ThenInclude(r => r.TipoDocumento)
                .Include(f => f.Receptor!)
                    .ThenInclude(r => r.Departamento)
                .Include(f => f.Receptor!)
                    .ThenInclude(r => r.Municipio)
                .Include(f => f.Detalles)
                .Include(f => f.Pagos)
                .Include(f => f.TipoDocumento)
                .Include(f => f.TipoContingencia)
                .Include(f => f.DocumentosRelacionados)
                    .ThenInclude(d => d.TipoDocumento)
                .Include(f => f.Apendices)
                .Include(f => f.Extension)
                .Include(f => f.VentaTercero)
                .Include(f => f.OtrosDocumentos)
                    .ThenInclude(o => o.Medico)
                .Include(f => f.Vendedor)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.TipoEstablecimiento)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.Departamento)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.Municipio)
                .Include(f => f.Caja)
                .Include(f => f.Tributos)
                .FirstOrDefaultAsync(f => f.Id == id && f.EmisorId == emisorId);

            if (factura == null)
                return null;

            return _mapper.Map<FacturaElectronicaResponseDto>(factura);
        }

        public async Task<FacturaElectronicaResponseDto?> GetByCodigoGeneracionAsync(string codigoGeneracion, int emisorId)
        {
            var factura = await _context.Facturas
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.Departamento)
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.Municipio)
                .Include(f => f.Emisor)
                    .ThenInclude(e => e.TipoEstablecimiento)
                .Include(f => f.Receptor!)
                    .ThenInclude(r => r.TipoDocumento)
                .Include(f => f.Receptor!)
                    .ThenInclude(r => r.Departamento)
                .Include(f => f.Receptor!)
                    .ThenInclude(r => r.Municipio)
                .Include(f => f.Detalles)
                .Include(f => f.Pagos)
                .Include(f => f.TipoDocumento)
                .Include(f => f.TipoContingencia)
                .Include(f => f.DocumentosRelacionados)
                    .ThenInclude(d => d.TipoDocumento)
                .Include(f => f.VentaTercero)
                .Include(f => f.OtrosDocumentos)
                    .ThenInclude(o => o.Medico)
                .Include(f => f.Vendedor)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.TipoEstablecimiento)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.Departamento)
                .Include(f => f.Sucursal!)
                    .ThenInclude(s => s.Municipio)
                .Include(f => f.Caja)
                .FirstOrDefaultAsync(f => f.CodigoGeneracion == codigoGeneracion && f.EmisorId == emisorId);

            if (factura == null)
                return null;

            return _mapper.Map<FacturaElectronicaResponseDto>(factura);
        }

        public async Task<PaginatedResponse<FacturaListDto>> GetAllAsync(PaginatedRequest request, int emisorId, int? sucursalId = null, string? search = null, DateTime? fechaDesde = null, DateTime? fechaHasta = null, int? vendedorId = null, int? usuarioId = null, string? tipoDte = null, string? estadoHacienda = null, List<int>? sucursalIds = null, int? catTipoTransmisionId = null, string? ambiente = null)
        {
            var query = _context.Facturas
                .AsNoTracking()
                .Where(f => f.EmisorId == emisorId);

            if (!string.IsNullOrEmpty(ambiente))
                query = query.Where(f => f.Ambiente == ambiente);

            // Filtrar por sucursal(es) si se proporciona (para roles restringidos)
            if (sucursalIds != null && sucursalIds.Any())
            {
                query = query.Where(f => f.SucursalId.HasValue && sucursalIds.Contains(f.SucursalId.Value));
            }
            else if (sucursalId.HasValue)
            {
                query = query.Where(f => f.SucursalId == sucursalId.Value);
            }

            // Filtrar por usuario (para Cajero)
            if (usuarioId.HasValue)
                query = query.Where(f => f.UsuarioId == usuarioId.Value);

            // Filtrar por tipo de DTE (código: "01" = Factura, "03" = CCF, etc.)
            if (!string.IsNullOrEmpty(tipoDte))
                query = query.Where(f => f.TipoDocumento.Codigo == tipoDte);

            // Filtrar por estado en Hacienda (PROCESADO, RECHAZADO, PENDIENTE_ENVIO, etc.)
            if (!string.IsNullOrEmpty(estadoHacienda))
                query = query.Where(f => f.EstadoHacienda == estadoHacienda);
            else if (string.IsNullOrEmpty(search) && !catTipoTransmisionId.HasValue)
                query = query.Where(f => f.EstadoHacienda != "PENDIENTE_ENVIO" && f.EstadoHacienda != "PENDIENTE_LOTE");

            // Filtrar por tipo de transmisión (1=Normal, 2=Contingencia/Diferido)
            // Si se pide diferidos (tipo 2), incluir también PENDIENTE_LOTE de transmisión normal
            // (facturas vencidas que necesitan reportarse en contingencia)
            if (catTipoTransmisionId.HasValue && catTipoTransmisionId.Value == 2)
                query = query.Where(f => f.CatTipoTransmisionId == 2 || f.EstadoHacienda == "PENDIENTE_LOTE");
            else if (catTipoTransmisionId.HasValue)
                query = query.Where(f => f.CatTipoTransmisionId == catTipoTransmisionId.Value);

            if (fechaDesde.HasValue)
            {
                var fechaDesdeUtc = DateTime.SpecifyKind(fechaDesde.Value.Date, DateTimeKind.Utc);
                query = query.Where(f => f.FechaEmision >= fechaDesdeUtc);
            }

            if (fechaHasta.HasValue)
            {
                // Agregar un día para incluir todo el día final
                var fechaHastaUtc = DateTime.SpecifyKind(fechaHasta.Value.Date.AddDays(1), DateTimeKind.Utc);
                query = query.Where(f => f.FechaEmision < fechaHastaUtc);
            }

            if (vendedorId.HasValue)
                query = query.Where(f => f.VendedorId == vendedorId.Value);

            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(f =>
                    f.NumeroControl.ToLower().Contains(searchLower) ||
                    f.CodigoGeneracion.ToLower().Contains(searchLower) ||
                    (f.Receptor != null && f.Receptor.NombreRazonSocial.ToLower().Contains(searchLower)) ||
                    (f.Receptor != null && f.Receptor.NumeroDocumento != null && f.Receptor.NumeroDocumento.ToLower().Contains(searchLower)));
            }

            query = query.OrderByDescending(f => f.FechaCreacion)
                .AsQueryable();

            var pagedData = await query
                .Select(f => new FacturaListDto
                {
                    Id = f.Id,
                    CodigoGeneracion = f.CodigoGeneracion,
                    NumeroControl = f.NumeroControl,
                    FechaEmision = f.FechaEmision,
                    HoraEmision = f.HoraEmision,
                    ReceptorNombre = f.Receptor != null ? f.Receptor.NombreRazonSocial : "Consumidor Final",
                    ReceptorNumeroDocumento = f.Receptor != null ? (f.Receptor.NumeroDocumento ?? string.Empty) : string.Empty,
                    TotalPagar = f.TotalPagar,
                    EstadoHacienda = f.EstadoHacienda,
                    FechaCreacion = f.FechaCreacion,
                    Activo = f.Activo,
                    SelloRecepcion = f.SelloRecibido,
                    FechaTransmision = f.FechaTransmision,
                    HoraTransmision = f.HoraTransmision,
                    TipoDte = f.TipoDocumento.Codigo,
                    VendedorId = f.VendedorId,
                    VendedorNombre = f.Vendedor != null ? f.Vendedor.Nombre : null,
                    VendedorCodigo = f.Vendedor != null ? f.Vendedor.Codigo : null,
                    CajaCodigo = f.Caja != null ? f.Caja.Codigo : null,
                    UsuarioId = f.UsuarioId,
                    CatTipoTransmisionId = f.CatTipoTransmisionId,
                    LoteId = f.LoteId,
                    SucursalId = f.SucursalId,
                    EventoContingenciaId = f.EventoContingenciaId,
                    Ambiente = f.Ambiente
                })
                .ToPaginatedListAsync(request.PageNumber, request.PageSize);

            return new PaginatedResponse<FacturaListDto>
            {
                Items = pagedData,
                TotalCount = pagedData.TotalCount,
                CurrentPage = pagedData.CurrentPage,
                PageSize = pagedData.PageSize,
                TotalPages = pagedData.TotalPages
            };
        }

        public async Task<List<FacturaListDto>> SearchAsync(string searchTerm, int emisorId, string? ambiente = null)
        {
            var searchLower = searchTerm.ToLower();
            var query = _context.Facturas
                .AsNoTracking()
                .Where(f => f.EmisorId == emisorId &&
                           (f.NumeroControl.ToLower().Contains(searchLower) ||
                             f.CodigoGeneracion.ToLower().Contains(searchLower) ||
                            (f.Receptor != null &&
                             (f.Receptor.NombreRazonSocial.ToLower().Contains(searchLower) ||
                              (f.Receptor.NumeroDocumento != null && f.Receptor.NumeroDocumento.ToLower().Contains(searchLower))))));

            if (!string.IsNullOrEmpty(ambiente))
                query = query.Where(f => f.Ambiente == ambiente);

            query = query
                .OrderByDescending(f => f.FechaCreacion)
                .Take(50);

            return await query
                .Select(f => new FacturaListDto
                {
                    Id = f.Id,
                    CodigoGeneracion = f.CodigoGeneracion,
                    NumeroControl = f.NumeroControl,
                    FechaEmision = f.FechaEmision,
                    HoraEmision = f.HoraEmision,
                    ReceptorNombre = f.Receptor != null ? f.Receptor.NombreRazonSocial : "Consumidor Final",
                    ReceptorNumeroDocumento = f.Receptor != null ? (f.Receptor.NumeroDocumento ?? string.Empty) : string.Empty,
                    TotalPagar = f.TotalPagar,
                    EstadoHacienda = f.EstadoHacienda,
                    FechaCreacion = f.FechaCreacion,
                    Activo = f.Activo,
                    SelloRecepcion = f.SelloRecibido,
                    FechaTransmision = f.FechaTransmision,
                    HoraTransmision = f.HoraTransmision,
                    TipoDte = f.TipoDocumento.Codigo,
                    VendedorId = f.VendedorId,
                    VendedorNombre = f.Vendedor != null ? f.Vendedor.Nombre : null,
                    VendedorCodigo = f.Vendedor != null ? f.Vendedor.Codigo : null,
                    CajaCodigo = f.Caja != null ? f.Caja.Codigo : null,
                    UsuarioId = f.UsuarioId,
                    CatTipoTransmisionId = f.CatTipoTransmisionId,
                    LoteId = f.LoteId,
                    SucursalId = f.SucursalId,
                    EventoContingenciaId = f.EventoContingenciaId,
                    Ambiente = f.Ambiente
                })
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene facturas con EstadoHacienda = 'PENDIENTE_ENVIO'
        /// </summary>
        public async Task<PaginatedResponse<FacturaListDto>> ObtenerFacturasPendientesAsync(
            int emisorId,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null,
            int pageNumber = 1,
            int pageSize = 10,
            int? sucursalId = null,
            string? search = null,
            string? ambiente = null)
        {
            var query = _context.Facturas
                .Where(f => f.EmisorId == emisorId && f.EstadoHacienda == "PENDIENTE_ENVIO");

            if (!string.IsNullOrEmpty(ambiente))
                query = query.Where(f => f.Ambiente == ambiente);

            if (sucursalId.HasValue)
                query = query.Where(f => f.SucursalId == sucursalId.Value);

            // Filtros de fecha
            if (fechaDesde.HasValue)
            {
                var fechaDesdeUtc = DateTime.SpecifyKind(fechaDesde.Value.Date, DateTimeKind.Utc);
                query = query.Where(f => f.FechaEmision >= fechaDesdeUtc);
            }

            if (fechaHasta.HasValue)
            {
                var fechaHastaUtc = DateTime.SpecifyKind(fechaHasta.Value.Date.AddDays(1), DateTimeKind.Utc);
                query = query.Where(f => f.FechaEmision < fechaHastaUtc);
            }

            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(f =>
                    (f.NumeroControl != null && f.NumeroControl.ToLower().Contains(searchLower)) ||
                    (f.CodigoGeneracion != null && f.CodigoGeneracion.ToLower().Contains(searchLower)) ||
                    (f.Receptor != null && f.Receptor.NombreRazonSocial.ToLower().Contains(searchLower)));
            }

            query = query.OrderByDescending(f => f.FechaEmision).AsQueryable();

            var pagedData = await query
                .Select(f => new FacturaListDto
                {
                    Id = f.Id,
                    CodigoGeneracion = f.CodigoGeneracion,
                    NumeroControl = f.NumeroControl,
                    FechaEmision = f.FechaEmision,
                    HoraEmision = f.HoraEmision,
                    ReceptorNombre = f.Receptor != null ? f.Receptor.NombreRazonSocial : "Consumidor Final",
                    ReceptorNumeroDocumento = f.Receptor != null ? (f.Receptor.NumeroDocumento ?? string.Empty) : string.Empty,
                    TotalPagar = f.TotalPagar,
                    EstadoHacienda = f.EstadoHacienda,
                    FechaCreacion = f.FechaCreacion,
                    Activo = f.Activo,
                    SelloRecepcion = f.SelloRecibido,
                    FechaTransmision = f.FechaTransmision,
                    HoraTransmision = f.HoraTransmision,
                    TipoDte = f.TipoDocumento != null ? f.TipoDocumento.Codigo : string.Empty,
                    VendedorId = f.VendedorId,
                    VendedorNombre = f.Vendedor != null ? f.Vendedor.Nombre : null,
                    VendedorCodigo = f.Vendedor != null ? f.Vendedor.Codigo : null,
                    CajaCodigo = f.Caja != null ? f.Caja.Codigo : null,
                    UsuarioId = f.UsuarioId,
                    CatTipoTransmisionId = f.CatTipoTransmisionId,
                    LoteId = f.LoteId,
                    SucursalId = f.SucursalId,
                    EventoContingenciaId = f.EventoContingenciaId,
                    Ambiente = f.Ambiente
                })
                .ToPaginatedListAsync(pageNumber, pageSize);

            return new PaginatedResponse<FacturaListDto>
            {
                Items = pagedData,
                TotalCount = pagedData.TotalCount,
                CurrentPage = pagedData.CurrentPage,
                PageSize = pagedData.PageSize,
                TotalPages = pagedData.TotalPages
            };
        }

        public async Task<PaginatedResponse<FacturaListDto>> ObtenerFacturasDiferidasAsync(
            PaginatedRequest request,
            int emisorId,
            List<int>? sucursalIds = null,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null,
            int? usuarioId = null,
            string? ambiente = null)
        {
            var query = _context.Facturas
                .Where(f => f.EmisorId == emisorId && (
                    f.EstadoHacienda == "PENDIENTE_LOTE"
                    || f.EventoContingenciaId != null
                    || f.CatTipoTransmisionId == 2
                    || f.LoteId != null
                ));

            if (!string.IsNullOrEmpty(ambiente))
                query = query.Where(f => f.Ambiente == ambiente);

            if (sucursalIds != null && sucursalIds.Count > 0)
            {
                query = query.Where(f => f.SucursalId.HasValue && sucursalIds.Contains(f.SucursalId.Value));
            }

            if (usuarioId.HasValue)
            {
                query = query.Where(f => f.UsuarioId == usuarioId.Value);
            }

            if (fechaDesde.HasValue)
            {
                var fechaDesdeUtc = DateTime.SpecifyKind(fechaDesde.Value.Date, DateTimeKind.Utc);
                query = query.Where(f => f.FechaEmision >= fechaDesdeUtc);
            }

            if (fechaHasta.HasValue)
            {
                var fechaHastaUtc = DateTime.SpecifyKind(fechaHasta.Value.Date.AddDays(1), DateTimeKind.Utc);
                query = query.Where(f => f.FechaEmision < fechaHastaUtc);
            }

            query = query.OrderByDescending(f => f.FechaEmision).AsQueryable();

            var pagedData = await query
                .Select(f => new FacturaListDto
                {
                    Id = f.Id,
                    CodigoGeneracion = f.CodigoGeneracion,
                    NumeroControl = f.NumeroControl,
                    FechaEmision = f.FechaEmision,
                    HoraEmision = f.HoraEmision,
                    ReceptorNombre = f.Receptor != null ? f.Receptor.NombreRazonSocial : "Consumidor Final",
                    ReceptorNumeroDocumento = f.Receptor != null ? (f.Receptor.NumeroDocumento ?? string.Empty) : string.Empty,
                    TotalPagar = f.TotalPagar,
                    EstadoHacienda = f.EstadoHacienda,
                    FechaCreacion = f.FechaCreacion,
                    Activo = f.Activo,
                    SelloRecepcion = f.SelloRecibido,
                    FechaTransmision = f.FechaTransmision,
                    HoraTransmision = f.HoraTransmision,
                    TipoDte = f.TipoDocumento != null ? f.TipoDocumento.Codigo : string.Empty,
                    VendedorId = f.VendedorId,
                    VendedorNombre = f.Vendedor != null ? f.Vendedor.Nombre : null,
                    VendedorCodigo = f.Vendedor != null ? f.Vendedor.Codigo : null,
                    CajaCodigo = f.Caja != null ? f.Caja.Codigo : null,
                    UsuarioId = f.UsuarioId,
                    CatTipoTransmisionId = f.CatTipoTransmisionId,
                    LoteId = f.LoteId,
                    SucursalId = f.SucursalId,
                    EventoContingenciaId = f.EventoContingenciaId,
                    Ambiente = f.Ambiente
                })
                .ToPaginatedListAsync(request.PageNumber, request.PageSize);

            return new PaginatedResponse<FacturaListDto>
            {
                Items = pagedData,
                TotalCount = pagedData.TotalCount,
                CurrentPage = pagedData.CurrentPage,
                PageSize = pagedData.PageSize,
                TotalPages = pagedData.TotalPages
            };
        }

        /// <summary>
        /// Calcula el tiempo restante antes de que venza el período de envío
        /// </summary>
        public async Task<TiempoRestanteDto> ObtenerTiempoRestanteAsync(int facturaId)
        {
            var factura = await _context.Facturas
                .FirstOrDefaultAsync(f => f.Id == facturaId);

            if (factura == null)
                throw new InvalidOperationException("Factura no encontrada");

            var fechaEmisionConHora = factura.FechaEmision.Date.Add(factura.HoraEmision);
            var ahora = DateTime.UtcNow;

            // Determinación de plazos corregida:
            // Group 2 (3 Meses): 01, 11, 14
            // Group 1 (1 Día): Resto (03, 05, etc)

            var tipoDocumento = await _context.CatTiposDocumento.FindAsync(factura.CatTipoDocumentoId);
            string codigoTipo = tipoDocumento?.Codigo ?? "01";
            var tiposTresMeses = new[] { "01", "11", "14" };

            DateTime fechaLimite;
            // string mensajePlazo; // unused

            if (tiposTresMeses.Contains(codigoTipo))
            {
                // 3 meses
                fechaLimite = fechaEmisionConHora.AddMonths(3);
                // mensajePlazo = "3 meses";
            }
            else
            {
                // 1 día (hasta 23:59:59 del día siguiente)
                fechaLimite = fechaEmisionConHora.Date.AddDays(1).Add(new TimeSpan(23, 59, 59));
                // mensajePlazo = "1 día";
            }

            var tiempoRestante = fechaLimite - ahora;
            var segundosRestantes = (long)Math.Max(0, tiempoRestante.TotalSeconds);

            string mensaje;
            if (segundosRestantes == 0)
            {
                mensaje = "El plazo de transmisión ha expirado";
            }
            else if (tiempoRestante.TotalHours > 24)
            {
                mensaje = $"Quedan {(int)tiempoRestante.TotalDays} días para transmitir";
            }
            else
            {
                mensaje = $"Quedan {(int)tiempoRestante.TotalHours}h {tiempoRestante.Minutes}m para transmitir";
            }

            return new TiempoRestanteDto
            {
                FacturaId = factura.Id,
                NumeroControl = factura.NumeroControl,
                FechaEmision = factura.FechaEmision,
                FechaLimiteEnvio = fechaLimite,
                SegundosRestantes = segundosRestantes,
                EsUltimoDiaMes = false, // Deprecado con nueva logica
                Mensaje = mensaje,
                EstaPorVencer = tiempoRestante.TotalHours < 24 // Alerta si queda menos de un día
            };
        }

        public async Task<List<BuscarParaNcResultDto>> BuscarParaNotaCreditoAsync(string searchTerm, int emisorId)
        {
            var tiposPermitidos = new[] { "03", "07" };
            var search = RemoverAcentos(searchTerm.Trim().ToLower());

            // Primero filtrar por emisor, estado y tipo (en BD)
            var facturasCandidatas = await _context.Facturas
                .AsNoTracking()
                .Include(f => f.TipoDocumento)
                .Include(f => f.Receptor)
                .Where(f => f.EmisorId == emisorId
                    && f.EstadoHacienda == "PROCESADO"
                    && f.TipoDocumento != null
                    && tiposPermitidos.Contains(f.TipoDocumento.Codigo))
                .OrderByDescending(f => f.FechaEmision)
                .Take(200) // Limitar candidatos para filtro en memoria
                .ToListAsync();

            // Filtrar en memoria con búsqueda sin acentos
            var facturas = facturasCandidatas.Where(f =>
                f.CodigoGeneracion.ToLower().Contains(search)
                || f.NumeroControl.ToLower().Contains(search)
                || (f.Receptor != null && RemoverAcentos(f.Receptor.NombreRazonSocial.ToLower()).Contains(search))
                || (f.Receptor != null && f.Receptor.NumeroDocumento != null && f.Receptor.NumeroDocumento.ToLower().Contains(search)))
                .Take(20)
                .ToList();

            var result = new List<BuscarParaNcResultDto>();
            foreach (var f in facturas)
            {
                var saldo = await _saldoDteService.ObtenerSaldoPorCodigoGeneracion(f.CodigoGeneracion, emisorId);

                result.Add(new BuscarParaNcResultDto
                {
                    Id = f.Id,
                    NumeroControl = f.NumeroControl,
                    CodigoGeneracion = f.CodigoGeneracion,
                    TipoDte = f.TipoDocumento?.Codigo ?? "",
                    TipoDocumentoNombre = f.TipoDocumento?.Valor ?? "",
                    FechaEmision = f.FechaEmision,
                    ReceptorNombre = f.Receptor?.NombreRazonSocial ?? "Consumidor Final",
                    ReceptorNumDocumento = f.Receptor?.NumeroDocumento ?? "",
                    MontoTotalOperacion = f.MontoTotalOperacion,
                    TotalPagar = f.TotalPagar,
                    EstadoHacienda = f.EstadoHacienda ?? "",
                    SaldoDisponible = saldo?.SaldoDisponible ?? f.MontoTotalOperacion,
                    MontoAcreditado = saldo?.MontoAcreditado ?? 0,
                    NceCount = saldo?.NceCount ?? 0
                });
            }

            return result;
        }

        public async Task<DetalleParaNcDto?> ObtenerDetalleParaNotaCreditoAsync(int facturaId, int emisorId)
        {
            var factura = await _context.Facturas
                .AsNoTracking()
                .Include(f => f.TipoDocumento)
                .Include(f => f.Receptor)
                .Include(f => f.Detalles)
                .FirstOrDefaultAsync(f => f.Id == facturaId && f.EmisorId == emisorId);

            if (factura == null) return null;

            var tipoDte = factura.TipoDocumento?.Codigo;
            var tiposPermitidos = new[] { "03", "07" };
            if (tipoDte == null || !tiposPermitidos.Contains(tipoDte))
                return null;

            if (factura.EstadoHacienda != "PROCESADO")
                return null;

            var saldo = await _saldoDteService.ObtenerSaldoPorCodigoGeneracion(factura.CodigoGeneracion, emisorId);

            return new DetalleParaNcDto
            {
                Id = factura.Id,
                NumeroControl = factura.NumeroControl,
                CodigoGeneracion = factura.CodigoGeneracion,
                TipoDte = tipoDte,
                FechaEmision = factura.FechaEmision,
                CondicionOperacion = factura.CatCondicionOperacionId,
                ReceptorId = factura.ReceptorId,
                ReceptorNombre = factura.Receptor?.NombreRazonSocial ?? "Consumidor Final",
                ReceptorNumDocumento = factura.Receptor?.NumeroDocumento ?? "",
                ReceptorNit = factura.Receptor?.Nrc, // En SV, NIT del receptor se almacena en Nrc o NumeroDocumento
                ReceptorNrc = factura.Receptor?.Nrc,
                ReceptorCorreo = factura.Receptor?.CorreoElectronico,
                ReceptorTelefono = factura.Receptor?.Telefono,
                TotalGravada = factura.TotalGravado,
                TotalExenta = factura.TotalExento,
                TotalNoSuj = factura.TotalNoSujeto,
                SubTotal = factura.SubTotal,
                TotalIva = factura.TotalIva,
                MontoTotalOperacion = factura.MontoTotalOperacion,
                TotalPagar = factura.TotalPagar,
                SaldoDisponible = saldo?.SaldoDisponible ?? factura.MontoTotalOperacion,
                MontoAcreditado = saldo?.MontoAcreditado ?? 0,
                NceCount = saldo?.NceCount ?? 0,
                Items = factura.Detalles?.Select(d => new DetalleItemParaNcDto
                {
                    NumItem = d.NumeroItem,
                    TipoItem = d.CatTipoItemId,
                    Codigo = d.CodigoProducto,
                    Descripcion = d.Descripcion,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    VentaGravada = d.VentaGravada,
                    VentaExenta = d.VentaExenta,
                    VentaNoSuj = d.VentaNoSujeta,
                    MontoDescuento = d.MontoDescuento,
                    IvaItem = d.IvaItem,
                    UnidadMedida = d.CatUnidadMedidaId,
                    NumeroDocumentoRelacionado = d.NumeroDocumentoRelacionado
                }).OrderBy(d => d.NumItem).ToList() ?? new()
            };
        }

        public async Task<List<BuscarParaNcResultDto>> BuscarParaNotaDebitoAsync(string searchTerm, int emisorId)
        {
            var tiposPermitidos = new[] { "03", "07" };
            var search = RemoverAcentos(searchTerm.Trim().ToLower());

            var facturasCandidatas = await _context.Facturas
                .AsNoTracking()
                .Include(f => f.TipoDocumento)
                .Include(f => f.Receptor)
                .Where(f => f.EmisorId == emisorId
                    && f.EstadoHacienda == "PROCESADO"
                    && f.TipoDocumento != null
                    && tiposPermitidos.Contains(f.TipoDocumento.Codigo))
                .OrderByDescending(f => f.FechaEmision)
                .Take(200)
                .ToListAsync();

            var facturas = facturasCandidatas.Where(f =>
                f.CodigoGeneracion.ToLower().Contains(search)
                || f.NumeroControl.ToLower().Contains(search)
                || (f.Receptor != null && RemoverAcentos(f.Receptor.NombreRazonSocial.ToLower()).Contains(search))
                || (f.Receptor != null && f.Receptor.NumeroDocumento != null && f.Receptor.NumeroDocumento.ToLower().Contains(search)))
                .Take(20)
                .ToList();

            return facturas.Select(f => new BuscarParaNcResultDto
            {
                Id = f.Id,
                NumeroControl = f.NumeroControl,
                CodigoGeneracion = f.CodigoGeneracion,
                TipoDte = f.TipoDocumento?.Codigo ?? "",
                TipoDocumentoNombre = f.TipoDocumento?.Valor ?? "",
                FechaEmision = f.FechaEmision,
                ReceptorNombre = f.Receptor?.NombreRazonSocial ?? "Consumidor Final",
                ReceptorNumDocumento = f.Receptor?.NumeroDocumento ?? "",
                MontoTotalOperacion = f.MontoTotalOperacion,
                TotalPagar = f.TotalPagar,
                EstadoHacienda = f.EstadoHacienda ?? "",
                SaldoDisponible = 0, // ND no tiene límite de monto — no aplica saldo
                MontoAcreditado = 0,
                NceCount = 0
            }).ToList();
        }

        public async Task<DetalleParaNcDto?> ObtenerDetalleParaNotaDebitoAsync(int facturaId, int emisorId)
        {
            var factura = await _context.Facturas
                .AsNoTracking()
                .Include(f => f.TipoDocumento)
                .Include(f => f.Receptor)
                .Include(f => f.Detalles)
                .FirstOrDefaultAsync(f => f.Id == facturaId && f.EmisorId == emisorId);

            if (factura == null) return null;

            var tipoDte = factura.TipoDocumento?.Codigo;
            var tiposPermitidos = new[] { "03", "07" };
            if (tipoDte == null || !tiposPermitidos.Contains(tipoDte))
                return null;

            if (factura.EstadoHacienda != "PROCESADO")
                return null;

            return new DetalleParaNcDto
            {
                Id = factura.Id,
                NumeroControl = factura.NumeroControl,
                CodigoGeneracion = factura.CodigoGeneracion,
                TipoDte = tipoDte,
                FechaEmision = factura.FechaEmision,
                CondicionOperacion = factura.CatCondicionOperacionId,
                ReceptorId = factura.ReceptorId,
                ReceptorNombre = factura.Receptor?.NombreRazonSocial ?? "Consumidor Final",
                ReceptorNumDocumento = factura.Receptor?.NumeroDocumento ?? "",
                ReceptorNit = factura.Receptor?.Nrc,
                ReceptorNrc = factura.Receptor?.Nrc,
                ReceptorCorreo = factura.Receptor?.CorreoElectronico,
                ReceptorTelefono = factura.Receptor?.Telefono,
                TotalGravada = factura.TotalGravado,
                TotalExenta = factura.TotalExento,
                TotalNoSuj = factura.TotalNoSujeto,
                SubTotal = factura.SubTotal,
                TotalIva = factura.TotalIva,
                MontoTotalOperacion = factura.MontoTotalOperacion,
                TotalPagar = factura.TotalPagar,
                SaldoDisponible = 0, // ND no tiene límite de monto
                MontoAcreditado = 0,
                NceCount = 0,
                Items = factura.Detalles?.Select(d => new DetalleItemParaNcDto
                {
                    NumItem = d.NumeroItem,
                    TipoItem = d.CatTipoItemId,
                    Codigo = d.CodigoProducto,
                    Descripcion = d.Descripcion,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    VentaGravada = d.VentaGravada,
                    VentaExenta = d.VentaExenta,
                    VentaNoSuj = d.VentaNoSujeta,
                    MontoDescuento = d.MontoDescuento,
                    IvaItem = d.IvaItem,
                    UnidadMedida = d.CatUnidadMedidaId,
                    NumeroDocumentoRelacionado = d.NumeroDocumentoRelacionado
                }).OrderBy(d => d.NumItem).ToList() ?? new()
            };
        }

        /// <summary>
        /// Remueve acentos y diacríticos de un string para búsqueda flexible
        /// </summary>
        private static string RemoverAcentos(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return texto;
            var normalized = texto.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var c in normalized)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }
    }
}
