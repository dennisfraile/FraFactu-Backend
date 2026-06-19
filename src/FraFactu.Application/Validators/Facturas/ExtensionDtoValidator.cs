using FluentValidation;
using FraFactu.Application.DTOs.Facturas;

namespace FraFactu.Application.Validators.Facturas
{
    /// <summary>
    /// Validador para campos de extensión de factura (información de entrega)
    /// </summary>
    public class ExtensionDtoValidator : AbstractValidator<ExtensionDto>
    {
        public ExtensionDtoValidator()
        {
            // NombEntrega - Máximo 100 caracteres
            When(x => !string.IsNullOrEmpty(x.NombEntrega), () =>
            {
                RuleFor(x => x.NombEntrega)
                    .MaximumLength(100)
                    .WithMessage("El nombre de quien entrega no puede exceder 100 caracteres");
            });

            // DocuEntrega - Formato documento (solo alfanumérico y guiones)
            When(x => !string.IsNullOrEmpty(x.DocuEntrega), () =>
            {
                RuleFor(x => x.DocuEntrega)
                    .MaximumLength(25)
                    .WithMessage("El documento de quien entrega no puede exceder 25 caracteres")
                    .Matches(@"^[a-zA-Z0-9\-]+$")
                    .WithMessage("El documento de quien entrega solo puede contener letras, números y guiones");
            });

            // NombRecibe - Máximo 100 caracteres
            When(x => !string.IsNullOrEmpty(x.NombRecibe), () =>
            {
                RuleFor(x => x.NombRecibe)
                    .MaximumLength(100)
                    .WithMessage("El nombre de quien recibe no puede exceder 100 caracteres");
            });

            // DocuRecibe - Formato documento (solo alfanumérico y guiones)
            When(x => !string.IsNullOrEmpty(x.DocuRecibe), () =>
            {
                RuleFor(x => x.DocuRecibe)
                    .MaximumLength(25)
                    .WithMessage("El documento de quien recibe no puede exceder 25 caracteres")
                    .Matches(@"^[a-zA-Z0-9\-]+$")
                    .WithMessage("El documento de quien recibe solo puede contener letras, números y guiones");
            });

            // Observaciones - Máximo 3000 caracteres
            When(x => !string.IsNullOrEmpty(x.Observaciones), () =>
            {
                RuleFor(x => x.Observaciones)
                    .MaximumLength(3000)
                    .WithMessage("Las observaciones no pueden exceder 3000 caracteres");
            });

            // PlacaVehiculo - Formato placa (opcional)
            // Formato típico en El Salvador: P123456 o similar
            When(x => !string.IsNullOrEmpty(x.PlacaVehiculo), () =>
            {
                RuleFor(x => x.PlacaVehiculo)
                    .MaximumLength(10)
                    .WithMessage("La placa del vehículo no puede exceder 10 caracteres")
                    .Matches(@"^[a-zA-Z0-9\-]+$")
                    .WithMessage("La placa del vehículo solo puede contener letras, números y guiones");
            });
        }
    }
}
