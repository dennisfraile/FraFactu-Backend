using FluentValidation;
using FraFactu.Application.DTOs.Inventario;

namespace FraFactu.Application.Validators.Inventario;

public class TrasladoInventarioDtoValidator : AbstractValidator<TrasladoInventarioDto>
{
    public TrasladoInventarioDtoValidator()
    {
        RuleFor(x => x.ProductoId)
            .GreaterThan(0).WithMessage("El producto es requerido");

        RuleFor(x => x.BodegaOrigenId)
            .GreaterThan(0).WithMessage("La bodega origen es requerida");

        RuleFor(x => x.BodegaDestinoId)
            .GreaterThan(0).WithMessage("La bodega destino es requerida")
            .NotEqual(x => x.BodegaOrigenId)
                .WithMessage("La bodega destino debe ser diferente a la bodega origen");

        RuleFor(x => x.Cantidad)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0");
    }
}
