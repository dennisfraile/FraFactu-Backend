using FluentValidation;
using FraFactu.Application.DTOs.Receptores;

namespace FraFactu.Application.Validators.Receptores
{
    public class UpdateReceptorDtoValidator : AbstractValidator<UpdateReceptorDto>
    {
        public UpdateReceptorDtoValidator()
        {
            RuleFor(x => x.NombreRazonSocial)
                .NotEmpty().WithMessage("El nombre o razón social es requerido")
                .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres");

            // Documento: opcional, pero si se proporciona debe cumplir formato
            RuleFor(x => x.NumeroDocumento)
                .MaximumLength(50).WithMessage("El número de documento no puede exceder 50 caracteres")
                .When(x => !string.IsNullOrEmpty(x.NumeroDocumento));

            // Correo: opcional, pero si se proporciona debe ser válido
            RuleFor(x => x.CorreoElectronico)
                .EmailAddress().WithMessage("El correo electrónico no tiene un formato válido")
                .MaximumLength(100).WithMessage("El correo electrónico no puede exceder 100 caracteres")
                .When(x => !string.IsNullOrEmpty(x.CorreoElectronico));
        }
    }
}
