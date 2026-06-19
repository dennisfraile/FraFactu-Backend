using FluentValidation;

namespace FraFactu.Application.Validators.Facturas
{
    /// <summary>
    /// Validador para Identificación del DTE según estándar de Hacienda
    /// </summary>
    public class IdentificacionDtoValidator : AbstractValidator<DTOs.Facturas.IdentificacionDto>
    {
        public IdentificacionDtoValidator()
        {
            RuleFor(x => x.Version)
                .Must((dto, version) =>
                {
                    // Según documentación MH (Normativa V2.0):
                    // - Factura (01) → version 2 (fe-f-v2.json)
                    // - CCF (03) → version 4 (fe-ccf-v4.json)
                    if (dto.TipoDte == "01") return version == 2;
                    if (dto.TipoDte == "03") return version == 4;
                    if (dto.TipoDte == "05") return version == 4;
                    if (dto.TipoDte == "06") return version == 4;
                    if (dto.TipoDte == "14") return version == 2;
                    return version >= 1 && version <= 10; // Otros tipos (rango válido)
                })
                .WithMessage("La versión no corresponde al tipo de DTE. Factura=2, CCF=4, NC=4, ND=4, FSE=2");

            // Tipos de DTE válidos según Hacienda:
            // 01: Factura, 03: CCF, 04: Nota de Remisión, 05: Nota de Crédito, 
            // 06: Nota de Débito, 07: Comp. Retención, 08: Comp. Liquidación,
            // 09: Doc. Contable Liquidación, 11: FEX, 14: FSE, 15: CSE
            RuleFor(x => x.TipoDte)
                .NotEmpty()
                .Must(tipo => new[] { "01", "03", "04", "05", "06", "07", "08", "09", "11", "14", "15" }.Contains(tipo))
                .WithMessage("El tipo de DTE debe ser válido según catálogo de Hacienda (01, 03, 04, 05, 06, 07, 08, 09, 11, 14, 15)");

            // Validación dinámica del Número de Control según el tipo de DTE (Normativa V2.0)
            // Formato: DTE-{tipoDte}-(M|B|S|P)###P###-{correlativo}
            // Ejemplo: DTE-01-M001P001-000000000000001 (31 caracteres)
            RuleFor(x => x.NumeroControl)
                .NotEmpty()
                .Length(31)
                .Must((dto, numeroControl) =>
                {
                    if (string.IsNullOrEmpty(dto.TipoDte) || string.IsNullOrEmpty(numeroControl))
                        return false;

                    // Patrón V2.0: DTE-{tipoDte}-(M|B|S|P)+3 dígitos+P+3 dígitos-{15 numéricos}
                    var patron = $@"^DTE-{dto.TipoDte}-(M|B|S|P)[0-9]{{3}}P[0-9]{{3}}-[0-9]{{15}}$";
                    return System.Text.RegularExpressions.Regex.IsMatch(numeroControl, patron);
                })
                .WithMessage("Número de control inválido. Formato: DTE-{tipoDte}-(M|B|S|P)###P###-000000000000000");

            RuleFor(x => x.CodigoGeneracion)
                .NotEmpty()
                .Length(36)
                .Matches(@"^[A-F0-9]{8}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{12}$")
                .WithMessage("Código de generación inválido. Debe ser un GUID válido");

            RuleFor(x => x.TipoModelo)
                .Must(x => x == 1 || x == 2)
                .WithMessage("Tipo de modelo debe ser 1 o 2");

            RuleFor(x => x.TipoOperacion)
                .Must(x => x == 1 || x == 2)
                .WithMessage("Tipo de operación debe ser 1 (Normal) o 2 (Contingencia)");

            RuleFor(x => x.FechaEmision)
                .NotEmpty()
                .LessThanOrEqualTo(DateTime.Now)
                .WithMessage("La fecha de emisión no puede ser posterior a hoy");

            // FSE (14): solo permite fechas hasta 1 mes de anterioridad
            RuleFor(x => x.FechaEmision)
                .GreaterThanOrEqualTo(DateTime.Now.AddMonths(-1).Date)
                .When(x => x.TipoDte == "14")
                .WithMessage("Para Sujeto Excluido, la fecha de emisión no puede ser anterior a 1 mes desde hoy");

            RuleFor(x => x.HoraEmision)
                .NotEmpty()
                .Matches(@"^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]$")
                .WithMessage("Formato de hora inválido. Use HH:mm:ss");

            RuleFor(x => x.TipoMoneda)
                .NotEmpty()
                .Equal("USD")
                .WithMessage("La moneda debe ser USD");
        }
    }
}
