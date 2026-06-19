using FluentValidation;
using FraFactu.Application.DTOs.Inventario;

namespace FraFactu.Application.Validators.Inventario;

public class EntradaInventarioDtoValidator : AbstractValidator<EntradaInventarioDto>
{
    public EntradaInventarioDtoValidator()
    {
        RuleFor(x => x.ProductoId)
            .GreaterThan(0).WithMessage("El producto es requerido");

        RuleFor(x => x.BodegaId)
            .GreaterThan(0).WithMessage("La bodega es requerida");

        RuleFor(x => x.Cantidad)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0");

        RuleFor(x => x.CostoUnitario)
            .GreaterThan(0).WithMessage("El costo unitario debe ser mayor a 0");

        RuleFor(x => x.TipoDocumento)
            .NotEmpty().WithMessage("El tipo de documento es requerido")
            .Must(tipo => new[] { "COMPRA", "DEVOLUCION_CLIENTE", "PRODUCCION", "AJUSTE", "OTRO" }
                .Contains(tipo.ToUpper()))
            .WithMessage("Tipo de documento inválido");

        RuleFor(x => x.Observaciones)
            .MaximumLength(1000).WithMessage("Las observaciones no pueden exceder 1000 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Observaciones));
    }
}
