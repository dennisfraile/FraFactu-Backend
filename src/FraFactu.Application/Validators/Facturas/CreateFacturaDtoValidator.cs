using FluentValidation;
using FraFactu.Application.DTOs.Facturas;

namespace FraFactu.Application.Validators.Facturas
{
    public class CreateFacturaDtoValidator : AbstractValidator<CreateFacturaDto>
    {
        public CreateFacturaDtoValidator()
        {
            RuleFor(x => x.ReceptorId)
                .GreaterThan(0).WithMessage("Debe seleccionar un receptor válido");

            RuleFor(x => x.Version)
                .InclusiveBetween(1, 10).WithMessage("La versión debe estar entre 1 y 10");

            RuleFor(x => x.Ambiente)
                .NotEmpty().WithMessage("El ambiente es requerido")
                .Must(amb => amb == "00" || amb == "01")
                .WithMessage("El ambiente debe ser 00 (Prueba) o 01 (Producción)");

            RuleFor(x => x.CatTipoDocumentoId)
                .GreaterThan(0).WithMessage("Debe seleccionar un tipo de documento válido");

            RuleFor(x => x.CatModeloFacturacionId)
                .InclusiveBetween(1, 2).WithMessage("El modelo de facturación debe ser 1 (Previo) o 2 (Diferido)");

            RuleFor(x => x.CatTipoTransmisionId)
                .InclusiveBetween(1, 3).WithMessage("El tipo de transmisión debe ser válido");

            RuleFor(x => x.FechaEmision)
                .NotEmpty().WithMessage("La fecha de emisión es requerida")
                .Must(fecha => fecha.Date <= DateTime.UtcNow.Date)
                .WithMessage("La fecha de emisión no puede ser futura");

            RuleFor(x => x.HoraEmision)
                .NotEmpty().WithMessage("La hora de emisión es requerida");

            RuleFor(x => x.CatMonedaId)
                .GreaterThan(0).WithMessage("Debe seleccionar una moneda válida");

            RuleFor(x => x.CatCondicionOperacionId)
                .GreaterThan(0).WithMessage("Debe seleccionar una condición de operación válida");

            RuleFor(x => x.PorcentajeDescuento)
                .InclusiveBetween(0, 100).WithMessage("El porcentaje de descuento debe estar entre 0 y 100");

            // VALIDACIONES DE CONTINGENCIA
            When(x => x.CatTipoContingenciaId.HasValue, () =>
            {
                RuleFor(x => x.MotivoContingencia)
                    .NotEmpty().WithMessage("Debe especificar el motivo de contingencia")
                    .MaximumLength(500).WithMessage("El motivo de contingencia no puede exceder 500 caracteres");
            });

            // VALIDACIONES DE DETALLES (CRÍTICO)
            RuleFor(x => x.Detalles)
                .NotNull().WithMessage("Debe incluir al menos un item en la factura")
                .Must(detalles => detalles != null && detalles.Any())
                .WithMessage("Debe incluir al menos un item en la factura");

            RuleForEach(x => x.Detalles)
                .SetValidator(new CreateFacturaDetalleDtoValidator());

            // VALIDACIONES DE PAGOS (CRÍTICO)
            RuleFor(x => x.Pagos)
                .NotNull().WithMessage("Debe incluir al menos una forma de pago")
                .Must(pagos => pagos != null && pagos.Any())
                .WithMessage("Debe incluir al menos una forma de pago");

            RuleForEach(x => x.Pagos)
                .SetValidator(new CreatePagoDtoValidator());

            // VALIDACIÓN CUSTOM: Total de pagos debe ser mayor a 0
            RuleFor(x => x.Pagos)
                .Must(pagos => pagos != null && pagos.Sum(p => p.Monto) > 0)
                .WithMessage("El total de pagos debe ser mayor a 0")
                .When(x => x.Pagos != null && x.Pagos.Any());
        }
    }
}
