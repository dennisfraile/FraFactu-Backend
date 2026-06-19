using FluentValidation;
using FraFactu.Application.DTOs.Compras;

namespace FraFactu.Application.Validators.Compras;

public class CrearCompraDetalleDtoValidator : AbstractValidator<CrearCompraDetalleDto>
{
    public CrearCompraDetalleDtoValidator()
    {
        RuleFor(x => x.ProductoId)
            .GreaterThan(0).WithMessage("Debe seleccionar un producto válido");

        RuleFor(x => x.BodegaId)
            .GreaterThan(0).WithMessage("Debe seleccionar una bodega válida")
            .When(x => x.EsParaInventario); // Solo requerido si es para inventario

        RuleFor(x => x.Cantidad)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0");

        RuleFor(x => x.CostoUnitario)
            .GreaterThan(0).WithMessage("El costo unitario debe ser mayor a 0");

        RuleFor(x => x.Subtotal)
            .GreaterThanOrEqualTo(0).WithMessage("El subtotal no puede ser negativo");

        RuleFor(x => x.IVA)
            .GreaterThanOrEqualTo(0).WithMessage("El IVA no puede ser negativo");

        RuleFor(x => x.Total)
            .GreaterThan(0).WithMessage("El total debe ser mayor a 0")
            .Equal(x => x.Subtotal + x.IVA)
                .WithMessage("El total debe ser igual a Subtotal + IVA");

        // Validar que Subtotal sea consistente con Cantidad * CostoUnitario
        RuleFor(x => x.Subtotal)
            .Must((dto, subtotal) =>
            {
                var subtotalCalculado = dto.Cantidad * dto.CostoUnitario;
                var diferencia = Math.Abs(subtotalCalculado - subtotal);
                return diferencia < 0.01m; // Tolerancia de 1 centavo
            })
            .WithMessage("El subtotal debe ser igual a Cantidad × Costo Unitario");
    }
}
