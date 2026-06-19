using FluentValidation;
using FraFactu.Application.DTOs.Emisores;

namespace FraFactu.Application.Validators.Emisores
{
    public class CreateEmisorDtoValidator : AbstractValidator<CreateEmisorDto>
    {
        public CreateEmisorDtoValidator()
        {
            RuleFor(x => x.Nit)
                .NotEmpty().WithMessage("El NIT es requerido")
                .Matches(@"^\d{9}$|^\d{14}$").WithMessage("El NIT debe tener 9 o 14 dígitos")
                .MaximumLength(20).WithMessage("El NIT no puede exceder 20 caracteres");

            RuleFor(x => x.NombreRazonSocial)
                .NotEmpty().WithMessage("La razón social es requerida")
                .MaximumLength(200).WithMessage("La razón social no puede exceder 200 caracteres")
                .MinimumLength(3).WithMessage("La razón social debe tener al menos 3 caracteres");

            RuleFor(x => x.CorreoElectronico)
                .NotEmpty().WithMessage("El correo electrónico es requerido")
                .EmailAddress().WithMessage("El correo electrónico no tiene un formato válido")
                .MaximumLength(100).WithMessage("El correo electrónico no puede exceder 100 caracteres");

            RuleFor(x => x.Direccion)
                .NotEmpty().WithMessage("La dirección es requerida")
                .MaximumLength(500).WithMessage("La dirección no puede exceder 500 caracteres");
        }
    }
}
