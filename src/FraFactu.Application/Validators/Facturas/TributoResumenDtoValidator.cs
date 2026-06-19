using FluentValidation;

namespace FraFactu.Application.Validators.Facturas
{
    /// <summary>
    /// Validador para TributoResumenDto (específico de CCF)
    /// </summary>
    public class TributoResumenDtoValidator : AbstractValidator<DTOs.Facturas.TributoResumenDto>
    {
        public TributoResumenDtoValidator()
        {
            // Codigo: 2 caracteres exactos
            RuleFor(x => x.Codigo)
                .NotEmpty()
                .WithMessage("El código del tributo es requerido")
                .Length(2, 2)
                .WithMessage("El código del tributo debe tener exactamente 2 caracteres");

            // Descripcion: 2-150 caracteres
            RuleFor(x => x.Descripcion)
                .NotEmpty()
                .WithMessage("La descripción del tributo es requerida")
                .Length(2, 150)
                .WithMessage("La descripción debe tener entre 2 y 150 caracteres");

            // Valor: mayor o igual a 0, menor a 100 billones
            RuleFor(x => x.Valor)
                .GreaterThanOrEqualTo(0)
                .WithMessage("El valor del tributo no puede ser negativo")
                .LessThan(100000000000m)
                .WithMessage("El valor del tributo es demasiado grande");
        }
    }
}
