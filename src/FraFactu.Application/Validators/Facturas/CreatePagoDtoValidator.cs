using FluentValidation;
using FraFactu.Application.DTOs.Facturas;

namespace FraFactu.Application.Validators.Facturas
{
    public class CreatePagoDtoValidator : AbstractValidator<CreatePagoDto>
    {
        public CreatePagoDtoValidator()
        {
            RuleFor(x => x.CatFormaPagoId)
                .GreaterThan(0).WithMessage("Debe seleccionar una forma de pago válida");

            RuleFor(x => x.Monto)
                .GreaterThan(0).WithMessage("El monto debe ser mayor a 0")
                .LessThanOrEqualTo(9999999.99m).WithMessage("El monto no puede exceder 9,999,999.99");

            RuleFor(x => x.CatPlazoId)
                .GreaterThan(0).WithMessage("El plazo debe ser válido")
                .When(x => x.CatPlazoId.HasValue);

            RuleFor(x => x.Periodo)
                .GreaterThan(0).WithMessage("El periodo debe ser mayor a 0")
                .When(x => x.Periodo.HasValue);
        }
    }
}
