using FluentValidation;

namespace FraFactu.Application.Validators.Facturas
{
    /// <summary>
    /// Validador para Receptor en DTE según estándar de Hacienda
    /// </summary>
    public class ReceptorDteDtoValidator : AbstractValidator<DTOs.Facturas.ReceptorDteDto>
    {
        public ReceptorDteDtoValidator()
        {
            RuleFor(x => x.TipoDocumento)
                .Matches(@"^(36|13|02|03|37)$")
                .When(x => !string.IsNullOrEmpty(x.TipoDocumento))
                .WithMessage("Tipo de documento inválido. Debe ser: 36 (NIT), 13 (DUI), 02 (Carnet Residente), 03 (Pasaporte), 37 (Otro)");

            // NumDocumento opcional cuando el receptor va "Sin documento" (FC tipo 01 admite tipoDoc+numDoc null).
            // Cuando viene presente, se exige formato 3-20 caracteres.
            RuleFor(x => x.NumDocumento)
                .MinimumLength(3)
                .MaximumLength(20)
                .When(x => !string.IsNullOrEmpty(x.NumDocumento))
                .WithMessage("Número de documento entre 3 y 20 caracteres");

            RuleFor(x => x.Nombre)
                .NotEmpty()
                .MaximumLength(250)
                .WithMessage("Nombre requerido, máximo 250 caracteres");

            // Validación específica para NIT
            When(x => x.TipoDocumento == "36", () =>
            {
                RuleFor(x => x.NumDocumento)
                    .Matches(@"^([0-9]{14}|[0-9]{9})$")
                    .WithMessage("Para NIT, número debe tener 9 o 14 dígitos");
            });

            // Validación específica para DUI
            When(x => x.TipoDocumento == "13", () =>
            {
                RuleFor(x => x.NumDocumento)
                    .Matches(@"^[0-9]{8}-[0-9]{1}$")
                    .WithMessage("Formato de DUI inválido. Use XXXXXXXX-X");
            });

            RuleFor(x => x.Correo)
                .EmailAddress()
                .When(x => !string.IsNullOrEmpty(x.Correo))
                .WithMessage("Correo electrónico inválido");

            When(x => x.Direccion != null, () =>
            {
                RuleFor(x => x.Direccion!)
                    .SetValidator(new DireccionDtoValidator());
            });
        }
    }
}
