using FluentValidation;
using FraFactu.Application.DTOs.Sucursales;

namespace FraFactu.Application.Validators.Sucursales
{
    public class CreateSucursalDtoValidator : AbstractValidator<CreateSucursalDto>
    {
        public CreateSucursalDtoValidator()
        {
            RuleFor(x => x.Codigo)
                .MaximumLength(20).WithMessage("El código no puede exceder 20 caracteres");

            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre de la sucursal es requerido")
                .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres")
                .MinimumLength(3).WithMessage("El nombre debe tener al menos 3 caracteres");

            RuleFor(x => x.Direccion)
                .MaximumLength(500).WithMessage("La dirección no puede exceder 500 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Direccion));

            RuleFor(x => x.Telefono)
                .MaximumLength(20).WithMessage("El teléfono no puede exceder 20 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Telefono));

            RuleFor(x => x.CorreoElectronico)
                .EmailAddress().WithMessage("El correo electrónico no tiene un formato válido")
                .MaximumLength(100).WithMessage("El correo electrónico no puede exceder 100 caracteres")
                .When(x => !string.IsNullOrEmpty(x.CorreoElectronico));
        }
    }
}
