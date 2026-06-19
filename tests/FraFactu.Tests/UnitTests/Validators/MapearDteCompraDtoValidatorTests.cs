using FraFactu.Application.DTOs.DtesRecibidos;
using FraFactu.Application.Validators.DtesRecibidos;
using FluentAssertions;
using Xunit;

namespace FraFactu.Tests.UnitTests.Validators;

/// <summary>
/// F1 (Plan inventario desde DTE): cubre las reglas estructurales del DTO
/// del wizard. Las validaciones contextuales (sucursal/producto/bodega
/// existen y son del emisor, totales cuadran, codigos duplicados) viven en
/// el service y estan cubiertas en
/// <see cref="IntegrationTests.Services.DteRecibidoServiceMapearTests"/>.
/// </summary>
public class MapearDteCompraDtoValidatorTests
{
    private readonly MapearDteCompraDtoValidator _validator = new();

    private static MapearItemDto ItemProductoExistente() => new()
    {
        DescripcionDte = "Item X",
        MontoDte = 113m,
        Accion = AccionMapeoItem.ProductoExistente,
        ProductoId = 10,
        Cantidad = 2m,
        BodegaId = 5,
        CostoUnitario = 50m
    };

    private static MapearItemDto ItemGasto() => new()
    {
        DescripcionDte = "Flete",
        MontoDte = 5m,
        Accion = AccionMapeoItem.Gasto
    };

    [Fact]
    public void Validar_PayloadMinimoValido_NoTieneErrores()
    {
        var dto = new MapearDteCompraDto
        {
            SucursalId = 1,
            Items = new List<MapearItemDto> { ItemProductoExistente() }
        };

        var res = _validator.Validate(dto);

        res.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validar_SinSucursal_DebeFallar()
    {
        var dto = new MapearDteCompraDto
        {
            SucursalId = 0,
            Items = new List<MapearItemDto> { ItemProductoExistente() }
        };

        var res = _validator.Validate(dto);

        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == "SucursalId");
    }

    [Fact]
    public void Validar_ItemsVacio_DebeFallar()
    {
        var dto = new MapearDteCompraDto { SucursalId = 1, Items = new() };

        var res = _validator.Validate(dto);

        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == "Items");
    }

    [Theory]
    [InlineData("")]
    [InlineData("INVENTARIO")]      // valor no permitido
    [InlineData("producto_existente")] // case sensitive
    public void Validar_AccionInvalida_DebeFallar(string accion)
    {
        var item = ItemProductoExistente();
        item.Accion = accion;
        var dto = new MapearDteCompraDto
        {
            SucursalId = 1,
            Items = new() { item }
        };

        var res = _validator.Validate(dto);

        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName.Contains("Accion"));
    }

    [Fact]
    public void Validar_ProductoExistenteSinProductoId_DebeFallar()
    {
        var item = ItemProductoExistente();
        item.ProductoId = null;
        var dto = new MapearDteCompraDto { SucursalId = 1, Items = new() { item } };

        var res = _validator.Validate(dto);

        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName.Contains("ProductoId"));
    }

    [Fact]
    public void Validar_ProductoExistenteSinBodega_DebeFallar()
    {
        var item = ItemProductoExistente();
        item.BodegaId = null;
        var dto = new MapearDteCompraDto { SucursalId = 1, Items = new() { item } };

        var res = _validator.Validate(dto);

        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName.Contains("BodegaId"));
    }

    [Fact]
    public void Validar_ProductoExistenteCantidadCero_DebeFallar()
    {
        var item = ItemProductoExistente();
        item.Cantidad = 0m;
        var dto = new MapearDteCompraDto { SucursalId = 1, Items = new() { item } };

        var res = _validator.Validate(dto);

        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName.Contains("Cantidad"));
    }

    [Fact]
    public void Validar_ProductoExistenteCostoNegativo_DebeFallar()
    {
        var item = ItemProductoExistente();
        item.CostoUnitario = -1m;
        var dto = new MapearDteCompraDto { SucursalId = 1, Items = new() { item } };

        var res = _validator.Validate(dto);

        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName.Contains("CostoUnitario"));
    }

    [Fact]
    public void Validar_ProductoNuevoSinDatos_DebeFallar()
    {
        var item = new MapearItemDto
        {
            Accion = AccionMapeoItem.ProductoNuevo,
            DescripcionDte = "x",
            MontoDte = 100m,
            Cantidad = 1m,
            BodegaId = 5,
            CostoUnitario = 100m,
            ProductoNuevo = null
        };
        var dto = new MapearDteCompraDto { SucursalId = 1, Items = new() { item } };

        var res = _validator.Validate(dto);

        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName.Contains("ProductoNuevo"));
    }

    [Fact]
    public void Validar_ProductoNuevoSinCodigo_EsValido_PorqueSeAutogenera()
    {
        var item = new MapearItemDto
        {
            Accion = AccionMapeoItem.ProductoNuevo,
            DescripcionDte = "x",
            MontoDte = 100m,
            Cantidad = 1m,
            BodegaId = 5,
            CostoUnitario = 100m,
            ProductoNuevo = new ProductoNuevoMapeoDto
            {
                Codigo = "",
                Nombre = "n",
                CatTipoItemId = 1,
                CatUnidadMedidaId = 1
            }
        };
        var dto = new MapearDteCompraDto { SucursalId = 1, Items = new() { item } };

        var res = _validator.Validate(dto);

        // Codigo vacio es valido: el servicio lo autogenera al crear el producto.
        res.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validar_ProductoNuevoCodigoMuyLargo_DebeFallar()
    {
        var item = new MapearItemDto
        {
            Accion = AccionMapeoItem.ProductoNuevo,
            DescripcionDte = "x",
            MontoDte = 100m,
            Cantidad = 1m,
            BodegaId = 5,
            CostoUnitario = 100m,
            ProductoNuevo = new ProductoNuevoMapeoDto
            {
                Codigo = new string('A', 51),  // supera MaximumLength(50)
                Nombre = "n",
                CatTipoItemId = 1,
                CatUnidadMedidaId = 1
            }
        };
        var dto = new MapearDteCompraDto { SucursalId = 1, Items = new() { item } };

        var res = _validator.Validate(dto);

        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName.Contains("Codigo"));
    }

    [Fact]
    public void Validar_GastoConMontoCero_DebeFallar()
    {
        var item = new MapearItemDto
        {
            Accion = AccionMapeoItem.Gasto,
            DescripcionDte = "Flete",
            MontoDte = 0m
        };
        var dto = new MapearDteCompraDto { SucursalId = 1, Items = new() { item } };

        var res = _validator.Validate(dto);

        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName.Contains("MontoDte"));
    }

    [Fact]
    public void Validar_GastoConMontoValido_NoTieneErrores()
    {
        var dto = new MapearDteCompraDto { SucursalId = 1, Items = new() { ItemGasto() } };

        var res = _validator.Validate(dto);

        res.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validar_MezclaProductoYGasto_NoTieneErrores()
    {
        var dto = new MapearDteCompraDto
        {
            SucursalId = 1,
            Items = new() { ItemProductoExistente(), ItemGasto() }
        };

        var res = _validator.Validate(dto);

        res.IsValid.Should().BeTrue();
    }
}
