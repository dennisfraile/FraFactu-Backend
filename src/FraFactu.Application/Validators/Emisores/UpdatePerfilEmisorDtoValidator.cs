using FluentValidation;
using FraFactu.Application.DTOs.Emisores;

namespace FraFactu.Application.Validators.Emisores
{
    public class UpdatePerfilEmisorDtoValidator : AbstractValidator<UpdatePerfilEmisorDto>
    {
        public UpdatePerfilEmisorDtoValidator()
        {
            // Nombre Comercial (opcional)
            RuleFor(x => x.NombreComercial)
                .MaximumLength(150).WithMessage("El nombre comercial no puede exceder 150 caracteres")
                .When(x => !string.IsNullOrEmpty(x.NombreComercial));

            // Correo
            RuleFor(x => x.CorreoElectronico)
                .NotEmpty().WithMessage("El correo electrónico es requerido")
                .EmailAddress().WithMessage("Debe ser un correo electrónico válido")
                .MaximumLength(100).WithMessage("El correo no puede exceder 100 caracteres");

            // Teléfono
            RuleFor(x => x.Telefono)
                .NotEmpty().WithMessage("El teléfono es requerido")
                .Matches(@"^\d{4}-\d{4}$|^\d{8}$").WithMessage("Formato de teléfono inválido (ej: 2222-3333)");

            // Dirección
            RuleFor(x => x.Direccion)
                .NotEmpty().WithMessage("La dirección es requerida")
                .MinimumLength(5).WithMessage("La dirección debe tener al menos 5 caracteres")
                .MaximumLength(500).WithMessage("La dirección no puede exceder 500 caracteres");

            // NRC (opcional)
            RuleFor(x => x.Nrc)
                .Matches(@"^\d{1,8}(-\d)?$").WithMessage("El NRC debe tener entre 1 y 8 dígitos")
                .When(x => !string.IsNullOrEmpty(x.Nrc));

            // Código de Actividad (opcional)
            RuleFor(x => x.CodigoActividad)
                .Matches(@"^\d{5,6}$").WithMessage("El código de actividad debe tener 5 o 6 dígitos")
                .When(x => !string.IsNullOrEmpty(x.CodigoActividad));

            // Descripción de Actividad (opcional)
            RuleFor(x => x.DescripcionActividad)
                .MaximumLength(150).WithMessage("La descripción de actividad no puede exceder 150 caracteres")
                .When(x => !string.IsNullOrEmpty(x.DescripcionActividad));

            // Configuracion SMTP
            RuleFor(x => x.SmtpHost)
                .MaximumLength(200).WithMessage("El host SMTP no puede exceder 200 caracteres")
                .When(x => !string.IsNullOrEmpty(x.SmtpHost));

            RuleFor(x => x.SmtpPort)
                .InclusiveBetween(1, 65535).WithMessage("El puerto SMTP debe estar entre 1 y 65535")
                .When(x => x.SmtpPort.HasValue);

            RuleFor(x => x.SmtpUser)
                .MaximumLength(200).WithMessage("El usuario SMTP no puede exceder 200 caracteres")
                .When(x => !string.IsNullOrEmpty(x.SmtpUser));

            RuleFor(x => x.SmtpPassword)
                .MaximumLength(200).WithMessage("La contraseña SMTP no puede exceder 200 caracteres")
                .When(x => !string.IsNullOrEmpty(x.SmtpPassword));

            RuleFor(x => x.EmailRemitente)
                .EmailAddress().WithMessage("El email remitente debe ser un correo valido")
                .MaximumLength(200).WithMessage("El email remitente no puede exceder 200 caracteres")
                .When(x => !string.IsNullOrEmpty(x.EmailRemitente));
        }
    }
}
