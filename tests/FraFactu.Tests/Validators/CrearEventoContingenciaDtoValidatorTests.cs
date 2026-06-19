using Xunit;
using FluentAssertions;
using FraFactu.Application.DTOs.Contingencia;
using FraFactu.Application.Validators.Contingencia;

namespace FraFactu.Tests.Validators;

/// <summary>
/// Tests unitarios para el validador de CrearEventoContingenciaDto
/// Cubre todas las reglas del schema JSON v3 y reglas de negocio MH
/// </summary>
public class CrearEventoContingenciaDtoValidatorTests
{
    private readonly CrearEventoContingenciaDtoValidator _validator;

    // Usar hora de El Salvador para que los tests sean consistentes con el validador
    private static DateTime AhoraElSalvador =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time"));

    public CrearEventoContingenciaDtoValidatorTests()
    {
        _validator = new CrearEventoContingenciaDtoValidator();
    }

    [Fact]
    public void Validar_DatosCompletosValidos_DebeSerValido()
    {
        // Arrange
        var ahora = AhoraElSalvador;
        var dto = new CrearEventoContingenciaDto
        {
            FechaInicioContingencia = ahora.AddHours(-3).Date,
            FechaFinContingencia = ahora.AddHours(-1).Date,
            HoraInicioContingencia = ahora.AddHours(-3).TimeOfDay,
            HoraFinContingencia = ahora.AddHours(-1).TimeOfDay,
            TipoContingencia = 2,
            MotivoContingencia = null,
            NombreResponsable = "Juan Pérez López",
            CatTipoDocResponsableId = 2, // 2 = DUI
            NumeroDocResponsable = "12345678-9",
            FacturaIds = new List<int> { 1, 2, 3 }
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(10)]
    public void Validar_TipoContingenciaInvalido_DebeSerInvalido(int tipo)
    {
        // Arrange
        var dto = CrearDtoValido();
        dto.TipoContingencia = tipo;

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TipoContingencia");
    }

    [Fact]
    public void Validar_Tipo5SinMotivo_DebeSerInvalido()
    {
        // Arrange
        var dto = CrearDtoValido();
        dto.TipoContingencia = 5;
        dto.MotivoContingencia = null;

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "MotivoContingencia" &&
            e.ErrorMessage.Contains("obligatorio"));
    }

    [Fact]
    public void Validar_Tipo5ConMotivo_DebeSerValido()
    {
        // Arrange
        var dto = CrearDtoValido();
        dto.TipoContingencia = 5;
        dto.MotivoContingencia = "Corte de energía por tormenta eléctrica";

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validar_MotivoMuyLargo_DebeSerInvalido()
    {
        // Arrange
        var dto = CrearDtoValido();
        dto.TipoContingencia = 5;
        dto.MotivoContingencia = new string('A', 501); // 501 caracteres

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "MotivoContingencia" &&
            e.ErrorMessage.Contains("500"));
    }

    [Fact]
    public void Validar_FechaFinMenorQueInicio_DebeSerInvalido()
    {
        // Arrange
        var dto = CrearDtoValido();
        dto.FechaInicioContingencia = AhoraElSalvador.AddDays(-1);
        dto.FechaFinContingencia = AhoraElSalvador.AddDays(-2); // Antes del inicio

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FechaFinContingencia");
    }

