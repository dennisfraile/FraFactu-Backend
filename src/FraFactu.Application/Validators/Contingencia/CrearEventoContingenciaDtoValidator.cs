using FluentValidation;
using FraFactu.Application.DTOs.Contingencia;

namespace FraFactu.Application.Validators.Contingencia;

/// <summary>
/// Validador para CrearEventoContingenciaDto
/// Implementa todas las reglas del schema JSON v4 y reglas de negocio MH
/// </summary>
public class CrearEventoContingenciaDtoValidator : AbstractValidator<CrearEventoContingenciaDto>
{
    public CrearEventoContingenciaDtoValidator()
    {
        // ==========================================
        // TIPO DE CONTINGENCIA
        // ==========================================

        RuleFor(x => x.TipoContingencia)
            .InclusiveBetween(1, 5)
            .WithMessage("Tipo de contingencia debe ser entre 1 y 5");

        // ==========================================
        // MOTIVO: OBLIGATORIO SI TIPO = 5
        // ==========================================

        When(x => x.TipoContingencia == 5, () =>
        {
            RuleFor(x => x.MotivoContingencia)
                .NotEmpty()
                .WithMessage("Motivo de contingencia es obligatorio cuando tipo = 5 (Otro)")
                .MinimumLength(1)
                .WithMessage("Motivo debe tener al menos 1 carácter")
                .MaximumLength(500)
                .WithMessage("Motivo no puede exceder 500 caracteres");
        });

        // ==========================================
        // FECHAS Y HORAS
        // ==========================================

        RuleFor(x => x.FechaFinContingencia)
            .GreaterThanOrEqualTo(x => x.FechaInicioContingencia)
            .WithMessage("Fecha de fin debe ser mayor o igual a fecha de inicio");

        // Validar que las fechas no sean futuras (usando hora de El Salvador)
        RuleFor(x => x.FechaInicioContingencia)
            .Must(fecha =>
            {
                var zonaElSalvador = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
                var ahora = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaElSalvador);
                return fecha <= ahora;
            })
            .WithMessage("Fecha de inicio no puede ser futura");

        RuleFor(x => x.FechaFinContingencia)
            .Must(fecha =>
            {
                var zonaElSalvador = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
                var ahora = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaElSalvador);
                return fecha <= ahora;
            })
            .WithMessage("Fecha de fin no puede ser futura");

        // ==========================================
        // PLAZO DE 24 HORAS (Regla de Negocio MH)
        // Calculado desde FechaInicioContingencia, NO desde FechaFinContingencia
        // ==========================================

        RuleFor(x => x)
            .Must(dto =>
            {
                var fechaInicio = dto.FechaInicioContingencia.Add(dto.HoraInicioContingencia);
                var zonaElSalvador = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
                var ahora = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaElSalvador);
                var diferencia = ahora - fechaInicio;
                return diferencia.TotalHours <= 24;
            })
            .WithMessage("Han pasado más de 24 horas desde el inicio de la contingencia");

        // ==========================================
        // RESPONSABLE DEL ESTABLECIMIENTO
        // ==========================================

        RuleFor(x => x.NombreResponsable)
            .NotEmpty()
            .WithMessage("Nombre del responsable es obligatorio")
            .MinimumLength(5)
            .WithMessage("Nombre del responsable debe tener al menos 5 caracteres")
            .MaximumLength(100)
            .WithMessage("Nombre del responsable no puede exceder 100 caracteres");

        RuleFor(x => x.CatTipoDocResponsableId)
            .GreaterThan(0)
            .WithMessage("Debe seleccionar un tipo de documento del responsable")
            .LessThanOrEqualTo(10)
            .WithMessage("Tipo de documento inválido");

        RuleFor(x => x.NumeroDocResponsable)
            .NotEmpty()
            .WithMessage("Número de documento del responsable es obligatorio")
            .MinimumLength(5)
            .WithMessage("Número de documento debe tener al menos 5 caracteres")
            .MaximumLength(25)
            .WithMessage("Número de documento no puede exceder 25 caracteres");

        // ==========================================
        // FACTURAS
        // ==========================================

        RuleFor(x => x.FacturaIds)
            .NotEmpty()
            .WithMessage("Debe incluir al menos 1 factura en el evento")
            .Must(list => list != null && list.Count >= 1)
            .WithMessage("Debe incluir al menos 1 factura en el evento")
            .Must(list => list == null || list.Count <= 1000)
            .WithMessage("Máximo 1000 facturas por evento de contingencia según schema JSON v4");

        // Validar que no haya IDs duplicados
        RuleFor(x => x.FacturaIds)
            .Must(list => list == null || list.Count == list.Distinct().Count())
            .WithMessage("No se pueden incluir facturas duplicadas en el evento");

        // Validar que los IDs sean positivos
        RuleForEach(x => x.FacturaIds)
            .GreaterThan(0)
            .WithMessage("Los IDs de facturas deben ser mayores a 0");
    }
}
