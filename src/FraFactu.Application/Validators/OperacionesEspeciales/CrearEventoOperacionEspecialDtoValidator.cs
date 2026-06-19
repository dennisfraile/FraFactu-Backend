using FluentValidation;
using FraFactu.Application.DTOs.OperacionesEspeciales;

namespace FraFactu.Application.Validators.OperacionesEspeciales;

/// <summary>
/// Validador para CrearEventoOperacionEspecialDto.
/// Implementa las reglas del esquema fe-eop-v1.json (Operaciones Especiales 17).
/// </summary>
public class CrearEventoOperacionEspecialDtoValidator : AbstractValidator<CrearEventoOperacionEspecialDto>
{
    private static readonly System.Text.RegularExpressions.Regex UuidRegex =
        new(@"^[A-F0-9]{8}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{12}$",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    public CrearEventoOperacionEspecialDtoValidator()
    {
        // ==========================================
        // CUERPO DEL DOCUMENTO (1 a 2000 ítems)
        // ==========================================

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Debe incluir al menos 1 ítem en el evento")
            .Must(list => list == null || list.Count <= 2000)
            .WithMessage("Máximo 2000 ítems por evento de operaciones especiales");

        RuleForEach(x => x.Items).SetValidator(new ItemOperacionEspecialDtoValidator());

        // ==========================================
        // TRIBUTOS DE RESUMEN (opcional)
        // ==========================================

        When(x => x.Tributos != null, () =>
        {
            RuleForEach(x => x.Tributos!).ChildRules(t =>
            {
                t.RuleFor(r => r.Codigo)
                    .NotEmpty().Length(2).WithMessage("El código de tributo debe tener 2 caracteres");
                t.RuleFor(r => r.Descripcion)
                    .NotEmpty().MaximumLength(300)
                    .WithMessage("La descripción del tributo es obligatoria (máx. 300 caracteres)");
            });
        });

        // ==========================================
        // APÉNDICE (opcional, 1 a 10 entradas)
        // ==========================================

        When(x => x.Apendice != null, () =>
        {
            RuleFor(x => x.Apendice!)
                .Must(list => list.Count >= 1 && list.Count <= 10)
                .WithMessage("El apéndice debe tener entre 1 y 10 entradas");

            RuleForEach(x => x.Apendice!).ChildRules(a =>
            {
                a.RuleFor(e => e.Campo).NotEmpty().MaximumLength(25);
                a.RuleFor(e => e.Etiqueta).NotEmpty().MaximumLength(50);
                a.RuleFor(e => e.Valor).NotEmpty().MaximumLength(150);
            });
        });
    }

    private class ItemOperacionEspecialDtoValidator : AbstractValidator<ItemOperacionEspecialDto>
    {
        public ItemOperacionEspecialDtoValidator()
        {
            RuleFor(i => i.TipoDocumento)
                .NotEmpty().WithMessage("El tipo de documento del ítem es obligatorio");

            RuleFor(i => i.Cantidad)
                .GreaterThanOrEqualTo(1).WithMessage("La cantidad debe ser mayor o igual a 1");

            RuleFor(i => i.Descripcion)
                .NotEmpty().MaximumLength(1500)
                .WithMessage("La descripción es obligatoria (máx. 1500 caracteres)");

            RuleFor(i => i.PrecioUni).GreaterThanOrEqualTo(0);
            RuleFor(i => i.VentaNoSuj).GreaterThanOrEqualTo(0);
            RuleFor(i => i.VentaExenta).GreaterThanOrEqualTo(0);
            RuleFor(i => i.VentaGravada).GreaterThanOrEqualTo(0);

            RuleFor(i => i.CodigoGeneracionRef)
                .Must(c => c == null || UuidRegex.IsMatch(c))
                .WithMessage("El código de generación de referencia debe ser un UUID válido en mayúsculas");

            RuleFor(i => i.NumDocumento)
                .MaximumLength(36).When(i => i.NumDocumento != null);

            RuleFor(i => i.DocDel)
                .MaximumLength(36).When(i => i.DocDel != null);

            RuleFor(i => i.DocAl)
                .MaximumLength(36).When(i => i.DocAl != null);

            When(i => i.Tributos != null, () =>
            {
                RuleFor(i => i.Tributos!)
                    .Must(list => list.Count >= 1).WithMessage("Si se envían tributos, debe haber al menos 1")
                    .Must(list => list.Count == list.Distinct().Count()).WithMessage("Los tributos del ítem no pueden repetirse");
                RuleForEach(i => i.Tributos!)
                    .Length(2).WithMessage("Cada código de tributo debe tener 2 caracteres");
            });
        }
    }
}
