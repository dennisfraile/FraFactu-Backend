using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.ProductosServicios;
using FraFactu.Application.DTOs.Usuarios;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Entities.Catalogos;
using FraFactu.Infrastructure.Persistence;

namespace FraFactu.Infrastructure.Services
{
    public class ProductoServicioService : IProductoServicioService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateProductoServicioDto> _createValidator;
        private readonly IValidator<UpdateProductoServicioDto> _updateValidator;

        public ProductoServicioService(
            ApplicationDbContext context,
            IMapper mapper,
            IValidator<CreateProductoServicioDto> createValidator,
            IValidator<UpdateProductoServicioDto> updateValidator)
        {
            _context = context;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<ProductoServicioDto?> GetByIdAsync(int id, int emisorId)
        {
            var productoServicio = await _context.ProductosServicios
                .Include(p => p.Emisor)
                .Include(p => p.UnidadMedida)
                .Include(p => p.TipoItem)
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Include(p => p.ProductoServicioSucursales)
                    .ThenInclude(ps => ps.Sucursal)
                .Include(p => p.TributosAdicionales)
                    .ThenInclude(t => t.CatTributo)
                .Where(p => p.Id == id && p.EmisorId == emisorId)
                .FirstOrDefaultAsync();

            return productoServicio != null ? _mapper.Map<ProductoServicioDto>(productoServicio) : null;
        }

        public async Task<PaginatedResponse<ProductoServicioListDto>> GetAllAsync(PaginatedRequest request, int emisorId, int? sucursalId = null, int? categoriaId = null, int? marcaId = null, string? unidadMedida = null, string? tipo = null, bool incluirInactivos = false)
        {
            var query = _context.ProductosServicios
                .Include(p => p.UnidadMedida)
                .Include(p => p.TipoItem)
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Include(p => p.ProductoServicioSucursales)
                    .ThenInclude(ps => ps.Sucursal)
                .Include(p => p.TributosAdicionales)
                    .ThenInclude(t => t.CatTributo)
                .Where(p => p.EmisorId == emisorId)
                .AsQueryable();

            if (!incluirInactivos)
            {
                query = query.Where(p => p.Activo);
            }

            if (sucursalId.HasValue)
            {
                // Bienes: filtrar por stock en bodegas de la sucursal
                // Servicios: filtrar por acceso a sucursal (AccesoTodasSucursales o asignación explícita)
                query = query.Where(p =>
                    (p.CatTipoItemId == 1 && _context.StocksBodega.Any(sb =>
                        sb.ProductoId == p.Id
                        && sb.Bodega.SucursalId == sucursalId.Value))
                    || (p.CatTipoItemId != 1 && (p.AccesoTodasSucursales
                        || p.ProductoServicioSucursales.Any(ps => ps.SucursalId == sucursalId.Value))));
            }

            if (categoriaId.HasValue)
                query = query.Where(p => p.CategoriaId == categoriaId.Value);

            if (marcaId.HasValue)
                query = query.Where(p => p.MarcaId == marcaId.Value);

            if (!string.IsNullOrEmpty(unidadMedida))
                query = query.Where(p => p.UnidadMedida != null && p.UnidadMedida.Valor.ToLower() == unidadMedida.ToLower());

            if (!string.IsNullOrEmpty(tipo))
            {
                // "producto" → CatTipoItemId 1 (Bien/Producto), "servicio" → CatTipoItemId 2 (Servicio)
                query = query.Where(p => p.TipoItem != null && p.TipoItem.Valor.ToLower().Contains(tipo.ToLower()));
            }

            // Búsqueda
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var searchLower = request.SearchTerm.ToLower();
                query = query.Where(p =>
                    p.Nombre.ToLower().Contains(searchLower) ||
                    p.Codigo.ToLower().Contains(searchLower) ||
                    (p.Descripcion != null && p.Descripcion.ToLower().Contains(searchLower)));
            }

            // Total de registros
            var totalCount = await query.CountAsync();

            // Ordenamiento
            var sortDesc = request.OrderDirection?.ToLower() == "desc";
            IOrderedQueryable<ProductoServicio> orderedQuery = request.OrderBy?.ToLower() switch
            {
                "codigo" => sortDesc ? query.OrderByDescending(p => p.Codigo) : query.OrderBy(p => p.Codigo),
                "precioventa" => sortDesc ? query.OrderByDescending(p => p.PrecioVenta) : query.OrderBy(p => p.PrecioVenta),
                "preciocosto" => sortDesc ? query.OrderByDescending(p => p.PrecioCosto) : query.OrderBy(p => p.PrecioCosto),
                "fechacreacion" => sortDesc ? query.OrderByDescending(p => p.FechaCreacion) : query.OrderBy(p => p.FechaCreacion),
                _ => sortDesc ? query.OrderByDescending(p => p.Nombre) : query.OrderBy(p => p.Nombre)
            };
            query = orderedQuery;

            // Paginación
            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var itemsDto = items.Select(p => new ProductoServicioListDto
            {
                Id = p.Id,
                Codigo = p.Codigo,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                PrecioVenta = p.PrecioVenta,
                PrecioCosto = p.PrecioCosto,
                Activo = p.Activo,
                UnidadMedida = p.UnidadMedida != null ? p.UnidadMedida.Valor : "N/A",
                TipoItem = p.TipoItem != null ? p.TipoItem.Valor : "N/A",
                CategoriaId = p.CategoriaId,
                CategoriaNombre = p.Categoria != null ? p.Categoria.Nombre : null,
                MarcaId = p.MarcaId,
                MarcaNombre = p.Marca != null ? p.Marca.Nombre : null,
                TipoImpuesto = (int)p.TipoImpuesto,
                PorcentajeIVA = p.PorcentajeIVA,
                PrecioIncluyeIva = p.PrecioIncluyeIva,
                CostoIncluyeIva = p.CostoIncluyeIva,
                CodigoBarras = p.CodigoBarras,
                StockMinimo = (int?)p.StockMinimo,
                StockMaximo = (int?)p.StockMaximo,
                PuntoReorden = (int?)p.PuntoReorden,
                PermiteVentaSinStock = p.PermiteVentaSinStock,
                FechaCreacion = p.FechaCreacion,
                AccesoTodasSucursales = p.AccesoTodasSucursales,
                Sucursales = p.ProductoServicioSucursales?.Select(ps => new SucursalAsignadaDto
                {
                    Id = ps.SucursalId,
                    Nombre = ps.Sucursal?.Nombre ?? string.Empty
                }).ToList() ?? new(),
                // F2: clasificacion contable + campos de activo fijo (solo
                // populados cuando TipoInventario = MobiliarioEquipo).
                TipoInventario = (int)p.TipoInventario,
                FechaAdquisicion = p.FechaAdquisicion,
                AniosVidaUtil = p.AniosVidaUtil,
                ValorActual = p.ValorActual,
                ValorResidual = p.ValorResidual,
                TributosAdicionales = p.TributosAdicionales != null
                    ? p.TributosAdicionales.Select(t => new ProductoTributoDto
                    {
                        Codigo = t.CatTributo != null ? t.CatTributo.Codigo : string.Empty,
                        TipoCalculo = t.TipoCalculo,
                        Valor = t.Valor
                    }).ToList()
                    : new List<ProductoTributoDto>()
            }).ToList();

            return new PaginatedResponse<ProductoServicioListDto>
            {
                Items = itemsDto,
                CurrentPage = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };
        }

