using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Usuarios;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;

namespace FraFactu.Infrastructure.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateUsuarioDto> _createValidator;
        private readonly IValidator<UpdateUsuarioDto> _updateValidator;
        private readonly IAuthService _authService;

        public UsuarioService(
            ApplicationDbContext context,
            IMapper mapper,
            IValidator<CreateUsuarioDto> createValidator,
            IValidator<UpdateUsuarioDto> updateValidator,
            IAuthService authService)
        {
            _context = context;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _authService = authService;
        }

        public async Task<PaginatedResponse<UsuarioListDto>> GetAllAsync(PaginatedRequest request, int? emisorId = null, bool? soloActivos = null, List<string>? rolesPermitidos = null, List<int>? sucursalIds = null)
        {
            var query = _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Emisor)
                .Include(u => u.UsuarioSucursales)
                    .ThenInclude(us => us.Sucursal)
                .Include(u => u.UsuarioCajas)
                    .ThenInclude(uc => uc.Caja)
                        .ThenInclude(c => c.Sucursal)
                .AsQueryable();

            if (soloActivos.HasValue && soloActivos.Value)
            {
                query = query.Where(u => u.Activo);
            }

            if (emisorId.HasValue)
            {
                query = query.Where(u => u.EmisorId == emisorId.Value);
            }

            // Filtrar por roles permitidos (ej: GerenteSucursal solo ve Cajero)
            if (rolesPermitidos != null && rolesPermitidos.Count > 0)
            {
                query = query.Where(u => rolesPermitidos.Contains(u.Rol.Nombre));
            }

            // Filtrar por sucursales (ej: GerenteSucursal solo ve usuarios de sus sucursales)
            if (sucursalIds != null && sucursalIds.Count > 0)
            {
                query = query.Where(u => u.UsuarioSucursales.Any(us => sucursalIds.Contains(us.SucursalId)));
            }

            // Filtrado por búsqueda
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchLower = request.SearchTerm.ToLower();
                query = query.Where(u =>
                    u.NombreCompleto.ToLower().Contains(searchLower) ||
                    u.Email.ToLower().Contains(searchLower));
            }

            // Paginación
            var totalItems = await query.CountAsync();
            var items = await query
                .OrderBy(u => u.NombreCompleto)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var dtos = items.Select(MapToListDto).ToList();

            return new PaginatedResponse<UsuarioListDto>
            {
                Items = dtos,
                CurrentPage = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)request.PageSize)
            };
        }

        public async Task<UsuarioDto?> GetByIdAsync(int id, int? emisorId = null)
        {
            var query = _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Emisor)
                .Include(u => u.UsuarioSucursales)
                    .ThenInclude(us => us.Sucursal)
                .Include(u => u.UsuarioCajas)
                    .ThenInclude(uc => uc.Caja)
                        .ThenInclude(c => c.Sucursal)
                .AsQueryable();

            if (emisorId.HasValue)
            {
                query = query.Where(u => u.EmisorId == emisorId.Value);
            }

            var usuario = await query.FirstOrDefaultAsync(u => u.Id == id);

            return usuario != null ? MapToDto(usuario) : null;
        }

        public async Task<UsuarioDto> CreateAsync(CreateUsuarioDto dto, string? callerRole = null)
        {
            var validationResult = await _createValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            // Verificar email único en todo el sistema
            if (await _context.Usuarios.AnyAsync(u => u.Email == dto.Email))
                throw new InvalidOperationException($"El email {dto.Email} ya está registrado.");

            // Validar que el rol exista
            var rolAsignado = await _context.Roles.FirstOrDefaultAsync(r => r.Id == dto.RolId);
            if (rolAsignado == null)
                throw new InvalidOperationException("El rol seleccionado no es válido.");

            // GerenteSucursal solo puede asignar el rol Cajero
            if (callerRole == "GerenteSucursal"
                && rolAsignado.Nombre != "Cajero")
            {
                throw new InvalidOperationException("Como Gerente de Sucursal solo puede asignar el rol Cajero.");
            }

            // Validar sucursales si no es AccesoTodasSucursales
            if (!dto.AccesoTodasSucursales)
            {
                if (dto.SucursalIds == null || dto.SucursalIds.Count == 0)
                    throw new InvalidOperationException("Debe asignar al menos una sucursal o seleccionar acceso a todas.");

                // Validar que todas las sucursales pertenezcan al emisor
                var sucursalesValidas = await _context.Sucursales
                    .Where(s => dto.SucursalIds.Contains(s.Id) && s.EmisorId == dto.EmisorId)
                    .CountAsync();

                if (sucursalesValidas != dto.SucursalIds.Count)
                    throw new InvalidOperationException("Una o más sucursales no pertenecen al emisor.");
            }

            var usuario = new Usuario
            {
                NombreCompleto = dto.NombreCompleto,
                Email = dto.Email,
                PasswordHash = string.IsNullOrEmpty(dto.Password) ? null : _authService.HashPassword(dto.Password),
                RequiereCambioPwd = false,
                PermiteCambioPwd = false,
                EmisorId = dto.EmisorId,
                RolId = dto.RolId,
                AccesoTodasSucursales = dto.AccesoTodasSucursales,
                FechaCreacion = DateTime.UtcNow,
                Activo = true
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            // Guardar sucursales asignadas
            if (!dto.AccesoTodasSucursales && dto.SucursalIds.Count > 0)
            {
                foreach (var sucursalId in dto.SucursalIds)
                {
                    _context.UsuariosSucursales.Add(new UsuarioSucursal
                    {
                        UsuarioId = usuario.Id,
                        SucursalId = sucursalId
                    });
                }
                await _context.SaveChangesAsync();
            }

            // Guardar cajas asignadas
            if (dto.CajaIds.Count > 0)
            {
                foreach (var cajaId in dto.CajaIds)
                {
                    _context.UsuariosCajas.Add(new UsuarioCaja
                    {
                        UsuarioId = usuario.Id,
                        CajaId = cajaId
                    });
                }
                await _context.SaveChangesAsync();
            }

            // Cargar relaciones para el DTO
            await _context.Entry(usuario).Reference(u => u.Rol).LoadAsync();
            await _context.Entry(usuario).Reference(u => u.Emisor).LoadAsync();
            await _context.Entry(usuario).Collection(u => u.UsuarioSucursales).LoadAsync();
            foreach (var us in usuario.UsuarioSucursales)
            {
                await _context.Entry(us).Reference(x => x.Sucursal).LoadAsync();
            }
            await _context.Entry(usuario).Collection(u => u.UsuarioCajas).LoadAsync();
            foreach (var uc in usuario.UsuarioCajas)
            {
                await _context.Entry(uc).Reference(x => x.Caja).LoadAsync();
                await _context.Entry(uc.Caja).Reference(c => c.Sucursal).LoadAsync();
            }

            return MapToDto(usuario);
        }

        public async Task<UsuarioDto> UpdateAsync(int id, UpdateUsuarioDto dto, int? emisorId = null, string? callerRole = null)
        {
            var validationResult = await _updateValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            var usuario = await _context.Usuarios
                .Include(u => u.UsuarioSucursales)
                .Include(u => u.UsuarioCajas)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null)
                throw new KeyNotFoundException($"Usuario con ID {id} no encontrado.");

            if (emisorId.HasValue && usuario.EmisorId != emisorId.Value)
                throw new KeyNotFoundException($"Usuario con ID {id} no encontrado en este emisor.");

            // Verificar email único si cambió
            if (dto.Email != usuario.Email && await _context.Usuarios.AnyAsync(u => u.Email == dto.Email))
                throw new InvalidOperationException($"El email {dto.Email} ya está registrado por otro usuario.");

            // Validar que el rol exista
            var rolAsignado = await _context.Roles.FirstOrDefaultAsync(r => r.Id == dto.RolId);
            if (rolAsignado == null)
                throw new InvalidOperationException("El rol seleccionado no es válido.");

            // GerenteSucursal solo puede asignar el rol Cajero
            // Excepción: si el rol no está cambiando (ej. editando su propio perfil o un Contador existente)
            if (callerRole == "GerenteSucursal"
                && rolAsignado.Nombre != "Cajero"
                && usuario.RolId != dto.RolId)
            {
                throw new InvalidOperationException("Como Gerente de Sucursal solo puede asignar el rol Cajero.");
            }

            var userEmisorId = emisorId ?? usuario.EmisorId;

            // Validar sucursales si no es AccesoTodasSucursales
            if (!dto.AccesoTodasSucursales)
            {
                if (dto.SucursalIds == null || dto.SucursalIds.Count == 0)
                    throw new InvalidOperationException("Debe asignar al menos una sucursal o seleccionar acceso a todas.");

                var sucursalesValidas = await _context.Sucursales
                    .Where(s => dto.SucursalIds.Contains(s.Id) && s.EmisorId == userEmisorId)
                    .CountAsync();

                if (sucursalesValidas != dto.SucursalIds.Count)
                    throw new InvalidOperationException("Una o más sucursales no pertenecen al emisor.");
            }

            // Si el email cambió, limpiar ProveedorExternoId para que Google re-vincule automáticamente
            if (dto.Email != usuario.Email)
            {
                usuario.ProveedorExternoId = null;
            }

            // Actualizar campos básicos
            usuario.NombreCompleto = dto.NombreCompleto;
            usuario.Email = dto.Email;
            usuario.RolId = dto.RolId;
            usuario.Activo = dto.Activo;
            usuario.AccesoTodasSucursales = dto.AccesoTodasSucursales;

            // Si se proporciona password, hashearlo y actualizarlo
            if (!string.IsNullOrEmpty(dto.Password))
            {
                usuario.PasswordHash = _authService.HashPassword(dto.Password);
            }

            // Actualizar sucursales asignadas: eliminar las anteriores y agregar las nuevas
            _context.UsuariosSucursales.RemoveRange(usuario.UsuarioSucursales);

            if (!dto.AccesoTodasSucursales && dto.SucursalIds.Count > 0)
            {
                foreach (var sucursalId in dto.SucursalIds)
                {
                    _context.UsuariosSucursales.Add(new UsuarioSucursal
                    {
                        UsuarioId = usuario.Id,
                        SucursalId = sucursalId
                    });
                }
            }

            // Actualizar cajas asignadas: eliminar las anteriores y agregar las nuevas
            _context.UsuariosCajas.RemoveRange(usuario.UsuarioCajas);

            if (dto.CajaIds.Count > 0)
            {
                foreach (var cajaId in dto.CajaIds)
                {
                    _context.UsuariosCajas.Add(new UsuarioCaja
                    {
                        UsuarioId = usuario.Id,
                        CajaId = cajaId
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Cargar relaciones
            await _context.Entry(usuario).Reference(u => u.Rol).LoadAsync();
            await _context.Entry(usuario).Reference(u => u.Emisor).LoadAsync();
            await _context.Entry(usuario).Collection(u => u.UsuarioSucursales).LoadAsync();
            foreach (var us in usuario.UsuarioSucursales)
            {
                await _context.Entry(us).Reference(x => x.Sucursal).LoadAsync();
            }
            await _context.Entry(usuario).Collection(u => u.UsuarioCajas).LoadAsync();
            foreach (var uc in usuario.UsuarioCajas)
            {
                await _context.Entry(uc).Reference(x => x.Caja).LoadAsync();
                await _context.Entry(uc.Caja).Reference(c => c.Sucursal).LoadAsync();
            }

            return MapToDto(usuario);
        }

        public async Task<bool> DeactivateAsync(int id, int? emisorId = null)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return false;

            if (emisorId.HasValue && usuario.EmisorId != emisorId.Value)
                return false;

            usuario.Activo = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleActiveAsync(int id, int? emisorId = null)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) throw new KeyNotFoundException($"Usuario con ID {id} no encontrado.");

            if (emisorId.HasValue && usuario.EmisorId != emisorId.Value)
                throw new KeyNotFoundException($"Usuario con ID {id} no encontrado.");

            usuario.Activo = !usuario.Activo;
            await _context.SaveChangesAsync();
            return usuario.Activo;
        }

        public async Task RegistrarAccesoSucursal(int usuarioId, int sucursalId, string accion)
        {
            // Validar existencia
            if (!await _context.Usuarios.AnyAsync(u => u.Id == usuarioId)) return;
            if (!await _context.Sucursales.AnyAsync(s => s.Id == sucursalId)) return;

            var historial = new HistorialUsuarioSucursal
            {
                UsuarioId = usuarioId,
                SucursalId = sucursalId,
                Accion = accion,
                FechaAcceso = DateTime.UtcNow,
                FechaCreacion = DateTime.UtcNow
            };

            _context.HistorialUsuariosSucursales.Add(historial);
            await _context.SaveChangesAsync();
        }

        // ==========================================
        // MÉTODOS DE MAPEO PRIVADOS
        // ==========================================

        private static UsuarioDto MapToDto(Usuario usuario)
        {
            return new UsuarioDto
            {
                Id = usuario.Id,
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email,
                RequiereCambioPwd = usuario.RequiereCambioPwd,
                ExpiracionPwdTemporal = usuario.ExpiracionPwdTemporal,
                PermiteCambioPwd = usuario.PermiteCambioPwd,
                Activo = usuario.Activo,
                FechaCreacion = usuario.FechaCreacion,
                EmisorId = usuario.EmisorId ?? 0,
                EmisorNombre = usuario.Emisor?.NombreRazonSocial ?? string.Empty,
                EmisorNit = usuario.Emisor?.Nit ?? string.Empty,
                RolId = usuario.RolId,
                RolNombre = usuario.Rol?.Nombre ?? string.Empty,
                AccesoTodasSucursales = usuario.AccesoTodasSucursales,
                Sucursales = usuario.UsuarioSucursales
                    .Select(us => new SucursalAsignadaDto
                    {
                        Id = us.SucursalId,
                        Nombre = us.Sucursal?.Nombre ?? string.Empty
                    }).ToList(),
                Cajas = usuario.UsuarioCajas
                    .Select(uc => new CajaAsignadaDto
                    {
                        Id = uc.CajaId,
                        Nombre = uc.Caja?.Nombre ?? string.Empty,
                        SucursalNombre = uc.Caja?.Sucursal?.Nombre
                    }).ToList(),
                UltimoAcceso = usuario.UltimoAcceso
            };
        }

        private static UsuarioListDto MapToListDto(Usuario usuario)
        {
            var sucursales = usuario.UsuarioSucursales?.ToList() ?? new List<UsuarioSucursal>();
            return new UsuarioListDto
            {
                Id = usuario.Id,
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email,
                Activo = usuario.Activo,
                RolId = usuario.RolId,
                RolNombre = usuario.Rol?.Nombre ?? string.Empty,
                EmisorNombre = usuario.Emisor?.NombreRazonSocial ?? string.Empty,
                AccesoTodasSucursales = usuario.AccesoTodasSucursales,
                CantidadSucursales = sucursales.Count,
                SucursalesNombres = usuario.AccesoTodasSucursales
                    ? "Todas"
                    : string.Join(", ", sucursales.Select(us => us.Sucursal?.Nombre ?? "")),
                SucursalIds = sucursales.Select(us => us.SucursalId).ToList(),
                Cajas = (usuario.UsuarioCajas ?? new List<UsuarioCaja>())
                    .Select(uc => new CajaAsignadaDto
                    {
                        Id = uc.CajaId,
                        Nombre = uc.Caja?.Nombre ?? string.Empty,
                        SucursalNombre = uc.Caja?.Sucursal?.Nombre
                    }).ToList(),
                FechaCreacion = usuario.FechaCreacion,
                UltimoAcceso = usuario.UltimoAcceso
            };
        }
    }
}
