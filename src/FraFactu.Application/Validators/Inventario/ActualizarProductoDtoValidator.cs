using FluentValidation;
using FraFactu.Application.DTOs.Inventario;

namespace FraFactu.Application.Validators.Inventario;

public class ActualizarProductoDtoValidator : AbstractValidator<ActualizarProductoDto>
{
    public ActualizarProductoDtoValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es requerido")
            .MaximumLength(300).WithMessage("El nombre no puede exceder 300 caracteres");

        RuleFor(x => x.CategoriaId)
            .GreaterThan(0).WithMessage("Debe seleccionar una categoría válida");

        RuleFor(x => x.CatUnidadMedidaId)
            .GreaterThan(0).WithMessage("Debe seleccionar una unidad de medida válida");

        RuleFor(x => x.StockMinimo)
            .GreaterThanOrEqualTo(0).WithMessage("El stock mínimo no puede ser negativo");

        RuleFor(x => x.StockMaximo)
            .GreaterThanOrEqualTo(x => x.StockMinimo)
                .WithMessage("El stock máximo debe ser mayor o igual al stock mínimo");

        RuleFor(x => x.PrecioCosto)
            .GreaterThan(0).WithMessage("El precio de costo debe ser mayor a 0");

        RuleFor(x => x.PrecioVenta)
            .GreaterThan(0).WithMessage("El precio de venta debe ser mayor a 0");
    }
}
