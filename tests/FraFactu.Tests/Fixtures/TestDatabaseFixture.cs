using Microsoft.EntityFrameworkCore;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Entities.Catalogos;

namespace FraFactu.Tests.Fixtures;

/// <summary>
/// Fixture para crear una base de datos en memoria compartida entre tests
/// Incluye seed de catálogos MH y datos básicos de negocio
/// </summary>
public class TestDatabaseFixture : IDisposable
{
    public ApplicationDbContext Context { get; private set; }
    private bool _isSeeded = false;

    public TestDatabaseFixture()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .EnableSensitiveDataLogging()
            .Options;

        Context = new ApplicationDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void SeedData()
    {
        // Solo seed una vez using flag
        if (_isSeeded) return;

        SeedCatalogos();
        SeedDatosBasicos();
        Context.SaveChanges();

        _isSeeded = true;
    }

    private void SeedCatalogos()
    {
        // Departamentos (CAT-12)
        Context.CatDepartamentos.AddRange(
            new CatDepartamento { Id = 1, Codigo = "01", Valor = "Ahuachapán" },
            new CatDepartamento { Id = 2, Codigo = "06", Valor = "San Salvador" },
            new CatDepartamento { Id = 3, Codigo = "14", Valor = "La Paz" }
        );

        // Municipios (CAT-13)
        Context.CatMunicipios.AddRange(
            new CatMunicipio { Id = 1, Codigo = "0601", Valor = "San Salvador" },
            new CatMunicipio { Id = 2, Codigo = "1401", Valor = "Zacatecoluca" }
        );

        // Tipos de Establecimiento (CAT-09)
        Context.CatTiposEstablecimiento.AddRange(
            new CatTipoEstablecimiento { Id = 1, Codigo = "01", Valor = "Casa Matriz" },
            new CatTipoEstablecimiento { Id = 2, Codigo = "02", Valor = "Sucursal" },
            new CatTipoEstablecimiento { Id = 3, Codigo = "20", Valor = "Otro" }
        );

        // Tipos de Documento (CAT-02)
        Context.CatTiposDocumento.AddRange(
            new CatTipoDocumento { Id = 1, Codigo = "01", Valor = "Factura" },
            new CatTipoDocumento { Id = 2, Codigo = "03", Valor = "Comprobante de Crédito Fiscal" },
            new CatTipoDocumento { Id = 3, Codigo = "14", Valor = "Factura de Sujeto Excluido" }
        );

        // Tipos de Documento de Identificación (CAT-17)
        Context.CatDocsIdentidadReceptor.AddRange(
            new CatTipoDocumentoIdentificacionReceptor { Id = 1, Codigo = "36", Valor = "NIT" },
            new CatTipoDocumentoIdentificacionReceptor { Id = 2, Codigo = "13", Valor = "DUI" },
            new CatTipoDocumentoIdentificacionReceptor { Id = 3, Codigo = "37", Valor = "Otro" }
        );

        // Unidades de Medida (CAT-14)
        Context.CatUnidadesMedida.AddRange(
            new CatUnidadMedida { Id = 1, Codigo = "59", Valor = "Unidad" },
            new CatUnidadMedida { Id = 2, Codigo = "99", Valor = "Otros" }
        );

        // Tipos de Item (CAT-15)
        Context.CatTiposItem.AddRange(
            new CatTipoItem { Id = 1, Codigo = "1", Valor = "Bienes" },
            new CatTipoItem { Id = 2, Codigo = "2", Valor = "Servicios" },
            new CatTipoItem { Id = 3, Codigo = "3", Valor = "Ambos" }
        );

        // Formas de Pago (CAT-23)
        Context.CatFormasPago.AddRange(
            new CatFormaPago { Id = 1, Codigo = "01", Valor = "Billetes y monedas" },
            new CatFormaPago { Id = 2, Codigo = "02", Valor = "Tarjeta Débito/Crédito" },
            new CatFormaPago { Id = 3, Codigo = "03", Valor = "Cheque" }
        );

        // Condiciones de Operación (CAT-20)
        Context.CatCondicionesOperacion.AddRange(
            new CatCondicionOperacion { Id = 1, Codigo = "1", Valor = "Contado" },
            new CatCondicionOperacion { Id = 2, Codigo = "2", Valor = "Crédito" },
            new CatCondicionOperacion { Id = 3, Codigo = "3", Valor = "Otro" }
        );

        // Tipos de Invalidación (CAT-26)
        Context.CatTiposInvalidacion.AddRange(
            new CatTipoInvalidacion { Id = 1, Codigo = "01", Valor = "Anulación por defecto del emisor" },
            new CatTipoInvalidacion { Id = 2, Codigo = "02", Valor = "Devolución de bienes" },
            new CatTipoInvalidacion { Id = 3, Codigo = "03", Valor = "Rescisión de contrato" }
        );

        // Modelos de Facturación (CAT-03)
        Context.CatModelosFacturacion.AddRange(
            new CatModeloFacturacion { Id = 1, Codigo = "1", Valor = "Modelo Previo" },
            new CatModeloFacturacion { Id = 2, Codigo = "2", Valor = "Modelo Diferido" }
        );

        // Tipos de Transmisión (CAT-04)
        Context.CatTiposTransmision.AddRange(
            new CatTipoTransmision { Id = 1, Codigo = "1", Valor = "Normal" },
            new CatTipoTransmision { Id = 2, Codigo = "2", Valor = "Contingencia" }
        );

        // Monedas (CAT-22)
        Context.CatMonedas.AddRange(
            new CatMoneda { Id = 1, Codigo = "USD", Valor = "Dólar" }
        );

        // Ambientes (CAT-01)
        Context.CatAmbientes.AddRange(
            new CatAmbienteDestino { Id = 1, Codigo = "00", Valor = "Pruebas" },
            new CatAmbienteDestino { Id = 2, Codigo = "01", Valor = "Producción" }
        );
    }

    private void SeedDatosBasicos()
    {
        // Emisor de prueba
        var emisor = new Emisor
        {
            Id = 1,
            Nit = "06140506141011",
            Nrc = "123456-7",
            NombreRazonSocial = "EMPRESA DE PRUEBAS SA DE CV",
            NombreComercial = "PRUEBAS SA",
            CodigoActividad = "47111",
            DescripcionActividad = "Venta al por menor en comercios no especializados",
            CatTipoEstablecimientoId = 1,
            CorreoElectronico = "test@empresa.com",
            Telefono = "22223333",
            CatDepartamentoId = 2,
            CatMunicipioId = 1,
            Direccion = "Colonia Escalón, San Salvador"
        };
        Context.Emisores.Add(emisor);

        // Sucursal de prueba
        var sucursal = new Sucursal
        {
            Id = 1,
            EmisorId = 1,
            Codigo = "0001",
            Nombre = "Sucursal Principal",
            Direccion = "Colonia Escalón, San Salvador",
            CatDepartamentoId = 2,
            CatMunicipioId = 1,
            CatTipoEstablecimientoId = 1,
            CodigoEstablecimiento = "0001"
        };
        Context.Sucursales.Add(sucursal);

        // Bodega de prueba
        var bodega = new Bodega
        {
            Id = 1,
            Codigo = "BOD001",
            Nombre = "Bodega Principal",
            Direccion = "San Salvador",
            SucursalId = 1,
            EsPrincipal = true,
            Activa = true
        };
        Context.Bodegas.Add(bodega);
    }

    public void Dispose()
    {
        Context?.Dispose();
    }
}
