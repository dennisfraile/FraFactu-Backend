using FluentValidation;

namespace FraFactu.Application.Validators.Facturas
{
    /// <summary>
    /// Validador para MedicoDto (parte de OtrosDocumentos en CCF)
    /// </summary>
    public class MedicoDtoValidator : AbstractValidator<DTOs.Facturas.MedicoDto>
    {
        public MedicoDtoValidator()
        {
            // Nombre requerido, max 100 caracteres
            RuleFor(x => x.Nombre)
                .NotEmpty()
                .WithMessage("El nombre del médico es requerido")
                .MaximumLength(100)
                .WithMessage("El nombre no puede exceder 100 caracteres");

            // NIT: 9 o 14 dígitos (si se proporciona)
            RuleFor(x => x.Nit)
                .Matches(@"^([0-9]{14}|[0-9]{9})$")
                .WithMessage("El NIT debe tener 9 o 14 dígitos")
                .When(x => !string.IsNullOrEmpty(x.Nit));

            // DocIdentificacion: 2-25 caracteres (si se proporciona)
            RuleFor(x => x.DocIdentificacion)
                .Length(2, 25)
                .WithMessage("El documento de identificación debe tener entre 2 y 25 caracteres")
                .When(x => !string.IsNullOrEmpty(x.DocIdentificacion));

            // Validación XOR: Nit O DocIdentificacion debe estar presente (no ambos vacíos)
            RuleFor(x => x)
                .Must(x => !string.IsNullOrEmpty(x.Nit) || !string.IsNullOrEmpty(x.DocIdentificacion))
                .WithMessage("Debe proporcionar NIT o Documento de Identificación")
                .WithName("Identificación");

            // TipoServicio: 1-6
            RuleFor(x => x.TipoServicio)
                .InclusiveBetween(1, 6)
                .WithMessage("El tipo de servicio debe estar entre 1 y 6");
        }
    }
}
