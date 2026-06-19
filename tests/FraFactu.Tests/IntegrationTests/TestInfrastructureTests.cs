using FraFactu.Tests.Fixtures;
using FluentAssertions;

namespace FraFactu.Tests.IntegrationTests;

/// <summary>
/// Test de ejemplo para verificar que la infraestructura de testing funciona correctamente
/// </summary>
public class TestInfrastructureTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDataBuilder _dataBuilder;

    public TestInfrastructureTests(TestDatabaseFixture fixture)
    {
        _dataBuilder = new TestDataBuilder();
    }

    [Fact]
    public void TestDataBuilder_ShouldGenerateNit()
    {
        // Act
        var nit = _dataBuilder.GenerarNit();

        // Assert
        nit.Should().NotBeNullOrEmpty();
        nit.Should().Contain("-");
    }

    [Fact]
    public void TestDataBuilder_ShouldGenerateEmail()
    {
        // Act
        var email = _dataBuilder.GenerarEmail();

        // Assert
        email.Should().NotBeNullOrEmpty();
        email.Should().Contain("@");
    }

    [Fact]
    public void TestDataBuilder_ShouldGeneratePrecio()
    {
        // Act
        var precio = _dataBuilder.GenerarPrecio();

        // Assert
        precio.Should().BeGreaterThan(0);
    }
}
