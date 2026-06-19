using System.Text.Json;

namespace FraFactu.Application.Examples;

/// <summary>
/// Demo simple para visualizar la salida JSON de la serialización
/// </summary>
public class JsonOutputDemo
{
    /// <summary>
    /// Muestra ejemplo de JSON serializado para un pago
    /// </summary>
    public static string GetPagoJsonExample()
    {
        // Ejemplo: PagoDto con FK int que se convierten a string
        var pagoInterno = new
        {
            Codigo = 5,      // int FK
            MontoPago = 565.00,
            Referencia = "TRANS-001",
            Plazo = (int?)null,
            Periodo = (decimal?)null
        };

        // Después del mapper se convierte a:
        var pagoMH = new
        {
            codigo = "05",   // string del catálogo
            montoPago = 565.00,
            referencia = "TRANS-001",
            plazo = (string?)null,
            periodo = (decimal?)null
        };

        var json = JsonSerializer.Serialize(pagoMH, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        return json;
    }

    /// <summary>
    /// Muestra ejemplo completo de resumen con conversiones
    /// </summary>
    public static string GetResumenJsonExample()
    {
        var resumenMH = new
        {
            totalGravada = 500.00,
            subTotal = 500.00,
            tributos = new[]
            {
                new { codigo = "20", descripcion = "IVA 13%", valor = 65.00 }
            },
            ivaRete1 = 0.00,
            totalPagar = 565.00,
            totalLetras = "QUINIENTOS SESENTA Y CINCO DOLARES",
            condicionOperacion = "1",  // Convertido de int 1 → string "1"
            pagos = new[]
            {
                new
                {
                    codigo = "05",     // Convertido de int 5 → string "05"
                    montoPago = 565.00,
                    referencia = "TRANS-001",
                    plazo = (string?)null,
                    periodo = (decimal?)null
                }
            }
            // numPagoElectronico NO está incluido (campo interno)
        };

        var json = JsonSerializer.Serialize(resumenMH, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        });

        return json;
    }

    /// <summary>
    /// Ejemplo de DTE completo serializado
    /// </summary>
    public static string GetDteCompletoExample()
    {
        var dteMH = new
        {
            identificacion = new
            {
                version = "3",
                ambiente = "00",
                tipoDte = "03",
                numeroControl = "DTE-03-00000001-000000000000001",
                codigoGeneracion = "37B9E9E1-CD13-4C73-A6B7-8B3F0E4A7219",
                tipoModelo = 1,
                tipoOperacion = 1,
                fecEmi = "2026-01-06",
                horEmi = "14:30:00",
                tipoMoneda = "USD"
            },
            emisor = new
            {
                nit = "06140506011234",
                nrc = "123456",
                nombre = "EMPRESA DE PRUEBA S.A. DE C.V.",
                nombreComercial = "Tech Solutions",
                codActividad = "62010",
                descActividad = "Desarrollo de software",
                direccion = new
                {
                    departamento = "06",
                    municipio = "14",
                    complemento = "Colonia Escalón, Calle Principal #123"
                },
                telefono = "2222-3333",
                correo = "facturacion@empresa.com",
                tipoEstablecimiento = "01"
            },
            receptor = new
            {
                nit = "06140506019999",
                nrc = "654321",
                nombre = "CLIENTE EJEMPLO S.A. DE C.V.",
                codActividad = "47110",
                descActividad = "Venta al por menor",
                direccion = new
                {
                    departamento = "06",
                    municipio = "14",
                    complemento = "Zona Industrial, Avenida Norte #456"
                }
            },
            cuerpoDocumento = new[]
            {
                new
                {
                    numItem = 1,
                    tipoItem = 2,        // MH usa integer directamente
                    cantidad = 10.0,
                    codigo = "SERV-001",
                    uniMedida = 59,      // MH usa integer directamente
                    descripcion = "Servicio de consultoría técnica",
                    precioUni = 50.00,
                    montoDescu = 0.00,
                    ventaNoSuj = 0.00,
                    ventaExenta = 0.00,
                    ventaGravada = 500.00
                }
            },
            resumen = new
            {
                totalGravada = 500.00,
                subTotalVentas = 500.00,
                totalDescu = 0.00,
                subTotal = 500.00,
                tributos = new[] { new { codigo = "20", descripcion = "IVA 13%", valor = 65.00 } },
                ivaPerci1 = 0.00,
                montoTotalOperacion = 565.00,
                totalPagar = 565.00,
                totalLetras = "QUINIENTOS SESENTA Y CINCO DOLARES",
                condicionOperacion = "1",  // ← Convertido: int → string
                pagos = new[]
                {
                    new
                    {
                        codigo = "05",     // ← Convertido: int 5 → string "05"
                        montoPago = 565.00,
                        referencia = "TRANS-20260106-001"
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(dteMH, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        });

        return json;
    }
}
