using FluentValidation;
using FraFactu.Application.DTOs.Facturas;

namespace FraFactu.Application.Validators.Facturas
{
    public class CreateFacturaDetalleDtoValidator : AbstractValidator<CreateFacturaDetalleDto>
    {
        public CreateFacturaDetalleDtoValidator()
        {
            RuleFor(x => x.NumeroItem)
                .GreaterThan(0).WithMessage("El número de item debe ser mayor a 0");

            RuleFor(x => x.CatTipoItemId)
                .GreaterThan(0).WithMessage("Debe seleccionar un tipo de item válido");

            RuleFor(x => x.Cantidad)
                .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0")
                .LessThanOrEqualTo(9999999.99m).WithMessage("La cantidad no puede exceder 9,999,999.99");

            RuleFor(x => x.CatUnidadMedidaId)
                .GreaterThan(0).WithMessage("Debe seleccionar una unidad de medida válida");

            RuleFor(x => x.Descripcion)
                .NotEmpty().WithMessage("La descripción es requerida")
                .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres");

            RuleFor(x => x.PrecioUnitario)
                .GreaterThan(0).WithMessage("El precio unitario debe ser mayor a 0")
                .LessThanOrEqualTo(9999999.99m).WithMessage("El precio unitario no puede exceder 9,999,999.99");

            RuleFor(x => x.MontoDescuento)
                .GreaterThanOrEqualTo(0).WithMessage("El monto de descuento no puede ser negativo");

            // Validar que solo uno de los tipos de venta esté con valor
            RuleFor(x => x)
                .Must(dto =>
                {
                    var tiposConValor = new[] { dto.VentaNoSujeta, dto.VentaExenta, dto.VentaGravada }
                        .Count(v => v > 0);
                    return tiposConValor == 1;
                })
                .WithMessage("Solo UNO de los tipos de venta (No Sujeta, Exenta o Gravada) debe tener valor mayor a 0");
        }
    }
}
