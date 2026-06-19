using FraFactu.Application.DTOs.Facturas;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Services
{
    /// <summary>
    /// Servicio de control de saldos DTE para Notas de Crédito Electrónicas
    /// </summary>
    public class SaldoDteService : ISaldoDteService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SaldoDteService> _logger;

        public SaldoDteService(ApplicationDbContext context, ILogger<SaldoDteService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SaldoDteDto?> ObtenerSaldoPorCodigoGeneracion(string codigoGeneracion, int emisorId)
        {
            var saldo = await _context.SaldosDte
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.DteCodigoGeneracion == codigoGeneracion && s.EmisorId == emisorId);

            if (saldo == null) return null;

            return new SaldoDteDto
            {
                Id = saldo.Id,
                DteCodigoGeneracion = saldo.DteCodigoGeneracion,
                TipoDte = saldo.TipoDte,
                MontoOriginal = saldo.MontoOriginal,
                MontoAcreditado = saldo.MontoAcreditado,
                SaldoDisponible = saldo.SaldoDisponible,
                NceCount = saldo.NceCount
            };
        }

        public async Task<bool> ValidarSaldoDisponible(string codigoGeneracion, decimal montoNCE, int emisorId)
        {
            var saldo = await _context.SaldosDte
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.DteCodigoGeneracion == codigoGeneracion && s.EmisorId == emisorId);

            if (saldo == null) return false;

            return saldo.SaldoDisponible >= montoNCE;
        }

        public async Task ActualizarSaldoAsync(string codigoGeneracion, decimal montoNCE, int emisorId)
        {
            var saldo = await _context.SaldosDte
                .FirstOrDefaultAsync(s => s.DteCodigoGeneracion == codigoGeneracion && s.EmisorId == emisorId);

            if (saldo == null)
                throw new InvalidOperationException($"No se encontró registro de saldo para el DTE {codigoGeneracion}");

            if (saldo.SaldoDisponible < montoNCE)
                throw new InvalidOperationException(
                    $"Saldo insuficiente. Disponible: {saldo.SaldoDisponible:F2}, NCE solicita: {montoNCE:F2}");

            saldo.MontoAcreditado += montoNCE;
            saldo.NceCount++;
            saldo.FechaActualizacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Saldo actualizado para DTE {CodigoGeneracion}: Acreditado={MontoAcreditado}, Disponible={SaldoDisponible}, NCEs={NceCount}",
                codigoGeneracion, saldo.MontoAcreditado, saldo.SaldoDisponible, saldo.NceCount);
        }

        public async Task InicializarSaldoAsync(string codigoGeneracion, string tipoDte, decimal montoTotal, int emisorId)
        {
            // Verificar que no exista ya un registro para este DTE
            var existe = await _context.SaldosDte
                .AnyAsync(s => s.DteCodigoGeneracion == codigoGeneracion);

            if (existe)
            {
                _logger.LogDebug("Saldo ya inicializado para DTE {CodigoGeneracion}", codigoGeneracion);
                return;
            }

            var saldo = new SaldoDte
            {
                DteCodigoGeneracion = codigoGeneracion,
                TipoDte = tipoDte,
                EmisorId = emisorId,
                MontoOriginal = montoTotal,
                MontoAcreditado = 0m,
                NceCount = 0,
                FechaCreacion = DateTime.UtcNow,
                FechaActualizacion = DateTime.UtcNow
            };

            _context.SaldosDte.Add(saldo);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Saldo inicializado para DTE {CodigoGeneracion} (tipo {TipoDte}): MontoOriginal={MontoOriginal}",
                codigoGeneracion, tipoDte, montoTotal);
        }
    }
}
