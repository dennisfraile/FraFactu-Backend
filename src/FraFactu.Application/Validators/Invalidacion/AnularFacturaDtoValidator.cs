using FluentValidation;
using FraFactu.Application.DTOs.Invalidacion;

namespace FraFactu.Application.Validators.Invalidacion
{
    public class AnularFacturaDtoValidator : AbstractValidator<AnularFacturaDto>
    {
        public AnularFacturaDtoValidator()
        {
            // Tipo de anulación
            RuleFor(x => x.TipoAnulacion)
                .InclusiveBetween(1, 3)
                .WithMessage("Tipo de anulación debe ser 1, 2 o 3");

            // Motivo requerido si tipo es 3
            When(x => x.TipoAnulacion == 3, () =>
            {
                RuleFor(x => x.MotivoAnulacion)
                    .NotEmpty()
                    .WithMessage("Motivo de anulación es requerido cuando tipo es 3 (Otro)")
                    .MinimumLength(5)
                    .MaximumLength(250);
            });

            // Factura de reemplazo requerida si tipo es 1 o 3
            When(x => x.TipoAnulacion == 1 || x.TipoAnulacion == 3, () =>
            {
                RuleFor(x => x.FacturaReemplazoId)
                    .NotNull()
                    .WithMessage("Factura de reemplazo es requerida para tipo 1 (Error) o 3 (Otro)");
            });

            // Factura de reemplazo debe ser null si tipo es 2
            When(x => x.TipoAnulacion == 2, () =>
            {
                RuleFor(x => x.FacturaReemplazoId)
                    .Null()
                    .WithMessage("No debe haber factura de reemplazo para tipo 2 (Rescisión)");
            });

            // Responsable
            RuleFor(x => x.NombreResponsable)
                .NotEmpty()
                .MinimumLength(5)
                .MaximumLength(100)
                .WithMessage("Nombre del responsable es requerido (5-100 caracteres)");

            RuleFor(x => x.CatTipoDocResponsableId)
                .InclusiveBetween(1, 5)
                .WithMessage("Debe seleccionar un tipo de documento válido del responsable (1-5)");

            RuleFor(x => x.NumDocResponsable)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(20)
                .WithMessage("Número de documento responsable es requerido (3-20 caracteres)");

            // Solicitante
            RuleFor(x => x.NombreSolicita)
                .NotEmpty()
                .MinimumLength(5)
                .MaximumLength(100)
                .WithMessage("Nombre del solicitante es requerido (5-100 caracteres)");

            RuleFor(x => x.CatTipoDocSolicitaId)
                .InclusiveBetween(1, 5)
                .WithMessage("Debe seleccionar un tipo de documento válido del solicitante (1-5)");

            RuleFor(x => x.NumDocSolicita)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(20)
                .WithMessage("Número de documento solicitante es requerido (3-20 caracteres)");
        }
    }
}
