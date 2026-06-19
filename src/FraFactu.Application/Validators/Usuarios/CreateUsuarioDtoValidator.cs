using FluentValidation;
using FraFactu.Application.DTOs.Usuarios;

namespace FraFactu.Application.Validators.Usuarios
{
    public class CreateUsuarioDtoValidator : AbstractValidator<CreateUsuarioDto>
    {
        public CreateUsuarioDtoValidator()
        {
            RuleFor(x => x.NombreCompleto)
                .NotEmpty().WithMessage("El nombre completo es requerido")
                .MaximumLength(200).WithMessage("El nombre completo no puede exceder 200 caracteres")
                .MinimumLength(3).WithMessage("El nombre completo debe tener al menos 3 caracteres");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El email es requerido")
                .EmailAddress().WithMessage("El email no tiene un formato válido")
                .MaximumLength(100).WithMessage("El email no puede exceder 100 caracteres");

            // Password es opcional (los usuarios inician sesión con Google)
            When(x => !string.IsNullOrEmpty(x.Password), () =>
            {
                RuleFor(x => x.Password)
                    .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres")
                    .Matches(@"[A-Z]").WithMessage("La contraseña debe contener al menos una letra mayúscula")
                    .Matches(@"[a-z]").WithMessage("La contraseña debe contener al menos una letra minúscula")
                    .Matches(@"[0-9]").WithMessage("La contraseña debe contener al menos un número");
            });

            RuleFor(x => x.EmisorId)
                .GreaterThan(0).WithMessage("Debe seleccionar un emisor válido");

            RuleFor(x => x.RolId)
                .GreaterThan(0).WithMessage("Debe seleccionar un rol válido");
        }
    }
}
