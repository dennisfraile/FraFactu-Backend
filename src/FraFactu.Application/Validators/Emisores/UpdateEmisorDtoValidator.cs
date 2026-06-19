using FluentValidation;
using FraFactu.Application.DTOs.Emisores;

namespace FraFactu.Application.Validators.Emisores
{
    public class UpdateEmisorDtoValidator : AbstractValidator<UpdateEmisorDto>
    {
        public UpdateEmisorDtoValidator()
        {
            // NIT
            RuleFor(x => x.Nit)
                .NotEmpty().WithMessage("El NIT es requerido")
                .Matches(@"^\d{9}$|^\d{14}$").WithMessage("El NIT debe tener 9 o 14 dígitos");

            // NRC
            RuleFor(x => x.Nrc)
                .NotEmpty().WithMessage("El NRC es requerido")
                .Matches(@"^\d{1,8}$").WithMessage("El NRC debe tener entre 1 y 8 dígitos");

            // Nombre/Razón Social
            RuleFor(x => x.NombreRazonSocial)
                .NotEmpty().WithMessage("El nombre o razón social es requerido")
                .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres");

            // Nombre Comercial (opcional)
            RuleFor(x => x.NombreComercial)
                .MaximumLength(150).WithMessage("El nombre comercial no puede exceder 150 caracteres")
                .When(x => !string.IsNullOrEmpty(x.NombreComercial));

            // Código Actividad
            RuleFor(x => x.CodigoActividad)
                .NotEmpty().WithMessage("El código de actividad es requerido")
                .Matches(@"^\d{5,6}$").WithMessage("El código debe tener 5 o 6 dígitos");

            // Descripción Actividad
            RuleFor(x => x.DescripcionActividad)
                .NotEmpty().WithMessage("La descripción de actividad es requerida")
                .MaximumLength(300).WithMessage("La descripción no puede exceder 300 caracteres");

            // Tipo Establecimiento (ID de catálogo, opcional en Emisor)
            RuleFor(x => x.CatTipoEstablecimientoId)
                .GreaterThan(0).WithMessage("El tipo de establecimiento debe ser mayor a 0")
                .LessThanOrEqualTo(20).WithMessage("Tipo de establecimiento inválido")
                .When(x => x.CatTipoEstablecimientoId.HasValue);

            // Correo
            RuleFor(x => x.CorreoElectronico)
                .NotEmpty().WithMessage("El correo electrónico es requerido")
                .EmailAddress().WithMessage("Debe ser un correo electrónico válido")
                .MaximumLength(100).WithMessage("El correo no puede exceder 100 caracteres");

            // Teléfono
            RuleFor(x => x.Telefono)
                .NotEmpty().WithMessage("El teléfono es requerido")
                .Matches(@"^\d{4}-\d{4}$|^\d{8}$").WithMessage("Formato de teléfono inválido (ej: 2222-3333)");

            // Departamento (ID de catálogo)
            RuleFor(x => x.CatDepartamentoId)
                .GreaterThan(0).WithMessage("El departamento es requerido")
                .LessThanOrEqualTo(14).WithMessage("ID de departamento inválido (1-14)");

            // Municipio (ID de catálogo)
            RuleFor(x => x.CatMunicipioId)
                .GreaterThan(0).WithMessage("El municipio es requerido");

            // Dirección
            RuleFor(x => x.Direccion)
                .NotEmpty().WithMessage("La dirección es requerida")
                .MinimumLength(5).WithMessage("La dirección debe tener al menos 5 caracteres")
                .MaximumLength(500).WithMessage("La dirección no puede exceder 500 caracteres");

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
