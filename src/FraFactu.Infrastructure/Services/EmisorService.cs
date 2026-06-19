using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Emisores;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;

namespace FraFactu.Infrastructure.Services
{
    public class EmisorService : IEmisorService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<EmisorService> _logger;
        private readonly IProveedorSmtpResolver? _proveedorSmtpResolver;

        public EmisorService(
            ApplicationDbContext context,
            IMapper mapper,
            Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor,
            IEncryptionService encryptionService,
            ILogger<EmisorService> logger,
            IProveedorSmtpResolver? proveedorSmtpResolver = null)
        {
            _context = context;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _encryptionService = encryptionService;
            _logger = logger;
            _proveedorSmtpResolver = proveedorSmtpResolver;
        }

        public async Task<PaginatedResponse<EmisorDto>> GetAllAsync(PaginatedRequest request)
        {
            var query = _context.Emisores
                .Include(e => e.AmbienteDestino)
                .Where(e => e.Activo)
                .OrderByDescending(e => e.FechaCreacion)
                .AsQueryable();

            var totalCount = await query.CountAsync();

            var emisores = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var emisoresDto = _mapper.Map<List<EmisorDto>>(emisores);

            return new PaginatedResponse<EmisorDto>
            {
                Items = emisoresDto,
                CurrentPage = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };
        }

        public async Task<EmisorDto?> GetByIdAsync(int id)
        {
            var emisor = await _context.Emisores
                .Include(e => e.AmbienteDestino)
                .FirstOrDefaultAsync(e => e.Id == id && e.Activo);

            if (emisor == null)
                return null;

            return _mapper.Map<EmisorDto>(emisor);
        }

        public async Task<List<EmisorResumenDto>> GetResumenByIdsAsync(IEnumerable<int> ids)
        {
            var idsList = ids?.Distinct().ToList() ?? new List<int>();
            if (idsList.Count == 0) return new List<EmisorResumenDto>();

            return await _context.Emisores
                .Where(e => idsList.Contains(e.Id) && e.Activo)
                .Select(e => new EmisorResumenDto
                {
                    Id = e.Id,
                    NombreRazonSocial = e.NombreRazonSocial,
                    Nit = e.Nit
                })
                .ToListAsync();
        }

        public async Task<EmisorDto?> GetMiPerfilAsync(int emisorId)
        {
            var emisor = await _context.Emisores
                .Include(e => e.AmbienteDestino)
                .FirstOrDefaultAsync(e => e.Id == emisorId && e.Activo);

            if (emisor == null)
                return null;

            var emisorDto = _mapper.Map<EmisorDto>(emisor);

            // Agregar estadísticas
            emisorDto.TotalUsuarios = await _context.Usuarios
                .CountAsync(u => u.EmisorId == emisorId && u.Activo);

            emisorDto.TotalFacturas = await _context.Facturas
                .CountAsync(f => f.EmisorId == emisorId);

            return emisorDto;
        }

