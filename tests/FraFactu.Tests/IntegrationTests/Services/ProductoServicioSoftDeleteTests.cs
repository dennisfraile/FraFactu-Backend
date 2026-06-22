using FraFactu.Application.DTOs.ProductosServicios;
using FraFactu.Application.Mapping;
using FraFactu.Application.Validators.ProductosServicios;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FraFactu.Tests.Fixtures;
using AutoMapper;
using FluentAssertions;
using Xunit;

namespace FraFactu.Tests.IntegrationTests.Services;

/// <summary>
/// F3.2 (G2): la baja de un producto exige motivo y registra auditoría
/// (quién y cuándo). La reactivación limpia esos campos.
/// </summary>
public class ProductoServicioSoftDeleteTests : IDisposable
{
    private const int EmisorId = 1;

    private readonly ApplicationDbContext _context;
    private readonly ProductoServicioService _service;

    public ProductoServicioSoftDeleteTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);

        var cfg = new MapperConfiguration(c => c.AddProfile<MappingProfile>());
        var mapper = cfg.CreateMapper();
        _service = new ProductoServicioService(
            _context,
            mapper,
            new CreateProductoServicioDtoValidator(),
            new UpdateProductoServicioDtoValidator());
    }

    private ProductoServicio SeedProductoActivo()
    {
        var producto = new ProductoServicio
        {
            Codigo = "P-001",
            Nombre = "Producto activo",
            PrecioVenta = 100m,
            CatUnidadMedidaId = 1,
            CatTipoItemId = 1,
            EmisorId = EmisorId,
            Activo = true
        };
        _context.ProductosServicios.Add(producto);
        _context.SaveChanges();
        return producto;
    }

    [Fact]
    public async Task DesactivarAsync_ConMotivo_DesactivaYRegistraAuditoria()
    {
        var producto = SeedProductoActivo();

        await _service.DesactivarAsync(
            producto.Id,
            EmisorId,
            new DesactivarProductoServicioDto { Motivo = "Descontinuado por el proveedor" },
            usuarioId: 7);

        var actualizado = await _context.ProductosServicios.FindAsync(producto.Id);
        actualizado!.Activo.Should().BeFalse();
        actualizado.MotivoDesactivacion.Should().Be("Descontinuado por el proveedor");
        actualizado.DesactivadoPorUsuarioId.Should().Be(7);
        actualizado.DesactivadoEn.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task DesactivarAsync_MotivoVacio_LanzaArgumentException(string? motivo)
    {
        var producto = SeedProductoActivo();

        var act = async () => await _service.DesactivarAsync(
            producto.Id,
            EmisorId,
            new DesactivarProductoServicioDto { Motivo = motivo! },
            usuarioId: 7);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DesactivarAsync_ProductoInexistente_LanzaKeyNotFound()
    {
        var act = async () => await _service.DesactivarAsync(
            99999,
            EmisorId,
            new DesactivarProductoServicioDto { Motivo = "x" },
            usuarioId: 7);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ToggleActiveAsync_Reactivar_LimpiaAuditoriaDeBaja()
    {
        var producto = SeedProductoActivo();
        await _service.DesactivarAsync(
            producto.Id,
            EmisorId,
            new DesactivarProductoServicioDto { Motivo = "Baja temporal" },
            usuarioId: 7);

        var reactivado = await _service.ToggleActiveAsync(producto.Id, EmisorId);

        reactivado.Should().BeTrue();
        var actualizado = await _context.ProductosServicios.FindAsync(producto.Id);
        actualizado!.Activo.Should().BeTrue();
        actualizado.MotivoDesactivacion.Should().BeNull();
        actualizado.DesactivadoPorUsuarioId.Should().BeNull();
        actualizado.DesactivadoEn.Should().BeNull();
    }

    public void Dispose() => _context.Dispose();
}
