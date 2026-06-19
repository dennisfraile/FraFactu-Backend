using FraFactu.Application.DTOs.Hacienda;
using FraFactu.Application.Services;
using FraFactu.Application.Interfaces.Repositories;
using System.Text.Json;

namespace FraFactu.Tests.Integration;

/// <summary>
/// Test de integración para verificar serialización JSON de DTEs
/// </summary>
public class DteJsonSerializationTests
{
    /// <summary>
    /// Crea un DTE de ejemplo con datos realistas
    /// </summary>
    private DteBaseDto CreateSampleDte()
    {
        return new DteBaseDto
        {
            Identificacion = new IdentificacionDto
            {
                Version = 3,
                // Ambiente se obtiene del emisor, no se envía en el DTO
                TipoDte = "03", // CCF
                NumeroControl = "DTE-03-00000001-000000000000001",
                CodigoGeneracion = "37B9E9E1-CD13-4C73-A6B7-8B3F0E4A7219",
                TipoModelo = 1,
                TipoOperacion = 1,
                FecEmi = "2026-01-06",
                HorEmi = "14:30:00",
                TipoMoneda = "USD"
            },

            Emisor = new EmisorDto
            {
                Nit = "06140506011234",
                Nrc = "123456",
                Nombre = "EMPRESA DE PRUEBA S.A. DE C.V.",
                CodActividad = "62010",
                DescActividad = "Desarrollo de software",
                NombreComercial = "Tech Solutions",
                TipoEstablecimiento = "01",
                Direccion = new DireccionDto
                {
                    Departamento = "06", // San Salvador
                    Municipio = "14", // San Salvador
                    Complemento = "Colonia Escalón, Calle Principal #123"
                },
                Telefonos = "2222-3333",
                Correo = "facturacion@empresa.com"
            },

            Receptor = new ReceptorDto
            {
                Nit = "06140506019999",
                Nrc = "654321",
                Nombre = "CLIENTE EJEMPLO S.A. DE C.V.",
                CodActividad = "47110",
                DescActividad = "Venta al por menor",
                Direccion = new DireccionDto
                {
                    Departamento = "06",
                    Municipio = "14",
                    Complemento = "Zona Industrial, Avenida Norte #456"
                }
            },

            CuerpoDocumento = new List<CuerpoDocumentoDto>
            {
                new CuerpoDocumentoDto
                {
                    NumItem = 1,
                    TipoItem = 2, // Servicio - FK a CatTipoItem
                    Cantidad = 10,
                    Codigo = "SERV-001",
                    UniMedida = 59, // Unidad - FK a CatUnidadMedida
                    Descripcion = "Servicio de consultoría técnica",
                    PrecioUni = 50.00,
                    MontoDescu = 0.00,
                    VentaNoSuj = 0.00,
                    VentaExenta = 0.00,
                    VentaGravada = 500.00,
                    Psv = 0.00,
                    NoGravado = 0.00,
                    IvaItem = 65.00 // Campo calculado interno
                }
            },

            Resumen = new ResumenDto
            {
                TotalNoSuj = 0.00,
                TotalExenta = 0.00,
                TotalGravada = 500.00,
                SubTotalVentas = 500.00,
                DescuNoSuj = 0.00,
                DescuExenta = 0.00,
                DescuGravada = 0.00,
                PorcentajeDescuento = 0.00,
                TotalDescu = 0.00,
                Tributos = new List<TributoResumenDto>
                {
                    new TributoResumenDto
                    {
                        Codigo = "20",
                        Descripcion = "Impuesto al Valor Agregado 13%",
                        Valor = 65.00
                    }
                },
                SubTotal = 500.00,
                IvaPerci1 = 0.00,
                IvaRete1 = 0.00,
                ReteRenta = 0.00,
                MontoTotalOperacion = 565.00,
                TotalNoGravado = 0.00,
                TotalPagar = 565.00,
                TotalLetras = "QUINIENTOS SESENTA Y CINCO DOLARES 00/100",
                SaldoFavor = 0.00,
                CondicionOperacion = 1, // Contado - FK a CatCondicionOperacion
                Pagos = new List<PagoDto>
                {
                    new PagoDto
                    {
                        Codigo = "05", // Transferencia - string format required by MH
                        MontoPago = 565.00,
                        Referencia = "TRANS-20260106-001",
                        Plazo = null, // Contado, no hay plazo
                        Periodo = null
                    }
                },
                NumPagoElectronico = "PE-20260106-001" // Campo interno, NO se envía a MH
            },

            Extension = new ExtensionDto
            {
                NombEntrega = "Juan Pérez",
                DocuEntrega = "01234567-8",
                NombRecibe = "María García",
                DocuRecibe = "98765432-1",
                Observaciones = "Entrega inmediata",
                PlacaVehiculo = null
            }
        };
    }

    /// <summary>
    /// Test: Verificar que la serialización produce JSON válido
    /// </summary>
    public void Test_DteSerializationProducesValidJson()
    {
        // Arrange
        var mockRepo = new MockCatalogRepository();
        var mapper = new DteJsonMapperService(mockRepo);
        var sampleDte = CreateSampleDte();

        // Act
        var mhJsonObject = mapper.MapToMhJson(sampleDte);
        var jsonString = JsonSerializer.Serialize(mhJsonObject, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        // Assert
        Console.WriteLine("=== JSON SERIALIZADO PARA MH ===");
        Console.WriteLine(jsonString);
        Console.WriteLine("\n=== VERIFICACIONES ===");

        // Verificar que contiene las secciones principales
        Assert.Contains("\"identificacion\"", jsonString);
        Assert.Contains("\"emisor\"", jsonString);
        Assert.Contains("\"receptor\"", jsonString);
        Assert.Contains("\"cuerpoDocumento\"", jsonString);
        Assert.Contains("\"resumen\"", jsonString);

        // Verificar conversiones FK → Codigo
        Assert.Contains("\"condicionOperacion\": \"1\"", jsonString); // int 1 → "1"
        Assert.Contains("\"codigo\": \"05\"", jsonString); // FK 5 → "05"

        // Verificar que campos internos NO están presentes
        Assert.DoesNotContain("\"numPagoElectronico\"", jsonString);
        Assert.DoesNotContain("\"ivaItem\"", jsonString);

        Console.WriteLine("✅ Todas las verificaciones pasaron");
    }
}

/// <summary>
/// Mock del repositorio de catálogos para testing
/// </summary>
public class MockCatalogRepository : ICatalogRepository
{
    public string GetFormaPagoCodigo(int id)
    {
        // Simulamos los catálogos más comunes
        return id switch
        {
            1 => "01", // Billetes y monedas
            2 => "02", // Tarjeta débito
            3 => "03", // Tarjeta crédito
            4 => "04", // Cheque
            5 => "05", // Transferencia
            _ => id.ToString("D2")
        };
    }

    public string? GetPlazoCodigo(int? id)
    {
        if (!id.HasValue) return null;
        return id switch
        {
            1 => "01", // Días
            2 => "02", // Meses
            3 => "03", // Años
            _ => id.Value.ToString("D2")
        };
    }

    public string GetCondicionOperacionCodigo(int id)
    {
        return id.ToString(); // 1, 2, 3
    }

    public int GetFormaPagoId(string codigo) => int.Parse(codigo);
    public int? GetPlazoId(string? codigo) => codigo != null ? int.Parse(codigo) : null;
    public int GetCondicionOperacionId(string codigo) => int.Parse(codigo);
}
