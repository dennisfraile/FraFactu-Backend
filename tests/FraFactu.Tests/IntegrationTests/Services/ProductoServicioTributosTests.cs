using AutoMapper;
using FraFactu.Application.DTOs.ProductosServicios;
using FraFactu.Application.Mapping;
using FraFactu.Application.Validators.ProductosServicios;
using FraFactu.Domain.Entities.Catalogos;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FraFactu.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace FraFactu.Tests.IntegrationTests.Services;

/// <summary>
/// Verifica la persistencia/proyección de TributosAdicionales por producto:
/// crear con tributos, update sincroniza, get/list proyecta el Codigo, vacío => lista vacía,
/// y el IVA "20" se ignora.
/// </summary>
public class ProductoServicioTributosTests : IDisposable
{
    private const int EmisorId = 1;

    private readonly ApplicationDbContext _context;
    private readonly ProductoServicioService _service;

    public ProductoServicioTributosTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);
        SeedTributos();

        var mapper = CreateMapper();
        _service = new ProductoServicioService(
            _context,
            mapper,
            new CreateProductoServicioDtoValidator(),
            new UpdateProductoServicioDtoValidator());
    }

    private void SeedTributos()
    {
        // Solo sembrar si el helper no los trae ya (idempotente para InMemory).
        if (!_context.CatTributos.Any())
        {
            _context.CatTributos.AddRange(
                new CatTributo { Id = 101, Codigo = "D1", Valor = "FOVIAL", DescripcionCorta = "FOVIAL", EsRetencion = false, EsValorPorcentual = false, Seccion = 1 },
                new CatTributo { Id = 102, Codigo = "59", Valor = "Turismo", DescripcionCorta = "Turismo", EsRetencion = false, EsValorPorcentual = true, Seccion = 1 },
                new CatTributo { Id = 103, Codigo = "C5", Valor = "Informativo", DescripcionCorta = "Informativo", EsRetencion = false, EsValorPorcentual = true, Seccion = 3 });
            _context.SaveChanges();
        }
    }

    private static IMapper CreateMapper()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile<MappingProfile>());
        return cfg.CreateMapper();
    }

    private CreateProductoServicioDto BuildCreateDto(List<ProductoTributoDto>? tributos = null)
    {
        return new CreateProductoServicioDto
        {
            Nombre = "Producto con tributos",
            Descripcion = "Test tributos adicionales",
            PrecioVenta = 100m,
            CatUnidadMedidaId = 1,
            CatTipoItemId = 1,
            TipoImpuesto = 1,
            PorcentajeIVA = 13m,
            AccesoTodasSucursales = true,
            TributosAdicionales = tributos
        };
    }

    [Fact]
    public async Task Crear_PersisteTributosAdicionales()
    {
        var dto = BuildCreateDto(new List<ProductoTributoDto>
        {
            new() { Codigo = "D1", TipoCalculo = "monto_fijo", Valor = 0.20m }, // Sección 1
            new() { Codigo = "C5", TipoCalculo = null, Valor = null }           // Sección 3
        });

        var creado = await _service.CreateAsync(dto, EmisorId);
        var leido = await _service.GetByIdAsync(creado.Id, EmisorId);

        leido.Should().NotBeNull();
        leido!.TributosAdicionales.Should().HaveCount(2);
        leido.TributosAdicionales.Should().ContainSingle(t => t.Codigo == "D1" && t.TipoCalculo == "monto_fijo" && t.Valor == 0.20m);
        leido.TributosAdicionales.Should().ContainSingle(t => t.Codigo == "C5" && t.TipoCalculo == null && t.Valor == null);
    }

    [Fact]
    public async Task Actualizar_SincronizaTributosAdicionales()
    {
        var creado = await _service.CreateAsync(BuildCreateDto(new List<ProductoTributoDto>
        {
            new() { Codigo = "D1", TipoCalculo = "monto_fijo", Valor = 0.20m }
        }), EmisorId);

        var updateDto = new UpdateProductoServicioDto
        {
            Codigo = creado.Codigo,
            Nombre = creado.Nombre,
            Descripcion = creado.Descripcion,
            PrecioVenta = creado.PrecioVenta,
            Activo = true,
            CatUnidadMedidaId = 1,
            CatTipoItemId = 1,
            TipoImpuesto = 1,
            PorcentajeIVA = 13m,
            AccesoTodasSucursales = true,
            TributosAdicionales = new List<ProductoTributoDto>
            {
                new() { Codigo = "59", TipoCalculo = "porcentaje", Valor = 5m } // reemplaza a D1
            }
        };

        await _service.UpdateAsync(creado.Id, updateDto, EmisorId);
        var leido = await _service.GetByIdAsync(creado.Id, EmisorId);

        leido!.TributosAdicionales.Should().HaveCount(1);
        leido.TributosAdicionales.Should().ContainSingle(t => t.Codigo == "59" && t.TipoCalculo == "porcentaje" && t.Valor == 5m);
        leido.TributosAdicionales.Should().NotContain(t => t.Codigo == "D1");
    }

    [Fact]
    public async Task Listar_ProyectaTributosAdicionales()
    {
        var creado = await _service.CreateAsync(BuildCreateDto(new List<ProductoTributoDto>
        {
            new() { Codigo = "D1", TipoCalculo = "monto_fijo", Valor = 0.20m }
        }), EmisorId);

        var resultados = await _service.SearchAsync("Producto con tributos", EmisorId);

        var item = resultados.Should().ContainSingle(p => p.Id == creado.Id).Subject;
        item.TributosAdicionales.Should().ContainSingle(t => t.Codigo == "D1" && t.Valor == 0.20m);
        item.TributosAdicionales.Single(t => t.Codigo == "D1").TipoCalculo.Should().Be("monto_fijo");
    }

    [Fact]
    public async Task Crear_SinTributos_DevuelveListaVacia()
    {
        var creado = await _service.CreateAsync(BuildCreateDto(null), EmisorId);
        var leido = await _service.GetByIdAsync(creado.Id, EmisorId);

        leido!.TributosAdicionales.Should().BeEmpty();
    }

    [Fact]
    public async Task Crear_IgnoraCodigoIva20()
    {
        var creado = await _service.CreateAsync(BuildCreateDto(new List<ProductoTributoDto>
        {
            new() { Codigo = "20", TipoCalculo = "porcentaje", Valor = 13m }, // IVA: debe ignorarse
            new() { Codigo = "D1", TipoCalculo = "monto_fijo", Valor = 0.20m }
        }), EmisorId);
        var leido = await _service.GetByIdAsync(creado.Id, EmisorId);

        leido!.TributosAdicionales.Should().ContainSingle(t => t.Codigo == "D1");
        leido.TributosAdicionales.Should().NotContain(t => t.Codigo == "20");
    }

    [Fact]
    public async Task Crear_ConCodigoDuplicadoEnDto_PersisteSoloUnaFila()
    {
        // Arrange: DTO con el mismo código "D1" repetido dos veces (input malformado).
        var dto = BuildCreateDto(new List<ProductoTributoDto>
        {
            new() { Codigo = "D1", TipoCalculo = "monto_fijo", Valor = 0.20m }, // primera ocurrencia
            new() { Codigo = "D1", TipoCalculo = "monto_fijo", Valor = 0.20m }  // duplicado: debe ignorarse
        });

        // Act
        var creado = await _service.CreateAsync(dto, EmisorId);
        var leido = await _service.GetByIdAsync(creado.Id, EmisorId);

        // Assert: sólo una fila de D1, sin excepción de constraint.
        leido.Should().NotBeNull();
        leido!.TributosAdicionales.Should().ContainSingle(t => t.Codigo == "D1");
    }

    public void Dispose() => _context?.Dispose();
}
