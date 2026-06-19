using FraFactu.Domain.Entities;

namespace FraFactu.Application.Interfaces;

public interface ISmartCareWebhookService
{
    /// <summary>
    /// Notifica a SmartCare que el estado de una factura cambió.
    /// Solo actúa si la factura tiene SmartCareWebhookUrl configurada.
    /// </summary>
    Task NotificarCambioEstadoAsync(FacturaElectronica factura);
}
