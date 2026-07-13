using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Services
{
    /// <summary>
    /// Implementación de la sincronización del detalle de lote (Fase 3 del refactor).
    /// </summary>
    public class FacturaLoteSync : IFacturaLoteSync
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FacturaLoteSync> _logger;

        public FacturaLoteSync(ApplicationDbContext context, ILogger<FacturaLoteSync> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Sincroniza el LoteDetalle y Lote cuando una factura cambia de estado
        /// fuera del flujo normal de lote (ej: envío individual).
        /// NO llama SaveChangesAsync — el caller debe guardar.
        /// </summary>
        public async Task SincronizarLoteDetalleAsync(FacturaElectronica factura, string? motivoRechazo = null)
        {
            if (factura.LoteId == null) return;

            var loteId = factura.LoteId.Value;
            var loteDetalle = await _context.LoteDetalles
                .FirstOrDefaultAsync(d => d.FacturaElectronicaId == factura.Id && d.LoteId == loteId);

            if (loteDetalle == null) return;

            if (factura.EstadoHacienda == "PROCESADO")
            {
                loteDetalle.EstadoDte = "PROCESADO";
                loteDetalle.SelloRecibido = factura.SelloRecibido;
                loteDetalle.FechaConsulta = DateTime.UtcNow;
            }
            else if (factura.EstadoHacienda == "RECHAZADO")
            {
                loteDetalle.EstadoDte = "RECHAZADO";
                loteDetalle.FechaConsulta = DateTime.UtcNow;
                loteDetalle.ObservacionesRechazo = motivoRechazo;
                factura.LoteId = null;
            }

            var lote = await _context.Lotes
                .Include(l => l.Detalles)
                .FirstOrDefaultAsync(l => l.Id == loteId);

            if (lote != null)
            {
                lote.TotalAprobados = lote.Detalles.Count(d => d.SelloRecibido != null);
                lote.TotalRechazados = lote.Detalles.Count(d => d.EstadoDte == "RECHAZADO");
                lote.TotalPendientes = lote.TotalDtes - lote.TotalAprobados - lote.TotalRechazados;
                lote.FechaUltimaConsulta = DateTime.UtcNow;
                lote.FechaModificacion = DateTime.UtcNow;

                if (lote.TotalPendientes <= 0 && lote.Estado != "Procesado")
                {
                    lote.Estado = "Procesado";
                }
            }

            _logger.LogInformation(
                "[LOTE-SYNC] Sincronizado LoteDetalle para factura {FacturaId} en lote {LoteId}: EstadoDte={Estado}",
                factura.Id, loteId, loteDetalle.EstadoDte);
        }
    }
}