        public async Task<ProductoServicioDto> CreateAsync(CreateProductoServicioDto dto, int emisorId)
        {
            // Validación
            var validationResult = await _createValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            // Guardar si el código fue proporcionado o será autogenerado
            bool codigoFueAutogenerado = string.IsNullOrWhiteSpace(dto.Codigo);

            // Generación automática de código si no se proporciona (lógica compartida
            // con el wizard de DTEs recibidos vía ProductoCodigoGenerator).
            if (codigoFueAutogenerado)
            {
                dto.Codigo = await ProductoCodigoGenerator.GenerarSiguienteCodigoAsync(
                    _context, emisorId, dto.CatTipoItemId);
            }

            // Verificar código único por emisor
            var codigoExists = await _context.ProductosServicios
                .AnyAsync(p => p.EmisorId == emisorId && p.Codigo == dto.Codigo && p.Activo);

            if (codigoExists)
            {
                if (codigoFueAutogenerado)
                {
                    var prefix = dto.CatTipoItemId == 2 ? "SERV" : "PROD";
                    dto.Codigo = $"{prefix}-{DateTime.UtcNow.Ticks.ToString()[^6..]}";
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Ya existe un producto/servicio con el código '{dto.Codigo}'");
                }
            }

            // Mapeo
            var productoServicio = _mapper.Map<ProductoServicio>(dto);
            productoServicio.EmisorId = emisorId;
            productoServicio.AccesoTodasSucursales = dto.AccesoTodasSucursales;

            // Agregar
            _context.ProductosServicios.Add(productoServicio);
            await _context.SaveChangesAsync();

            // Guardar sucursales asignadas
            var sucursalIds = dto.SucursalIds ?? new List<int>();
            if (!dto.AccesoTodasSucursales && sucursalIds.Count > 0)
            {
                // Validar sucursales pertenecen al emisor
                var sucursalesValidas = await _context.Sucursales
                    .Where(s => sucursalIds.Contains(s.Id) && s.EmisorId == emisorId)
                    .CountAsync();

                if (sucursalesValidas != sucursalIds.Count)
                    throw new InvalidOperationException("Una o más sucursales no pertenecen al emisor.");

                foreach (var sid in sucursalIds)
                {
                    _context.ProductosServiciosSucursales.Add(new ProductoServicioSucursal
                    {
                        ProductoServicioId = productoServicio.Id,
                        SucursalId = sid
                    });
                }
                await _context.SaveChangesAsync();
            }

            // Guardar tributos adicionales (Sección 1 + Sección 3). El IVA "20" se ignora.
            await SincronizarTributosAdicionalesAsync(productoServicio, dto.TributosAdicionales, reemplazar: false);
            await _context.SaveChangesAsync();

            // Recargar con navegación
            await _context.Entry(productoServicio).Reference(p => p.Emisor).LoadAsync();
            await _context.Entry(productoServicio).Reference(p => p.UnidadMedida).LoadAsync();
            await _context.Entry(productoServicio).Reference(p => p.TipoItem).LoadAsync();
            await _context.Entry(productoServicio).Collection(p => p.ProductoServicioSucursales).LoadAsync();
            foreach (var ps in productoServicio.ProductoServicioSucursales)
            {
                await _context.Entry(ps).Reference(x => x.Sucursal).LoadAsync();
            }
            await _context.Entry(productoServicio).Collection(p => p.TributosAdicionales).LoadAsync();
            foreach (var t in productoServicio.TributosAdicionales)
            {
                await _context.Entry(t).Reference(x => x.CatTributo).LoadAsync();
            }

            return _mapper.Map<ProductoServicioDto>(productoServicio);
        }

