using FraFactu.Domain.Entities;
using FraFactu.Application.DTOs.Inventario;

namespace FraFactu.Application.Services;

/// <summary>
/// Servicio de integración entre inventario y facturación
/// </summary>
public interface IInventarioIntegrationService
{
    /// <summary>
    /// Reservar stock al crear una factura
    /// CantidadDisponible → CantidadReservada
    /// </summary>
    Task ReservarStockAsync(int facturaId);

    /// <summary>
    /// Confirmar venta cuando MH aprueba (PROCESADO 001/002)
    /// CantidadReservada → Registrar MovimientoInventario (SALIDA)
    /// </summary>
    Task ConfirmarVentaAsync(int facturaId);

    /// <summary>
    /// Liberar reservas cuando MH rechaza
    /// CantidadReservada → CantidadDisponible
    /// </summary>
    Task LiberarReservasAsync(int facturaId);

    /// <summary>
    /// Revertir venta por invalidación
    /// Registrar MovimientoInventario (ENTRADA/DEVOLUCION)
    /// </summary>
    Task RevertirVentaAsync(int invalidacionId);

    /// <summary>
    /// Validar que hay stock disponible para una factura
    /// </summary>
    Task<bool> ValidarStockDisponibleAsync(int facturaId);

    /// <summary>
    /// Descontar stock inmediatamente (Salida directa)
    /// CantidadDisponible → Registrar MovimientoInventario (SALIDA)
    /// Sin pasar por reserva
    /// </summary>
    Task DescontarStockInmediatoAsync(int facturaId);

    /// <summary>
    /// Revierte el descuento de stock de una factura rechazada/error (devuelve CantidadDisponible)
    /// </summary>
    Task RevertirStockFacturaAsync(int facturaId);

    /// <summary>
    /// Registrar ajuste manual de inventario
    /// </summary>
    Task<MovimientoInventario> RegistrarAjusteAsync(AjusteInventarioDto dto, int emisorId, int? usuarioId);

    /// <summary>
    /// Realizar traslado de inventario entre bodegas
    /// </summary>
    Task TrasladarAsync(TrasladoInventarioDto dto, int usuarioId);
}
