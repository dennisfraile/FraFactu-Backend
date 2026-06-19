using FluentValidation;

namespace FraFactu.Application.Validators.Facturas
{
    /// <summary>
    /// Validador para Ítem de Documento según estándar de Hacienda
    /// </summary>
    public class ItemDocumentoDtoValidator : AbstractValidator<DTOs.Facturas.ItemDocumentoDto>
    {
        public ItemDocumentoDtoValidator()
        {
            RuleFor(x => x.NumItem)
                .GreaterThanOrEqualTo(1)
                .LessThanOrEqualTo(2000)
                .WithMessage("Número de ítem entre 1 y 2000");

            RuleFor(x => x.TipoItem)
                .Must(x => x >= 1 && x <= 4)
                .WithMessage("Tipo de ítem debe ser 1 (Bien), 2 (Servicio), 3 (Ambos) o 4 (Otros)");

            RuleFor(x => x.Cantidad)
                .GreaterThan(0)
                .WithMessage("Cantidad debe ser mayor a 0");

            RuleFor(x => x.Descripcion)
                .NotEmpty()
                .MaximumLength(1000)
                .WithMessage("Descripción requerida, máximo 1000 caracteres");

            RuleFor(x => x.PrecioUni)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Precio unitario no puede ser negativo");

            RuleFor(x => x.VentaGravada)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Venta gravada no puede ser negativa");

            RuleFor(x => x.VentaExenta)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Venta exenta no puede ser negativa");

            RuleFor(x => x.VentaNoSuj)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Venta no sujeta no puede ser negativa");

            RuleFor(x => x.IvaItem)
                .GreaterThanOrEqualTo(0)
                .WithMessage("IVA del ítem no puede ser negativo");

            RuleFor(x => x.UniMedida)
                .GreaterThanOrEqualTo(1)
                .LessThanOrEqualTo(99)
                .WithMessage("Unidad de medida entre 1 y 99");

            // ===================================================================
            // VALIDACIONES DE FÓRMULAS SEGÚN MINISTERIO DE HACIENDA
            // ===================================================================

            // Validar que al menos uno de los tipos de venta/compra tenga valor
            RuleFor(x => x)
                .Custom((item, context) =>
                {
                    // FSE (tipo 14): usa Compra en vez de VentaGravada/Exenta/NoSuj
                    bool esItemFSE = item.Compra.HasValue && item.Compra.Value > 0;

                    if (!esItemFSE && item.VentaGravada == 0 && item.VentaExenta == 0 && item.VentaNoSuj == 0
                        && (!item.NoGravado.HasValue || item.NoGravado.Value == 0))
                    {
                        context.AddFailure("VentaGravada",
                            "Al menos uno de los tipos de venta (Gravada, Exenta, No Sujeta) o Compra (FSE) debe tener un valor mayor a 0");
                    }

                    // FSE: Validar fórmula compra = (precioUni × cantidad) - montoDescu
                    if (esItemFSE)
                    {
                        decimal montoBase = item.PrecioUni * item.Cantidad;
                        decimal descuento = item.MontoDescuento ?? 0m;
                        decimal compraEsperada = montoBase - descuento;

                        if (Math.Abs(compraEsperada - item.Compra!.Value) > 0.01m)
                        {
                            context.AddFailure("Compra",
                                $"Compra ({item.Compra.Value:F2}) no coincide con el cálculo esperado " +
                                $"(PrecioUni × Cantidad - Descuento = {compraEsperada:F2})");
                        }
                    }

                    // FÓRMULA 1: ventaGravada = (precioUni × cantidad) - descuentoItem
                    // Validar que VentaGravada se calcule correctamente
                    if (item.VentaGravada > 0)
                    {
                        decimal montoBase = item.PrecioUni * item.Cantidad;
                        decimal descuento = item.MontoDescuento ?? 0m;
                        decimal ventaEsperada = montoBase - descuento;

                        // Tolerancia de 0.01 para redondeos
                        if (Math.Abs(ventaEsperada - item.VentaGravada) > 0.01m)
                        {
                            context.AddFailure("VentaGravada",
                                $"VentaGravada ({item.VentaGravada:F2}) no coincide con el cálculo esperado " +
                                $"(PrecioUni × Cantidad - Descuento = {ventaEsperada:F2})");
                        }

                        // FÓRMULA 2: ivaItem
                        // NOTA: La validación específica de IVA se hace en CreateFacturaElectronicaDtoValidator
                        // donde se tiene acceso al TipoDte para aplicar la fórmula correcta:
                        // - FACTURA (01): ivaItem = (ventaGravada / 1.13) × 0.13 (ingeniería inversa)
                        // - CCF (03): ivaItem = ventaGravada × 0.13 (directo)
                        // Aquí solo validamos que no sea negativo
                        if (item.IvaItem < 0)
                        {
                            context.AddFailure("IvaItem",
                                "IvaItem no puede ser negativo");
                        }
                    }

                    // Validar que ítems Exentos/No Sujetos no tengan IVA
                    if ((item.VentaExenta > 0 || item.VentaNoSuj > 0) && item.IvaItem > 0)
                    {
                        context.AddFailure("IvaItem",
                            "Los ítems Exentos o No Sujetos no pueden tener IVA");
                    }

                    // ADVERTENCIA MH: Ítems Exentos o No Sujetos NO pueden tener tributos
                    // Nota: NoGravado es campo obligatorio (number) en MH para CCF, no implica incompatibilidad con tributos
                    if ((item.VentaExenta > 0 || item.VentaNoSuj > 0)
                        && item.Tributos != null && item.Tributos.Any())
                    {
                        context.AddFailure("Tributos",
                            "Los ítems Exentos, No Sujetos o No Afectos NO pueden tener tributos asociados según regulación del MH");
                    }
                });

            // PSV (Precio Sugerido Venta) - específico CCF, opcional
            RuleFor(x => x.Psv)
                .GreaterThanOrEqualTo(0)
                .WithMessage("El precio sugerido de venta no puede ser negativo")
                .When(x => x.Psv.HasValue);

            // NoGravado - específico CCF, opcional
            RuleFor(x => x.NoGravado)
                .Must(val => val >= -100000000000m && val < 100000000000m)
                .WithMessage("NoGravado debe estar entre -100 billones y 100 billones")
                .When(x => x.NoGravado.HasValue);

            // CodTributo - requerido cuando TipoItem=4 (Impuesto)
            When(x => x.TipoItem == 4, () =>
            {
                RuleFor(x => x.CodTributo)
                    .NotEmpty()
                    .WithMessage("CodTributo es requerido cuando TipoItem es 4 (Impuesto)");

                RuleFor(x => x.CodTributo)
                    .Must(cod => new[] { "A8", "57", "90", "D4", "D5", "25", "A6" }.Contains(cod))
                    .WithMessage("CodTributo debe ser uno de: A8, 57, 90, D4, D5, 25, A6")
                    .When(x => !string.IsNullOrEmpty(x.CodTributo));
            });
        }
    }
}
