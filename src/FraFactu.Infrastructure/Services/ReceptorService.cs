using System.Globalization;
using System.Text;
using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using FraFactu.Infrastructure.Helpers.Pagination;
using FraFactu.Infrastructure.Helpers;
using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Receptores;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;

namespace FraFactu.Infrastructure.Services
{
    public class ReceptorService : IReceptorService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateReceptorDto> _createValidator;
        private readonly IValidator<UpdateReceptorDto> _updateValidator;

        public ReceptorService(
            ApplicationDbContext context,
            IMapper mapper,
            IValidator<CreateReceptorDto> createValidator,
            IValidator<UpdateReceptorDto> updateValidator)
        {
            _context = context;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<ReceptorDto?> GetByIdAsync(int id, int emisorId)
        {
            var receptor = await _context.Receptores
                .AsNoTracking()
                .Include(r => r.Emisor)
                .Include(r => r.TipoDocumento)
                .Include(r => r.Departamento)
                .Include(r => r.Municipio)
                .Where(r => r.Id == id && r.EmisorId == emisorId)
                .FirstOrDefaultAsync();

            return receptor != null ? _mapper.Map<ReceptorDto>(receptor) : null;
        }

        public async Task<PaginatedResponse<ReceptorListDto>> GetAllAsync(
            PaginatedRequest request, int emisorId, bool? soloActivos = null,
            string? search = null, string? orderBy = null, string? orderDirection = null,
            DateTime? fechaDesde = null, DateTime? fechaHasta = null,
            bool soloSujetosExcluidos = false)
        {
            var query = _context.Receptores
                .AsNoTracking()
                .Include(r => r.TipoDocumento)  // Include para TipoDocumentoNombre
                .Where(r => r.EmisorId == emisorId)
                .AsQueryable();

            if (soloActivos.HasValue && soloActivos.Value)
            {
                query = query.Where(r => r.Activo);
            }

            // Sujetos excluidos: no tienen NRC (no son contribuyentes)
            if (soloSujetosExcluidos)
            {
                query = query.Where(r => r.Nrc == null || r.Nrc == "");
            }

            // Búsqueda por nombre, documento, correo o teléfono
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                query = query.Where(r =>
                    r.NombreRazonSocial.ToLower().Contains(term) ||
                    (r.NumeroDocumento != null && r.NumeroDocumento.ToLower().Contains(term)) ||
                    (r.CorreoElectronico != null && r.CorreoElectronico.ToLower().Contains(term)) ||
                    (r.Telefono != null && r.Telefono.ToLower().Contains(term)));
            }
            // Fallback: usar SearchTerm de PaginatedRequest si no viene search explícito
            else if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(r =>
                    r.NombreRazonSocial.ToLower().Contains(term) ||
                    (r.NumeroDocumento != null && r.NumeroDocumento.ToLower().Contains(term)));
            }

            // Filtro por rango de fechas (FechaCreacion) — PostgreSQL requiere UTC
            if (fechaDesde.HasValue)
            {
                var desde = FechaHelper.ToUtc(fechaDesde.Value.Date);
                query = query.Where(r => r.FechaCreacion >= desde);
            }
            if (fechaHasta.HasValue)
            {
                var hasta = FechaHelper.ToUtc(fechaHasta.Value.Date.AddDays(1));
                query = query.Where(r => r.FechaCreacion < hasta);
            }

            // Ordenamiento dinámico
            query = (orderBy?.ToLower(), orderDirection?.ToLower()) switch
            {
                ("nombre", "asc") => query.OrderBy(r => r.NombreRazonSocial),
                ("nombre", "desc") => query.OrderByDescending(r => r.NombreRazonSocial),
                ("fecha", "asc") => query.OrderBy(r => r.FechaCreacion),
                ("fecha", "desc") => query.OrderByDescending(r => r.FechaCreacion),
                ("contribuyentes", _) => query.OrderByDescending(r => r.Nrc != null && r.Nrc != "").ThenBy(r => r.NombreRazonSocial),
                ("clientes", _) => query.OrderBy(r => r.Nrc != null && r.Nrc != "").ThenBy(r => r.NombreRazonSocial),
                _ => query.OrderByDescending(r => r.Id)
            };

            // Paginación usando helper
            var pagedItems = await query.ToPaginatedListAsync(request.PageNumber, request.PageSize);

            var itemsDto = _mapper.Map<List<ReceptorListDto>>(pagedItems);

