using FluentValidation;
using FraFactu.Application.DTOs.Compras;

namespace FraFactu.Application.Validators.Compras;

public class ActualizarProveedorDtoValidator : AbstractValidator<ActualizarProveedorDto>
{
    public ActualizarProveedorDtoValidator()
    {
        RuleFor(x => x.NIT)
            .NotEmpty().WithMessage("El NIT es requerido")
            .Length(14).WithMessage("El NIT debe tener exactamente 14 dígitos")
            .Matches("^[0-9]{14}$").WithMessage("El NIT debe contener solo dígitos sin guiones");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es requerido")
            .MaximumLength(300).WithMessage("El nombre no puede exceder 300 caracteres");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("El email debe ser válido")
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}
