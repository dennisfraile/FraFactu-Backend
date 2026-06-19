using FluentValidation;
using FraFactu.Application.DTOs.Receptores;

namespace FraFactu.Application.Validators.Receptores
{
    public class CreateReceptorDtoValidator : AbstractValidator<CreateReceptorDto>
    {
        public CreateReceptorDtoValidator()
        {
            RuleFor(x => x.NombreRazonSocial)
                .NotEmpty().WithMessage("El nombre/razón social es requerido")
                .MaximumLength(200).WithMessage("El nombre/razón social no puede exceder 200 caracteres")
                .MinimumLength(3).WithMessage("El nombre/razón social debe tener al menos 3 caracteres");

            // Documento: opcional, pero si se proporciona debe cumplir formato
            RuleFor(x => x.NumeroDocumento)
                .MaximumLength(50).WithMessage("El número de documento no puede exceder 50 caracteres")
                .When(x => !string.IsNullOrEmpty(x.NumeroDocumento));

            When(x => x.CatTipoDocumentoId == 1 && !string.IsNullOrEmpty(x.NumeroDocumento), () =>
            {
                RuleFor(x => x.NumeroDocumento)
                    .Matches(@"^\d{9}$|^\d{14}$").WithMessage("El NIT debe tener 9 o 14 dígitos");
            });

            When(x => x.CatTipoDocumentoId == 2 && !string.IsNullOrEmpty(x.NumeroDocumento), () =>
            {
                RuleFor(x => x.NumeroDocumento)
                    .Matches(@"^\d{8}-\d$").WithMessage("El DUI debe tener el formato 12345678-9");
            });

            // Dirección: opcional
            RuleFor(x => x.Direccion)
                .MaximumLength(500).WithMessage("La dirección no puede exceder 500 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Direccion));

            // Correo: opcional, pero si se proporciona debe ser válido
            RuleFor(x => x.CorreoElectronico)
                .EmailAddress().WithMessage("El correo electrónico no tiene un formato válido")
                .MaximumLength(100).WithMessage("El correo electrónico no puede exceder 100 caracteres")
                .When(x => !string.IsNullOrEmpty(x.CorreoElectronico));

            RuleFor(x => x.Telefono)
                .MaximumLength(20).WithMessage("El teléfono no puede exceder 20 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Telefono));
        }
    }
}