        public async Task<ProductoServicioDto> UpdateAsync(int id, UpdateProductoServicioDto dto, int emisorId)
        {
            // Validación
            var validationResult = await _updateValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            // Buscar producto/servicio
            var productoServicio = await _context.ProductosServicios
                .Include(p => p.Emisor)
                .Include(p => p.UnidadMedida)
                .Include(p => p.TipoItem)
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Include(p => p.ProductoServicioSucursales)
                .Include(p => p.TributosAdicionales)
                    .ThenInclude(t => t.CatTributo)
                .Where(p => p.Id == id && p.EmisorId == emisorId)
                .FirstOrDefaultAsync();

            if (productoServicio == null)
            {
                throw new KeyNotFoundException($"Producto/Servicio con ID {id} no encontrado");
            }

            // Verificar código único (si cambió)
            if (productoServicio.Codigo != dto.Codigo)
            {
                var codigoExists = await _context.ProductosServicios
                    .AnyAsync(p => p.Id != id && p.EmisorId == emisorId && p.Codigo == dto.Codigo && p.Activo);

                if (codigoExists)
                {
                    throw new InvalidOperationException($"Ya existe un producto/servicio con el código '{dto.Codigo}'");
                }
            }

            // Mapear cambios (excepto sucursales que se manejan manualmente)
            _mapper.Map(dto, productoServicio);
            productoServicio.AccesoTodasSucursales = dto.AccesoTodasSucursales;

            // Reemplazar sucursales: remove-all + re-add
            _context.ProductosServiciosSucursales.RemoveRange(productoServicio.ProductoServicioSucursales);

            var sucursalIds = dto.SucursalIds ?? new List<int>();
            if (!dto.AccesoTodasSucursales && sucursalIds.Count > 0)
            {
                // Validar sucursales pertenecen al emisor
                var sucursalesValidas = await _context.Sucursales
                    .Where(s => sucursalIds.Contains(s.Id) && s.EmisorId == emisorId)
                    .CountAsync();

                if (sucursalesValidas != sucursalIds.Count)
                    throw new InvalidOperationException("Una o más sucursales no pertenecen al emisor.");

                foreach (var sid in sucursalIds)
                {
                    _context.ProductosServiciosSucursales.Add(new ProductoServicioSucursal
                    {
                        ProductoServicioId = productoServicio.Id,
                        SucursalId = sid
                    });
                }
            }

            // Reemplazar tributos adicionales: remove-all + re-add (igual que sucursales).
            await SincronizarTributosAdicionalesAsync(productoServicio, dto.TributosAdicionales, reemplazar: true);

            // Guardar
            await _context.SaveChangesAsync();

            // Recargar sucursales
            await _context.Entry(productoServicio).Collection(p => p.ProductoServicioSucursales).LoadAsync();
            foreach (var ps in productoServicio.ProductoServicioSucursales)
            {
                await _context.Entry(ps).Reference(x => x.Sucursal).LoadAsync();
            }
            await _context.Entry(productoServicio).Collection(p => p.TributosAdicionales).LoadAsync();
            foreach (var t in productoServicio.TributosAdicionales)
            {
                await _context.Entry(t).Reference(x => x.CatTributo).LoadAsync();
            }

            return _mapper.Map<ProductoServicioDto>(productoServicio);
        }

