using FluentValidation;
using FraFactu.Application.DTOs.Inventario;

namespace FraFactu.Application.Validators.Inventario;

public class SalidaInventarioDtoValidator : AbstractValidator<SalidaInventarioDto>
{
    public SalidaInventarioDtoValidator()
    {
        RuleFor(x => x.ProductoId)
            .GreaterThan(0).WithMessage("El producto es requerido");

        RuleFor(x => x.BodegaId)
            .GreaterThan(0).WithMessage("La bodega es requerida");

        RuleFor(x => x.Cantidad)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0");

        RuleFor(x => x.TipoDocumento)
            .NotEmpty().WithMessage("El tipo de documento es requerido")
            .Must(tipo => new[] { "VENTA", "DEVOLUCION_PROVEEDOR", "MERMA", "USO_INTERNO", "AJUSTE", "OTRO" }
                .Contains(tipo.ToUpper()))
            .WithMessage("Tipo de documento inválido");
    }
}
