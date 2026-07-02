using System;
using System.Linq;
using System.Reflection;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using FraFactu.API.Controllers;

namespace FraFactu.Tests.UnitTests.Controllers;

/// <summary>
/// Verifica por reflexión que SuperAdmin está en los [Authorize] de los GET de
/// lectura de los 9 controllers de datos maestros, y NO en los de escritura.
/// El harness de integración bypassa la autorización (FakePolicyEvaluator),
/// por eso el gate de rol se valida aquí.
/// </summary>
public class MasterDataReadAuthorizationTests
{
    private static string[] RolesDe(Type controller, string metodo)
    {
        var method = controller.GetMethod(metodo)
            ?? throw new InvalidOperationException($"No existe {controller.Name}.{metodo}");
        var attr = method
            .GetCustomAttributes<AuthorizeAttribute>(inherit: false)
            .FirstOrDefault(a => !string.IsNullOrEmpty(a.Roles));
        attr.Should().NotBeNull($"{controller.Name}.{metodo} debe declarar [Authorize(Roles=...)]");
        return attr!.Roles!
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    [Theory]
    [InlineData(typeof(SucursalesController), "GetById")]
    [InlineData(typeof(SucursalesController), "GetAll")]
    [InlineData(typeof(SucursalesController), "GetAllActivas")]
    [InlineData(typeof(BodegasController), "GetById")]
    [InlineData(typeof(BodegasController), "GetAll")]
    [InlineData(typeof(BodegasController), "GetAllActivas")]
    [InlineData(typeof(BodegasController), "GetBySucursal")]
    [InlineData(typeof(BodegasController), "GetStock")]
    [InlineData(typeof(CategoriasController), "GetById")]
    [InlineData(typeof(CategoriasController), "GetAll")]
    [InlineData(typeof(MarcasController), "GetById")]
    [InlineData(typeof(MarcasController), "GetAll")]
    [InlineData(typeof(ProductosServiciosController), "GetById")]
    [InlineData(typeof(ProductosServiciosController), "GetAll")]
    [InlineData(typeof(ProductosServiciosController), "Search")]
    [InlineData(typeof(ReceptoresController), "GetById")]
    [InlineData(typeof(ReceptoresController), "GetAll")]
    [InlineData(typeof(ReceptoresController), "Search")]
    [InlineData(typeof(VendedoresController), "GetAll")]
    [InlineData(typeof(VendedoresController), "GetById")]
    [InlineData(typeof(TiposGastoController), "GetAll")]
    [InlineData(typeof(TiposGastoController), "GetById")]
    [InlineData(typeof(ProveedoresController), "ObtenerPorId")]
    [InlineData(typeof(ProveedoresController), "ObtenerPorNIT")]
    [InlineData(typeof(ProveedoresController), "Listar")]
    public void LosGetDeLectura_IncluyenSuperAdmin(Type controller, string metodo)
    {
        RolesDe(controller, metodo).Should().Contain("SuperAdmin");
    }

    [Theory]
    [InlineData(typeof(SucursalesController), "Create")]
    [InlineData(typeof(SucursalesController), "Update")]
    [InlineData(typeof(SucursalesController), "ToggleActive")]
    [InlineData(typeof(BodegasController), "Create")]
    [InlineData(typeof(BodegasController), "Update")]
    [InlineData(typeof(BodegasController), "ToggleActive")]
    [InlineData(typeof(CategoriasController), "Create")]
    [InlineData(typeof(CategoriasController), "Update")]
    [InlineData(typeof(CategoriasController), "Delete")]
    [InlineData(typeof(CategoriasController), "ToggleActive")]
    [InlineData(typeof(MarcasController), "Create")]
    [InlineData(typeof(MarcasController), "Update")]
    [InlineData(typeof(MarcasController), "Delete")]
    [InlineData(typeof(MarcasController), "ToggleActive")]
    [InlineData(typeof(ProductosServiciosController), "Create")]
    [InlineData(typeof(ProductosServiciosController), "Update")]
    [InlineData(typeof(ProductosServiciosController), "ToggleActive")]
    [InlineData(typeof(ProductosServiciosController), "Desactivar")]
    [InlineData(typeof(ReceptoresController), "Create")]
    [InlineData(typeof(ReceptoresController), "Update")]
    [InlineData(typeof(VendedoresController), "Create")]
    [InlineData(typeof(VendedoresController), "Update")]
    [InlineData(typeof(VendedoresController), "ToggleActive")]
    [InlineData(typeof(TiposGastoController), "Create")]
    [InlineData(typeof(TiposGastoController), "Update")]
    [InlineData(typeof(TiposGastoController), "Delete")]
    [InlineData(typeof(ProveedoresController), "Crear")]
    [InlineData(typeof(ProveedoresController), "Actualizar")]
    [InlineData(typeof(ProveedoresController), "CambiarEstado")]
    [InlineData(typeof(ProveedoresController), "Eliminar")]
    public void LosEndpointsDeEscritura_NoIncluyenSuperAdmin(Type controller, string metodo)
    {
        RolesDe(controller, metodo).Should().NotContain("SuperAdmin");
    }
}
