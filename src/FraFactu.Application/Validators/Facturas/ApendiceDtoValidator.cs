using FluentValidation;
using FraFactu.Application.DTOs.Facturas;

namespace FraFactu.Application.Validators.Facturas
{
    /// <summary>
    /// Validador para ApendiceDto
    /// </summary>
    public class ApendiceDtoValidator : AbstractValidator<ApendiceDto>
    {
        public ApendiceDtoValidator()
        {
            // Campo: requerido, max 25 caracteres
            RuleFor(x => x.Campo)
                .NotEmpty()
                .WithMessage("Campo es requerido")
                .MaximumLength(25)
                .WithMessage("Campo no puede exceder 25 caracteres");

            // Etiqueta: requerida, max 50 caracteres
            RuleFor(x => x.Etiqueta)
                .NotEmpty()
                .WithMessage("Etiqueta es requerida")
                .MaximumLength(50)
                .WithMessage("Etiqueta no puede exceder 50 caracteres");

            // Valor: requerido, max 150 caracteres
            RuleFor(x => x.Valor)
                .NotEmpty()
                .WithMessage("Valor es requerido")
                .MaximumLength(150)
                .WithMessage("Valor no puede exceder 150 caracteres");
        }
    }
}