        public async Task<EmisorDto> CreateAsync(CreateEmisorDto dto)
        {
            // Validar NIT único
            var existeNit = await _context.Emisores.AnyAsync(e => e.Nit == dto.Nit);
            if (existeNit)
                throw new InvalidOperationException($"Ya existe un emisor con el NIT {dto.Nit}");

            var emisor = new Emisor
            {
                Nit = dto.Nit,
                Nrc = dto.Nrc,
                NombreRazonSocial = dto.NombreRazonSocial,
                NombreComercial = dto.NombreComercial,
                CodigoActividad = dto.CodigoActividad,
                DescripcionActividad = dto.DescripcionActividad,
                CorreoElectronico = dto.CorreoElectronico,
                Telefono = dto.Telefono,
                CatDepartamentoId = dto.CatDepartamentoId,
                CatMunicipioId = dto.CatMunicipioId,
                CatDistritoId = dto.CatDistritoId,
                Direccion = dto.Direccion,
                // Configuración MH
                CatAmbienteDestinoId = dto.CatAmbienteDestinoId,
                MhUsuario = dto.MhUsuario ?? string.Empty,
                MhClaveApi = !string.IsNullOrEmpty(dto.MhClaveApi) ? _encryptionService.Encrypt(dto.MhClaveApi) : string.Empty,
                MhLlavePrivada = !string.IsNullOrEmpty(dto.MhLlavePrivada) ? _encryptionService.Encrypt(dto.MhLlavePrivada) : string.Empty,
                MhLlavePublica = dto.MhLlavePublica ?? string.Empty,
                MhPassPrivada = !string.IsNullOrEmpty(dto.MhPassPrivada) ? _encryptionService.Encrypt(dto.MhPassPrivada) : string.Empty,
                // Credenciales Produccion MH
                MhUsuarioProd = dto.MhUsuarioProd,
                MhClaveApiProd = !string.IsNullOrEmpty(dto.MhClaveApiProd) ? _encryptionService.Encrypt(dto.MhClaveApiProd) : null,
                MhLlavePrivadaProd = !string.IsNullOrEmpty(dto.MhLlavePrivadaProd) ? _encryptionService.Encrypt(dto.MhLlavePrivadaProd) : null,
                MhLlavePublicaProd = dto.MhLlavePublicaProd,
                MhPassPrivadaProd = !string.IsNullOrEmpty(dto.MhPassPrivadaProd) ? _encryptionService.Encrypt(dto.MhPassPrivadaProd) : null,
                // Configuracion SMTP
                SmtpHost = dto.SmtpHost,
                SmtpPort = dto.SmtpPort,
                SmtpUser = dto.SmtpUser,
                SmtpPassword = !string.IsNullOrEmpty(dto.SmtpPassword) ? _encryptionService.Encrypt(dto.SmtpPassword) : null,
                EmailRemitente = dto.EmailRemitente,
                EmailHabilitado = dto.EmailHabilitado ?? false,
                FechaCreacion = DateTime.UtcNow,
                Activo = true
            };

            // Auto-detectar SMTP si no se proporcionó SmtpHost
            if (string.IsNullOrEmpty(emisor.SmtpHost) && !string.IsNullOrEmpty(emisor.SmtpUser))
                await ConfigurarSmtpAutomaticoAsync(emisor, emisor.SmtpUser);

            _context.Emisores.Add(emisor);
            await _context.SaveChangesAsync();

            return _mapper.Map<EmisorDto>(emisor);
        }

        public async Task<EmisorDto?> UpdateAsync(int id, UpdateEmisorDto dto)
        {
            var emisor = await _context.Emisores.FindAsync(id);
            if (emisor == null)
                return null;

            // Validar NIT único (excepto el actual)
            var existeNit = await _context.Emisores
                .AnyAsync(e => e.Nit == dto.Nit && e.Id != id);
            if (existeNit)
                throw new InvalidOperationException($"Ya existe otro emisor con el NIT {dto.Nit}");

            // Actualizar campos
            emisor.Nit = dto.Nit;
            emisor.Nrc = dto.Nrc;
            emisor.NombreRazonSocial = dto.NombreRazonSocial;
            emisor.NombreComercial = dto.NombreComercial;
            emisor.CodigoActividad = dto.CodigoActividad;
            emisor.DescripcionActividad = dto.DescripcionActividad;
            if (dto.CatTipoEstablecimientoId.HasValue)
                emisor.CatTipoEstablecimientoId = dto.CatTipoEstablecimientoId.Value;
            emisor.CorreoElectronico = dto.CorreoElectronico;
            emisor.Telefono = dto.Telefono;
            emisor.CatDepartamentoId = dto.CatDepartamentoId;
            emisor.CatMunicipioId = dto.CatMunicipioId;
            emisor.CatDistritoId = dto.CatDistritoId;
            emisor.Direccion = dto.Direccion;
            // Configuración MH
            if (dto.CatAmbienteDestinoId.HasValue)
                emisor.CatAmbienteDestinoId = dto.CatAmbienteDestinoId.Value;
            if (dto.MhUsuario != null) emisor.MhUsuario = dto.MhUsuario;
            if (dto.MhClaveApi != null) emisor.MhClaveApi = _encryptionService.Encrypt(dto.MhClaveApi);
            if (dto.MhLlavePrivada != null) emisor.MhLlavePrivada = _encryptionService.Encrypt(dto.MhLlavePrivada);
            if (dto.MhLlavePublica != null) emisor.MhLlavePublica = dto.MhLlavePublica;
            if (dto.MhPassPrivada != null) emisor.MhPassPrivada = _encryptionService.Encrypt(dto.MhPassPrivada);
            // Credenciales Produccion MH
            if (dto.MhUsuarioProd != null) emisor.MhUsuarioProd = dto.MhUsuarioProd;
            if (dto.MhClaveApiProd != null) emisor.MhClaveApiProd = _encryptionService.Encrypt(dto.MhClaveApiProd);
            if (dto.MhLlavePrivadaProd != null) emisor.MhLlavePrivadaProd = _encryptionService.Encrypt(dto.MhLlavePrivadaProd);
            if (dto.MhLlavePublicaProd != null) emisor.MhLlavePublicaProd = dto.MhLlavePublicaProd;
            if (dto.MhPassPrivadaProd != null) emisor.MhPassPrivadaProd = _encryptionService.Encrypt(dto.MhPassPrivadaProd);
            // Configuracion SMTP
            if (dto.SmtpHost != null) emisor.SmtpHost = dto.SmtpHost;
            if (dto.SmtpPort.HasValue) emisor.SmtpPort = dto.SmtpPort;
            if (dto.SmtpUser != null) emisor.SmtpUser = dto.SmtpUser;
            if (dto.SmtpPassword != null) emisor.SmtpPassword = _encryptionService.Encrypt(dto.SmtpPassword);
            if (dto.EmailRemitente != null) emisor.EmailRemitente = dto.EmailRemitente;
            if (dto.EmailHabilitado.HasValue) emisor.EmailHabilitado = dto.EmailHabilitado.Value;
            if (dto.LecturaCorreoHabilitada.HasValue) emisor.LecturaCorreoHabilitada = dto.LecturaCorreoHabilitada.Value;
            // Auto-detectar SMTP si SmtpUser cambió y no se proporcionó SmtpHost
            if (dto.SmtpUser != null && dto.SmtpHost == null)
                await ConfigurarSmtpAutomaticoAsync(emisor, dto.SmtpUser);
            emisor.Activo = dto.Activo;

            await _context.SaveChangesAsync();

            return _mapper.Map<EmisorDto>(emisor);
        }

