using FluentValidation;

namespace FraFactu.Application.Validators.Facturas
{
    /// <summary>
    /// Validador para VentaTerceroDto (específico de CCF)
    /// </summary>
    public class VentaTerceroDtoValidator : AbstractValidator<DTOs.Facturas.VentaTerceroDto>
    {
        public VentaTerceroDtoValidator()
        {
            // NIT requerido, 9 o 14 dígitos
            RuleFor(x => x.Nit)
                .NotEmpty()
                .WithMessage("El NIT del tercero es requerido")
                .Matches(@"^([0-9]{14}|[0-9]{9})$")
                .WithMessage("El NIT debe tener 9 o 14 dígitos");

            // Nombre requerido, 3-200 caracteres
            RuleFor(x => x.Nombre)
                .NotEmpty()
                .WithMessage("El nombre del tercero es requerido")
                .Length(3, 200)
                .WithMessage("El nombre debe tener entre 3 y 200 caracteres");
        }
    }
}
