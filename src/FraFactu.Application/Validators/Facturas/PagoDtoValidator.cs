using FluentValidation;

namespace FraFactu.Application.Validators.Facturas
{
    /// <summary>
    /// Validador para Pago según estándar de Hacienda
    /// </summary>
    public class PagoDtoValidator : AbstractValidator<DTOs.Facturas.PagoDto>
    {
        public PagoDtoValidator()
        {
            RuleFor(x => x.Monto)
                .GreaterThan(0)
                .WithMessage("Monto de pago debe ser mayor a 0");

            RuleFor(x => x.Referencia)
                .MaximumLength(50)
                .When(x => !string.IsNullOrEmpty(x.Referencia))
                .WithMessage("Referencia máximo 50 caracteres");
        }
    }
}
