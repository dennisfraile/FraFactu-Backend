using FluentValidation;
using FraFactu.Application.DTOs.Compras;

namespace FraFactu.Application.Validators.Compras;

public class AnularCompraDtoValidator : AbstractValidator<AnularCompraDto>
{
    public AnularCompraDtoValidator()
    {
        RuleFor(x => x.Motivo)
            .NotEmpty().WithMessage("El motivo de anulación es requerido")
            .MinimumLength(10).WithMessage("El motivo debe tener al menos 10 caracteres")
            .MaximumLength(500).WithMessage("El motivo no puede exceder 500 caracteres");
    }
}
