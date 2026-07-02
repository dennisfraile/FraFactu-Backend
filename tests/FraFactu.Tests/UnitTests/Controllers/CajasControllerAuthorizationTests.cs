using System;
using System.Linq;
using System.Reflection;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using FraFactu.API.Controllers;

namespace FraFactu.Tests.UnitTests.Controllers;

/// <summary>
/// Verifica por reflexión los roles declarados en los [Authorize] de
/// CajasController. El harness de integración bypassa la autorización
/// (FakePolicyEvaluator), por eso el gate de rol se valida aquí.
/// </summary>
public class CajasControllerAuthorizationTests
{
    private static string[] RolesDe(string metodo)
    {
        var method = typeof(CajasController).GetMethod(metodo)
            ?? throw new InvalidOperationException($"No existe el método {metodo}");
        var attr = method
            .GetCustomAttributes<AuthorizeAttribute>(inherit: false)
            .FirstOrDefault(a => !string.IsNullOrEmpty(a.Roles));
        attr.Should().NotBeNull($"{metodo} debe declarar [Authorize(Roles=...)]");
        return attr!.Roles!
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    [Theory]
    [InlineData("GetAll")]
    [InlineData("GetById")]
    public void LosGet_DebenIncluirSuperAdmin(string metodo)
    {
        RolesDe(metodo).Should().Contain("SuperAdmin");
    }

    [Theory]
    [InlineData("GetAll")]
    [InlineData("GetById")]
    public void LosGet_DeclaranElSetDeLecturaEsperado(string metodo)
    {
        RolesDe(metodo).Should().BeEquivalentTo(new[]
        {
            "SuperAdmin", "EmisorAdmin", "GerenteSucursal", "Cajero", "Auditor", "Contador"
        });
    }

    [Theory]
    [InlineData("Create")]
    [InlineData("Update")]
    [InlineData("Delete")]
    [InlineData("ToggleActive")]
    [InlineData("AsignarCajaAUsuario")]
    [InlineData("DesasignarCajaDeUsuario")]
    public void LaEscritura_NoDebeIncluirSuperAdmin(string metodo)
    {
        RolesDe(metodo).Should().NotContain("SuperAdmin");
    }
}
