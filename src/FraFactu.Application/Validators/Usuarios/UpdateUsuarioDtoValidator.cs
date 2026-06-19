using FluentValidation;
using FraFactu.Application.DTOs.Usuarios;

namespace FraFactu.Application.Validators.Usuarios
{
    public class UpdateUsuarioDtoValidator : AbstractValidator<UpdateUsuarioDto>
    {
        public UpdateUsuarioDtoValidator()
        {
            RuleFor(x => x.NombreCompleto)
                .NotEmpty().WithMessage("El nombre completo es requerido")
                .MaximumLength(200).WithMessage("El nombre completo no puede exceder 200 caracteres")
                .MinimumLength(3).WithMessage("El nombre completo debe tener al menos 3 caracteres");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El email es requerido")
                .EmailAddress().WithMessage("El email no tiene un formato válido")
                .MaximumLength(100).WithMessage("El email no puede exceder 100 caracteres");

            RuleFor(x => x.RolId)
                .GreaterThan(0).WithMessage("Debe seleccionar un rol válido");
        }
    }
}
