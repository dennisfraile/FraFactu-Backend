using FluentValidation;
using FraFactu.Application.DTOs.Retorno;

namespace FraFactu.Application.Validators.Retorno;

/// <summary>
/// Validador para CrearEventoRetornoDto (Evento de Retorno 18, esquema fe-eret-v1.json).
/// </summary>
public class CrearEventoRetornoDtoValidator : AbstractValidator<CrearEventoRetornoDto>
{
    public CrearEventoRetornoDtoValidator()
    {
        // ==========================================
        // IDENTIFICACIÓN
        // ==========================================

        RuleFor(x => x.TipoModelo).InclusiveBetween(1, 2)
            .WithMessage("tipoModelo debe ser 1 (Normal) o 2 (Diferido)");
        RuleFor(x => x.TipoOperacion).InclusiveBetween(1, 2)
            .WithMessage("tipoOperacion debe ser 1 (Normal) o 2 (Contingencia)");
        RuleFor(x => x.TipoContingencia).InclusiveBetween(1, 5)
            .When(x => x.TipoContingencia.HasValue)
            .WithMessage("tipoContingencia debe estar entre 1 y 5");
        RuleFor(x => x.MotivoContin).MaximumLength(500).When(x => x.MotivoContin != null);
        RuleFor(x => x.Fusion).Length(9, 14).When(x => !string.IsNullOrEmpty(x.Fusion))
            .WithMessage("fusion debe tener entre 9 y 14 caracteres");

        // ==========================================
        // DOCUMENTOS RELACIONADOS (1 a 50)
        // ==========================================

        RuleFor(x => x.DocumentoRelacionado)
            .NotEmpty().WithMessage("Debe incluir al menos 1 documento relacionado")
            .Must(l => l == null || l.Count <= 50).WithMessage("Máximo 50 documentos relacionados");

        RuleForEach(x => x.DocumentoRelacionado).ChildRules(d =>
        {
            d.RuleFor(r => r.TipoDocumento).NotEmpty();
            d.RuleFor(r => r.CodigoGeneracion).NotEmpty().MaximumLength(36);
        });

        // ==========================================
        // CUERPO DEL DOCUMENTO (1 a 2000)
        // ==========================================

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Debe incluir al menos 1 ítem en el evento")
            .Must(l => l == null || l.Count <= 2000).WithMessage("Máximo 2000 ítems");

        RuleForEach(x => x.Items).SetValidator(new ItemRetornoDtoValidator());

        // ==========================================
        // TERCEROS: ventaTercero | compraTercero (oneOf, excluyentes)
        // ==========================================

        RuleFor(x => x)
            .Must(x => !(x.VentaTercero != null && x.CompraTercero != null))
            .WithMessage("ventaTercero y compraTercero son mutuamente excluyentes: no puede enviar ambos");

        // ==========================================
        // TRIBUTOS DE RESUMEN (opcional)
        // ==========================================

        When(x => x.Tributos != null, () =>
        {
            RuleForEach(x => x.Tributos!).ChildRules(t =>
            {
                t.RuleFor(r => r.Codigo).NotEmpty().Length(2);
                t.RuleFor(r => r.Descripcion).NotEmpty().MaximumLength(300);
            });
        });

        // ==========================================
        // APÉNDICE (opcional, 1 a 10)
        // ==========================================

        When(x => x.Apendice != null, () =>
        {
            RuleFor(x => x.Apendice!)
                .Must(l => l.Count >= 1 && l.Count <= 10)
                .WithMessage("El apéndice debe tener entre 1 y 10 entradas");

            RuleForEach(x => x.Apendice!).ChildRules(a =>
            {
                a.RuleFor(e => e.Campo).NotEmpty().MaximumLength(25);
                a.RuleFor(e => e.Etiqueta).NotEmpty().MaximumLength(50);
                a.RuleFor(e => e.Valor).NotEmpty().MaximumLength(150);
            });
        });
    }

    private class ItemRetornoDtoValidator : AbstractValidator<ItemRetornoDto>
    {
        public ItemRetornoDtoValidator()
        {
            RuleFor(i => i.CodigoGeneracion).NotEmpty().MaximumLength(36)
                .WithMessage("El código de generación del DTE del ítem es obligatorio");
            RuleFor(i => i.Cantidad).GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0");
            RuleFor(i => i.Descripcion).NotEmpty().MaximumLength(1500);
            RuleFor(i => i.UniMedida).InclusiveBetween(1, 99);
            RuleFor(i => i.MontoDescu).GreaterThanOrEqualTo(0);
            RuleFor(i => i.VentaNoSuj).GreaterThanOrEqualTo(0);
            RuleFor(i => i.VentaExenta).GreaterThanOrEqualTo(0);
            RuleFor(i => i.VentaGravada).GreaterThanOrEqualTo(0);
            RuleFor(i => i.Compra).GreaterThanOrEqualTo(0);
            RuleFor(i => i.Codigo).MaximumLength(25).When(i => i.Codigo != null);
            RuleFor(i => i.CodTributo).Length(2).When(i => i.CodTributo != null);

            When(i => i.Tributos != null, () =>
            {
                RuleFor(i => i.Tributos!)
                    .Must(l => l.Count >= 1).WithMessage("Si se envían tributos, debe haber al menos 1")
                    .Must(l => l.Count == l.Distinct().Count()).WithMessage("Los tributos del ítem no pueden repetirse");
                RuleForEach(i => i.Tributos!).Length(2);
            });
        }
    }
}
