using System.Text.Json;
using FluentAssertions;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Entities.Catalogos;
using FraFactu.Infrastructure.Services;
using Xunit;

namespace FraFactu.Tests.UnitTests.Services;

public class ConstruirDireccionEmisorTests
{
    private static FacturaElectronica SeedFactura(
        (int id, string cod)? sucDep = null, (int id, string cod)? sucMuni = null, (int id, string cod)? sucDis = null, string? sucDir = "",
        (int id, string cod)? emiDep = null, (int id, string cod)? emiMuni = null, (int id, string cod)? emiDis = null, string emiDir = "")
    {
        var emisor = new Emisor
        {
            Id = 1, Nit = "00000000000000", NombreRazonSocial = "Test", Direccion = emiDir,
            CatDepartamentoId = emiDep?.id ?? 0, CatMunicipioId = emiMuni?.id ?? 0, CatDistritoId = emiDis?.id
        };
        if (emiDep.HasValue)  emisor.Departamento = new CatDepartamento { Id = emiDep.Value.id, Codigo = emiDep.Value.cod, Valor = "X" };
        if (emiMuni.HasValue) emisor.Municipio    = new CatMunicipio    { Id = emiMuni.Value.id, Codigo = emiMuni.Value.cod, Valor = "X", CodigoDepartamento = emiDep?.cod ?? "06" };
        if (emiDis.HasValue)  emisor.Distrito     = new CatDistrito     { Id = emiDis.Value.id, Codigo = emiDis.Value.cod, Valor = "X", CodigoDepartamento = emiDep?.cod ?? "06", CodigoMunicipio = emiMuni?.cod ?? "23" };

        var sucursal = new Sucursal
        {
            Id = 1, EmisorId = 1, Codigo = "M001", Nombre = "S", Direccion = sucDir ?? "",
            CatDepartamentoId = sucDep?.id ?? 0, CatMunicipioId = sucMuni?.id ?? 0, CatDistritoId = sucDis?.id
        };
        if (sucDep.HasValue)  sucursal.Departamento = new CatDepartamento { Id = sucDep.Value.id, Codigo = sucDep.Value.cod, Valor = "X" };
        if (sucMuni.HasValue) sucursal.Municipio    = new CatMunicipio    { Id = sucMuni.Value.id, Codigo = sucMuni.Value.cod, Valor = "X", CodigoDepartamento = sucDep?.cod ?? "06" };
        if (sucDis.HasValue)  sucursal.Distrito     = new CatDistrito     { Id = sucDis.Value.id, Codigo = sucDis.Value.cod, Valor = "X", CodigoDepartamento = sucDep?.cod ?? "06", CodigoMunicipio = sucMuni?.cod ?? "23" };

        return new FacturaElectronica { Id = 1, EmisorId = 1, Emisor = emisor, SucursalId = 1, Sucursal = sucursal };
    }

    [Fact]
    public void Sucursal_TriadaCompleta_UsaSucursal()
    {
        var f = SeedFactura(
            sucDep: (7, "06"), sucMuni: (24, "23"), sucDis: (111, "14"), sucDir: "Calle Suc",
            emiDep: (7, "06"), emiMuni: (24, "23"), emiDis: (111, "14"), emiDir: "Calle Emi");
        var dir = FacturaService.ConstruirDireccionEmisor(f);
        var json = JsonSerializer.Serialize(dir);
        json.Should().Contain("\"complemento\":\"Calle Suc\"");
    }

    [Fact]
    public void Sucursal_TriadaCompleta_DireccionVacia_UsaFallbackComplementoEmisor()
    {
        var f = SeedFactura(
            sucDep: (7, "06"), sucMuni: (24, "23"), sucDis: (111, "14"), sucDir: "",
            emiDep: (7, "06"), emiMuni: (24, "23"), emiDis: (111, "14"), emiDir: "Calle Emi");
        var dir = FacturaService.ConstruirDireccionEmisor(f);
        var json = JsonSerializer.Serialize(dir);
        json.Should().Contain("\"complemento\":\"Calle Emi\"");
    }

    [Fact]
    public void Sucursal_Incompleta_EmisorCompleto_UsaFallbackEmisor()
    {
        var f = SeedFactura(
            sucDep: null, sucMuni: null, sucDis: null, sucDir: "",
            emiDep: (7, "06"), emiMuni: (24, "23"), emiDis: (111, "14"), emiDir: "Calle Emi");
        var dir = FacturaService.ConstruirDireccionEmisor(f);
        var json = JsonSerializer.Serialize(dir);
        json.Should().Contain("\"departamento\":\"06\"");
        json.Should().Contain("\"complemento\":\"Calle Emi\"");
    }

    [Fact]
    public void Sucursal_Incompleta_Emisor_Incompleto_DevuelveNull()
    {
        // Cambio 2026-06-12: el helper ya no lanza pre-MH. Devuelve null y
        // dejamos que MH sea la fuente de verdad — rechaza con [096]
        // "/emisor/direccion/*: required". Eso evita el orphan PENDIENTE_ENVIO
        // que el throw producía (la factura existía pero /descartar solo
        // acepta RECHAZADO/ERROR). El FE mapea [096] /emisor/direccion al
        // mensaje humano "Complete los datos del emisor en Configuración".
        var f = SeedFactura(
            sucDep: null, sucMuni: null, sucDis: null, sucDir: "",
            emiDep: (7, "06"), emiMuni: (24, "23"), emiDis: null, emiDir: "Calle Emi");
        var dir = FacturaService.ConstruirDireccionEmisor(f);
        dir.Should().BeNull();
    }

    [Fact]
    public void Sucursal_TriadaCompleta_ComplementoVacio_AmbosLados_DevuelveNull()
    {
        // Edge case: sucursal con triada catálogo completa pero Direccion vacía
        // y emisor.Direccion también vacía. Antes el helper lanzaba aquí; ahora
        // devuelve null para que MH responda [096] /emisor/direccion/complemento.
        var f = SeedFactura(
            sucDep: (7, "06"), sucMuni: (24, "23"), sucDis: (111, "14"), sucDir: "",
            emiDep: (7, "06"), emiMuni: (24, "23"), emiDis: (111, "14"), emiDir: "");
        var dir = FacturaService.ConstruirDireccionEmisor(f);
        dir.Should().BeNull();
    }
}