        public async Task<bool> ToggleActiveAsync(int id, int emisorId)
        {
            var productoServicio = await _context.ProductosServicios
                .Where(p => p.Id == id && p.EmisorId == emisorId)
                .FirstOrDefaultAsync();

            if (productoServicio == null)
            {
                throw new KeyNotFoundException($"Producto/Servicio con ID {id} no encontrado");
            }

            productoServicio.Activo = !productoServicio.Activo;
            await _context.SaveChangesAsync();

            return productoServicio.Activo;
        }

        public async Task<SyncProductoToggleResponseDto> SetActivoByCodigoAsync(int emisorId, string codigo, bool activo)
        {
            // Buscamos sin filtrar por Activo: el unique index parcial (EmisorId, Codigo)
            // WHERE Activo=true permite que existan filas inactivas con el mismo codigo,
            // pero solo deberia haber 1 cuando se llama via sync (Inventory respeta el
            // mismo invariante). Si hubiera mas de uno, tomamos el activo primero.
            var producto = await _context.ProductosServicios
                .Where(p => p.EmisorId == emisorId && p.Codigo == codigo)
                .OrderByDescending(p => p.Activo)
                .ThenByDescending(p => p.Id)
                .FirstOrDefaultAsync();

            if (producto == null)
            {
                return new SyncProductoToggleResponseDto
                {
                    Encontrado = false,
                    Cambio = false,
                    Activo = activo
                };
            }

            if (producto.Activo == activo)
            {
                return new SyncProductoToggleResponseDto
                {
                    Encontrado = true,
                    Cambio = false,
                    Activo = producto.Activo,
                    Id = producto.Id
                };
            }

            producto.Activo = activo;
            await _context.SaveChangesAsync();

            return new SyncProductoToggleResponseDto
            {
                Encontrado = true,
                Cambio = true,
                Activo = producto.Activo,
                Id = producto.Id
            };
        }

