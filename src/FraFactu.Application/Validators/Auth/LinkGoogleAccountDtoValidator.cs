using FluentValidation;

namespace FraFactu.Application.Validators.Auth
{
    /// <summary>
    /// Validador para LinkGoogleAccountDto
    /// </summary>
    public class LinkGoogleAccountDtoValidator : AbstractValidator<FraFactu.Application.DTOs.Auth.LinkGoogleAccountDto>
    {
        public LinkGoogleAccountDtoValidator()
        {
            RuleFor(x => x.IdToken)
                .NotEmpty().WithMessage("El ID Token de Google es requerido");
        }
    }
}
