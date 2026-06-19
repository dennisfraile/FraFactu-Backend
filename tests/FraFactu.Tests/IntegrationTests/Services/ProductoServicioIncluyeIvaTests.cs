using AutoMapper;
using FraFactu.Application.DTOs.ProductosServicios;
using FraFactu.Application.Mapping;
using FraFactu.Application.Validators.ProductosServicios;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FraFactu.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace FraFactu.Tests.IntegrationTests.Services;

/// <summary>
/// Verifica que los flags PrecioIncluyeIva / CostoIncluyeIva del producto
/// persistan correctamente a través del ciclo crear/actualizar/consultar.
/// AutoMapper los copia por nombre desde/hacia los DTOs.
/// </summary>
public class ProductoServicioIncluyeIvaTests : IDisposable
{
    private const int EmisorId = 1;

    private readonly ApplicationDbContext _context;
    private readonly ProductoServicioService _service;

    public ProductoServicioIncluyeIvaTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);

        var mapper = CreateMapper();
        _service = new ProductoServicioService(
            _context,
            mapper,
            new CreateProductoServicioDtoValidator(),
            new UpdateProductoServicioDtoValidator());
    }

    private static IMapper CreateMapper()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile<MappingProfile>());
        return cfg.CreateMapper();
    }

    private CreateProductoServicioDto BuildCreateDto(
        bool? precioIncluyeIva = null,
        bool? costoIncluyeIva = null)
    {
        var dto = new CreateProductoServicioDto
        {
            Nombre = "Producto IVA Test",
            Descripcion = "Producto para test de flags incluye-IVA",
            PrecioVenta = 150.00m,
            PrecioCosto = 100.00m,
            CatUnidadMedidaId = 1,
            CatTipoItemId = 1, // Bien
            TipoImpuesto = 1,  // Gravado
            PorcentajeIVA = 13m,
            AccesoTodasSucursales = true
        };

        if (precioIncluyeIva.HasValue) dto.PrecioIncluyeIva = precioIncluyeIva.Value;
        if (costoIncluyeIva.HasValue) dto.CostoIncluyeIva = costoIncluyeIva.Value;

        return dto;
    }

    [Fact]
    public async Task Crear_GuardaFlagsIncluyeIva()
    {
        // Arrange
        var dto = BuildCreateDto(precioIncluyeIva: true, costoIncluyeIva: true);

        // Act
        var creado = await _service.CreateAsync(dto, EmisorId);
        var leido = await _service.GetByIdAsync(creado.Id, EmisorId);

        // Assert
        leido.Should().NotBeNull();
        leido!.PrecioIncluyeIva.Should().BeTrue();
        leido.CostoIncluyeIva.Should().BeTrue();
    }

    [Fact]
    public async Task Actualizar_CambiaFlagsIncluyeIva()
    {
        // Arrange: crear con flags en false
        var createDto = BuildCreateDto(precioIncluyeIva: false, costoIncluyeIva: false);
        var creado = await _service.CreateAsync(createDto, EmisorId);

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
            PrecioCosto = creado.PrecioCosto,
            PrecioIncluyeIva = true, // cambia a true
            CostoIncluyeIva = false,
            AccesoTodasSucursales = true
        };

        // Act
        await _service.UpdateAsync(creado.Id, updateDto, EmisorId);
        var leido = await _service.GetByIdAsync(creado.Id, EmisorId);

        // Assert
        leido.Should().NotBeNull();
        leido!.PrecioIncluyeIva.Should().BeTrue();
    }

    [Fact]
    public async Task Crear_DefaultFlagsEnFalse()
    {
        // Arrange: DTO sin tocar los flags (deben quedar en su default false)
        var dto = BuildCreateDto();

        // Act
        var creado = await _service.CreateAsync(dto, EmisorId);
        var leido = await _service.GetByIdAsync(creado.Id, EmisorId);

        // Assert
        leido.Should().NotBeNull();
        leido!.PrecioIncluyeIva.Should().BeFalse();
        leido.CostoIncluyeIva.Should().BeFalse();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
