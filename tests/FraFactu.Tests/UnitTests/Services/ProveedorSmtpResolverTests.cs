using FraFactu.Infrastructure.Services;
using Xunit;

namespace FraFactu.Tests.UnitTests.Services;

/// <summary>
/// Mapeo de hosts MX → SMTP saliente del proveedor. Lógica pura (sin red):
/// valida que dominios alojados en Google/Microsoft/Yahoo/Zoho se resuelvan
/// al SMTP correcto independientemente del nombre del dominio.
/// </summary>
public class ProveedorSmtpResolverTests
{
    [Theory]
    [InlineData("aspmx.l.google.com", "smtp.gmail.com", 587)]
    [InlineData("alt1.aspmx.l.google.com", "smtp.gmail.com", 587)]
    [InlineData("empresa-com.mail.protection.outlook.com", "smtp.office365.com", 587)]
    [InlineData("mx1.zoho.com", "smtp.zoho.com", 587)]
    public void MapearProveedorPorMx_ReconoceProveedor(string mx, string hostEsperado, int puertoEsperado)
    {
        var resultado = ProveedorSmtpResolver.MapearProveedorPorMx(new[] { mx });

        Assert.NotNull(resultado);
        Assert.Equal(hostEsperado, resultado!.Value.host);
        Assert.Equal(puertoEsperado, resultado.Value.port);
    }

    [Fact]
    public void MapearProveedorPorMx_DominioYahoo_DevuelveSmtpYahoo()
    {
        var resultado = ProveedorSmtpResolver.MapearProveedorPorMx(new[] { "mta5.am0.yahoodns.net" });

        Assert.NotNull(resultado);
        Assert.Equal("smtp.mail.yahoo.com", resultado!.Value.host);
    }

    [Fact]
    public void MapearProveedorPorMx_ProveedorDesconocido_DevuelveNull()
    {
        var resultado = ProveedorSmtpResolver.MapearProveedorPorMx(new[] { "mx.proveedor-desconocido.net" });

        Assert.Null(resultado);
    }

    [Fact]
    public void MapearProveedorPorMx_SinRegistros_DevuelveNull()
    {
        Assert.Null(ProveedorSmtpResolver.MapearProveedorPorMx(Array.Empty<string>()));
    }
}
