using System.Collections.Generic;
using System.Linq;
using Xunit;
using FluentAssertions;
using FraFactu.Infrastructure.Persistence.Configurations;

namespace FraFactu.Tests.Persistence
{
    /// <summary>
    /// Valida la integridad del seed CAT-008 (CatDistrito) contra el catálogo
    /// CAT-013 (municipios válidos por departamento, fuente oficial MH).
    /// </summary>
    public class CatDistritoSeedTests
    {
        // CAT-013: códigos de municipio válidos por departamento (catálogo oficial MH).
        private static readonly Dictionary<string, HashSet<string>> MunicipiosValidos = new()
        {
            ["00"] = new() { "00" },
            ["01"] = new() { "13", "14", "15" },
            ["02"] = new() { "14", "15", "16", "17" },
            ["03"] = new() { "17", "18", "19", "20" },
            ["04"] = new() { "34", "35", "36" },
            ["05"] = new() { "23", "24", "25", "26", "27", "28" },
            ["06"] = new() { "20", "21", "22", "23", "24" },
            ["07"] = new() { "17", "18" },
            ["08"] = new() { "23", "24", "25" },
            ["09"] = new() { "10", "11" },
            ["10"] = new() { "14", "15" },
            ["11"] = new() { "24", "25", "26" },
            ["12"] = new() { "21", "22", "23" },
            ["13"] = new() { "27", "28" },
            ["14"] = new() { "19", "20" },
        };

        [Fact]
        public void Seed_Tiene262DistritosMasOtro()
        {
            CatDistritoConfig.SeedData.Should().HaveCount(263); // 262 distritos + "Otro"
        }

        [Theory]
        [InlineData("01", 12)] // Ahuachapán
        [InlineData("02", 13)] // Santa Ana
        [InlineData("03", 16)] // Sonsonate
        [InlineData("04", 33)] // Chalatenango
        [InlineData("05", 22)] // La Libertad
        [InlineData("06", 19)] // San Salvador
        [InlineData("07", 16)] // Cuscatlán
        [InlineData("08", 22)] // La Paz
        [InlineData("09", 9)]  // Cabañas
        [InlineData("10", 13)] // San Vicente
        [InlineData("11", 23)] // Usulután
        [InlineData("12", 20)] // San Miguel
        [InlineData("13", 26)] // Morazán
        [InlineData("14", 18)] // La Unión
        public void Seed_ConteoPorDepartamento_EsCorrecto(string departamento, int esperado)
        {
            CatDistritoConfig.SeedData.Count(d => d.CodigoDepartamento == departamento)
                .Should().Be(esperado);
        }

        [Fact]
        public void Seed_CadaDistritoApuntaAUnMunicipioValido()
        {
            var huérfanos = CatDistritoConfig.SeedData
                .Where(d => !MunicipiosValidos.TryGetValue(d.CodigoDepartamento, out var muns)
                            || !muns.Contains(d.CodigoMunicipio))
                .Select(d => $"{d.Valor} (dep {d.CodigoDepartamento}, muni {d.CodigoMunicipio})")
                .ToList();

            huérfanos.Should().BeEmpty("todo distrito debe referenciar un municipio CAT-013 válido de su departamento");
        }

        [Fact]
        public void Seed_NoHayCodigosDuplicadosPorDepartamento()
        {
            var duplicados = CatDistritoConfig.SeedData
                .GroupBy(d => (d.CodigoDepartamento, d.Codigo))
                .Where(g => g.Count() > 1)
                .Select(g => $"dep {g.Key.CodigoDepartamento} cod {g.Key.Codigo}")
                .ToList();

            duplicados.Should().BeEmpty();
        }

        [Fact]
        public void Seed_NoHayIdsDuplicados()
        {
            CatDistritoConfig.SeedData.Select(d => d.Id).Distinct().Count()
                .Should().Be(CatDistritoConfig.SeedData.Length);
        }
    }
}
