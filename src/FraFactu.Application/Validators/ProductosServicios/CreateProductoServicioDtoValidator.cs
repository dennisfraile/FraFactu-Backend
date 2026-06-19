using FluentValidation;
using FraFactu.Application.DTOs.ProductosServicios;

namespace FraFactu.Application.Validators.ProductosServicios
{
    public class CreateProductoServicioDtoValidator : AbstractValidator<CreateProductoServicioDto>
    {
        public CreateProductoServicioDtoValidator()
        {
            RuleFor(x => x.Codigo)
                .MaximumLength(50).WithMessage("El código no puede exceder 50 caracteres");

            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre del producto es requerido")
                .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres")
                .MinimumLength(3).WithMessage("El nombre debe tener al menos 3 caracteres");

            RuleFor(x => x.Descripcion)
                .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Descripcion));

            RuleFor(x => x.PrecioVenta)
                .GreaterThan(0).WithMessage("El precio de venta debe ser mayor a 0")
                .LessThanOrEqualTo(9999999.99m).WithMessage("El precio de venta no puede exceder 9,999,999.99");

            RuleFor(x => x.CatUnidadMedidaId)
                .GreaterThan(0).WithMessage("Debe seleccionar una unidad de medida válida");

            RuleFor(x => x.CatTipoItemId)
                .GreaterThan(0).WithMessage("Debe seleccionar un tipo de item válido");
        }
    }
}
