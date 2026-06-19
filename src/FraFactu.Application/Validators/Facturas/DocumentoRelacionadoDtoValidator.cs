using System.Text.RegularExpressions;
using FluentValidation;
using FraFactu.Application.DTOs.Facturas;

namespace FraFactu.Application.Validators.Facturas
{
    /// <summary>
    /// Validador para DocumentoRelacionadoDto
    /// Soporta documentos relacionados para NC (05), NR (04), Contingencia (09), etc.
    /// </summary>
    public class DocumentoRelacionadoDtoValidator : AbstractValidator<DocumentoRelacionadoDto>
    {
        // Tipos permitidos según MH: FCF(01), CCF(03), NR(04), NC(05), CR(07), DCL(09), FSE(14)
        private static readonly HashSet<string> TiposPermitidos = new()
        {
            "01", "03", "04", "05", "07", "09", "14"
        };

        public DocumentoRelacionadoDtoValidator()
        {
            // TipoDocumento: tipos válidos según MH
            RuleFor(x => x.TipoDocumento)
                .NotEmpty()
                .WithMessage("TipoDocumento es requerido")
                .Must(td => TiposPermitidos.Contains(td))
                .WithMessage($"TipoDocumento debe ser uno de: {string.Join(", ", TiposPermitidos)}");

            // TipoGeneracion: 1=Propio, 2=Externo
            RuleFor(x => x.TipoGeneracion)
                .InclusiveBetween(1, 2)
                .WithMessage("TipoGeneracion debe ser 1 (Propio) o 2 (Externo)");

            // NumeroDocumento: requerido
            RuleFor(x => x.NumeroDocumento)
                .NotEmpty()
                .WithMessage("NumeroDocumento es requerido")
                .MaximumLength(100)
                .WithMessage("NumeroDocumento no puede exceder 100 caracteres");

            // Si TipoGeneracion == 2 (Externo), NumeroDocumento debe ser UUID (36 chars)
            RuleFor(x => x.NumeroDocumento)
                .Must(BeValidUuid)
                .When(x => x.TipoGeneracion == 2)
                .WithMessage("Cuando TipoGeneracion es 2 (Externo), NumeroDocumento debe ser un UUID válido (36 caracteres)");

            // FechaEmision: no puede ser futura
            RuleFor(x => x.FechaEmision)
                .NotEmpty()
                .WithMessage("FechaEmision es requerida")
                .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
                .WithMessage("FechaEmision no puede ser futura");
        }

        private static bool BeValidUuid(string? value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            return value.Length == 36 && Guid.TryParse(value, out _);
        }
    }
}
