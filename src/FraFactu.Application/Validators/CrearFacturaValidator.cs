using FluentValidation;

namespace FraFactu.Application.Validators;

/// <summary>
/// Ejemplo de validator - se puede extender según DTOs
/// </summary>
public class CrearFacturaValidator : AbstractValidator<string>
{
    public CrearFacturaValidator()
    {
        RuleFor(x => x)
            .NotEmpty()
            .WithMessage("El dato es requerido.");
    }
}
