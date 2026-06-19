using FluentValidation;
using FraFactu.Application.DTOs.Auth;

namespace FraFactu.Application.Validators.Auth
{
    public class CambiarPasswordDtoValidator : AbstractValidator<CambiarPasswordDto>
    {
        public CambiarPasswordDtoValidator()
        {
            RuleFor(x => x.PasswordActual)
                .NotEmpty().WithMessage("El password actual es requerido");

            RuleFor(x => x.PasswordNuevo)
                .NotEmpty().WithMessage("El nuevo password es requerido")
                .MinimumLength(8).WithMessage("El password debe tener al menos 8 caracteres")
                .Matches(@"[A-Z]").WithMessage("El password debe contener al menos una mayúscula")
                .Matches(@"[a-z]").WithMessage("El password debe contener al menos una minúscula")
                .Matches(@"[0-9]").WithMessage("El password debe contener al menos un número")
                .Matches(@"[\W_]").WithMessage("El password debe contener al menos un carácter especial");

            RuleFor(x => x.PasswordNuevoConfirmacion)
                .NotEmpty().WithMessage("La confirmación del password es requerida")
                .Equal(x => x.PasswordNuevo).WithMessage("Los passwords no coinciden");
        }
    }
}
