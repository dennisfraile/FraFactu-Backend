using FluentValidation;

namespace FraFactu.Application.Validators.Auth
{
    /// <summary>
    /// Validador para GoogleAuthDto
    /// </summary>
    public class GoogleAuthDtoValidator : AbstractValidator<FraFactu.Application.DTOs.Auth.GoogleAuthDto>
    {
        public GoogleAuthDtoValidator()
        {
            RuleFor(x => x.AccessToken)
                .NotEmpty().WithMessage("El access token de Google es requerido");
        }
    }
}
