using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FraFactu.Infrastructure.Services
{
    /// <summary>
    /// F3 (G4). Implementación de idempotencia respaldada por la tabla
    /// <c>movimientos_externos_registrados</c> (índice único por emisor + clave).
    /// </summary>
    public class IdempotenciaMovimientosService : IIdempotenciaMovimientosService
    {
        private readonly ApplicationDbContext _context;

        public IdempotenciaMovimientosService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IntentarRegistrarAsync(int emisorId, string movimientoIdExterno, string? tipoDocumento, int itemsProcesados)
        {
            if (string.IsNullOrWhiteSpace(movimientoIdExterno))
                throw new ArgumentException("La clave externa del movimiento es obligatoria.", nameof(movimientoIdExterno));

            if (await YaRegistradoAsync(emisorId, movimientoIdExterno))
                return false;

            _context.MovimientosExternosRegistrados.Add(new MovimientoExternoRegistrado
            {
                EmisorId = emisorId,
                MovimientoIdExterno = movimientoIdExterno,
                TipoDocumento = tipoDocumento,
                ItemsProcesados = itemsProcesados
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public Task<bool> YaRegistradoAsync(int emisorId, string movimientoIdExterno) =>
            _context.MovimientosExternosRegistrados
                .AnyAsync(m => m.EmisorId == emisorId && m.MovimientoIdExterno == movimientoIdExterno);
    }
}
