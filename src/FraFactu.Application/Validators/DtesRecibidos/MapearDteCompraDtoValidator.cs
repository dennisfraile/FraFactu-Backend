using FluentValidation;
using FraFactu.Application.DTOs.DtesRecibidos;

namespace FraFactu.Application.Validators.DtesRecibidos;

/// <summary>
/// F1 (Plan inventario desde DTE): valida estructura del wizard de mapeo
/// antes de tocar BD. Las reglas que dependen de datos del emisor (sucursal,
/// productos, bodegas, codigos duplicados, tolerancia de total) se validan
/// en el service contra el contexto.
/// </summary>
public class MapearDteCompraDtoValidator : AbstractValidator<MapearDteCompraDto>
{
    public MapearDteCompraDtoValidator()
    {
        RuleFor(x => x.SucursalId)
            .GreaterThan(0).WithMessage("Debe indicar la sucursal donde se registrara la compra");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Debe mapear al menos un item del DTE");

        RuleForEach(x => x.Items).SetValidator(new MapearItemDtoValidator());
    }
}

public class MapearItemDtoValidator : AbstractValidator<MapearItemDto>
{
    public MapearItemDtoValidator()
    {
        RuleFor(x => x.Accion)
            .NotEmpty().WithMessage("La accion del item es requerida")
            .Must(a => AccionMapeoItem.Valores.Contains(a))
            .WithMessage($"Accion invalida. Valores permitidos: {string.Join(", ", AccionMapeoItem.Valores)}");

        // PRODUCTO_EXISTENTE: requiere ProductoId + datos de inventario.
        // Cascade=Stop para que NotNull dispare antes que GreaterThan (con null
        // GreaterThan reporta "valid" por design de FluentValidation y se pierde
        // el mensaje claro de "campo requerido").
        When(x => x.Accion == AccionMapeoItem.ProductoExistente, () =>
        {
            RuleFor(x => x.ProductoId)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("ProductoId es requerido para PRODUCTO_EXISTENTE")
                .GreaterThan(0).WithMessage("ProductoId debe ser mayor a 0");

            RuleFor(x => x.Cantidad)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("Cantidad es requerida para items de inventario")
                .GreaterThan(0).WithMessage("Cantidad debe ser mayor a 0");

            RuleFor(x => x.BodegaId)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("BodegaId es requerido para items de inventario")
                .GreaterThan(0).WithMessage("BodegaId debe ser mayor a 0");

            RuleFor(x => x.CostoUnitario)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("CostoUnitario es requerido para items de inventario")
                .GreaterThanOrEqualTo(0).WithMessage("CostoUnitario no puede ser negativo");
        });

        // PRODUCTO_NUEVO: lo mismo + datos para crear el ProductoServicio.
        When(x => x.Accion == AccionMapeoItem.ProductoNuevo, () =>
        {
            RuleFor(x => x.ProductoNuevo)
                .NotNull().WithMessage("Debe enviar los datos del producto nuevo");

            When(x => x.ProductoNuevo != null, () =>
            {
                // El codigo es opcional: si va vacio, el servicio lo autogenera
                // (PROD-/SERV- via ProductoCodigoGenerator), igual que el flujo
                // normal de productos. Solo validamos longitud cuando viene.
                RuleFor(x => x.ProductoNuevo!.Codigo)
                    .MaximumLength(50);

                RuleFor(x => x.ProductoNuevo!.Nombre)
                    .NotEmpty().WithMessage("El nombre del producto nuevo es requerido")
                    .MaximumLength(200);

                RuleFor(x => x.ProductoNuevo!.CatTipoItemId)
                    .GreaterThan(0).WithMessage("Debe seleccionar el tipo de item (Bien o Servicio)");

                RuleFor(x => x.ProductoNuevo!.CatUnidadMedidaId)
                    .GreaterThan(0).WithMessage("Debe seleccionar la unidad de medida");
            });

            RuleFor(x => x.Cantidad)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("Cantidad es requerida")
                .GreaterThan(0).WithMessage("Cantidad debe ser mayor a 0");

            RuleFor(x => x.BodegaId)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("BodegaId es requerido")
                .GreaterThan(0).WithMessage("BodegaId debe ser mayor a 0");

            RuleFor(x => x.CostoUnitario)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("CostoUnitario es requerido")
                .GreaterThanOrEqualTo(0).WithMessage("CostoUnitario no puede ser negativo");
        });

        // GASTO: solo el monto importa.
        When(x => x.Accion == AccionMapeoItem.Gasto, () =>
        {
            RuleFor(x => x.MontoDte)
                .GreaterThan(0).WithMessage("El monto del gasto debe ser mayor a 0");
        });
    }
}
