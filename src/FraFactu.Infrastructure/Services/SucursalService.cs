using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using FraFactu.Application.DTOs.Sucursales;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services.Helpers;

namespace FraFactu.Infrastructure.Services
{
    public class SucursalService : ISucursalService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateSucursalDto> _createValidator;

        public SucursalService(
            ApplicationDbContext context,
            IMapper mapper,
            IValidator<CreateSucursalDto> createValidator)
        {
            _context = context;
            _mapper = mapper;
            _createValidator = createValidator;
        }

        public async Task<SucursalDto?> GetByIdAsync(int id, int emisorId)
        {
            var sucursal = await _context.Sucursales
                .Include(s => s.Emisor)
                .Include(s => s.Facturas)
                .Where(s => s.Id == id && s.EmisorId == emisorId)
                .FirstOrDefaultAsync();

            return sucursal != null ? _mapper.Map<SucursalDto>(sucursal) : null;
        }

        public async Task<List<SucursalListDto>> GetAllAsync(int emisorId)
        {
            var sucursales = await _context.Sucursales
                .Where(s => s.EmisorId == emisorId)
                .OrderBy(s => s.Codigo)
                .ToListAsync();

            return _mapper.Map<List<SucursalListDto>>(sucursales);
        }

        public async Task<SucursalDto> CreateAsync(CreateSucursalDto dto, int emisorId)
        {
            // Validación
            var validationResult = await _createValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }


            // Generación automática de código si no se proporciona (máx 4 chars para MH)
            if (string.IsNullOrWhiteSpace(dto.Codigo))
            {
                var count = await _context.Sucursales.CountAsync(s => s.EmisorId == emisorId);
                dto.Codigo = (count + 1).ToString().PadLeft(4, '0');
            }

            // Generación automática de CodigoEstablecimiento si no se proporciona
            if (string.IsNullOrWhiteSpace(dto.CodigoEstablecimiento))
            {
                var count = await _context.Sucursales.CountAsync(s => s.EmisorId == emisorId && s.Activo);
                dto.CodigoEstablecimiento = (count + 1).ToString().PadLeft(4, '0');
            }

            // Verificar CodigoEstablecimiento único por emisor
            if (await _context.Sucursales.AnyAsync(s => s.EmisorId == emisorId
                && s.CodigoEstablecimiento == dto.CodigoEstablecimiento && s.Activo))
            {
                throw new InvalidOperationException($"Ya existe una sucursal con el código de establecimiento '{dto.CodigoEstablecimiento}'");
            }

            // Verificar código único por emisor
            var codigoExists = await _context.Sucursales
                .AnyAsync(s => s.EmisorId == emisorId && s.Codigo == dto.Codigo && s.Activo);

            if (codigoExists)
            {
                throw new InvalidOperationException($"Ya existe una sucursal con el código '{dto.Codigo}'");
            }

            // Mapeo
            var sucursal = _mapper.Map<Sucursal>(dto);
            sucursal.EmisorId = emisorId;

            // Agregar
            _context.Sucursales.Add(sucursal);
            await _context.SaveChangesAsync();

            // Recargar con navegación
            await _context.Entry(sucursal).Reference(s => s.Emisor).LoadAsync();
            await _context.Entry(sucursal).Collection(s => s.Facturas).LoadAsync();

            return _mapper.Map<SucursalDto>(sucursal);
        }

        public async Task<SucursalDto> UpdateAsync(int id, UpdateSucursalDto dto, int emisorId)
        {
            var sucursal = await _context.Sucursales.FindAsync(id);
            if (sucursal == null || sucursal.EmisorId != emisorId)
                throw new KeyNotFoundException($"Sucursal con ID {id} no encontrada");

            // Verificar código único si cambia
            if (sucursal.Codigo != dto.Codigo)
            {
                var codigoExists = await _context.Sucursales
                    .AnyAsync(s => s.EmisorId == emisorId && s.Codigo == dto.Codigo && s.Id != id && s.Activo);

                if (codigoExists)
                    throw new InvalidOperationException($"Ya existe una sucursal con el código '{dto.Codigo}'");
            }

            // Verificar CodigoEstablecimiento único si cambia
            if (sucursal.CodigoEstablecimiento != dto.CodigoEstablecimiento)
            {
                var codEstabExists = await _context.Sucursales
                    .AnyAsync(s => s.EmisorId == emisorId && s.CodigoEstablecimiento == dto.CodigoEstablecimiento && s.Id != id && s.Activo);

                if (codEstabExists)
                    throw new InvalidOperationException($"Ya existe una sucursal con el código de establecimiento '{dto.CodigoEstablecimiento}'");
            }

            // Mapeo manual o con automapper (aquí usamos manual para preservar entidad trackeada)
            sucursal.Codigo = dto.Codigo;
            sucursal.Nombre = dto.Nombre;
            sucursal.Direccion = dto.Direccion;
            sucursal.Telefono = dto.Telefono;
            sucursal.CorreoElectronico = dto.CorreoElectronico;
            sucursal.CatDepartamentoId = dto.CatDepartamentoId;
            sucursal.CatMunicipioId = dto.CatMunicipioId;
            sucursal.CatDistritoId = dto.CatDistritoId;
            sucursal.CatTipoEstablecimientoId = dto.CatTipoEstablecimientoId;
            sucursal.CodigoEstablecimiento = dto.CodigoEstablecimiento;
            sucursal.ContingenciaNombreResponsable = dto.ContingenciaNombreResponsable;
            sucursal.ContingenciaTipoDocResponsable = dto.ContingenciaTipoDocResponsable;
            sucursal.ContingenciaNumeroDocResponsable = dto.ContingenciaNumeroDocResponsable;

            await _context.SaveChangesAsync();

            // Recargar relaciones
            await _context.Entry(sucursal).Reference(s => s.Emisor).LoadAsync();
            await _context.Entry(sucursal).Reference(s => s.Departamento).LoadAsync();
            await _context.Entry(sucursal).Reference(s => s.Municipio).LoadAsync();
            if (sucursal.CatDistritoId != null)
                await _context.Entry(sucursal).Reference(s => s.Distrito).LoadAsync();
            await _context.Entry(sucursal).Reference(s => s.TipoEstablecimiento).LoadAsync();

            return _mapper.Map<SucursalDto>(sucursal);
        }

        public async Task<bool> ToggleActiveAsync(int id, int emisorId)
        {
            var sucursal = await _context.Sucursales
                .Where(s => s.Id == id && s.EmisorId == emisorId)
                .FirstOrDefaultAsync();

            if (sucursal == null)
            {
                throw new KeyNotFoundException($"Sucursal con ID {id} no encontrado");
            }

            // Si se va a desactivar, verificar que ninguna bodega tenga stock
            if (sucursal.Activo)
            {
                var bodegasConStock = await _context.StocksBodega
                    .Where(s => s.Bodega!.SucursalId == id && s.Bodega.Activa && (s.CantidadDisponible + s.CantidadReservada) > 0)
                    .Select(s => s.Bodega!.Nombre)
                    .Distinct()
                    .ToListAsync();

                if (bodegasConStock.Any())
                {
                    var nombres = string.Join(", ", bodegasConStock);
                    throw new InvalidOperationException($"No se puede desactivar la sucursal porque las siguientes bodegas tienen productos con stock: {nombres}. Debe transferir o ajustar el inventario antes de desactivarla.");
                }
            }

            sucursal.Activo = !sucursal.Activo;
            await _context.SaveChangesAsync();

            return sucursal.Activo;
        }
    }
}