        public async Task<SyncProductoUpsertResponseDto> UpsertByCodigoAsync(SyncProductoUpsertRequestDto request)
        {
            var emisorId = request.EmisorId;
            var codigo = (request.Codigo ?? string.Empty).Trim();
            if (emisorId <= 0 || string.IsNullOrEmpty(codigo))
            {
                throw new InvalidOperationException("EmisorId y Codigo son requeridos para upsert.");
            }

            // Renombrado: si viene CodigoAnterior y difiere del nuevo, buscamos el producto
            // por el codigo VIEJO para cambiarle el codigo (en vez de crear un duplicado con
            // el nuevo). El codigo es la clave de match cross-app, asi que esta es la unica
            // forma limpia de renombrarlo.
            var codigoAnterior = (request.CodigoAnterior ?? string.Empty).Trim();
            var esRename = codigoAnterior.Length > 0
                && !string.Equals(codigoAnterior, codigo, StringComparison.Ordinal);

            // Igual que SetActivoByCodigoAsync: tomamos el activo primero (el unique
            // index parcial WHERE Activo=true permite un activo y N inactivos por codigo).
            // En rename buscamos por el codigo anterior.
            var codigoBusqueda = esRename ? codigoAnterior : codigo;
            var existente = await _context.ProductosServicios
                .Where(p => p.EmisorId == emisorId && p.Codigo == codigoBusqueda)
                .OrderByDescending(p => p.Activo)
                .ThenByDescending(p => p.Id)
                .FirstOrDefaultAsync();

            // Fallback retry-safe: si era rename y no aparece por el codigo viejo (ej. un
            // reintento donde ya se habia renombrado, o el producto nunca se propago a
            // Smartix), lo buscamos por el codigo nuevo para no duplicar.
            if (existente == null && esRename)
            {
                existente = await _context.ProductosServicios
                    .Where(p => p.EmisorId == emisorId && p.Codigo == codigo)
                    .OrderByDescending(p => p.Activo)
                    .ThenByDescending(p => p.Id)
                    .FirstOrDefaultAsync();
            }

            var esServicio = string.Equals(request.Tipo, "Servicio", StringComparison.OrdinalIgnoreCase);
            int catTipoItemId = esServicio ? 2 : 1;

            // Resolver UnidadMedida string -> CatUnidadMedidaId. Buscamos por Valor
            // (ej. "Unidad", "Litro"); si no matchea cae al primer disponible para
            // no romper el upsert por catalogos faltantes.
            var unidad = await _context.Set<Domain.Entities.Catalogos.CatUnidadMedida>()
                .FirstOrDefaultAsync(u => u.Valor.ToLower() == request.UnidadMedida.ToLower())
                ?? await _context.Set<Domain.Entities.Catalogos.CatUnidadMedida>().FirstOrDefaultAsync();
            int catUnidadMedidaId = unidad?.Id ?? 1;

            if (existente == null)
            {
                var nuevo = new Domain.Entities.ProductoServicio
                {
                    EmisorId = emisorId,
                    Codigo = codigo,
                    Nombre = request.Nombre,
                    Descripcion = request.Descripcion,
                    PrecioVenta = request.PrecioVenta,
                    PrecioCosto = request.PrecioCosto,
                    CodigoBarras = request.CodigoBarras,
                    StockMinimo = request.StockMinimo,
                    CatTipoItemId = catTipoItemId,
                    CatUnidadMedidaId = catUnidadMedidaId,
                    TipoImpuesto = NormalizarTipoImpuesto(request.TipoImpuesto),
                    PorcentajeIVA = NormalizarPorcentajeIVA(request.TipoImpuesto, request.PorcentajeIVA),
                    AccesoTodasSucursales = true,
                    Activo = request.Activo
                };
                _context.ProductosServicios.Add(nuevo);
                await _context.SaveChangesAsync();
                return new SyncProductoUpsertResponseDto
                {
                    Creado = true,
                    Cambio = true,
                    Id = nuevo.Id,
                    Codigo = nuevo.Codigo,
                    Activo = nuevo.Activo
                };
            }

            // Actualizar solo si algo cambio (idempotencia "real": evitamos round-trip
            // de SaveChanges si los datos ya estaban iguales).
            bool cambio = false;
            // Renombrado de codigo: si el producto encontrado todavia tiene un codigo
            // distinto al nuevo, validamos que el nuevo no este tomado por OTRO producto
            // activo del emisor (el unique index parcial lo rechazaria, pero damos un
            // error claro) y recien ahi lo pisamos.
            if (existente.Codigo != codigo)
            {
                var codigoEnUso = await _context.ProductosServicios
                    .AnyAsync(p => p.EmisorId == emisorId && p.Id != existente.Id
                        && p.Codigo == codigo && p.Activo);
                if (codigoEnUso)
                {
                    throw new InvalidOperationException(
                        $"Ya existe un producto activo con el codigo '{codigo}' en este emisor.");
                }
                existente.Codigo = codigo;
                cambio = true;
            }
            if (existente.Nombre != request.Nombre) { existente.Nombre = request.Nombre; cambio = true; }
            if (existente.Descripcion != request.Descripcion) { existente.Descripcion = request.Descripcion; cambio = true; }
            if (existente.PrecioVenta != request.PrecioVenta) { existente.PrecioVenta = request.PrecioVenta; cambio = true; }
            if (existente.PrecioCosto != request.PrecioCosto) { existente.PrecioCosto = request.PrecioCosto; cambio = true; }
            if (existente.CodigoBarras != request.CodigoBarras) { existente.CodigoBarras = request.CodigoBarras; cambio = true; }
            if (existente.StockMinimo != request.StockMinimo) { existente.StockMinimo = request.StockMinimo; cambio = true; }
            if (existente.Activo != request.Activo) { existente.Activo = request.Activo; cambio = true; }
            if (existente.CatTipoItemId != catTipoItemId) { existente.CatTipoItemId = catTipoItemId; cambio = true; }
            if (existente.CatUnidadMedidaId != catUnidadMedidaId) { existente.CatUnidadMedidaId = catUnidadMedidaId; cambio = true; }
            var tipoImpuestoNuevo = NormalizarTipoImpuesto(request.TipoImpuesto);
            if (existente.TipoImpuesto != tipoImpuestoNuevo)
            {
                existente.TipoImpuesto = tipoImpuestoNuevo;
                cambio = true;
            }
            var porcentajeIvaNuevo = NormalizarPorcentajeIVA(request.TipoImpuesto, request.PorcentajeIVA);
            if (existente.PorcentajeIVA != porcentajeIvaNuevo)
            {
                existente.PorcentajeIVA = porcentajeIvaNuevo;
                cambio = true;
            }

            if (cambio) await _context.SaveChangesAsync();

            return new SyncProductoUpsertResponseDto
            {
                Creado = false,
                Cambio = cambio,
                Id = existente.Id,
                Codigo = existente.Codigo,
                Activo = existente.Activo
            };
        }

