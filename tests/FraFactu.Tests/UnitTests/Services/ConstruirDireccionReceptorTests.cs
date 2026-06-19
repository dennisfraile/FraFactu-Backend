using FluentAssertions;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Entities.Catalogos;
using FraFactu.Infrastructure.Services;
using Xunit;

namespace FraFactu.Tests.UnitTests.Services;

public class ConstruirDireccionReceptorTests
{
    private static string? Prop(object o, string nombre) =>
        (string?)o.GetType().GetProperty(nombre)!.GetValue(o);

    private static Receptor SeedReceptor(
        int? depId = null, string? depCod = null,
        int? muniId = null, string? muniCod = null,
        int? disId = null, string? disCod = null,
        string? direccion = "")
    {
        var r = new Receptor
        {
            Id = 1,
            EmisorId = 1,
            NombreRazonSocial = "Test",
            NumeroDocumento = "12345678-9",
            CatTipoDocumentoIdentificacionReceptorId = 2,
            Direccion = direccion ?? "",
            CatDepartamentoId = depId,
            CatMunicipioId = muniId,
            CatDistritoId = disId
        };
        if (depId.HasValue)  r.Departamento = new CatDepartamento { Id = depId.Value, Codigo = depCod!, Valor = "X" };
        if (muniId.HasValue) r.Municipio    = new CatMunicipio    { Id = muniId.Value, Codigo = muniCod!, Valor = "X", CodigoDepartamento = depCod ?? "06" };
        if (disId.HasValue)  r.Distrito     = new CatDistrito     { Id = disId.Value, Codigo = disCod!, Valor = "X", CodigoDepartamento = depCod ?? "06", CodigoMunicipio = muniCod ?? "23" };
        return r;
    }

    [Fact]
    public void Todo_Vacio_DevuelveNull()
    {
        var r = SeedReceptor();
        var dir = FacturaService.ConstruirDireccionReceptor(r);
        dir.Should().BeNull();
    }

    [Fact]
    public void Falta_Departamento_DevuelveNull()
    {
        var r = SeedReceptor(null, null, 24, "23", 111, "14", "Calle X");
        var dir = FacturaService.ConstruirDireccionReceptor(r);
        dir.Should().BeNull();
    }

    [Fact]
    public void Falta_Municipio_DevuelveNull()
    {
        var r = SeedReceptor(7, "06", null, null, 111, "14", "Calle X");
        var dir = FacturaService.ConstruirDireccionReceptor(r);
        dir.Should().BeNull();
    }

    [Fact]
    public void Falta_Distrito_DevuelveNull()
    {
        var r = SeedReceptor(7, "06", 24, "23", null, null, "Calle X");
        var dir = FacturaService.ConstruirDireccionReceptor(r);
        dir.Should().BeNull();
    }

    [Fact]
    public void Complemento_Vacio_DevuelveNull()
    {
        var r = SeedReceptor(7, "06", 24, "23", 111, "14", "");
        var dir = FacturaService.ConstruirDireccionReceptor(r);
        dir.Should().BeNull();
    }

    [Fact]
    public void Complemento_Whitespace_DevuelveNull()
    {
        var r = SeedReceptor(7, "06", 24, "23", 111, "14", "   ");
        var dir = FacturaService.ConstruirDireccionReceptor(r);
        dir.Should().BeNull();
    }

    [Fact]
    public void Tetrada_Completa_RetornaCodigosMHCorrectos()
    {
        var r = SeedReceptor(7, "06", 24, "23", 111, "14", "Colonia Test");
        var dir = FacturaService.ConstruirDireccionReceptor(r)!;
        Prop(dir, "departamento").Should().Be("06");
        Prop(dir, "municipio").Should().Be("23");
        Prop(dir, "distrito").Should().Be("14");
        Prop(dir, "complemento").Should().Be("Colonia Test");
    }
}
