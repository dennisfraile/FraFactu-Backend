using FluentValidation;
using FraFactu.Application.DTOs.Compras;

namespace FraFactu.Application.Validators.Compras;

public class CrearCompraExternaDtoValidator : AbstractValidator<CrearCompraExternaDto>
{
    public CrearCompraExternaDtoValidator()
    {
        RuleFor(x => x.ProveedorId)
            .GreaterThan(0).WithMessage("Debe seleccionar un proveedor válido");

        RuleFor(x => x.NumeroFactura)
            .NotEmpty().WithMessage("El número de factura es requerido")
            .MaximumLength(100).WithMessage("El número de factura no puede exceder 100 caracteres");

        RuleFor(x => x.FechaEmision)
            .NotEmpty().WithMessage("La fecha de emisión es requerida")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("La fecha de emisión no puede ser futura");

        RuleFor(x => x.Subtotal)
            .GreaterThan(0).WithMessage("El subtotal debe ser mayor a 0");

        RuleFor(x => x.IVA)
            .GreaterThanOrEqualTo(0).WithMessage("El IVA no puede ser negativo");

        RuleFor(x => x.Total)
            .GreaterThan(0).WithMessage("El total debe ser mayor a 0")
            .Equal(x => x.Subtotal + x.IVA)
                .WithMessage("El total debe ser igual a Subtotal + IVA");

        // Validar que tenga al menos detalles o gastos
        RuleFor(x => x)
            .Must(x => x.Detalles.Any() || x.Gastos.Any())
            .WithMessage("La compra debe tener al menos un detalle o un gasto");

        // Validar detalles
        RuleFor(x => x.Detalles)
            .Must(detalles => detalles == null || detalles.Count <= 1000)
            .WithMessage("No puede haber más de 1000 detalles por compra");

        RuleForEach(x => x.Detalles)
            .SetValidator(new CrearCompraDetalleDtoValidator());

        // Validar gastos
        RuleForEach(x => x.Gastos)
            .SetValidator(new CrearGastoDtoValidator());

        // Validar que la suma de detalles + gastos = total
        RuleFor(x => x)
            .Must(x =>
            {
                var totalDetalles = x.Detalles.Sum(d => d.Total);
                var totalGastos = x.Gastos.Sum(g => g.Monto);
                var sumaCalculada = totalDetalles + totalGastos;
                var diferencia = Math.Abs(sumaCalculada - x.Total);
                return diferencia < 0.01m; // Tolerancia de 1 centavo
            })
            .WithMessage("La suma de detalles y gastos debe ser igual al total de la compra");
    }
}
