using FluentValidation;
using FraFactu.Application.DTOs.Compras;

namespace FraFactu.Application.Validators.Compras;

public class CrearGastoDtoValidator : AbstractValidator<CrearGastoDto>
{
    public CrearGastoDtoValidator()
    {
        RuleFor(x => x.Descripcion)
            .NotEmpty().WithMessage("La descripción del gasto es requerida")
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres");

        RuleFor(x => x.Monto)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a 0");

        RuleFor(x => x.CentroCosto)
            .MaximumLength(100).WithMessage("El centro de costo no puede exceder 100 caracteres")
            .When(x => !string.IsNullOrEmpty(x.CentroCosto));

        RuleFor(x => x.CuentaContable)
            .MaximumLength(50).WithMessage("La cuenta contable no puede exceder 50 caracteres")
            .When(x => !string.IsNullOrEmpty(x.CuentaContable));
    }
}
