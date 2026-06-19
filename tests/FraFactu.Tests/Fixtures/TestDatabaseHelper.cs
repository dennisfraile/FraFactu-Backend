using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Entities.Catalogos;

namespace FraFactu.Tests.Fixtures;

/// <summary>
/// Helper para crear bases de datos de prueba
/// Cada test crea su propia instancia para evitar conflictos de tracking
/// </summary>
public class TestDatabaseHelper
{
    public static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .EnableSensitiveDataLogging()
            // F4: el provider InMemory no soporta transacciones reales; sin
            // este Ignore lanza al primer BeginTransactionAsync. Para producir
            // PostgreSQL la transaccion sigue siendo real.
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }

    public static void SeedTestData(ApplicationDbContext context)
    {
        SeedCatalogos(context);
        SeedDatosBasicos(context);
        context.SaveChanges();
    }

    private static void SeedCatalogos(ApplicationDbContext context)
    {
        // Departamentos (CAT-12)
        context.CatDepartamentos.AddRange(
            new CatDepartamento { Codigo = "01", Valor = "Ahuachapán" },
            new CatDepartamento { Codigo = "06", Valor = "San Salvador" },
            new CatDepartamento { Codigo = "14", Valor = "La Paz" }
        );

        // Municipios (CAT-13)
        context.CatMunicipios.AddRange(
            new CatMunicipio { Codigo = "0601", Valor = "San Salvador" },
            new CatMunicipio { Codigo = "1401", Valor = "Zacatecoluca" }
        );

        // Tipos de Establecimiento (CAT-09)
        context.CatTiposEstablecimiento.AddRange(
            new CatTipoEstablecimiento { Codigo = "01", Valor = "Casa Matriz" },
            new CatTipoEstablecimiento { Codigo = "02", Valor = "Sucursal" },
            new CatTipoEstablecimiento { Codigo = "20", Valor = "Otro" }
        );

        // Tipos de Documento (CAT-02)
        context.CatTiposDocumento.AddRange(
            new CatTipoDocumento { Codigo = "01", Valor = "Factura" },
            new CatTipoDocumento { Codigo = "03", Valor = "Comprobante de Crédito Fiscal" },
            new CatTipoDocumento { Codigo = "14", Valor = "Factura de Sujeto Excluido" }
        );

        // Tipos de Documento de Identificación (CAT-17)
        context.CatDocsIdentidadReceptor.AddRange(
            new CatTipoDocumentoIdentificacionReceptor { Codigo = "36", Valor = "NIT" },
            new CatTipoDocumentoIdentificacionReceptor { Codigo = "13", Valor = "DUI" },
            new CatTipoDocumentoIdentificacionReceptor { Codigo = "37", Valor = "Otro" }
        );

        // Unidades de Medida (CAT-14)
        context.CatUnidadesMedida.AddRange(
            new CatUnidadMedida { Codigo = "59", Valor = "Unidad" },
            new CatUnidadMedida { Codigo = "99", Valor = "Otros" }
        );

        // Tipos de Item (CAT-15)
        context.CatTiposItem.AddRange(
            new CatTipoItem { Codigo = "1", Valor = "Bienes" },
            new CatTipoItem { Codigo = "2", Valor = "Servicios" },
            new CatTipoItem { Codigo = "3", Valor = "Ambos" }
        );

        // Formas de Pago (CAT-23)
        context.CatFormasPago.AddRange(
            new CatFormaPago { Codigo = "01", Valor = "Billetes y monedas" },
            new CatFormaPago { Codigo = "02", Valor = "Tarjeta Débito/Crédito" },
            new CatFormaPago { Codigo = "03", Valor = "Cheque" }
        );

        // Condiciones de Operación (CAT-20)
        context.CatCondicionesOperacion.AddRange(
            new CatCondicionOperacion { Codigo = "1", Valor = "Contado" },
            new CatCondicionOperacion { Codigo = "2", Valor = "Crédito" },
            new CatCondicionOperacion { Codigo = "3", Valor = "Otro" }
        );

        // Tipos de Invalidación (CAT-26)
        context.CatTiposInvalidacion.AddRange(
            new CatTipoInvalidacion { Codigo = "01", Valor = "Anulación por defecto del emisor" },
            new CatTipoInvalidacion { Codigo = "02", Valor = "Devolución de bienes" },
            new CatTipoInvalidacion { Codigo = "03", Valor = "Rescisión de contrato" }
        );

        // Modelos de Facturación (CAT-03)
        context.CatModelosFacturacion.AddRange(
            new CatModeloFacturacion { Codigo = "1", Valor = "Modelo Previo" },
            new CatModeloFacturacion { Codigo = "2", Valor = "Modelo Diferido" }
        );

        // Tipos de Transmisión (CAT-04)
        context.CatTiposTransmision.AddRange(
            new CatTipoTransmision { Codigo = "1", Valor = "Normal" },
            new CatTipoTransmision { Codigo = "2", Valor = "Contingencia" }
        );

        // Monedas (CAT-22)
        context.CatMonedas.AddRange(
            new CatMoneda { Codigo = "USD", Valor = "Dólar" }
        );

        // Ambientes (CAT-01)
        context.CatAmbientes.AddRange(
            new CatAmbienteDestino { Codigo = "00", Valor = "Pruebas" },
            new CatAmbienteDestino { Codigo = "01", Valor = "Producción" }
        );
    }

    private static void SeedDatosBasicos(ApplicationDbContext context)
    {
        // Guardar catálogos primero para obtener sus IDs
        context.SaveChanges();

        // Roles del sistema
        context.Roles.AddRange(
            new Rol { Nombre = "SuperAdmin" },
            new Rol { Nombre = "EmisorAdmin" },
            new Rol { Nombre = "GerenteSucursal" },
            new Rol { Nombre = "Cajero" },
            new Rol { Nombre = "Contador" },
            new Rol { Nombre = "Vendedor" }
        );
        context.SaveChanges();

        // Ahora obtener los IDs generados
        var depto = context.CatDepartamentos.First(d => d.Codigo == "06");
        var municipio = context.CatMunicipios.First(m => m.Codigo == "0601");
        var tipoEst = context.CatTiposEstablecimiento.First(t => t.Codigo == "01");
        var ambiente = context.CatAmbientes.First(a => a.Codigo == "00");

        // Emisor de prueba
        var emisor = new Emisor
        {
            Nit = "06140506141011",
            Nrc = "123456-7",
            NombreRazonSocial = "EMPRESA DE PRUEBAS SA DE CV",
            NombreComercial = "PRUEBAS SA",
            CodigoActividad = "47111",
            DescripcionActividad = "Venta al por menor en comercios no especializados",
            CatTipoEstablecimientoId = tipoEst.Id,
            CatAmbienteDestinoId = ambiente.Id,
            CorreoElectronico = "test@empresa.com",
            Telefono = "22223333",
            CatDepartamentoId = depto.Id,
            CatMunicipioId = municipio.Id,
            Direccion = "Colonia Escalón, San Salvador"
        };
        context.Emisores.Add(emisor);
        context.SaveChanges();

        // Sucursal de prueba
        var sucursal = new Sucursal
        {
            EmisorId = emisor.Id,
            Codigo = "0001",
            Nombre = "Sucursal Principal",
            Direccion = "Colonia Escalón, San Salvador",
            CatDepartamentoId = depto.Id,
            CatMunicipioId = municipio.Id,
            CatTipoEstablecimientoId = tipoEst.Id,
            CodigoEstablecimiento = "0001"
        };
        context.Sucursales.Add(sucursal);
        context.SaveChanges();

        // Bodega de prueba
        var bodega = new Bodega
        {
            Codigo = "BOD001",
            Nombre = "Bodega Principal",
            Direccion = "San Salvador",
            SucursalId = sucursal.Id,
            EsPrincipal = true,
            Activa = true
        };
        context.Bodegas.Add(bodega);

        // Categoría de prueba para productos
        var categoria = new Categoria
        {
            Nombre = "General",
            Descripcion = "Categoría general"
        };
        context.Categorias.Add(categoria);
    }
}
