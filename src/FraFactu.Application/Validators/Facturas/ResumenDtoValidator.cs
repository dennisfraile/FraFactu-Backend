using FluentValidation;

namespace FraFactu.Application.Validators.Facturas
{
    /// <summary>
    /// Validador para Resumen de Factura según estándar de Hacienda
    /// </summary>
    public class ResumenDtoValidator : AbstractValidator<DTOs.Facturas.ResumenDto>
    {
        public ResumenDtoValidator()
        {
            RuleFor(x => x.TotalNoSuj)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Total no sujeto no puede ser negativo");

            RuleFor(x => x.TotalExenta)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Total exento no puede ser negativo");

            RuleFor(x => x.TotalGravada)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Total gravado no puede ser negativo");

            RuleFor(x => x.TotalPagar)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Total a pagar no puede ser negativo");

            RuleFor(x => x.CondicionOperacion)
                .Must(x => x >= 1 && x <= 3)
                .WithMessage("Condición de operación debe ser 1 (Contado), 2 (Crédito) o 3 (Otro)");

            RuleFor(x => x.TotalLetras)
                .NotEmpty()
                .MaximumLength(200)
                .WithMessage("Total en letras requerido, máximo 200 caracteres");

            RuleFor(x => x.Pagos)
                .NotEmpty()
                .WithMessage("Debe especificar al menos una forma de pago");

            RuleForEach(x => x.Pagos)
                .SetValidator(new PagoDtoValidator());

            // Validar que la suma de pagos sea igual al total a pagar
            RuleFor(x => x)
                .Custom((resumen, context) =>
                {
                    if (resumen.Pagos != null && resumen.Pagos.Count > 0)
                    {
                        decimal sumaPagos = resumen.Pagos.Sum(p => p.Monto);
                        if (Math.Abs(sumaPagos - resumen.TotalPagar) > 0.01m)
                        {
                            context.AddFailure("Pagos",
                                $"La suma de los pagos ({sumaPagos:F2}) no coincide con el total a pagar ({resumen.TotalPagar:F2})");
                        }
                    }
                });

            // NOTA: La validación de IVA depende del TipoDte y se hace en CreateFacturaElectronicaDtoValidator
            // Para FACTURA (01), el IVA ya está incluido en los precios
            // Para CCF, el IVA se calcula como 13% de TotalGravada

            /*
            // Validar que TotalIva sea correcto (13% de TotalGravada)
            RuleFor(x => x)
                .Custom((resumen, context) =>
                {
                    decimal ivaCalculado = resumen.TotalGravada * 0.13m;
                    if (Math.Abs(ivaCalculado - resumen.TotalIva) > 0.01m)
                    {
                        context.AddFailure("TotalIva",
                            $"El IVA ({resumen.TotalIva:F2}) no coincide con el 13% de la venta gravada ({ivaCalculado:F2})");
                    }
                });
            */
        }
    }
}
