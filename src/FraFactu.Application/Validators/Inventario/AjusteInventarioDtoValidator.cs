using FluentValidation;
using FraFactu.Application.DTOs.Inventario;

namespace FraFactu.Application.Validators.Inventario;

public class AjusteInventarioDtoValidator : AbstractValidator<AjusteInventarioDto>
{
    public AjusteInventarioDtoValidator()
    {
        RuleFor(x => x.ProductoId)
            .GreaterThan(0).WithMessage("El producto es requerido");

        RuleFor(x => x.BodegaId)
            .GreaterThan(0).WithMessage("La bodega es requerida");

        RuleFor(x => x.Cantidad)
            .NotEqual(0).WithMessage("La cantidad del ajuste no puede ser 0");

        RuleFor(x => x.Motivo)
            .NotEmpty().WithMessage("El motivo del ajuste es requerido")
            .Must(motivo => new[] { "CORRECCION", "MERMA", "FALTANTE", "SOBRANTE", "DAÑADO", "OTRO" }
                .Contains(motivo.ToUpper()))
            .WithMessage("Motivo de ajuste inválido");

        RuleFor(x => x.Observaciones)
            .NotEmpty().WithMessage("Las observaciones son obligatorias para auditoría")
            .MinimumLength(10).WithMessage("Las observaciones deben tener al menos 10 caracteres")
            .MaximumLength(1000).WithMessage("Las observaciones no pueden exceder 1000 caracteres");
    }
}
