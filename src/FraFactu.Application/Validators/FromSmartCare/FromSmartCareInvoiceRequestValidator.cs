using FluentValidation;
using FraFactu.Application.DTOs;

namespace FraFactu.Application.Validators.FromSmartCare;

/// <summary>
/// Validator del payload que SmartCare manda al endpoint <c>POST /api/from-smartcare/prefills</c>.
/// Como el prefill solo levanta el wizard pre-cargado (no emite todavía), los campos
/// que el usuario completará en la UI (caja, forma de pago) son opcionales aquí.
/// </summary>
public class FromSmartCareInvoiceRequestValidator : AbstractValidator<FromSmartCareInvoiceRequestDto>
{
    private static readonly string[] TiposDteValidos = { "01", "03" };

    public FromSmartCareInvoiceRequestValidator()
    {
        RuleFor(x => x.CorrelationId)
            .NotEmpty().WithMessage("CorrelationId es obligatorio.")
            .Must(BeGuid).WithMessage("CorrelationId debe ser un UUID válido.");

        RuleFor(x => x.WebhookUrl)
            .NotEmpty().WithMessage("WebhookUrl es obligatorio.")
            .Must(BeHttpOrHttpsUrl).WithMessage("WebhookUrl debe ser una URL http(s) válida.");

        RuleFor(x => x.SucursalSmartixId).GreaterThan(0);

        RuleFor(x => x.TipoDte)
            .Must(t => TiposDteValidos.Contains(t))
            .WithMessage($"TipoDte debe ser uno de: {string.Join(", ", TiposDteValidos)}.");

        // FormaPagoSugerida es opcional — el usuario la elige en el wizard.
        // Si viene, debe ser un código no vacío.
        RuleFor(x => x.FormaPagoSugerida)
            .NotEmpty().When(x => x.FormaPagoSugerida is not null)
            .WithMessage("FormaPagoSugerida no puede ser cadena vacía si se especifica.");

        RuleFor(x => x.Receptor).NotNull().SetValidator(new FromSmartCareReceptorValidator());

        RuleFor(x => x.Lineas)
            .NotEmpty().WithMessage("Debe incluir al menos una línea.")
            .Must(l => l.Count <= 2000).WithMessage("No se admiten más de 2000 líneas.");

        RuleForEach(x => x.Lineas).SetValidator(new FromSmartCareInvoiceLineValidator());

        When(x => x.TipoDte == "03", () =>
        {
            RuleFor(x => x.Receptor.Nrc).NotEmpty().WithMessage("CCF requiere NRC del receptor.");
            RuleFor(x => x.Receptor.NumeroDocumento).NotEmpty().WithMessage("CCF requiere NIT del receptor.");
            RuleFor(x => x.Receptor.CodigoActividad).NotEmpty().WithMessage("CCF requiere código de actividad.");
            RuleFor(x => x.Receptor.DepartamentoId).NotNull().WithMessage("CCF requiere departamento.");
            RuleFor(x => x.Receptor.MunicipioId).NotNull().WithMessage("CCF requiere municipio.");
            RuleFor(x => x.Receptor.Direccion).NotEmpty().WithMessage("CCF requiere dirección.");
            RuleFor(x => x.Receptor.DistritoId).NotNull().WithMessage("CCF requiere distrito (CAT-008).");
        });
    }

    private static bool BeGuid(string value) => Guid.TryParse(value, out _);
    private static bool BeHttpOrHttpsUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var u)
        && (u.Scheme == Uri.UriSchemeHttps || u.Scheme == Uri.UriSchemeHttp);
}

internal class FromSmartCareReceptorValidator : AbstractValidator<FromSmartCareReceptorDto>
{
    public FromSmartCareReceptorValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Correo).EmailAddress().When(x => !string.IsNullOrEmpty(x.Correo));
    }
}

internal class FromSmartCareInvoiceLineValidator : AbstractValidator<FromSmartCareInvoiceLineDto>
{
    public FromSmartCareInvoiceLineValidator()
    {
        RuleFor(x => x.Cantidad).GreaterThan(0);
        RuleFor(x => x.PrecioUnitario).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Descripcion).NotEmpty().When(x => x.SmartixServicioId == null);
        RuleFor(x => x.TipoItem).InclusiveBetween(1, 4).When(x => x.TipoItem.HasValue);

        // Régimen del impuesto: si viene, debe ser 1, 2 o 3 (CAT MH).
        // Null permitido (clientes SmartCare legacy pre-2026-06-12).
        RuleFor(x => x.TipoImpuesto)
            .InclusiveBetween(1, 3)
            .When(x => x.TipoImpuesto.HasValue);

        // PorcentajeIVA: si viene, debe estar entre 0 y 100. Null permitido.
        RuleFor(x => x.PorcentajeIVA)
            .InclusiveBetween(0m, 100m)
            .When(x => x.PorcentajeIVA.HasValue);
    }
}