        public async Task<List<ProductoServicioListDto>> SearchAsync(string searchTerm, int emisorId, int? sucursalId = null)
        {
            var query = _context.ProductosServicios
                .Include(p => p.UnidadMedida)
                .Include(p => p.TipoItem)
                .Include(p => p.ProductoServicioSucursales)
                    .ThenInclude(ps => ps.Sucursal)
                .Include(p => p.TributosAdicionales)
                    .ThenInclude(t => t.CatTributo)
                .Where(p => p.EmisorId == emisorId && p.Activo &&
                    (p.Nombre.ToLower().Contains(searchTerm.ToLower()) ||
                     p.Codigo.ToLower().Contains(searchTerm.ToLower())));

            if (sucursalId.HasValue)
            {
                // Bienes: filtrar por stock en bodegas de la sucursal
                // Servicios: filtrar por acceso a sucursal (AccesoTodasSucursales o asignación explícita)
                query = query.Where(p =>
                    (p.CatTipoItemId == 1 && _context.StocksBodega.Any(sb =>
                        sb.ProductoId == p.Id
                        && sb.Bodega.SucursalId == sucursalId.Value))
                    || (p.CatTipoItemId != 1 && (p.AccesoTodasSucursales
                        || p.ProductoServicioSucursales.Any(ps => ps.SucursalId == sucursalId.Value))));
            }

            var productosServicios = await query
                .Take(20) // Límite de resultados
                .ToListAsync();

            return _mapper.Map<List<ProductoServicioListDto>>(productosServicios);
        }

