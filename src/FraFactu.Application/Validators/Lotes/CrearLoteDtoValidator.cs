using FluentValidation;

namespace FraFactu.Application.Validators.Lotes;

/// <summary>
/// Validador para CrearLoteDto
/// </summary>
public class CrearLoteDtoValidator : AbstractValidator<Application.DTOs.Lotes.CrearLoteDto>
{
    public CrearLoteDtoValidator()
    {
        RuleFor(x => x.FacturaIds)
            .NotEmpty()
            .WithMessage("Debe incluir al menos una factura en el lote");

        RuleFor(x => x.FacturaIds)
            .Must(ids => ids.Count <= 100)
            .WithMessage("El lote no puede exceder 100 facturas");

        RuleFor(x => x.FacturaIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("No puede haber facturas duplicadas en el lote");

        RuleFor(x => x.FacturaIds)
            .Must(ids => ids.All(id => id > 0))
            .WithMessage("Todos los IDs de factura deben ser válidos (mayores a 0)");
    }
}
