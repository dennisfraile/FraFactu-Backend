using FraFactu.Domain.Entities;
using FraFactu.Domain.Entities.Catalogos;
using FraFactu.Infrastructure.Services;
using Xunit;

namespace FraFactu.Tests.UnitTests.Services;

/// <summary>
/// Cobertura de los helpers que arman el JSON del DTE en <see cref="FacturaService"/>,
/// nacidos del rechazo MH [096]:
///  - <c>NormalizarNumDocumentoReceptor</c>: el numDocumento debe respetar el formato del
///    tipoDocumento (NIT sin guiones, DUI solo dígitos sin guion — cambio post-DTE-v2 MH
///    2026-06-10: el guion del DUI dejó de ser aceptado).
///  - <c>ConstruirDireccionEmisor</c>: la triada departamento/municipio/distrito (CAT-008)
///    debe tomarse de una sola fuente, sin mezclar sucursal y emisor.
/// </summary>
public class FacturaServiceDteJsonHelpersTests
{
    // ---------------- NormalizarNumDocumentoReceptor ----------------

    [Theory]
    // NIT (36): solo dígitos, sin guiones
    [InlineData("36", "0614-251090-101-1", "06142510901011")]
    [InlineData("36", "06142510901011", "06142510901011")]
    // DUI (13): solo 9 dígitos sin guion (MH v2 rechaza el guion desde 2026-06-10)
    [InlineData("13", "012345678", "012345678")]
    [InlineData("13", "01234567-8", "012345678")]
    // Otros tipos: sin transformar (trim)
    [InlineData("03", "A1234567", "A1234567")]
    [InlineData("37", "  XYZ-99 ", "XYZ-99")]
    public void NormalizarNumDocumentoReceptor_FormateaSegunTipo(string tipo, string entrada, string esperado)
    {
        Assert.Equal(esperado, FacturaService.NormalizarNumDocumentoReceptor(tipo, entrada));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizarNumDocumentoReceptor_VacioODlNull_DevuelveNull(string? entrada)
    {
        // MH fe-f-v2 declara numDocumento como ["string","null"] con minLength=1.
        // Si llegan vacios o whitespace, el unico valor valido del payload es null
        // (NO string vacio "" que rebotaria [096] "no cumple el tamaño minimo permitido").
        Assert.Null(FacturaService.NormalizarNumDocumentoReceptor("36", entrada));
    }

    [Fact]
    public void NormalizarNumDocumentoReceptor_Dui_SinNueveDigitos_NoInventaFormato()
    {
        // 8 dígitos: no formatea (deja que MH detalle el error en vez de enviar algo a medias)
        Assert.Equal("12345678", FacturaService.NormalizarNumDocumentoReceptor("13", "12345678"));
    }

    // ---------------- NormalizarTipoDocumentoReceptor ----------------

    [Theory]
    [InlineData("36", "06142510901011", "36")]
    [InlineData("13", "012345678", "13")]
    [InlineData("37", "ABC123", "37")]
    public void NormalizarTipoDocumentoReceptor_ConNumeroDocumento_DevuelveTipo(
        string tipo, string num, string esperado)
    {
        Assert.Equal(esperado, FacturaService.NormalizarTipoDocumentoReceptor(tipo, num));
    }

    [Theory]
    [InlineData("36", null)]
    [InlineData("13", "")]
    [InlineData("37", "   ")]
    public void NormalizarTipoDocumentoReceptor_SinNumeroDocumento_DevuelveNull(string tipo, string? num)
    {
        // Sin documento → MH exige que ambos campos vayan null (no hay sentido en
        // "DUI sin numero"). Cubre el caso "Sin documento" del FE.
        Assert.Null(FacturaService.NormalizarTipoDocumentoReceptor(tipo, num));
    }

    [Theory]
    [InlineData(null, "012345678")]
    [InlineData("", "012345678")]
    public void NormalizarTipoDocumentoReceptor_SinTipoConNumero_DevuelveNull(string? tipo, string num)
    {
        // Inverso: si vino numero pero no tipo, no podemos inventarlo —
        // mandamos ambos null y MH valida el resto del receptor.
        Assert.Null(FacturaService.NormalizarTipoDocumentoReceptor(tipo, num));
    }

    // ---------------- ConstruirDireccionEmisor ----------------

    private static string? Prop(object o, string nombre) =>
        (string?)o.GetType().GetProperty(nombre)!.GetValue(o);

    private static Emisor EmisorBase() => new()
    {
        Departamento = new CatDepartamento { Codigo = "06" },
        Municipio = new CatMunicipio { Codigo = "14" },
        Distrito = new CatDistrito { Codigo = "01" },
        Direccion = "Dir Emisor"
    };

    [Fact]
    public void ConstruirDireccionEmisor_SucursalCompleta_UsaSucursal()
    {
        var factura = new FacturaElectronica
        {
            Emisor = EmisorBase(),
            Sucursal = new Sucursal
            {
                Departamento = new CatDepartamento { Codigo = "01" },
                Municipio = new CatMunicipio { Codigo = "02" },
                Distrito = new CatDistrito { Codigo = "03" },
                Direccion = "Dir Sucursal"
            }
        };

        var dir = FacturaService.ConstruirDireccionEmisor(factura);

        Assert.Equal("01", Prop(dir, "departamento"));
        Assert.Equal("02", Prop(dir, "municipio"));
        Assert.Equal("03", Prop(dir, "distrito"));
        Assert.Equal("Dir Sucursal", Prop(dir, "complemento"));
    }

    [Fact]
    public void ConstruirDireccionEmisor_SucursalSinDistrito_NoMezcla_UsaEmisorEntero()
    {
        // Caso del bug: sucursal con depto/municipio pero sin distrito CAT-008.
        var factura = new FacturaElectronica
        {
            Emisor = EmisorBase(),
            Sucursal = new Sucursal
            {
                Departamento = new CatDepartamento { Codigo = "01" },
                Municipio = new CatMunicipio { Codigo = "02" },
                Distrito = null,
                Direccion = "Dir Sucursal"
            }
        };

        var dir = FacturaService.ConstruirDireccionEmisor(factura);

        // Debe venir TODO del emisor; nunca depto/municipio de sucursal + distrito de emisor.
        Assert.Equal("06", Prop(dir, "departamento"));
        Assert.Equal("14", Prop(dir, "municipio"));
        Assert.Equal("01", Prop(dir, "distrito"));
        Assert.Equal("Dir Emisor", Prop(dir, "complemento"));
    }

    [Fact]
    public void ConstruirDireccionEmisor_SinSucursal_UsaEmisor()
    {
        var factura = new FacturaElectronica { Emisor = EmisorBase(), Sucursal = null };

        var dir = FacturaService.ConstruirDireccionEmisor(factura);

        Assert.Equal("06", Prop(dir, "departamento"));
        Assert.Equal("14", Prop(dir, "municipio"));
        Assert.Equal("01", Prop(dir, "distrito"));
        Assert.Equal("Dir Emisor", Prop(dir, "complemento"));
    }

    [Fact]
    public void ConstruirDireccionEmisor_SucursalCompletaSinComplemento_CaeAlDelEmisor()
    {
        var factura = new FacturaElectronica
        {
            Emisor = EmisorBase(),
            Sucursal = new Sucursal
            {
                Departamento = new CatDepartamento { Codigo = "01" },
                Municipio = new CatMunicipio { Codigo = "02" },
                Distrito = new CatDistrito { Codigo = "03" },
                Direccion = ""
            }
        };

        var dir = FacturaService.ConstruirDireccionEmisor(factura);

        Assert.Equal("03", Prop(dir, "distrito"));
        Assert.Equal("Dir Emisor", Prop(dir, "complemento"));
    }
}
