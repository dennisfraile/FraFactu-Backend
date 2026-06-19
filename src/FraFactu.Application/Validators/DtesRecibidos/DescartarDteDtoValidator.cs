using FluentValidation;
using FraFactu.Application.DTOs.DtesRecibidos;

namespace FraFactu.Application.Validators.DtesRecibidos;

public class DescartarDteDtoValidator : AbstractValidator<DescartarDteDto>
{
    public DescartarDteDtoValidator()
    {
        RuleFor(x => x.Motivo)
            .NotEmpty().WithMessage("El motivo de descarte es requerido")
            .MinimumLength(5).WithMessage("El motivo debe tener al menos 5 caracteres")
            .MaximumLength(500).WithMessage("El motivo no puede exceder 500 caracteres");
    }
}
