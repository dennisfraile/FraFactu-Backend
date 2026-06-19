using AutoMapper;
using FraFactu.Application.DTOs.DtesRecibidos;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FraFactu.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FraFactu.Tests.IntegrationTests.Services;

/// <summary>
/// F1 (Plan inventario desde DTE): cubre el wizard <c>MapearYCrearCompraAsync</c>.
/// Valida happy path (productos + gastos), creacion de productos nuevos,
/// aislamiento cross-tenant (productos/sucursales/bodegas de otros emisores),
/// tolerancia de totales y codigos duplicados.
/// </summary>
public class DteRecibidoServiceMapearTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DteRecibidoService _service;
    private readonly Emisor _emisor;
    private readonly Sucursal _sucursal;
    private readonly Bodega _bodega;
    private readonly Categoria _categoria;

    public DteRecibidoServiceMapearTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);
        _emisor = _context.Emisores.First();
        _sucursal = _context.Sucursales.First(s => s.EmisorId == _emisor.Id);
        _bodega = _context.Bodegas.First(b => b.SucursalId == _sucursal.Id);
        _categoria = _context.Categorias.First();

        var parser = new DteParserService(Mock.Of<ILogger<DteParserService>>());
        var ingesta = new DteIngestaService(_context, parser, Mock.Of<ILogger<DteIngestaService>>());
        _service = new DteRecibidoService(
            _context, Mock.Of<IMapper>(), ingesta, parser, Mock.Of<ILogger<DteRecibidoService>>());
    }

    private DteRecibido SeedDte(
        decimal subtotal = 200m,
        decimal iva = 26m,
        decimal total = 226m,
        EstadoDteRecibido estado = EstadoDteRecibido.PENDIENTE)
    {
        var dte = new DteRecibido
        {
            EmisorId = _emisor.Id,
            CodigoGeneracion = $"DTE-{Guid.NewGuid()}",
            TipoDte = "03",
            NumeroControl = "DTE-03-0001-000000000000001",
            FechaEmision = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            EmisorNit = "06141804941035",
            EmisorNombre = "PROVEEDOR SA",
            JsonDte = "{}",
            SubTotal = subtotal,
            IVA = iva,
            Total = total,
            Estado = estado,
            FuenteRecepcion = FuenteRecepcionDte.CORREO,
            FechaCreacion = DateTime.UtcNow
        };
        _context.DtesRecibidos.Add(dte);
        _context.SaveChanges();
        return dte;
    }

    private ProductoServicio SeedProducto(string codigo, int? emisorIdOverride = null)
    {
        var unidad = _context.CatUnidadesMedida.First();
        var tipoItem = _context.CatTiposItem.First(t => t.Codigo == "1");
        var prod = new ProductoServicio
        {
            Codigo = codigo,
            Nombre = $"Producto {codigo}",
            CatTipoItemId = tipoItem.Id,
            CatUnidadMedidaId = unidad.Id,
            EmisorId = emisorIdOverride ?? _emisor.Id,
            PrecioVenta = 10m,
            AccesoTodasSucursales = true,
            FechaCreacion = DateTime.UtcNow
        };
        _context.ProductosServicios.Add(prod);
        _context.SaveChanges();
        return prod;
    }

    [Fact]
    public async Task Mapear_ProductoExistenteCubreTotal_CreaCompraConDetalleEnInventario()
    {
        var dte = SeedDte();
        var producto = SeedProducto("PROD-001");

        var dto = new MapearDteCompraDto
        {
            SucursalId = _sucursal.Id,
            Items = new List<MapearItemDto>
            {
                new()
                {
                    DescripcionDte = "Producto X",
                    MontoDte = 226m,
                    Accion = AccionMapeoItem.ProductoExistente,
                    ProductoId = producto.Id,
                    Cantidad = 4m,
                    BodegaId = _bodega.Id,
                    CostoUnitario = 50m
                }
            }
        };

        var res = await _service.MapearYCrearCompraAsync(dte.Id, dto, _emisor.Id);

        res.CompraId.Should().BeGreaterThan(0);
        res.ItemsProductos.Should().Be(1);
        res.ItemsGastos.Should().Be(0);
        res.ProductosCreados.Should().Be(0);
        res.TotalMapeado.Should().Be(226m);
        res.TotalDte.Should().Be(226m);

        var compra = await _context.ComprasExternas.SingleAsync(c => c.Id == res.CompraId);
        compra.Estado.Should().Be("BORRADOR");
        compra.Origen.Should().Be("DTE");
        compra.CodigoGeneracionDte.Should().Be(dte.CodigoGeneracion);
        compra.SucursalId.Should().Be(_sucursal.Id);

        var detalles = await _context.CompraExternaDetalles
            .Where(d => d.CompraExternaId == res.CompraId).ToListAsync();
        detalles.Should().ContainSingle();
        detalles[0].EsParaInventario.Should().BeTrue();
        detalles[0].ProductoId.Should().Be(producto.Id);
        detalles[0].BodegaId.Should().Be(_bodega.Id);
        detalles[0].Cantidad.Should().Be(4m);
        detalles[0].CostoUnitario.Should().Be(50m);
        detalles[0].Subtotal.Should().Be(200m);
        detalles[0].IVA.Should().Be(26m);
        detalles[0].Total.Should().Be(226m);

        (await _context.GastosAdministrativos.AnyAsync(g => g.CompraExternaId == res.CompraId))
            .Should().BeFalse();

        var dteActualizado = await _context.DtesRecibidos.SingleAsync(d => d.Id == dte.Id);
        dteActualizado.Estado.Should().Be(EstadoDteRecibido.VINCULADO);
        dteActualizado.CompraExternaId.Should().Be(res.CompraId);
    }

    [Fact]
    public async Task Mapear_MezclaProductoYGasto_PersisteAmbos()
    {
        // DTE: 100 producto + 13 IVA + 13 gasto-administrativo = 126 total
        var dte = SeedDte(subtotal: 100m, iva: 13m, total: 126m);
        var producto = SeedProducto("PROD-002");

        var dto = new MapearDteCompraDto
        {
            SucursalId = _sucursal.Id,
            Items = new List<MapearItemDto>
            {
                new()
                {
                    DescripcionDte = "Item inventariable",
                    MontoDte = 113m,
                    Accion = AccionMapeoItem.ProductoExistente,
                    ProductoId = producto.Id,
                    Cantidad = 2m,
                    BodegaId = _bodega.Id,
                    CostoUnitario = 50m
                },
                new()
                {
                    DescripcionDte = "Flete cobrado",
                    MontoDte = 13m,
                    Accion = AccionMapeoItem.Gasto
                }
            }
        };

        var res = await _service.MapearYCrearCompraAsync(dte.Id, dto, _emisor.Id);

        res.ItemsProductos.Should().Be(1);
        res.ItemsGastos.Should().Be(1);

        var detalles = await _context.CompraExternaDetalles
            .Where(d => d.CompraExternaId == res.CompraId).ToListAsync();
        detalles.Should().HaveCount(1);

        var gastos = await _context.GastosAdministrativos
            .Where(g => g.CompraExternaId == res.CompraId).ToListAsync();
        gastos.Should().HaveCount(1);
        gastos[0].Descripcion.Should().Be("Flete cobrado");
        gastos[0].Monto.Should().Be(13m);
    }

    [Fact]
    public async Task Mapear_ProductoNuevo_CreaProductoEnElCatalogoYDetalleInventario()
    {
        var dte = SeedDte();
        var unidad = _context.CatUnidadesMedida.First();
        var tipoItem = _context.CatTiposItem.First(t => t.Codigo == "1");

        var dto = new MapearDteCompraDto
        {
            SucursalId = _sucursal.Id,
            Items = new List<MapearItemDto>
            {
                new()
                {
                    DescripcionDte = "Producto sin código en catálogo",
                    MontoDte = 226m,
                    Accion = AccionMapeoItem.ProductoNuevo,
                    ProductoNuevo = new ProductoNuevoMapeoDto
                    {
                        Codigo = "NUEVO-001",
                        Nombre = "Producto nuevo desde DTE",
                        CatTipoItemId = tipoItem.Id,
                        CatUnidadMedidaId = unidad.Id,
                        CategoriaId = _categoria.Id
                    },
                    Cantidad = 4m,
                    BodegaId = _bodega.Id,
                    CostoUnitario = 50m
                }
            }
        };

        var res = await _service.MapearYCrearCompraAsync(dte.Id, dto, _emisor.Id);

        res.ProductosCreados.Should().Be(1);
        res.ItemsProductos.Should().Be(1);

        var creado = await _context.ProductosServicios.SingleAsync(p => p.Codigo == "NUEVO-001");
        creado.EmisorId.Should().Be(_emisor.Id);
        creado.Nombre.Should().Be("Producto nuevo desde DTE");
        creado.PrecioCosto.Should().Be(50m);
        creado.PrecioVenta.Should().Be(0m);
        creado.CategoriaId.Should().Be(_categoria.Id);

        var detalle = await _context.CompraExternaDetalles
            .SingleAsync(d => d.CompraExternaId == res.CompraId);
        detalle.ProductoId.Should().Be(creado.Id);

        // F2: default sin TipoInventario explicito = Ventas.
        creado.TipoInventario.Should().Be(FraFactu.Domain.Enums.TipoInventario.Ventas);
        creado.FechaAdquisicion.Should().BeNull();
        creado.ValorActual.Should().BeNull();
    }

    [Fact]
    public async Task Mapear_ProductoNuevoBienSinCodigo_AutogeneraCodigoProd()
    {
        var dte = SeedDte();
        var unidad = _context.CatUnidadesMedida.First();
        var tipoBien = _context.CatTiposItem.First(t => t.Codigo == "1");

        var dto = new MapearDteCompraDto
        {
            SucursalId = _sucursal.Id,
            Items = new List<MapearItemDto>
            {
                new()
                {
                    DescripcionDte = "Bien sin codigo",
                    MontoDte = 226m,
                    Accion = AccionMapeoItem.ProductoNuevo,
                    ProductoNuevo = new ProductoNuevoMapeoDto
                    {
                        Codigo = "", // vacio -> debe autogenerarse
                        Nombre = "Bien autogenerado",
                        CatTipoItemId = tipoBien.Id,
                        CatUnidadMedidaId = unidad.Id
                    },
                    Cantidad = 4m,
                    BodegaId = _bodega.Id,
                    CostoUnitario = 50m
                }
            }
        };

        var res = await _service.MapearYCrearCompraAsync(dte.Id, dto, _emisor.Id);

        res.ProductosCreados.Should().Be(1);
        var creado = await _context.ProductosServicios.SingleAsync(p => p.Nombre == "Bien autogenerado");
        creado.Codigo.Should().MatchRegex(@"^PROD-\d{5}$");
    }

    [Fact]
    public async Task Mapear_ProductoNuevoServicioSinCodigo_AutogeneraCodigoServ()
    {
        var dte = SeedDte();
        var unidad = _context.CatUnidadesMedida.First();
        var tipoServicio = _context.CatTiposItem.First(t => t.Codigo == "2");

        var dto = new MapearDteCompraDto
        {
            SucursalId = _sucursal.Id,
            Items = new List<MapearItemDto>
            {
                new()
                {
                    DescripcionDte = "Servicio sin codigo",
                    MontoDte = 226m,
                    Accion = AccionMapeoItem.ProductoNuevo,
                    ProductoNuevo = new ProductoNuevoMapeoDto
                    {
                        Codigo = "",
                        Nombre = "Servicio autogenerado",
                        CatTipoItemId = tipoServicio.Id,
                        CatUnidadMedidaId = unidad.Id
                    },
                    Cantidad = 4m,
                    BodegaId = _bodega.Id,
                    CostoUnitario = 50m
                }
            }
        };

        var res = await _service.MapearYCrearCompraAsync(dte.Id, dto, _emisor.Id);

        res.ProductosCreados.Should().Be(1);
        var creado = await _context.ProductosServicios.SingleAsync(p => p.Nombre == "Servicio autogenerado");
        creado.Codigo.Should().MatchRegex(@"^SERV-\d{5}$");
    }

    [Fact]
    public async Task Mapear_ProductoNuevoConCodigoManual_RespetaElCodigo()
    {
        var dte = SeedDte();
        var unidad = _context.CatUnidadesMedida.First();
        var tipoBien = _context.CatTiposItem.First(t => t.Codigo == "1");

        var dto = new MapearDteCompraDto
        {
            SucursalId = _sucursal.Id,
            Items = new List<MapearItemDto>
            {
                new()
                {
                    DescripcionDte = "Bien con SKU manual",
                    MontoDte = 226m,
                    Accion = AccionMapeoItem.ProductoNuevo,
                    ProductoNuevo = new ProductoNuevoMapeoDto
                    {
                        Codigo = "MI-SKU-99",
                        Nombre = "Bien con codigo manual",
                        CatTipoItemId = tipoBien.Id,
                        CatUnidadMedidaId = unidad.Id
                    },
                    Cantidad = 4m,
                    BodegaId = _bodega.Id,
                    CostoUnitario = 50m
                }
            }
        };

        var res = await _service.MapearYCrearCompraAsync(dte.Id, dto, _emisor.Id);

        res.ProductosCreados.Should().Be(1);
        var creado = await _context.ProductosServicios.SingleAsync(p => p.Nombre == "Bien con codigo manual");
        creado.Codigo.Should().Be("MI-SKU-99");
    }

    [Fact]
    public async Task Mapear_ProductoNuevoMobiliarioEquipo_SiembraCamposDeActivoFijo()
    {
        // F2: al crear un activo fijo via el wizard, el servicio debe sembrar
        // FechaAdquisicion = FechaEmision del DTE y ValorActual = CostoUnitario.
        // AniosVidaUtil y ValorResidual viajan en el payload (opcionales).
        var dte = SeedDte();
        var unidad = _context.CatUnidadesMedida.First();
        var tipoItem = _context.CatTiposItem.First(t => t.Codigo == "1");

        var dto = new MapearDteCompraDto
        {
            SucursalId = _sucursal.Id,
            Items = new List<MapearItemDto>
            {
                new()
                {
                    DescripcionDte = "Laptop Dell Latitude",
                    MontoDte = 226m,
                    Accion = AccionMapeoItem.ProductoNuevo,
                    ProductoNuevo = new ProductoNuevoMapeoDto
                    {
                        Codigo = "ACT-LAPTOP-001",
                        Nombre = "Laptop Dell Latitude",
                        CatTipoItemId = tipoItem.Id,
                        CatUnidadMedidaId = unidad.Id,
                        TipoInventario = 1, // MobiliarioEquipo
                        AniosVidaUtil = 5,
                        ValorResidual = 50m
                    },
                    Cantidad = 1m,
                    BodegaId = _bodega.Id,
                    CostoUnitario = 200m
                }
            }
        };
        // Ajusto subtotal/iva del dte para que cuadre con 1 x 200 + IVA prorrateado.
        dte.SubTotal = 200m;
        dte.IVA = 26m;
        dte.Total = 226m;
        await _context.SaveChangesAsync();

        var res = await _service.MapearYCrearCompraAsync(dte.Id, dto, _emisor.Id);

        res.ProductosCreados.Should().Be(1);

        var creado = await _context.ProductosServicios.SingleAsync(p => p.Codigo == "ACT-LAPTOP-001");
        creado.TipoInventario.Should().Be(FraFactu.Domain.Enums.TipoInventario.MobiliarioEquipo);
        creado.FechaAdquisicion.Should().Be(DateTime.SpecifyKind(dte.FechaEmision, DateTimeKind.Utc));
        creado.ValorActual.Should().Be(200m);
        creado.PrecioCosto.Should().Be(200m);
        creado.AniosVidaUtil.Should().Be(5);
        creado.ValorResidual.Should().Be(50m);
    }

    [Fact]
    public async Task Mapear_TotalNoCuadra_LanzaInvalidOperation()
    {
        var dte = SeedDte(); // total 226
        var producto = SeedProducto("PROD-003");

        var dto = new MapearDteCompraDto
        {
            SucursalId = _sucursal.Id,
            Items = new List<MapearItemDto>
            {
                new()
                {
                    DescripcionDte = "Solo mapea la mitad",
                    MontoDte = 113m,
                    Accion = AccionMapeoItem.ProductoExistente,
                    ProductoId = producto.Id,
                    Cantidad = 2m,
                    BodegaId = _bodega.Id,
                    CostoUnitario = 50m
                }
            }
        };

        var act = async () => await _service.MapearYCrearCompraAsync(dte.Id, dto, _emisor.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no coincide con el total del DTE*");

        // No debe haber compra ni detalles ni cambio de estado del DTE.
        (await _context.ComprasExternas.AnyAsync()).Should().BeFalse();
        var dteSinTocar = await _context.DtesRecibidos.SingleAsync();
        dteSinTocar.Estado.Should().Be(EstadoDteRecibido.PENDIENTE);
    }

    [Fact]
    public async Task Mapear_ProductoDeOtroEmisor_LanzaKeyNotFound()
    {
        var dte = SeedDte();
        var otroEmisor = new Emisor
        {
            Nit = "00000000000002",
            Nrc = "2",
            NombreRazonSocial = "OTRO",
            CodigoActividad = "00000",
            DescripcionActividad = "x",
            CorreoElectronico = "o@x.com",
            Telefono = "0",
            CatDepartamentoId = _emisor.CatDepartamentoId,
            CatMunicipioId = _emisor.CatMunicipioId,
            Direccion = "x"
        };
        _context.Emisores.Add(otroEmisor);
        await _context.SaveChangesAsync();
        var productoAjeno = SeedProducto("AJENO-001", emisorIdOverride: otroEmisor.Id);

        var dto = new MapearDteCompraDto
        {
            SucursalId = _sucursal.Id,
            Items = new List<MapearItemDto>
            {
                new()
                {
                    DescripcionDte = "Cross-tenant",
                    MontoDte = 226m,
                    Accion = AccionMapeoItem.ProductoExistente,
                    ProductoId = productoAjeno.Id,
                    Cantidad = 4m,
                    BodegaId = _bodega.Id,
                    CostoUnitario = 50m
                }
            }
        };

        var act = async () => await _service.MapearYCrearCompraAsync(dte.Id, dto, _emisor.Id);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Productos no encontrados*");
    }

    [Fact]
    public async Task Mapear_BodegaDeOtraSucursal_LanzaInvalidOperation()
    {
        var dte = SeedDte();
        var producto = SeedProducto("PROD-004");

        // Crear otra sucursal del mismo emisor con su bodega.
        var depto = _context.CatDepartamentos.First();
        var muni = _context.CatMunicipios.First();
        var tipoEst = _context.CatTiposEstablecimiento.First();
        var otraSuc = new Sucursal
        {
            EmisorId = _emisor.Id,
            Codigo = "0002",
            Nombre = "Otra",
            Direccion = "x",
            CatDepartamentoId = depto.Id,
            CatMunicipioId = muni.Id,
            CatTipoEstablecimientoId = tipoEst.Id,
            CodigoEstablecimiento = "0002"
        };
        _context.Sucursales.Add(otraSuc);
        _context.SaveChanges();
        var bodegaAjena = new Bodega
        {
            Codigo = "BOD002",
            Nombre = "Ajena",
            Direccion = "x",
            SucursalId = otraSuc.Id,
            Activa = true
        };
        _context.Bodegas.Add(bodegaAjena);
        _context.SaveChanges();

        var dto = new MapearDteCompraDto
        {
            SucursalId = _sucursal.Id,
            Items = new List<MapearItemDto>
            {
                new()
                {
                    DescripcionDte = "Bodega de otra sucursal",
                    MontoDte = 226m,
                    Accion = AccionMapeoItem.ProductoExistente,
                    ProductoId = producto.Id,
                    Cantidad = 4m,
                    BodegaId = bodegaAjena.Id,
                    CostoUnitario = 50m
                }
            }
        };

        var act = async () => await _service.MapearYCrearCompraAsync(dte.Id, dto, _emisor.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*bodegas no pertenecen a la sucursal*");
    }

    [Fact]
    public async Task Mapear_CodigoProductoNuevoYaExiste_LanzaInvalidOperation()
    {
        var dte = SeedDte();
        SeedProducto("YA-EXISTE");
        var unidad = _context.CatUnidadesMedida.First();
        var tipoItem = _context.CatTiposItem.First(t => t.Codigo == "1");

        var dto = new MapearDteCompraDto
        {
            SucursalId = _sucursal.Id,
            Items = new List<MapearItemDto>
            {
                new()
                {
                    DescripcionDte = "Quiero crear con codigo existente",
                    MontoDte = 226m,
                    Accion = AccionMapeoItem.ProductoNuevo,
                    ProductoNuevo = new ProductoNuevoMapeoDto
                    {
                        Codigo = "YA-EXISTE",
                        Nombre = "Conflicto",
                        CatTipoItemId = tipoItem.Id,
                        CatUnidadMedidaId = unidad.Id
                    },
                    Cantidad = 4m,
                    BodegaId = _bodega.Id,
                    CostoUnitario = 50m
                }
            }
        };

        var act = async () => await _service.MapearYCrearCompraAsync(dte.Id, dto, _emisor.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ya existen*");
    }

    [Fact]
    public async Task Mapear_DteYaVinculado_LanzaInvalidOperation()
    {
        var dte = SeedDte(estado: EstadoDteRecibido.VINCULADO);
        var producto = SeedProducto("PROD-005");

        var dto = new MapearDteCompraDto
        {
            SucursalId = _sucursal.Id,
            Items = new List<MapearItemDto>
            {
                new()
                {
                    DescripcionDte = "x",
                    MontoDte = 226m,
                    Accion = AccionMapeoItem.ProductoExistente,
                    ProductoId = producto.Id,
                    Cantidad = 4m,
                    BodegaId = _bodega.Id,
                    CostoUnitario = 50m
                }
            }
        };

        var act = async () => await _service.MapearYCrearCompraAsync(dte.Id, dto, _emisor.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ya está vinculado*");
    }

    [Fact]
    public async Task Mapear_SucursalDeOtroEmisor_LanzaKeyNotFound()
    {
        var dte = SeedDte();
        var producto = SeedProducto("PROD-006");

        var otroEmisor = new Emisor
        {
            Nit = "00000000000003",
            Nrc = "3",
            NombreRazonSocial = "OTRO",
            CodigoActividad = "00000",
            DescripcionActividad = "x",
            CorreoElectronico = "o3@x.com",
            Telefono = "0",
            CatDepartamentoId = _emisor.CatDepartamentoId,
            CatMunicipioId = _emisor.CatMunicipioId,
            Direccion = "x"
        };
        _context.Emisores.Add(otroEmisor);
        _context.SaveChanges();
        var sucursalAjena = new Sucursal
        {
            EmisorId = otroEmisor.Id,
            Codigo = "X001",
            Nombre = "Sucursal ajena",
            Direccion = "x",
            CatDepartamentoId = _emisor.CatDepartamentoId,
            CatMunicipioId = _emisor.CatMunicipioId,
            CodigoEstablecimiento = "9999"
        };
        _context.Sucursales.Add(sucursalAjena);
        _context.SaveChanges();

        var dto = new MapearDteCompraDto
        {
            SucursalId = sucursalAjena.Id,
            Items = new List<MapearItemDto>
            {
                new()
                {
                    DescripcionDte = "x",
                    MontoDte = 226m,
                    Accion = AccionMapeoItem.ProductoExistente,
                    ProductoId = producto.Id,
                    Cantidad = 4m,
                    BodegaId = _bodega.Id,
                    CostoUnitario = 50m
                }
            }
        };

        var act = async () => await _service.MapearYCrearCompraAsync(dte.Id, dto, _emisor.Id);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Sucursal*");
    }

    public void Dispose() => _context.Dispose();
}
