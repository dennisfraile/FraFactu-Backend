using FluentValidation;
using FraFactu.Application.DTOs.Inventario;

namespace FraFactu.Application.Validators
{
    public class AjusteInventarioDtoValidator : AbstractValidator<AjusteInventarioDto>
    {
        public AjusteInventarioDtoValidator()
        {
            RuleFor(x => x.ProductoId).GreaterThan(0);
            RuleFor(x => x.BodegaId).GreaterThan(0);
            RuleFor(x => x.Cantidad).NotEqual(0);
            RuleFor(x => x.Motivo)
                .NotEmpty()
                .Must(m => new[] { "CORRECCION", "MERMA", "FALTANTE", "SOBRANTE", "DAÑADO", "OTRO" }.Contains(m))
                .WithMessage("El motivo no es válido.");
            RuleFor(x => x.Observaciones)
                .NotEmpty()
                .Length(10, 1000);
        }
    }
}