    [Fact]
    public void Validar_FechaInicioFutura_DebeSerInvalido()
    {
        // Arrange
        var dto = CrearDtoValido();
        dto.FechaInicioContingencia = AhoraElSalvador.AddDays(1); // Futura

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "FechaInicioContingencia" &&
            e.ErrorMessage.Contains("futura"));
    }

    [Fact]
    public void Validar_FechaFinFutura_DebeSerInvalido()
    {
        // Arrange
        var dto = CrearDtoValido();
        dto.FechaFinContingencia = AhoraElSalvador.AddDays(1); // Futura

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "FechaFinContingencia" &&
            e.ErrorMessage.Contains("futura"));
    }

    [Fact]
    public void Validar_PlazoMayorA24Horas_DebeSerInvalido()
    {
        // Arrange - inicio hace 30 horas, fin hace 28 horas (ambas > 24h)
        var ahora = AhoraElSalvador;
        var dto = CrearDtoValido();
        var inicio = ahora.AddHours(-30);
        var fin = ahora.AddHours(-28);
        dto.FechaInicioContingencia = inicio.Date;
        dto.HoraInicioContingencia = inicio.TimeOfDay;
        dto.FechaFinContingencia = fin.Date;
        dto.HoraFinContingencia = fin.TimeOfDay;

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("24 horas"));
    }

    [Theory]
    [InlineData("A")]
    [InlineData("AB")]
    [InlineData("ABC")]
    [InlineData("ABCD")]
    public void Validar_NombreResponsableMuyCorto_DebeSerInvalido(string nombre)
    {
        // Arrange
        var dto = CrearDtoValido();
        dto.NombreResponsable = nombre;

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NombreResponsable");
    }

    [Fact]
    public void Validar_NombreResponsableMuyLargo_DebeSerInvalido()
    {
        // Arrange
        var dto = CrearDtoValido();
        dto.NombreResponsable = new string('A', 101); // 101 caracteres

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NombreResponsable");
    }

    [Theory]
    [InlineData(1)]  // NIT
    [InlineData(2)]  // DUI
    [InlineData(3)]  // Carnet residente
    [InlineData(4)]  // Pasaporte
    [InlineData(5)]  // Otro
    public void Validar_TiposDocumentoValidosIds_DebeSerValido(int tipoId)
    {
        // Arrange
        var dto = CrearDtoValido();
        dto.CatTipoDocResponsableId = tipoId;

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(99)]
    public void Validar_TipoDocumentoInvalidoId_DebeSerInvalido(int tipoId)
    {
        // Arrange
        var dto = CrearDtoValido();
        dto.CatTipoDocResponsableId = tipoId;

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CatTipoDocResponsableId");
    }

    [Fact]
    public void Validar_NumeroDocumentoMuyCorto_DebeSerInvalido()
    {
        // Arrange
        var dto = CrearDtoValido();
        dto.NumeroDocResponsable = "1234"; // 4 caracteres

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NumeroDocResponsable");
    }

    [Fact]
    public void Validar_SinFacturas_DebeSerInvalido()
    {
        // Arrange
        var dto = CrearDtoValido();
        dto.FacturaIds = new List<int>();

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "FacturaIds" &&
            e.ErrorMessage.Contains("al menos 1"));
    }

    [Fact]
    public void Validar_MasDe1000Facturas_DebeSerInvalido()
    {
        // Arrange
        var dto = CrearDtoValido();
        dto.FacturaIds = Enumerable.Range(1, 1001).ToList(); // 1001 facturas

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "FacturaIds" &&
            e.ErrorMessage.Contains("1000"));
    }

    [Fact]
    public void Validar_FacturasDuplicadas_DebeSerInvalido()
    {
        // Arrange
        var dto = CrearDtoValido();
        dto.FacturaIds = new List<int> { 1, 2, 3, 2, 4 }; // 2 está duplicado

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "FacturaIds" &&
            e.ErrorMessage.Contains("duplicadas"));
    }

    [Fact]
    public void Validar_FacturasConIDsNegativos_DebeSerInvalido()
    {
        // Arrange
        var dto = CrearDtoValido();
        dto.FacturaIds = new List<int> { 1, -1, 3 };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("mayores a 0"));
    }

    [Fact]
    public void Validar_1000FacturasExacto_DebeSerValido()
    {
        // Arrange
        var dto = CrearDtoValido();
        dto.FacturaIds = Enumerable.Range(1, 1000).ToList(); // Exactamente 1000

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    // ==========================================
    // HELPERS
    // ==========================================

    private CrearEventoContingenciaDto CrearDtoValido()
    {
        var ahora = AhoraElSalvador;
        var inicio = ahora.AddHours(-3);
        var fin = ahora.AddHours(-1);
        return new CrearEventoContingenciaDto
        {
            FechaInicioContingencia = inicio.Date,
            FechaFinContingencia = fin.Date,
            HoraInicioContingencia = inicio.TimeOfDay,
            HoraFinContingencia = fin.TimeOfDay,
            TipoContingencia = 2,
            MotivoContingencia = null,
            NombreResponsable = "Juan Pérez",
            CatTipoDocResponsableId = 2, // 2 = DUI
            NumeroDocResponsable = "12345678-9",
            FacturaIds = new List<int> { 1, 2, 3 }
        };
    }
}
