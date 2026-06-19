using FluentValidation;

namespace FraFactu.Application.Validators.Facturas
{
    /// <summary>
    /// Validador para OtroDocumentoDto (específico de CCF)
    /// </summary>
    public class OtroDocumentoDtoValidator : AbstractValidator<DTOs.Facturas.OtroDocumentoDto>
    {
        public OtroDocumentoDtoValidator()
        {
            // CodDocAsociado: 1-4
            RuleFor(x => x.CodDocAsociado)
                .InclusiveBetween(1, 4)
                .WithMessage("El código de documento asociado debe estar entre 1 y 4");

            // Si CodDocAsociado = 3 (Médico), el campo Medico es requerido
            When(x => x.CodDocAsociado == 3, () =>
            {
                RuleFor(x => x.Medico)
                    .NotNull()
                    .WithMessage("El campo Medico es requerido cuando CodDocAsociado es 3")
                    .SetValidator(new MedicoDtoValidator()!);

                // DescDocumento y DetalleDocumento deben ser null
                RuleFor(x => x.DescDocumento)
                    .Null()
                    .WithMessage("DescDocumento debe ser null cuando CodDocAsociado es 3");

                RuleFor(x => x.DetalleDocumento)
                    .Null()
                    .WithMessage("DetalleDocumento debe ser null cuando CodDocAsociado es 3");
            });

            // Si CodDocAsociado != 3, DescDocumento y DetalleDocumento son requeridos
            When(x => x.CodDocAsociado != 3, () =>
            {
                RuleFor(x => x.DescDocumento)
                    .NotEmpty()
                    .WithMessage("La descripción del documento es requerida")
                    .MaximumLength(100)
                    .WithMessage("La descripción no puede exceder 100 caracteres");

                RuleFor(x => x.DetalleDocumento)
                    .NotEmpty()
                    .WithMessage("El detalle del documento es requerido")
                    .MaximumLength(300)
                    .WithMessage("El detalle no puede exceder 300 caracteres");

                // Medico debe ser null
                RuleFor(x => x.Medico)
                    .Null()
                    .WithMessage("El campo Medico debe ser null cuando CodDocAsociado no es 3");
            });
        }
    }
}
