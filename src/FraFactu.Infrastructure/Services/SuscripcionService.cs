using AutoMapper;
using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Suscripciones;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FraFactu.Infrastructure.Services
{
    public class SuscripcionService : ISuscripcionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<SuscripcionService> _logger;
        private readonly IEmailService _emailService;

        public SuscripcionService(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<SuscripcionService> logger,
            IEmailService emailService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _emailService = emailService;
        }

        // ==========================================
        // CRUD SUSCRIPCIONES (SuperAdmin)
        // ==========================================

        public async Task<PaginatedResponse<SuscripcionResponseDto>> GetAllAsync(PaginatedRequest request)
        {
            var query = _context.Suscripciones
                .Include(s => s.Emisor)
                .AsQueryable();

            // Búsqueda
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var search = request.SearchTerm.ToLower();
                query = query.Where(s =>
                    s.Emisor.NombreRazonSocial.ToLower().Contains(search) ||
                    s.NombrePlan.ToLower().Contains(search) ||
                    s.Emisor.Nit.ToLower().Contains(search));
            }

            // Ordenamiento
            query = request.OrderBy?.ToLower() switch
            {
                "emisor" => request.OrderDirection == "desc"
                    ? query.OrderByDescending(s => s.Emisor.NombreRazonSocial)
                    : query.OrderBy(s => s.Emisor.NombreRazonSocial),
                "vencimiento" => request.OrderDirection == "desc"
                    ? query.OrderByDescending(s => s.FechaVencimiento)
                    : query.OrderBy(s => s.FechaVencimiento),
                "precio" => request.OrderDirection == "desc"
                    ? query.OrderByDescending(s => s.Precio)
                    : query.OrderBy(s => s.Precio),
                _ => query.OrderByDescending(s => s.FechaCreacion)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var dtos = items.Select(s =>
            {
                var dto = _mapper.Map<SuscripcionResponseDto>(s);
                CalcularEstado(dto);
                return dto;
            }).ToList();

            return new PaginatedResponse<SuscripcionResponseDto>
            {
                Items = dtos,
                CurrentPage = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };
        }

        public async Task<SuscripcionResponseDto> GetByIdAsync(int id)
        {
            var suscripcion = await _context.Suscripciones
                .Include(s => s.Emisor)
                .FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new InvalidOperationException($"Suscripción con Id {id} no encontrada");

            var dto = _mapper.Map<SuscripcionResponseDto>(suscripcion);
            CalcularEstado(dto);
            return dto;
        }

        public async Task<SuscripcionResponseDto> CreateAsync(CreateSuscripcionDto dto)
        {
            // Validar que el emisor existe
            var emisor = await _context.Emisores.FindAsync(dto.EmisorId)
                ?? throw new InvalidOperationException($"Emisor con Id {dto.EmisorId} no encontrado");

            // Validar que el emisor no tenga ya una suscripción activa
            var existente = await _context.Suscripciones
                .AnyAsync(s => s.EmisorId == dto.EmisorId && s.Activo);

            if (existente)
                throw new InvalidOperationException($"El emisor '{emisor.NombreRazonSocial}' ya tiene una suscripción activa");

            var suscripcion = _mapper.Map<Suscripcion>(dto);
            // Asegurar que las fechas sean UTC para PostgreSQL
            suscripcion.FechaInicio = DateTime.SpecifyKind(suscripcion.FechaInicio, DateTimeKind.Utc);
            suscripcion.FechaVencimiento = DateTime.SpecifyKind(suscripcion.FechaVencimiento, DateTimeKind.Utc);

            // Calcular fecha de próximo cobro (siempre día 1 del mes siguiente)
            var fechaInicio = suscripcion.FechaInicio;
            var primerDiaMesSiguiente = new DateTime(fechaInicio.Year, fechaInicio.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
            suscripcion.FechaProximoCobro = primerDiaMesSiguiente;

            // Prorrateo: si no empieza el día 1, cobrar proporcional al primer mes
            if (fechaInicio.Day > 1)
            {
                var diasEnMes = DateTime.DaysInMonth(fechaInicio.Year, fechaInicio.Month);
                var diasRestantes = diasEnMes - fechaInicio.Day + 1; // incluye el día de inicio
                suscripcion.PrecioProrrateado = Math.Round(suscripcion.Precio * diasRestantes / diasEnMes, 2);
            }

            suscripcion.DiasGracia = 3; // Default 3 días de gracia

            _context.Suscripciones.Add(suscripcion);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[SUSCRIPCION] Suscripción creada para emisor {EmisorId} - Plan: {Plan}, Próximo cobro: {ProximoCobro}, Prorrateo: {Prorrateo}",
                dto.EmisorId, dto.NombrePlan, suscripcion.FechaProximoCobro, suscripcion.PrecioProrrateado);

            return await GetByIdAsync(suscripcion.Id);
        }

        public async Task<SuscripcionResponseDto> UpdateAsync(int id, UpdateSuscripcionDto dto)
        {
            var suscripcion = await _context.Suscripciones
                .Include(s => s.Emisor)
                .FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new InvalidOperationException($"Suscripción con Id {id} no encontrada");

            // Validar que no se active si ya existe otra suscripción activa para el mismo emisor
            if (dto.Activo && !suscripcion.Activo)
            {
                var otraActiva = await _context.Suscripciones
                    .AnyAsync(s => s.EmisorId == suscripcion.EmisorId && s.Activo && s.Id != id);

                if (otraActiva)
                    throw new InvalidOperationException($"El emisor '{suscripcion.Emisor.NombreRazonSocial}' ya tiene otra suscripción activa");
            }

            _mapper.Map(dto, suscripcion);
            // Asegurar que las fechas sean UTC para PostgreSQL
            suscripcion.FechaInicio = DateTime.SpecifyKind(suscripcion.FechaInicio, DateTimeKind.Utc);
            suscripcion.FechaVencimiento = DateTime.SpecifyKind(suscripcion.FechaVencimiento, DateTimeKind.Utc);
            suscripcion.FechaProximoCobro = DateTime.SpecifyKind(suscripcion.FechaProximoCobro, DateTimeKind.Utc);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[SUSCRIPCION] Suscripción {Id} actualizada", id);

            return await GetByIdAsync(id);
        }

        public async Task DeleteAsync(int id)
        {
            var suscripcion = await _context.Suscripciones.FindAsync(id)
                ?? throw new InvalidOperationException($"Suscripción con Id {id} no encontrada");

            suscripcion.Activo = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("[SUSCRIPCION] Suscripción {Id} desactivada", id);
        }

        // ==========================================
        // MI SUSCRIPCIÓN (EmisorAdmin)
        // ==========================================

        public async Task<MiSuscripcionResponseDto> GetMiSuscripcionAsync(int emisorId)
        {
            var suscripcion = await _context.Suscripciones
                .FirstOrDefaultAsync(s => s.EmisorId == emisorId && s.Activo);

            if (suscripcion == null)
            {
                return new MiSuscripcionResponseDto
                {
                    TieneSuscripcion = false,
                    Estado = "SinSuscripcion",
                    MostrarBanner = false
                };
            }

            var hoy = DateTime.UtcNow.Date;
            var fechaCobro = suscripcion.FechaProximoCobro.Date;
            var fechaLimite = fechaCobro.AddDays(suscripcion.DiasGracia); // Cobro + 3 días de gracia
            var diasParaCobro = (fechaCobro - hoy).Days;

            var response = new MiSuscripcionResponseDto
            {
                TieneSuscripcion = true,
                NombrePlan = suscripcion.NombrePlan,
                Precio = suscripcion.Precio,
                FechaVencimiento = suscripcion.FechaVencimiento,
                FechaProximoCobro = suscripcion.FechaProximoCobro,
                DiasParaCobro = diasParaCobro,
                DiasGracia = suscripcion.DiasGracia
            };

            if (suscripcion.PagoAlDia)
            {
                // Ya pagó este período
                response.Estado = "Activa";
                response.MostrarBanner = false;
            }
            else if (hoy > fechaLimite)
            {
                // Pasó la fecha de cobro + días de gracia → Vencida
                response.Estado = "Vencida";
                response.MostrarBanner = true;
                response.MensajeBanner = $"Tu suscripción ha vencido. La fecha límite de pago fue el {fechaLimite:dd/MM/yyyy}. Contacta a soporte para renovar.";
                response.ColorBanner = "error";
            }
            else if (hoy >= fechaCobro)
            {
                // Pasó la fecha de cobro pero dentro del plazo de gracia
                var diasRestantesGracia = (fechaLimite - hoy).Days;
                response.Estado = "EnGracia";
                response.MostrarBanner = true;
                response.MensajeBanner = $"Tu pago está pendiente. Tienes {diasRestantesGracia} día(s) para realizar el pago antes del {fechaLimite:dd/MM/yyyy}.";
                response.ColorBanner = "warning";
            }
            else if (diasParaCobro <= 7)
            {
                response.Estado = "PorVencer";
                response.MostrarBanner = true;
                response.MensajeBanner = $"Tu próximo cobro es el {fechaCobro:dd/MM/yyyy}. Realiza tu pago para continuar sin interrupciones.";
                response.ColorBanner = "warning";
            }
            else
            {
                response.Estado = "Activa";
                response.MostrarBanner = false;
            }

            return response;
        }

        // ==========================================
        // CONFIGURACIÓN PROVEEDOR (SuperAdmin)
        // ==========================================

        public async Task<ConfiguracionProveedorDto> GetConfiguracionProveedorAsync()
        {
            var config = await _context.ConfiguracionProveedor.FirstOrDefaultAsync();

            if (config == null)
            {
                // Crear registro por defecto
                config = new ConfiguracionProveedor
                {
                    Nombre = "JD SmartCode",
                    Email = "",
                    Telefono = "",
                };
                _context.ConfiguracionProveedor.Add(config);
                await _context.SaveChangesAsync();
            }

            return _mapper.Map<ConfiguracionProveedorDto>(config);
        }

        public async Task<ConfiguracionProveedorDto> UpdateConfiguracionProveedorAsync(ConfiguracionProveedorDto dto)
        {
            var config = await _context.ConfiguracionProveedor.FirstOrDefaultAsync();

            if (config == null)
            {
                config = _mapper.Map<ConfiguracionProveedor>(dto);
                _context.ConfiguracionProveedor.Add(config);
            }
            else
            {
                _mapper.Map(dto, config);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("[SUSCRIPCION] Configuración de proveedor actualizada");

            return _mapper.Map<ConfiguracionProveedorDto>(config);
        }

        // ==========================================
        // VISTA PREVIA PDF (SuperAdmin)
        // ==========================================

        public async Task<byte[]> GenerarPdfPreviewAsync(int suscripcionId)
        {
            var suscripcion = await _context.Suscripciones
                .Include(s => s.Emisor)
                .FirstOrDefaultAsync(s => s.Id == suscripcionId)
                ?? throw new InvalidOperationException($"Suscripción con Id {suscripcionId} no encontrada");

            var config = await _context.ConfiguracionProveedor.FirstOrDefaultAsync();

            // Crear factura temporal para la preview (no se guarda en BD)
            var anio = DateTime.UtcNow.Year;
            var ultimaFactura = await _context.FacturasSuscripcion
                .Where(f => f.NumeroFactura.StartsWith($"INV-{anio}-"))
                .OrderByDescending(f => f.NumeroFactura)
                .FirstOrDefaultAsync();

            int secuencial = 1;
            if (ultimaFactura != null)
            {
                var partes = ultimaFactura.NumeroFactura.Split('-');
                if (partes.Length == 3 && int.TryParse(partes[2], out var ultimo))
                    secuencial = ultimo + 1;
            }

            var hoy = DateTime.UtcNow;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var finMes = inicioMes.AddMonths(1).AddDays(-1);

            var facturaPreview = new FacturaSuscripcion
            {
                NumeroFactura = $"INV-{anio}-{secuencial:D4}",
                SuscripcionId = suscripcion.Id,
                FechaEmision = hoy,
                PeriodoServicioInicio = inicioMes,
                PeriodoServicioFin = finMes,
                Monto = suscripcion.Precio
            };

            return GenerarPdfFacturaSuscripcion(suscripcion, facturaPreview, config);
        }

        public async Task<byte[]> GenerarPdfMiSuscripcionAsync(int emisorId)
        {
            var suscripcion = await _context.Suscripciones
                .Include(s => s.Emisor)
                .FirstOrDefaultAsync(s => s.EmisorId == emisorId && s.Activo)
                ?? throw new InvalidOperationException("No tienes una suscripción activa");

            var config = await _context.ConfiguracionProveedor.FirstOrDefaultAsync();

            var hoy = DateTime.UtcNow;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var finMes = inicioMes.AddMonths(1).AddDays(-1);

            // Buscar si ya existe una factura generada para este mes
            var facturaExistente = await _context.FacturasSuscripcion
                .Where(f => f.SuscripcionId == suscripcion.Id
                    && f.PeriodoServicioInicio.Month == hoy.Month
                    && f.PeriodoServicioInicio.Year == hoy.Year)
                .OrderByDescending(f => f.FechaEmision)
                .FirstOrDefaultAsync();

            // Si no existe, crear una temporal (sin guardar) para generar el PDF
            var factura = facturaExistente ?? new FacturaSuscripcion
            {
                NumeroFactura = $"INV-{hoy.Year}-PREVIEW",
                SuscripcionId = suscripcion.Id,
                FechaEmision = hoy,
                PeriodoServicioInicio = inicioMes,
                PeriodoServicioFin = finMes,
                Monto = suscripcion.Precio
            };

            return GenerarPdfFacturaSuscripcion(suscripcion, factura, config);
        }

        // ==========================================
        // ENVÍO MANUAL DE RECORDATORIO (SuperAdmin)
        // ==========================================

        public async Task EnviarRecordatorioManualAsync(int suscripcionId)
        {
            var suscripcion = await _context.Suscripciones
                .Include(s => s.Emisor)
                .FirstOrDefaultAsync(s => s.Id == suscripcionId)
                ?? throw new InvalidOperationException($"Suscripción con Id {suscripcionId} no encontrada");

            await GenerarYEnviarFacturaSuscripcion(suscripcion);

            _logger.LogInformation("[SUSCRIPCION] Recordatorio manual enviado para suscripción {Id} - Emisor: {Emisor}",
                suscripcionId, suscripcion.Emisor.NombreRazonSocial);
        }

        // ==========================================
        // MARCAR COMO PAGADO (SuperAdmin)
        // ==========================================

        public async Task<SuscripcionResponseDto> MarcarComoPagadoAsync(int suscripcionId)
        {
            var suscripcion = await _context.Suscripciones
                .Include(s => s.Emisor)
                .FirstOrDefaultAsync(s => s.Id == suscripcionId)
                ?? throw new InvalidOperationException($"Suscripción con Id {suscripcionId} no encontrada");

            if (!suscripcion.Activo)
                throw new InvalidOperationException("No se puede marcar como pagada una suscripción inactiva");

            suscripcion.PagoAlDia = true;
            suscripcion.FechaUltimoPago = DateTime.UtcNow;

            // Avanzar FechaProximoCobro al día 1 del mes siguiente si ya pasó
            var hoy = DateTime.UtcNow.Date;
            if (hoy >= suscripcion.FechaProximoCobro.Date)
            {
                suscripcion.FechaProximoCobro = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("[SUSCRIPCION] Pago registrado para suscripción {Id} - Emisor: {Emisor}. Próximo cobro: {ProximoCobro}",
                suscripcionId, suscripcion.Emisor.NombreRazonSocial, suscripcion.FechaProximoCobro);

            return await GetByIdAsync(suscripcionId);
        }

        // ==========================================
        // REACTIVAR SUSCRIPCIÓN (SuperAdmin)
        // ==========================================

        public async Task<SuscripcionResponseDto> ReactivarAsync(int suscripcionId)
        {
            var suscripcion = await _context.Suscripciones
                .Include(s => s.Emisor)
                .FirstOrDefaultAsync(s => s.Id == suscripcionId)
                ?? throw new InvalidOperationException($"Suscripción con Id {suscripcionId} no encontrada");

            if (suscripcion.Activo)
                throw new InvalidOperationException("La suscripción ya está activa");

            // Verificar que el emisor no tenga otra suscripción activa
            var otraActiva = await _context.Suscripciones
                .AnyAsync(s => s.EmisorId == suscripcion.EmisorId && s.Activo && s.Id != suscripcionId);

            if (otraActiva)
                throw new InvalidOperationException($"El emisor '{suscripcion.Emisor.NombreRazonSocial}' ya tiene otra suscripción activa");

            // Reactivar y recalcular fechas
            suscripcion.Activo = true;
            suscripcion.PagoAlDia = false;

            var hoy = DateTime.UtcNow.Date;
            suscripcion.FechaProximoCobro = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
            suscripcion.FechaVencimiento = suscripcion.FechaProximoCobro.AddMonths(12); // Renovar por 12 meses

            await _context.SaveChangesAsync();

            _logger.LogInformation("[SUSCRIPCION] Suscripción {Id} reactivada para emisor {Emisor}. Próximo cobro: {ProximoCobro}",
                suscripcionId, suscripcion.Emisor.NombreRazonSocial, suscripcion.FechaProximoCobro);

            return await GetByIdAsync(suscripcionId);
        }

        // ==========================================
        // HISTORIAL DE FACTURAS
        // ==========================================

        public async Task<List<FacturaSuscripcionResponseDto>> GetHistorialFacturasAsync(int suscripcionId)
        {
            var facturas = await _context.FacturasSuscripcion
                .Include(f => f.Suscripcion)
                    .ThenInclude(s => s.Emisor)
                .Where(f => f.SuscripcionId == suscripcionId)
                .OrderByDescending(f => f.FechaEmision)
                .ToListAsync();

            return _mapper.Map<List<FacturaSuscripcionResponseDto>>(facturas);
        }

        // ==========================================
        // MÉTODOS PARA BACKGROUND JOB
        // ==========================================

        public async Task ProcesarRecordatoriosMensualesAsync()
        {
            _logger.LogInformation("[SUSCRIPCION-JOB] Procesando cobros mensuales (día 1 del mes)");

            var hoy = DateTime.UtcNow.Date;

            // Buscar suscripciones cuyo FechaProximoCobro es hoy (día 1)
            var suscripcionesACobrar = await _context.Suscripciones
                .Include(s => s.Emisor)
                .Where(s => s.Activo && s.FechaProximoCobro.Date == hoy)
                .ToListAsync();

            foreach (var suscripcion in suscripcionesACobrar)
            {
                try
                {
                    await GenerarYEnviarFacturaSuscripcion(suscripcion);

                    // Avanzar FechaProximoCobro al día 1 del mes siguiente y resetear pago
                    suscripcion.FechaProximoCobro = suscripcion.FechaProximoCobro.AddMonths(1);
                    suscripcion.PagoAlDia = false;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("[SUSCRIPCION-JOB] Cobro mensual enviado a {Emisor}. Próximo cobro: {ProximoCobro}",
                        suscripcion.Emisor.NombreRazonSocial, suscripcion.FechaProximoCobro);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[SUSCRIPCION-JOB] Error enviando cobro mensual a emisor {EmisorId}",
                        suscripcion.EmisorId);
                }
            }
        }

        public async Task ProcesarRecordatoriosProximidadAsync()
        {
            var hoy = DateTime.UtcNow.Date;

            // 1. Recordatorio 7 días antes del cobro
            _logger.LogInformation("[SUSCRIPCION-JOB] Verificando recordatorios de proximidad (7 días antes del cobro)");
            var fechaObjetivo = hoy.AddDays(7);

            var suscripcionesPorCobrar = await _context.Suscripciones
                .Include(s => s.Emisor)
                .Where(s => s.Activo && s.FechaProximoCobro.Date == fechaObjetivo)
                .ToListAsync();

            foreach (var suscripcion in suscripcionesPorCobrar)
            {
                try
                {
                    await GenerarYEnviarFacturaSuscripcion(suscripcion, esProximidad: true);
                    _logger.LogInformation("[SUSCRIPCION-JOB] Recordatorio de proximidad enviado a {Emisor}",
                        suscripcion.Emisor.NombreRazonSocial);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[SUSCRIPCION-JOB] Error enviando recordatorio de proximidad a emisor {EmisorId}",
                        suscripcion.EmisorId);
                }
            }

            // 2. Correo de vencimiento: cobro + días de gracia ya pasaron
            _logger.LogInformation("[SUSCRIPCION-JOB] Verificando suscripciones vencidas (cobro + gracia)");

            var suscripcionesVencidas = await _context.Suscripciones
                .Include(s => s.Emisor)
                .Where(s => s.Activo)
                .ToListAsync();

            foreach (var suscripcion in suscripcionesVencidas)
            {
                var fechaLimite = suscripcion.FechaProximoCobro.Date.AddDays(suscripcion.DiasGracia);
                // Enviar correo exactamente el día que vence la gracia
                if (hoy == fechaLimite)
                {
                    try
                    {
                        await GenerarYEnviarFacturaSuscripcion(suscripcion, esVencimiento: true);
                        _logger.LogInformation("[SUSCRIPCION-JOB] Correo de vencimiento enviado a {Emisor}",
                            suscripcion.Emisor.NombreRazonSocial);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[SUSCRIPCION-JOB] Error enviando correo de vencimiento a emisor {EmisorId}",
                            suscripcion.EmisorId);
                    }
                }
            }
        }

        // ==========================================
        // MÉTODOS PRIVADOS
        // ==========================================

        private void CalcularEstado(SuscripcionResponseDto dto)
        {
            var hoy = DateTime.UtcNow.Date;
            var fechaCobro = dto.FechaProximoCobro.Date;
            var fechaLimite = fechaCobro.AddDays(dto.DiasGracia);
            var diasParaCobro = (fechaCobro - hoy).Days;
            dto.DiasRestantes = diasParaCobro;

            if (!dto.Activo)
                dto.Estado = "Inactiva";
            else if (dto.PagoAlDia)
                dto.Estado = "Activa"; // Pagó, está al día
            else if (hoy > fechaLimite)
                dto.Estado = "Vencida";
            else if (hoy >= fechaCobro)
                dto.Estado = "En gracia";
            else if (diasParaCobro <= 7)
                dto.Estado = "Por vencer";
            else
                dto.Estado = "Activa";
        }

        private async Task GenerarYEnviarFacturaSuscripcion(Suscripcion suscripcion, bool esProximidad = false, bool esVencimiento = false)
        {
            var config = await _context.ConfiguracionProveedor.FirstOrDefaultAsync();

            // Generar número de factura secuencial
            var anio = DateTime.UtcNow.Year;
            var ultimaFactura = await _context.FacturasSuscripcion
                .Where(f => f.NumeroFactura.StartsWith($"INV-{anio}-"))
                .OrderByDescending(f => f.NumeroFactura)
                .FirstOrDefaultAsync();

            int secuencial = 1;
            if (ultimaFactura != null)
            {
                var partes = ultimaFactura.NumeroFactura.Split('-');
                if (partes.Length == 3 && int.TryParse(partes[2], out var ultimo))
                    secuencial = ultimo + 1;
            }

            var numeroFactura = $"INV-{anio}-{secuencial:D4}";

            var hoy = DateTime.UtcNow;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var finMes = inicioMes.AddMonths(1).AddDays(-1);

            // Crear registro de factura
            var facturaSuscripcion = new FacturaSuscripcion
            {
                NumeroFactura = numeroFactura,
                SuscripcionId = suscripcion.Id,
                FechaEmision = hoy,
                PeriodoServicioInicio = inicioMes,
                PeriodoServicioFin = finMes,
                Monto = suscripcion.Precio
            };

            _context.FacturasSuscripcion.Add(facturaSuscripcion);

            // Generar PDF
            var pdfBytes = GenerarPdfFacturaSuscripcion(suscripcion, facturaSuscripcion, config);

            // Preparar email
            var emailDestinatario = suscripcion.Emisor.CorreoElectronico;
            if (string.IsNullOrWhiteSpace(emailDestinatario))
            {
                _logger.LogWarning("[SUSCRIPCION] Emisor {EmisorId} no tiene correo electrónico configurado",
                    suscripcion.EmisorId);
                await _context.SaveChangesAsync();
                return;
            }

            string asunto;
            string cuerpoHtml;

            if (esVencimiento)
            {
                asunto = $"Tu suscripción Smartix ha vencido - Acción requerida";
                cuerpoHtml = GenerarPlantillaEmailVencimiento(suscripcion, config);
            }
            else if (esProximidad)
            {
                asunto = $"Tu próximo cobro Smartix es el {suscripcion.FechaProximoCobro:dd/MM/yyyy} - Recordatorio";
                cuerpoHtml = GenerarPlantillaEmailProximidad(suscripcion, facturaSuscripcion, config);
            }
            else
            {
                var mesAnio = hoy.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-ES"));
                asunto = $"Recordatorio de pago - Suscripción Smartix - {mesAnio}";
                cuerpoHtml = GenerarPlantillaEmailMensual(suscripcion, facturaSuscripcion, config);
            }

            // Enviar email con PDF adjunto usando el SMTP del emisor
            var adjuntos = new List<(byte[] contenido, string nombre, string mimeType)>
            {
                (pdfBytes, $"Factura-{numeroFactura}.pdf", "application/pdf")
            };

            try
            {
                await EnviarEmailSuscripcionAsync(emailDestinatario, asunto, cuerpoHtml, adjuntos);
                facturaSuscripcion.EnviadoPorEmail = true;
                facturaSuscripcion.FechaEnvio = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SUSCRIPCION] Error al enviar email de suscripción a {Email}", emailDestinatario);
            }

            await _context.SaveChangesAsync();
        }

        private async Task EnviarEmailSuscripcionAsync(
            string destinatario,
            string asunto,
            string cuerpoHtml,
            List<(byte[] contenido, string nombre, string mimeType)> adjuntos)
        {
            // Usar SMTP por defecto del sistema (no del emisor, ya que es una factura del proveedor)
            using var client = new System.Net.Mail.SmtpClient();
            // Se delega al IEmailService existente o se usa configuración por defecto
            // Por ahora, usamos System.Net.Mail directamente con la configuración por defecto

            // Nota: Se recomienda inyectar la configuración de email por defecto
            // Para mantener consistencia, delegamos al método existente del EmailService
            _logger.LogInformation("[SUSCRIPCION] Enviando email de suscripción a {Email} - Asunto: {Asunto}",
                destinatario, asunto);

            // Buscar cualquier emisor para reutilizar el método de envío del EmailService
            var primerEmisor = await _context.Emisores.FirstOrDefaultAsync(e => e.Activo);
            if (primerEmisor != null)
            {
                await _emailService.EnviarEmailGenericoAsync(destinatario, asunto, cuerpoHtml, adjuntos);
            }
        }

        // ==========================================
        // GENERACIÓN PDF (QuestPDF)
        // ==========================================

        private byte[] GenerarPdfFacturaSuscripcion(
            Suscripcion suscripcion,
            FacturaSuscripcion factura,
            ConfiguracionProveedor? config)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var cultura = new System.Globalization.CultureInfo("es-ES");
            var nombreProveedor = config?.Nombre ?? "JD SmartCode";
            var subtitulo = "Sistema de Facturación Electrónica";

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.MarginHorizontal(1.8f, Unit.Centimetre);
                    page.MarginVertical(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor("#333333"));

                    // ============================
                    // HEADER: Nombre del sistema + subtítulo
                    // ============================
                    page.Header().Column(header =>
                    {
                        header.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(text =>
                                {
                                    text.Span("Smartix").FontSize(18).Bold().FontColor("#2C5F8A");
                                    text.Span($"  |  {subtitulo}").FontSize(10).FontColor("#666666");
                                });
                            });
                        });

                        // Línea separadora azul oscuro debajo del header
                        header.Item().PaddingTop(6).LineHorizontal(2).LineColor("#2C5F8A");

                        // Contacto del proveedor (debajo de la línea)
                        header.Item().PaddingTop(4).AlignLeft().Text(text =>
                        {
                            text.Span($"{nombreProveedor}").FontSize(7.5f).FontColor("#888888");
                            if (!string.IsNullOrWhiteSpace(config?.Website))
                                text.Span($" • {config.Website}").FontSize(7.5f).FontColor("#888888");
                            if (!string.IsNullOrWhiteSpace(config?.Email))
                                text.Span($" • {config.Email}").FontSize(7.5f).FontColor("#888888");
                            if (!string.IsNullOrWhiteSpace(config?.Telefono))
                                text.Span($" • {config.Telefono}").FontSize(7.5f).FontColor("#888888");
                        });
                    });

                    // ============================
                    // CONTENIDO PRINCIPAL
                    // ============================
                    page.Content().PaddingTop(15).Column(col =>
                    {
                        // INVOICE Nº - Alineado a la derecha
                        col.Item().AlignRight().Text(text =>
                        {
                            text.Span("INVOICE  ").FontSize(22).Bold().FontColor("#333333");
                            text.Span($"Nº {factura.NumeroFactura}").FontSize(13).Bold().FontColor("#333333");
                        });

                        col.Item().PaddingVertical(15);

                        // ============================
                        // PROVEEDOR / CLIENTE - Dos columnas con fondo gris claro
                        // ============================
                        col.Item().Background("#F5F7FA").Padding(12).Row(row =>
                        {
                            // PROVEEDOR
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("PROVEEDOR").FontSize(8).Bold().FontColor("#2C5F8A");
                                c.Item().PaddingTop(4).Text(nombreProveedor).FontSize(11).Bold();
                                if (!string.IsNullOrWhiteSpace(config?.NombreContacto))
                                    c.Item().Text(config.NombreContacto).FontSize(9).FontColor("#555555");
                                if (!string.IsNullOrWhiteSpace(config?.Email))
                                    c.Item().Text(config.Email).FontSize(9).FontColor("#555555");
                                if (!string.IsNullOrWhiteSpace(config?.Telefono))
                                    c.Item().Text(config.Telefono).FontSize(9).FontColor("#555555");
                            });

                            // Separador vertical
                            row.ConstantItem(1).Background("#D0D5DD");

                            // CLIENTE
                            row.RelativeItem().PaddingLeft(12).Column(c =>
                            {
                                c.Item().Text("CLIENTE").FontSize(8).Bold().FontColor("#2C5F8A");
                                c.Item().PaddingTop(4).Text(suscripcion.Emisor.NombreRazonSocial).FontSize(11).Bold();
                                if (!string.IsNullOrWhiteSpace(suscripcion.Emisor.NombreComercial))
                                    c.Item().Text(suscripcion.Emisor.NombreComercial).FontSize(9).FontColor("#555555");
                                c.Item().Text(suscripcion.Emisor.CorreoElectronico).FontSize(9).FontColor("#555555");
                                c.Item().Text(suscripcion.Emisor.Telefono).FontSize(9).FontColor("#555555");
                            });
                        });

                        col.Item().PaddingVertical(12);

                        // ============================
                        // FECHAS - Tres columnas con borde
                        // ============================
                        col.Item().Border(1).BorderColor("#D0D5DD").Row(row =>
                        {
                            // Fecha de Emisión
                            row.RelativeItem().BorderRight(1).BorderColor("#D0D5DD").Padding(10).Column(c =>
                            {
                                c.Item().Text("Fecha de Emisión").FontSize(8).FontColor("#2C5F8A");
                                c.Item().PaddingTop(3).Text(
                                    factura.FechaEmision.ToString("dd 'de' MMMM 'de' yyyy", cultura))
                                    .FontSize(10).Bold();
                            });

                            // Período de Servicio
                            row.RelativeItem().BorderRight(1).BorderColor("#D0D5DD").Padding(10).Column(c =>
                            {
                                c.Item().Text("Período de Servicio").FontSize(8).FontColor("#2C5F8A");
                                c.Item().PaddingTop(3).Text(
                                    $"{factura.PeriodoServicioInicio:dd/MM/yyyy} — {factura.PeriodoServicioFin:dd/MM/yyyy}")
                                    .FontSize(10).Bold();
                            });

                            // Fecha de Vencimiento
                            row.RelativeItem().Padding(10).Column(c =>
                            {
                                c.Item().Text("Fecha de Vencimiento").FontSize(8).FontColor("#2C5F8A");
                                c.Item().PaddingTop(3).Text(
                                    suscripcion.FechaVencimiento.ToString("dd 'de' MMMM 'de' yyyy", cultura))
                                    .FontSize(10).Bold();
                            });
                        });

                        col.Item().PaddingVertical(15);

                        // ============================
                        // TABLA DE DETALLE
                        // ============================
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(5);   // Descripción
                                columns.ConstantColumn(65);  // Cantidad
                                columns.ConstantColumn(90);  // Precio Unit.
                                columns.ConstantColumn(80);  // Total
                            });

                            // Header de la tabla - fondo azul oscuro
                            table.Header(header =>
                            {
                                header.Cell().Background("#2C5F8A").Padding(8)
                                    .Text("Descripción").FontColor("#FFFFFF").Bold().FontSize(9);
                                header.Cell().Background("#2C5F8A").Padding(8).AlignCenter()
                                    .Text("Cantidad").FontColor("#FFFFFF").Bold().FontSize(9);
                                header.Cell().Background("#2C5F8A").Padding(8).AlignRight()
                                    .Text("Precio Unit.").FontColor("#FFFFFF").Bold().FontSize(9);
                                header.Cell().Background("#2C5F8A").Padding(8).AlignRight()
                                    .Text("Total").FontColor("#FFFFFF").Bold().FontSize(9);
                            });

                            // Fila de detalle
                            table.Cell().BorderBottom(1).BorderColor("#E0E0E0").Padding(8).Column(c =>
                            {
                                c.Item().Text($"Suscripción Smartix — {suscripcion.NombrePlan}").FontSize(9.5f);
                                c.Item().Text($"Período: {factura.PeriodoServicioInicio:d/MM/yyyy} al {factura.PeriodoServicioFin:d/MM/yyyy}")
                                    .FontSize(8).Italic().FontColor("#777777");
                            });
                            table.Cell().BorderBottom(1).BorderColor("#E0E0E0").Padding(8).AlignCenter()
                                .Text("1").FontSize(9.5f);
                            table.Cell().BorderBottom(1).BorderColor("#E0E0E0").Padding(8).AlignRight()
                                .Text($"${suscripcion.Precio:N2}").FontSize(9.5f);
                            table.Cell().BorderBottom(1).BorderColor("#E0E0E0").Padding(8).AlignRight()
                                .Text($"${suscripcion.Precio:N2}").FontSize(9.5f);
                        });

                        col.Item().PaddingVertical(5);

                        // ============================
                        // TOTAL A PAGAR - Fila con fondo azul
                        // ============================
                        col.Item().Row(row =>
                        {
                            row.RelativeItem(); // Espacio vacío a la izquierda
                            row.ConstantItem(250).Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn();
                                    c.ConstantColumn(90);
                                });

                                t.Cell().Background("#2C5F8A").Padding(10).AlignRight()
                                    .Text("TOTAL A PAGAR:").FontColor("#FFFFFF").Bold().FontSize(10);
                                t.Cell().Background("#2C5F8A").Padding(10).AlignRight()
                                    .Text($"${suscripcion.Precio:N2}").FontColor("#FFFFFF").Bold().FontSize(12);
                            });
                        });

                        col.Item().PaddingVertical(15);

                        // ============================
                        // DATOS BANCARIOS - Tabla con labels
                        // ============================
                        if (config != null && !string.IsNullOrWhiteSpace(config.Banco))
                        {
                            col.Item().Column(c =>
                            {
                                c.Item().Text("Datos para Transferencia Bancaria:").Bold().FontSize(10).Underline();
                                c.Item().PaddingTop(6).Table(bankTable =>
                                {
                                    bankTable.ColumnsDefinition(cols =>
                                    {
                                        cols.ConstantColumn(120); // Label
                                        cols.RelativeColumn();    // Valor
                                    });

                                    if (!string.IsNullOrWhiteSpace(config.TitularCuenta))
                                    {
                                        bankTable.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(5)
                                            .Text("Titular:").Bold().FontSize(9);
                                        bankTable.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(5)
                                            .Text(config.TitularCuenta).FontSize(9);
                                    }

                                    bankTable.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(5)
                                        .Text("Banco:").Bold().FontSize(9);
                                    bankTable.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(5)
                                        .Text(config.Banco).FontSize(9);

                                    if (!string.IsNullOrWhiteSpace(config.TipoCuenta))
                                    {
                                        bankTable.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(5)
                                            .Text("Tipo de Cuenta:").Bold().FontSize(9);
                                        bankTable.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(5)
                                            .Text(config.TipoCuenta).FontSize(9);
                                    }

                                    if (!string.IsNullOrWhiteSpace(config.NumeroCuenta))
                                    {
                                        bankTable.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(5)
                                            .Text("Número de Cuenta:").Bold().FontSize(9);
                                        bankTable.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(5)
                                            .Text(config.NumeroCuenta).FontSize(9);
                                    }
                                });
                            });
                        }

                        col.Item().PaddingVertical(10);

                        // ============================
                        // NOTAS
                        // ============================
                        col.Item().Column(c =>
                        {
                            c.Item().Text("Notas:").Bold().FontSize(10);
                            c.Item().PaddingTop(4).Text(text =>
                            {
                                text.Span("El pago debe realizarse a más tardar el ").FontSize(9);
                                text.Span(suscripcion.FechaVencimiento.ToString("d 'de' MMMM 'de' yyyy", cultura))
                                    .FontSize(9).Bold();
                                text.Span(". Favor enviar comprobante de transferencia a ").FontSize(9);
                                text.Span(config?.Email ?? "soporte").FontSize(9).FontColor("#2C5F8A");
                                if (!string.IsNullOrWhiteSpace(config?.Telefono))
                                {
                                    text.Span($" o al WhatsApp {config.Telefono}").FontSize(9);
                                }
                                text.Span(".").FontSize(9);
                            });
                            if (!string.IsNullOrWhiteSpace(suscripcion.Notas))
                                c.Item().PaddingTop(4).Text(suscripcion.Notas).FontSize(9).FontColor("#555555");
                        });

                        col.Item().PaddingVertical(15);

                        // ============================
                        // MENSAJE DE AGRADECIMIENTO
                        // ============================
                        col.Item().AlignCenter().Text($"¡Gracias por confiar en Smartix!")
                            .FontSize(12).Bold().FontColor("#2C5F8A");
                    });

                    // ============================
                    // FOOTER
                    // ============================
                    page.Footer().Column(footer =>
                    {
                        footer.Item().LineHorizontal(0.5f).LineColor("#CCCCCC");
                        footer.Item().PaddingTop(6).AlignCenter().Text(text =>
                        {
                            text.Span($"{nombreProveedor}").FontSize(7.5f).FontColor("#999999");
                            if (!string.IsNullOrWhiteSpace(config?.Website))
                                text.Span($" • {config.Website}").FontSize(7.5f).FontColor("#999999");
                            if (!string.IsNullOrWhiteSpace(config?.Email))
                                text.Span($" • {config.Email}").FontSize(7.5f).FontColor("#999999");
                            if (!string.IsNullOrWhiteSpace(config?.Telefono))
                                text.Span($" • {config.Telefono}").FontSize(7.5f).FontColor("#999999");
                        });
                    });
                });
            }).GeneratePdf();

            return pdfBytes;
        }

        // ==========================================
        // PLANTILLAS EMAIL HTML
        // ==========================================

        private string GenerarPlantillaEmailMensual(
            Suscripcion suscripcion,
            FacturaSuscripcion factura,
            ConfiguracionProveedor? config)
        {
            var mesAnio = DateTime.UtcNow.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-ES"));

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f5f5f5; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #1976D2, #1565C0); padding: 30px; text-align: center; color: white; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .header p {{ margin: 5px 0 0; opacity: 0.9; }}
        .content {{ padding: 30px; }}
        .info-box {{ background: #E3F2FD; border-left: 4px solid #1976D2; padding: 15px; margin: 20px 0; border-radius: 0 4px 4px 0; }}
        .amount {{ font-size: 28px; font-weight: bold; color: #1976D2; text-align: center; margin: 20px 0; }}
        .details-table {{ width: 100%; border-collapse: collapse; margin: 15px 0; }}
        .details-table td {{ padding: 8px 12px; border-bottom: 1px solid #eee; }}
        .details-table td:first-child {{ color: #666; font-weight: 500; }}
        .bank-box {{ background: #F5F5F5; padding: 15px; border-radius: 6px; margin: 15px 0; }}
        .footer {{ background: #FAFAFA; padding: 20px; text-align: center; font-size: 12px; color: #999; }}
        .btn {{ display: inline-block; background: #1976D2; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; margin: 10px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Recordatorio de Pago</h1>
            <p>Suscripción Smartix - {mesAnio}</p>
        </div>
        <div class='content'>
            <p>Estimado/a <strong>{suscripcion.Emisor.NombreRazonSocial}</strong>,</p>
            <p>Le enviamos el recordatorio de pago correspondiente a su suscripción del servicio de Facturación Electrónica Smartix.</p>

            <div class='info-box'>
                <table class='details-table'>
                    <tr><td>Plan:</td><td><strong>{suscripcion.NombrePlan}</strong></td></tr>
                    <tr><td>Período:</td><td>{factura.PeriodoServicioInicio:dd/MM/yyyy} - {factura.PeriodoServicioFin:dd/MM/yyyy}</td></tr>
                    <tr><td>Factura N°:</td><td>{factura.NumeroFactura}</td></tr>
                    <tr><td>Fecha de vencimiento:</td><td><strong style='color: #E53935;'>{suscripcion.FechaVencimiento:dd/MM/yyyy}</strong></td></tr>
                </table>
            </div>

            <div class='amount'>
                Total a pagar: ${suscripcion.Precio:N2}
            </div>

            {(config != null && !string.IsNullOrWhiteSpace(config.Banco) ? $@"
            <div class='bank-box'>
                <p style='margin: 0 0 10px; font-weight: bold;'>Datos para Transferencia Bancaria:</p>
                <table class='details-table'>
                    <tr><td>Banco:</td><td>{config.Banco}</td></tr>
                    {(!string.IsNullOrWhiteSpace(config.TipoCuenta) ? $"<tr><td>Tipo cuenta:</td><td>{config.TipoCuenta}</td></tr>" : "")}
                    {(!string.IsNullOrWhiteSpace(config.NumeroCuenta) ? $"<tr><td>N° cuenta:</td><td>{config.NumeroCuenta}</td></tr>" : "")}
                    {(!string.IsNullOrWhiteSpace(config.TitularCuenta) ? $"<tr><td>Titular:</td><td>{config.TitularCuenta}</td></tr>" : "")}
                </table>
            </div>" : "")}

            <p>Adjunto encontrará la factura en formato PDF para su referencia.</p>
            <p>Si ya realizó el pago, por favor ignore este mensaje.</p>
        </div>
        <div class='footer'>
            <p>{config?.Nombre ?? "JD SmartCode"} | Smartix - Sistema de Facturación Electrónica</p>
            {(!string.IsNullOrWhiteSpace(config?.Email) ? $"<p>{config.Email}</p>" : "")}
        </div>
    </div>
</body>
</html>";
        }

        private string GenerarPlantillaEmailProximidad(
            Suscripcion suscripcion,
            FacturaSuscripcion factura,
            ConfiguracionProveedor? config)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f5f5f5; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #FF9800, #F57C00); padding: 30px; text-align: center; color: white; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .header p {{ margin: 5px 0 0; opacity: 0.9; }}
        .content {{ padding: 30px; }}
        .warning-box {{ background: #FFF3E0; border-left: 4px solid #FF9800; padding: 15px; margin: 20px 0; border-radius: 0 4px 4px 0; }}
        .amount {{ font-size: 28px; font-weight: bold; color: #FF9800; text-align: center; margin: 20px 0; }}
        .details-table {{ width: 100%; border-collapse: collapse; margin: 15px 0; }}
        .details-table td {{ padding: 8px 12px; border-bottom: 1px solid #eee; }}
        .details-table td:first-child {{ color: #666; font-weight: 500; }}
        .bank-box {{ background: #F5F5F5; padding: 15px; border-radius: 6px; margin: 15px 0; }}
        .footer {{ background: #FAFAFA; padding: 20px; text-align: center; font-size: 12px; color: #999; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Tu suscripción está por vencer</h1>
            <p>Quedan 7 días - Vence el {suscripcion.FechaVencimiento:dd/MM/yyyy}</p>
        </div>
        <div class='content'>
            <p>Estimado/a <strong>{suscripcion.Emisor.NombreRazonSocial}</strong>,</p>

            <div class='warning-box'>
                <p style='margin: 0;'>Su suscripción al servicio de Facturación Electrónica Smartix <strong>vence el {suscripcion.FechaVencimiento:dd/MM/yyyy}</strong>. Le recomendamos realizar el pago lo antes posible para continuar disfrutando del servicio sin interrupciones.</p>
            </div>

            <table class='details-table'>
                <tr><td>Plan:</td><td><strong>{suscripcion.NombrePlan}</strong></td></tr>
                <tr><td>Fecha de vencimiento:</td><td><strong style='color: #E53935;'>{suscripcion.FechaVencimiento:dd/MM/yyyy}</strong></td></tr>
                <tr><td>Factura N°:</td><td>{factura.NumeroFactura}</td></tr>
            </table>

            <div class='amount'>
                Total a pagar: ${suscripcion.Precio:N2}
            </div>

            {(config != null && !string.IsNullOrWhiteSpace(config.Banco) ? $@"
            <div class='bank-box'>
                <p style='margin: 0 0 10px; font-weight: bold;'>Datos para Transferencia Bancaria:</p>
                <table class='details-table'>
                    <tr><td>Banco:</td><td>{config.Banco}</td></tr>
                    {(!string.IsNullOrWhiteSpace(config.TipoCuenta) ? $"<tr><td>Tipo cuenta:</td><td>{config.TipoCuenta}</td></tr>" : "")}
                    {(!string.IsNullOrWhiteSpace(config.NumeroCuenta) ? $"<tr><td>N° cuenta:</td><td>{config.NumeroCuenta}</td></tr>" : "")}
                    {(!string.IsNullOrWhiteSpace(config.TitularCuenta) ? $"<tr><td>Titular:</td><td>{config.TitularCuenta}</td></tr>" : "")}
                </table>
            </div>" : "")}

            <p>Adjunto encontrará la factura en formato PDF.</p>
            <p>Si ya realizó el pago, por favor ignore este mensaje.</p>
        </div>
        <div class='footer'>
            <p>{config?.Nombre ?? "JD SmartCode"} | Smartix - Sistema de Facturación Electrónica</p>
            {(!string.IsNullOrWhiteSpace(config?.Email) ? $"<p>{config.Email}</p>" : "")}
        </div>
    </div>
</body>
</html>";
        }

        private string GenerarPlantillaEmailVencimiento(
            Suscripcion suscripcion,
            ConfiguracionProveedor? config)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f5f5f5; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #D32F2F, #B71C1C); padding: 30px; text-align: center; color: white; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .header p {{ margin: 5px 0 0; opacity: 0.9; }}
        .content {{ padding: 30px; }}
        .error-box {{ background: #FFEBEE; border-left: 4px solid #D32F2F; padding: 15px; margin: 20px 0; border-radius: 0 4px 4px 0; }}
        .details-table {{ width: 100%; border-collapse: collapse; margin: 15px 0; }}
        .details-table td {{ padding: 8px 12px; border-bottom: 1px solid #eee; }}
        .details-table td:first-child {{ color: #666; font-weight: 500; }}
        .footer {{ background: #FAFAFA; padding: 20px; text-align: center; font-size: 12px; color: #999; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Tu suscripcion ha vencido</h1>
            <p>Se ha superado el plazo de gracia de {suscripcion.DiasGracia} dias</p>
        </div>
        <div class='content'>
            <p>Estimado/a <strong>{suscripcion.Emisor.NombreRazonSocial}</strong>,</p>

            <div class='error-box'>
                <p style='margin: 0;'>Tu suscripcion al servicio de Facturacion Electronica Smartix <strong>ha vencido</strong>. El plazo de gracia de {suscripcion.DiasGracia} dias posterior a la fecha de cobro ha finalizado.</p>
            </div>

            <table class='details-table'>
                <tr><td>Plan:</td><td><strong>{suscripcion.NombrePlan}</strong></td></tr>
                <tr><td>Fecha de cobro:</td><td>{suscripcion.FechaProximoCobro:dd/MM/yyyy}</td></tr>
                <tr><td>Monto pendiente:</td><td><strong style='color: #D32F2F;'>${suscripcion.Precio:N2}</strong></td></tr>
            </table>

            <p>Para reactivar tu servicio, por favor realiza el pago lo antes posible y contacta a nuestro equipo de soporte.</p>

            {(config != null && !string.IsNullOrWhiteSpace(config.Email) ? $"<p>Contacto: <a href='mailto:{config.Email}'>{config.Email}</a></p>" : "")}
            {(config != null && !string.IsNullOrWhiteSpace(config.Telefono) ? $"<p>WhatsApp: {config.Telefono}</p>" : "")}
        </div>
        <div class='footer'>
            <p>{config?.Nombre ?? "JD SmartCode"} | Smartix - Sistema de Facturacion Electronica</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
