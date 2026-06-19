using Bogus;

namespace FraFactu.Tests.Fixtures;

/// <summary>
/// Builder para crear datos de prueba usando Bogus (Faker)
/// VERSIÓN ULTRA-SIMPLIFICADA - Solo genera data básica
/// Las entidades se configurarán manualmente en cada test según sea necesario
/// </summary>
public class TestDataBuilder
{
    private readonly Faker _faker;

    public TestDataBuilder()
    {
        Randomizer.Seed = new Random(123); // Seed fijo para resultados reproducibles
        _faker = new Faker("es_MX");
    }

    /// <summary>
    /// Genera un NIT aleatorio válido
    /// </summary>
    public string GenerarNit() => _faker.Random.Replace("####-######-###-#");

    /// <summary>
    /// Genera un NRC aleatorio válido
    /// </summary>
    public string GenerarNrc() => _faker.Random.Replace("######-#");

    /// <summary>
    /// Genera un nombre de compañía
    /// </summary>
    public string GenerarNombreCompania() => _faker.Company.CompanyName();

    /// <summary>
    /// Genera un email
    /// </summary>
    public string GenerarEmail() => _faker.Internet.Email();

    /// <summary>
    /// Genera un teléfono
    /// </summary>
    public string GenerarTelefono() => _faker.Phone.PhoneNumber("####-####");

    /// <summary>
    /// Genera una dirección
    /// </summary>
    public string GenerarDireccion() => _faker.Address.FullAddress();

    /// <summary>
    /// Genera un código de producto
    /// </summary>
    public string GenerarCodigoProducto() => _faker.Random.Replace("PROD-####");

    /// <summary>
    /// Genera un nombre de producto
    /// </summary>
    public string GenerarNombreProducto() => _faker.Commerce.ProductName();

    /// <summary>
    /// Genera un precio aleatorio
    /// </summary>
    public decimal GenerarPrecio(decimal min = 1, decimal max = 1000) => _faker.Finance.Amount(min, max);
}