            return new PaginatedResponse<ReceptorListDto>
            {
                Items = itemsDto,
                CurrentPage = pagedItems.CurrentPage,
                PageSize = pagedItems.PageSize,
                TotalCount = pagedItems.TotalCount,
                TotalPages = pagedItems.TotalPages
            };
        }

        public async Task<ReceptorDto> CreateAsync(CreateReceptorDto dto, int emisorId)
        {
            // Validación
            var validationResult = await _createValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            // Normalización "Sin documento": empty string → null para que el filtro único
            // (NumeroDocumento IS NOT NULL) admita múltiples receptores sin documento por emisor.
            if (string.IsNullOrWhiteSpace(dto.NumeroDocumento))
                dto.NumeroDocumento = null!;

            // Validar unicidad de NumeroDocumento (skip si "Sin documento").
            if (!string.IsNullOrEmpty(dto.NumeroDocumento)
                && await _context.Receptores.AnyAsync(r => r.EmisorId == emisorId
                && r.NumeroDocumento == dto.NumeroDocumento && r.Activo))
                throw new InvalidOperationException($"Ya existe un cliente con el documento {dto.NumeroDocumento}");

            // Validar unicidad de NRC (solo si tiene valor)
            if (!string.IsNullOrEmpty(dto.Nrc) && await _context.Receptores.AnyAsync(r => r.EmisorId == emisorId
                && r.Nrc == dto.Nrc && r.Activo))
                throw new InvalidOperationException($"Ya existe un cliente con el NRC {dto.Nrc}");

            // Mapeo
            var receptor = _mapper.Map<Receptor>(dto);
            receptor.EmisorId = emisorId; // Asignar emisor desde contexto

            // Agregar
            _context.Receptores.Add(receptor);
            await _context.SaveChangesAsync();

            return _mapper.Map<ReceptorDto>(receptor);
        }

        public async Task<ReceptorDto> UpdateAsync(int id, UpdateReceptorDto dto, int emisorId)
        {
            // Validación
            var validationResult = await _updateValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            // Buscar receptor
            var receptor = await _context.Receptores
                .Where(r => r.Id == id && r.EmisorId == emisorId)
                .FirstOrDefaultAsync();

            if (receptor == null)
            {
                throw new KeyNotFoundException($"Receptor con ID {id} no encontrado");
            }

            // Normalización "Sin documento": empty string → null (par natural con CatTipoDocumentoId nullable).
            if (string.IsNullOrWhiteSpace(dto.NumeroDocumento))
                dto.NumeroDocumento = null!;

            // Validar unicidad de NumeroDocumento si cambió (skip si "Sin documento").
            if (!string.IsNullOrEmpty(dto.NumeroDocumento)
                && receptor.NumeroDocumento != dto.NumeroDocumento
                && await _context.Receptores.AnyAsync(r => r.EmisorId == emisorId
                && r.NumeroDocumento == dto.NumeroDocumento && r.Activo && r.Id != id))
                throw new InvalidOperationException($"Ya existe otro cliente con el documento {dto.NumeroDocumento}");

            // Validar unicidad de NRC si cambió
            if (!string.IsNullOrEmpty(dto.Nrc) && receptor.Nrc != dto.Nrc
                && await _context.Receptores.AnyAsync(r => r.EmisorId == emisorId
                && r.Nrc == dto.Nrc && r.Activo && r.Id != id))
                throw new InvalidOperationException($"Ya existe otro cliente con el NRC {dto.Nrc}");

            // Mapear cambios
            _mapper.Map(dto, receptor);

            // Guardar
            await _context.SaveChangesAsync();

            return _mapper.Map<ReceptorDto>(receptor);
        }

        public async Task<List<ReceptorListDto>> SearchAsync(string searchTerm, int emisorId, bool soloSujetosExcluidos = false)
        {
            var term = RemoveDiacritics(searchTerm.ToLower());
            var query = _context.Receptores
                .AsNoTracking()
                .Include(r => r.TipoDocumento)
                .Where(r => r.EmisorId == emisorId && r.Activo);

            // Sujetos excluidos: no tienen NRC y deben tener actividad económica
            if (soloSujetosExcluidos)
                query = query.Where(r => (r.Nrc == null || r.Nrc == "") && r.CodigoActividad != null && r.CodigoActividad != "");

            var receptores = await query.ToListAsync();

            var filtered = receptores
                .Where(r => RemoveDiacritics(r.NombreRazonSocial.ToLower()).Contains(term) ||
                             (r.NumeroDocumento != null && r.NumeroDocumento.ToLower().Contains(term)))
                .OrderByDescending(r => r.Id)
                .Take(20)
                .ToList();

            return _mapper.Map<List<ReceptorListDto>>(filtered);
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
