using FluentValidation;
using FraFactu.Application.DTOs.Compras;

namespace FraFactu.Application.Validators.Compras;

public class CrearProveedorDtoValidator : AbstractValidator<CrearProveedorDto>
{
    public CrearProveedorDtoValidator()
    {
        RuleFor(x => x.NIT)
            .NotEmpty().WithMessage("El NIT es requerido")
            .MaximumLength(20).WithMessage("El NIT no puede exceder 20 caracteres")
            .Matches(@"^[0-9]{14}$").WithMessage("El NIT debe contener exactamente 14 dígitos");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es requerido")
            .MaximumLength(300).WithMessage("El nombre no puede exceder 300 caracteres");

        RuleFor(x => x.NombreComercial)
            .MaximumLength(300).WithMessage("El nombre comercial no puede exceder 300 caracteres")
            .When(x => !string.IsNullOrEmpty(x.NombreComercial));

        RuleFor(x => x.Direccion)
            .MaximumLength(500).WithMessage("La dirección no puede exceder 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Direccion));

        RuleFor(x => x.Telefono)
            .MaximumLength(50).WithMessage("El teléfono no puede exceder 50 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Telefono));

        RuleFor(x => x.Email)
            .MaximumLength(200).WithMessage("El email no puede exceder 200 caracteres")
            .EmailAddress().WithMessage("El email debe ser válido")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Contacto)
            .MaximumLength(200).WithMessage("El contacto no puede exceder 200 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Contacto));

        RuleFor(x => x.SitioWeb)
            .MaximumLength(300).WithMessage("El sitio web no puede exceder 300 caracteres")
            .When(x => !string.IsNullOrEmpty(x.SitioWeb));

        RuleFor(x => x.Notas)
            .MaximumLength(1000).WithMessage("Las notas no pueden exceder 1000 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Notas));
    }
}