        public async Task<EmisorDto?> UpdateMiPerfilAsync(int emisorId, UpdatePerfilEmisorDto dto)
        {
            var emisor = await _context.Emisores.FindAsync(emisorId);
            if (emisor == null)
                return null;

            // Plan B Hub-as-Emisor — Fase 3 Task 19.
            // Si el emisor está vinculado a un Hub, los campos fiscales identitarios
            // (Nrc, NombreComercial, actividad económica, ubicación, contacto, tipo
            // de establecimiento) son source-of-truth en SmartHub. Detectamos intentos
            // de edición desde Smartix y los descartamos con warning. El resto del
            // payload (Mh*, Smtp*, Gmail*, ambiente, logo) se aplica normalmente.
            if (emisor.HubId.HasValue)
            {
                LogIntentosDeEdicionFiscal(emisor, dto);
            }
            else
            {
                // Emisor legacy sin vincular a Hub: comportamiento original intacto.
                emisor.NombreComercial = dto.NombreComercial;
                emisor.CorreoElectronico = dto.CorreoElectronico;
                emisor.Telefono = dto.Telefono;
                emisor.Direccion = dto.Direccion;
                emisor.Nrc = dto.Nrc ?? string.Empty;
                emisor.CodigoActividad = dto.CodigoActividad ?? string.Empty;
                emisor.DescripcionActividad = dto.DescripcionActividad ?? string.Empty;

                if (dto.CatDepartamentoId.HasValue)
                    emisor.CatDepartamentoId = dto.CatDepartamentoId.Value;
                if (dto.CatMunicipioId.HasValue)
                    emisor.CatMunicipioId = dto.CatMunicipioId.Value;
                if (dto.CatDistritoId.HasValue)
                    emisor.CatDistritoId = dto.CatDistritoId;
                if (dto.CatTipoEstablecimientoId.HasValue)
                    emisor.CatTipoEstablecimientoId = dto.CatTipoEstablecimientoId.Value;
            }



            // CONFIGURACIÓN HACIENDA
            // Validar permiso de administrador (Admin Emisor)
            var userRole = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (!string.IsNullOrEmpty(userRole) && (userRole.Equals("EmisorAdmin", StringComparison.OrdinalIgnoreCase) || userRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase)))
            {
                if (dto.CatAmbienteDestinoId.HasValue)
                    emisor.CatAmbienteDestinoId = dto.CatAmbienteDestinoId.Value;

                if (dto.MhUsuario != null) emisor.MhUsuario = dto.MhUsuario;
                if (dto.MhClaveApi != null) emisor.MhClaveApi = _encryptionService.Encrypt(dto.MhClaveApi);
                if (dto.MhLlavePrivada != null) emisor.MhLlavePrivada = _encryptionService.Encrypt(dto.MhLlavePrivada);
                if (dto.MhLlavePublica != null) emisor.MhLlavePublica = dto.MhLlavePublica;
                if (dto.MhPassPrivada != null) emisor.MhPassPrivada = _encryptionService.Encrypt(dto.MhPassPrivada);

                // Credenciales Produccion MH
                if (dto.MhUsuarioProd != null) emisor.MhUsuarioProd = dto.MhUsuarioProd;
                if (dto.MhClaveApiProd != null) emisor.MhClaveApiProd = _encryptionService.Encrypt(dto.MhClaveApiProd);
                if (dto.MhLlavePrivadaProd != null) emisor.MhLlavePrivadaProd = _encryptionService.Encrypt(dto.MhLlavePrivadaProd);
                if (dto.MhLlavePublicaProd != null) emisor.MhLlavePublicaProd = dto.MhLlavePublicaProd;
                if (dto.MhPassPrivadaProd != null) emisor.MhPassPrivadaProd = _encryptionService.Encrypt(dto.MhPassPrivadaProd);

                // Configuracion SMTP (misma restriccion de rol)
                if (dto.SmtpHost != null) emisor.SmtpHost = dto.SmtpHost;
                if (dto.SmtpPort.HasValue) emisor.SmtpPort = dto.SmtpPort;
                if (dto.SmtpUser != null) emisor.SmtpUser = dto.SmtpUser;
                if (dto.SmtpPassword != null) emisor.SmtpPassword = _encryptionService.Encrypt(dto.SmtpPassword);
                if (dto.EmailRemitente != null) emisor.EmailRemitente = dto.EmailRemitente;
                if (dto.EmailHabilitado.HasValue) emisor.EmailHabilitado = dto.EmailHabilitado.Value;
                if (dto.LecturaCorreoHabilitada.HasValue) emisor.LecturaCorreoHabilitada = dto.LecturaCorreoHabilitada.Value;
                // Auto-detectar SMTP si SmtpUser cambió y no se proporcionó SmtpHost
                if (dto.SmtpUser != null && dto.SmtpHost == null)
                    await ConfigurarSmtpAutomaticoAsync(emisor, dto.SmtpUser);
            }

