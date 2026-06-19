using FluentValidation;

namespace FraFactu.Application.Validators.Facturas
{
    /// <summary>
    /// Validador para Dirección según estándar de Hacienda
    /// </summary>
    public class DireccionDtoValidator : AbstractValidator<DTOs.Facturas.DireccionDto>
    {
        public DireccionDtoValidator()
        {
            RuleFor(x => x.Departamento)
                .NotEmpty()
                .Matches(@"^(0[1-9]|1[0-4])$")
                .WithMessage("Departamento inválido. Debe ser entre 01 y 14");

            RuleFor(x => x.Municipio)
                .NotEmpty()
                .Matches(@"^[0-9]{2}$")
                .WithMessage("Municipio debe ser numérico de 2 dígitos");

            // CAT-008: toda dirección en el estándar MH V2.0 exige distrito.
            // Si el receptor envía dirección, el distrito es obligatorio.
            RuleFor(x => x.Distrito)
                .NotEmpty()
                .Matches(@"^[0-9]{2}$")
                .WithMessage("Distrito requerido (CAT-008), código numérico de 2 dígitos");

            RuleFor(x => x.Complemento)
                .NotEmpty()
                .MinimumLength(1)
                .MaximumLength(200)
                .WithMessage("Complemento requerido, máximo 200 caracteres");
        }
    }
}
