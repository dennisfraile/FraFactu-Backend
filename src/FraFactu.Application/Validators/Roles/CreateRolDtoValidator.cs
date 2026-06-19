using FluentValidation;
using FraFactu.Application.DTOs.Roles;

namespace FraFactu.Application.Validators.Roles
{
    public class CreateRolDtoValidator : AbstractValidator<CreateRolDto>
    {
        public CreateRolDtoValidator()
        {
            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre del rol es requerido")
                .MaximumLength(100).WithMessage("El nombre del rol no puede exceder 100 caracteres")
                .MinimumLength(3).WithMessage("El nombre del rol debe tener al menos 3 caracteres");

            RuleFor(x => x.PermisosIds)
                .NotNull().WithMessage("Debe asignar al menos un permiso")
                .Must(permisos => permisos != null && permisos.Any())
                .WithMessage("Debe asignar al menos un permiso al rol");

            RuleForEach(x => x.PermisosIds)
                .GreaterThan(0).WithMessage("Los IDs de permisos deben ser válidos");
        }
    }
}
