using FluentValidation;
using FraFactu.Application.DTOs.DtesRecibidos;

namespace FraFactu.Application.Validators.DtesRecibidos;

public class ConfiguracionLecturaCorreoDtoValidator : AbstractValidator<ConfiguracionLecturaCorreoDto>
{
    public ConfiguracionLecturaCorreoDtoValidator()
    {
        RuleFor(x => x.LecturaCorreoHabilitada)
            .NotNull().WithMessage("Debe indicar si la lectura de correo está habilitada");
    }
}
