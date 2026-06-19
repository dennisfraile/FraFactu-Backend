using Xunit;
using FluentAssertions;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;

namespace FraFactu.Tests.UnitTests.Entities;

public class DteRecibidoTests
{
    [Fact]
    public void PuedeCrearDteRecibido_ConCamposRequeridos()
    {
        // Arrange & Act
        var dte = new DteRecibido
        {
            EmisorId = 1,
            CodigoGeneracion = "A1B2C3D4-E5F6-7890-ABCD-EF1234567890",
            TipoDte = "01",
            FechaEmision = new DateTime(2026, 3, 15),
            EmisorNit = "06141804941035",
            EmisorNombre = "Proveedor Test S.A. de C.V.",
            JsonDte = "{}",
            Total = 282.50m
        };

        // Assert
        dte.EmisorId.Should().Be(1);
        dte.CodigoGeneracion.Should().Be("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");
        dte.TipoDte.Should().Be("01");
        dte.FechaEmision.Should().Be(new DateTime(2026, 3, 15));
        dte.EmisorNit.Should().Be("06141804941035");
        dte.EmisorNombre.Should().Be("Proveedor Test S.A. de C.V.");
        dte.JsonDte.Should().Be("{}");
        dte.Total.Should().Be(282.50m);
    }

    [Fact]
    public void EstadoPorDefecto_EsPendiente()
    {
        // Arrange & Act
        var dte = new DteRecibido();

        // Assert
        dte.Estado.Should().Be(EstadoDteRecibido.PENDIENTE);
    }

    [Fact]
    public void CompraExternaId_EsNullablePorDefecto()
    {
        // Arrange & Act
        var dte = new DteRecibido();

        // Assert
        dte.CompraExternaId.Should().BeNull();
    }

    [Fact]
    public void ActivoPorDefecto_EsTrue()
    {
        // Arrange & Act
        var dte = new DteRecibido();

        // Assert
        dte.Activo.Should().BeTrue();
    }
}