            await _context.SaveChangesAsync();

            return _mapper.Map<EmisorDto>(emisor);
        }

        public async Task<bool> DeactivateAsync(int id)
        {
            var emisor = await _context.Emisores.FindAsync(id);
            if (emisor == null)
                return false;

            emisor.Activo = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var emisor = await _context.Emisores.FindAsync(id);
            if (emisor == null)
                throw new KeyNotFoundException($"Emisor con ID {id} no encontrado");

            emisor.Activo = !emisor.Activo;
            await _context.SaveChangesAsync();

            return emisor.Activo;
        }


        /// <summary>
        /// Plan B Hub-as-Emisor — Fase 3 Task 19.
        /// Loguea como warning cada campo fiscal identitario que el cliente intenta
        /// modificar cuando el emisor ya está vinculado a un Hub. Útil para detectar
        /// UIs no actualizadas (frontend sin el banner read-only) o llamadas API
        /// directas que aún apuntan a este endpoint para editar datos fiscales.
        /// El método NO aplica los cambios — eso es responsabilidad del caller, que
        /// simplemente omite el bloque de asignación cuando hay HubId.
        /// </summary>
        private void LogIntentosDeEdicionFiscal(Emisor emisor, UpdatePerfilEmisorDto dto)
        {
            var intentos = new List<string>();

            if (dto.NombreComercial != null && dto.NombreComercial != emisor.NombreComercial)
                intentos.Add($"NombreComercial: '{emisor.NombreComercial}' → '{dto.NombreComercial}'");
            if (!string.IsNullOrEmpty(dto.CorreoElectronico) && dto.CorreoElectronico != emisor.CorreoElectronico)
                intentos.Add($"CorreoElectronico: '{emisor.CorreoElectronico}' → '{dto.CorreoElectronico}'");
            if (!string.IsNullOrEmpty(dto.Telefono) && dto.Telefono != emisor.Telefono)
                intentos.Add($"Telefono: '{emisor.Telefono}' → '{dto.Telefono}'");
            if (!string.IsNullOrEmpty(dto.Direccion) && dto.Direccion != emisor.Direccion)
                intentos.Add($"Direccion: '{emisor.Direccion}' → '{dto.Direccion}'");
            if (dto.Nrc != null && dto.Nrc != emisor.Nrc)
                intentos.Add($"Nrc: '{emisor.Nrc}' → '{dto.Nrc}'");
            if (dto.CodigoActividad != null && dto.CodigoActividad != emisor.CodigoActividad)
                intentos.Add($"CodigoActividad: '{emisor.CodigoActividad}' → '{dto.CodigoActividad}'");
            if (dto.DescripcionActividad != null && dto.DescripcionActividad != emisor.DescripcionActividad)
                intentos.Add($"DescripcionActividad: '{emisor.DescripcionActividad}' → '{dto.DescripcionActividad}'");
            if (dto.CatDepartamentoId.HasValue && dto.CatDepartamentoId.Value != emisor.CatDepartamentoId)
                intentos.Add($"CatDepartamentoId: {emisor.CatDepartamentoId} → {dto.CatDepartamentoId.Value}");
            if (dto.CatMunicipioId.HasValue && dto.CatMunicipioId.Value != emisor.CatMunicipioId)
                intentos.Add($"CatMunicipioId: {emisor.CatMunicipioId} → {dto.CatMunicipioId.Value}");
            if (dto.CatTipoEstablecimientoId.HasValue && dto.CatTipoEstablecimientoId.Value != emisor.CatTipoEstablecimientoId)
                intentos.Add($"CatTipoEstablecimientoId: {emisor.CatTipoEstablecimientoId} → {dto.CatTipoEstablecimientoId.Value}");

            if (intentos.Count > 0)
            {
                _logger.LogWarning(
                    "[UpdateMiPerfil] Emisor {EmisorId} (HubId={HubId}) intentó editar campos fiscales identitarios via Smartix; cambios descartados (source of truth = SmartHub). Campos: {Campos}",
                    emisor.Id, emisor.HubId, string.Join("; ", intentos));
            }
        }

        private async Task ConfigurarSmtpAutomaticoAsync(Emisor emisor, string smtpUser, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(smtpUser)) return;

            var domain = smtpUser.Split('@').LastOrDefault()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(domain)) return;

            // 1) Proveedores de consumo conocidos por su dominio literal (sin red).
            var proveedor = ProveedorPorDominioLiteral(domain);

            // 2) Dominio corporativo no reconocido (p. ej. Google Workspace tipo
            //    empresa.com): deducir el proveedor por sus registros MX. Sin esto,
            //    cuentas Workspace quedaban con SmtpHost=null y el envío fallaba mudo.
            if (proveedor is null && _proveedorSmtpResolver is not null)
            {
                proveedor = await _proveedorSmtpResolver.ResolverPorMxAsync(domain, ct);
            }

            if (proveedor is { } p)
            {
                emisor.SmtpHost = p.host;
                emisor.SmtpPort = p.port;
            }
            else if (string.IsNullOrEmpty(emisor.SmtpHost))
            {
                // No se pudo deducir el host y el usuario no lo indicó: dejarlo
                // explícito en logs para que sea diagnosticable, en vez de fallar
                // mudo al intentar enviar (ValidarSmtpEmisor → "no tiene SmtpHost").
                _logger.LogWarning(
                    "[EMAIL] No se pudo autodetectar el servidor SMTP de '{SmtpUser}' (dominio '{Domain}'). " +
                    "Configure SmtpHost/SmtpPort manualmente o conecte Gmail por OAuth.", smtpUser, domain);
            }

            emisor.EmailRemitente ??= smtpUser;

            // Auto-habilitar email si SMTP está completamente configurado
            if (!string.IsNullOrEmpty(emisor.SmtpHost) && !string.IsNullOrEmpty(emisor.SmtpPassword))
            {
                emisor.EmailHabilitado = true;
            }
        }

        private static (string host, int port)? ProveedorPorDominioLiteral(string domain) => domain switch
        {
            "gmail.com" or "googlemail.com" => ("smtp.gmail.com", 587),
            "outlook.com" or "hotmail.com" or "live.com" => ("smtp-mail.outlook.com", 587),
            "yahoo.com" or "yahoo.es" => ("smtp.mail.yahoo.com", 587),
            _ => null
        };

        public async Task<bool> UpdateLogoAsync(int emisorId, string logoUrl)
        {
            var emisor = await _context.Emisores.FindAsync(emisorId);
            if (emisor == null)
                return false;

            emisor.LogoUrl = logoUrl;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task UpdateGmailOAuthAsync(int emisorId, string encryptedRefreshToken, string gmailEmail)
        {
            var emisor = await _context.Emisores.FindAsync(emisorId)
                ?? throw new InvalidOperationException($"Emisor {emisorId} no encontrado");

            emisor.GmailRefreshToken = encryptedRefreshToken;
            emisor.GmailEmail = gmailEmail;
            emisor.GmailConectado = true;
            emisor.EmailHabilitado = true;
            await _context.SaveChangesAsync();
        }

        public async Task DisconnectGmailOAuthAsync(int emisorId)
        {
            var emisor = await _context.Emisores.FindAsync(emisorId)
                ?? throw new InvalidOperationException($"Emisor {emisorId} no encontrado");

            emisor.GmailRefreshToken = null;
            emisor.GmailEmail = null;
            emisor.GmailConectado = false;
            await _context.SaveChangesAsync();
        }
    }
}