        /// <summary>
        /// Sincroniza las filas ProductoServicioTributo de un producto a partir del DTO.
        /// Espeja el patrón de ProductoServicioSucursales: cuando <paramref name="reemplazar"/>
        /// es true (update) borra las previas; luego inserta las nuevas. Ignora el IVA ("20").
        /// NO llama SaveChanges (lo hace el caller).
        /// </summary>
        private async Task SincronizarTributosAdicionalesAsync(
            ProductoServicio productoServicio,
            List<ProductoTributoDto>? tributos,
            bool reemplazar)
        {
            if (reemplazar && productoServicio.TributosAdicionales.Count > 0)
            {
                _context.ProductosServiciosTributos.RemoveRange(productoServicio.TributosAdicionales);
            }

            if (tributos == null || tributos.Count == 0)
                return;

            // Deduplicar por Código (primera ocurrencia) para evitar constraint duplicado en (ProductoServicioId, CatTributoId).
            var tributosSinDuplicados = tributos
                .GroupBy(t => (t.Codigo ?? string.Empty).Trim())
                .Select(g => g.First())
                .ToList();

            foreach (var t in tributosSinDuplicados)
            {
                var codigo = (t.Codigo ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(codigo) || codigo == "20") // IVA no se guarda aquí
                    continue;

                var catTributoId = await ConvertirTributoCodigoACatTributoIdAsync(codigo);
                if (catTributoId == null)
                    continue; // código desconocido: se ignora (robustez)

                _context.ProductosServiciosTributos.Add(new ProductoServicioTributo
                {
                    ProductoServicioId = productoServicio.Id,
                    CatTributoId = catTributoId.Value,
                    TipoCalculo = t.TipoCalculo,
                    Valor = t.Valor
                });
            }
        }

        /// <summary>
        /// Convierte un código de tributo de Hacienda al Id del catálogo cat_tributos.
        /// Devuelve null si no existe (a diferencia del helper de FacturaService que cae a IVA).
        /// </summary>
        private async Task<int?> ConvertirTributoCodigoACatTributoIdAsync(string codigo)
        {
            var tributo = await _context.CatTributos
                .FirstOrDefaultAsync(ct => ct.Codigo == codigo);
            return tributo?.Id;
        }

        /// <summary>
        /// Defensivo: valida que el int del request es 1/2/3; si no, cae a Gravado.
        /// Evita persistir basura por bug de cliente o proxy mal configurado.
        /// </summary>
        private static Domain.Enums.TipoImpuesto NormalizarTipoImpuesto(int valor) =>
            valor is 1 or 2 or 3 ? (Domain.Enums.TipoImpuesto)valor : Domain.Enums.TipoImpuesto.Gravado;

        /// <summary>
        /// Si TipoImpuesto != Gravado, IVA no aplica → siempre null. Si Gravado,
        /// usa el valor del request si está en (0,100], si no cae a 13 (tasa estándar SV).
        /// </summary>
        private static decimal? NormalizarPorcentajeIVA(int tipoImpuestoRequest, decimal? porcentajeRequest)
        {
            if (NormalizarTipoImpuesto(tipoImpuestoRequest) != Domain.Enums.TipoImpuesto.Gravado)
                return null;
            if (porcentajeRequest is > 0 and <= 100)
                return porcentajeRequest;
            return 13m;
        }
    }
}
