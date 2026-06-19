using FluentValidation;
using FraFactu.Application.DTOs.Inventario;

namespace FraFactu.Application.Validators.Inventario;

/// <summary>
/// Validator para CrearProductoDto
/// </summary>
public class CrearProductoDtoValidator : AbstractValidator<CrearProductoDto>
{
    public CrearProductoDtoValidator()
    {
        // Código
        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El código es requerido")
            .MaximumLength(50).WithMessage("El código no puede exceder 50 caracteres");

        // Código de barras
        RuleFor(x => x.CodigoBarras)
            .MaximumLength(50).WithMessage("El código de barras no puede exceder 50 caracteres")
            .When(x => !string.IsNullOrEmpty(x.CodigoBarras));

        // Nombre
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es requerido")
            .MaximumLength(300).WithMessage("El nombre no puede exceder 300 caracteres");

        // Descripción
        RuleFor(x => x.Descripcion)
            .MaximumLength(1000).WithMessage("La descripción no puede exceder 1000 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Descripcion));

        // Categoría
        RuleFor(x => x.CategoriaId)
            .GreaterThan(0).WithMessage("Debe seleccionar una categoría válida");

        // Unidad de medida
        RuleFor(x => x.CatUnidadMedidaId)
            .GreaterThan(0).WithMessage("Debe seleccionar una unidad de medida válida");

        // Stock
        RuleFor(x => x.StockMinimo)
            .GreaterThanOrEqualTo(0).WithMessage("El stock mínimo no puede ser negativo");

        RuleFor(x => x.StockMaximo)
            .GreaterThanOrEqualTo(0).WithMessage("El stock máximo no puede ser negativo")
            .GreaterThanOrEqualTo(x => x.StockMinimo)
                .WithMessage("El stock máximo debe ser mayor o igual al stock mínimo")
                .When(x => x.StockMaximo > 0);

        RuleFor(x => x.PuntoReorden)
            .GreaterThanOrEqualTo(0).WithMessage("El punto de reorden no puede ser negativo")
            .LessThanOrEqualTo(x => x.StockMaximo)
                .WithMessage("El punto de reorden debe ser menor o igual al stock máximo")
                .When(x => x.StockMaximo > 0);

        // Precios
        RuleFor(x => x.PrecioCosto)
            .GreaterThan(0).WithMessage("El precio de costo debe ser mayor a 0");

        RuleFor(x => x.PrecioVenta)
            .GreaterThan(0).WithMessage("El precio de venta debe ser mayor a 0");

        // IVA
        RuleFor(x => x.PorcentajeIVA)
            .InclusiveBetween(0, 100).WithMessage("El porcentaje de IVA debe estar entre 0 y 100")
            .When(x => x.AplicaIVA);
    }
}
