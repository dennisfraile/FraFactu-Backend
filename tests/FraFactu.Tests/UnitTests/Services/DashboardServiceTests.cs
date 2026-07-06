using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FraFactu.Tests.UnitTests.Services
{
    /// <summary>
    /// Tests de caracterización del DashboardService. Fijan el comportamiento de
    /// ObtenerKPIsAsync y ObtenerDashboardCajeroAsync (agregación) para respaldar
    /// el refactor a agregación en SQL. Corren sobre EF InMemory: validan la lógica
    /// de agregación, no la traducción SQL de Npgsql (eso lo cubre el smoke Docker).
    /// </summary>
    public class DashboardServiceTests
    {
        private static ApplicationDbContext NewContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static FacturaElectronica Factura(
            int emisorId, DateTime fecha, string estado, decimal total,
            string ambiente = "00", int? sucursalId = null, int? usuarioId = null)
            => new FacturaElectronica
            {
                EmisorId = emisorId,
                FechaEmision = fecha,
                EstadoHacienda = estado,
                TotalPagar = total,
                Ambiente = ambiente,
                SucursalId = sucursalId,
                UsuarioId = usuarioId
            };

        // ---------- KPIs ----------

        [Fact]
        public async Task ObtenerKPIsAsync_AgregaSoloProcesadas_YCuentaPendientesYRechazadas()
        {
            using var ctx = NewContext();
            var inicio = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var fin = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
            ctx.Facturas.AddRange(
                Factura(1, new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc), "PROCESADO", 100m),
                Factura(1, new DateTime(2026, 1, 6, 0, 0, 0, DateTimeKind.Utc), "PROCESADO", 300m),
                Factura(1, new DateTime(2026, 1, 7, 0, 0, 0, DateTimeKind.Utc), "PENDIENTE_ENVIO", 999m),
                Factura(1, new DateTime(2026, 1, 8, 0, 0, 0, DateTimeKind.Utc), "RECHAZADO", 999m),
                Factura(2, new DateTime(2026, 1, 9, 0, 0, 0, DateTimeKind.Utc), "PROCESADO", 500m) // otro emisor: excluido
            );
            await ctx.SaveChangesAsync();
            var svc = new DashboardService(ctx);

            var kpis = await svc.ObtenerKPIsAsync(1, inicio, fin);

            Assert.Equal(2, kpis.TotalFacturas);
            Assert.Equal(2, kpis.FacturasAprobadas);
            Assert.Equal(400m, kpis.TotalVentas);
            Assert.Equal(200m, kpis.PromedioVenta);
            Assert.Equal(1, kpis.FacturasPendientes);
            Assert.Equal(1, kpis.FacturasRechazadas);
        }

        [Fact]
        public async Task ObtenerKPIsAsync_SinProcesadas_DevuelveCeros()
        {
            using var ctx = NewContext();
            var inicio = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var fin = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
            ctx.Facturas.Add(Factura(1, new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc), "PENDIENTE_ENVIO", 50m));
            await ctx.SaveChangesAsync();
            var svc = new DashboardService(ctx);

            var kpis = await svc.ObtenerKPIsAsync(1, inicio, fin);

            Assert.Equal(0, kpis.TotalFacturas);
            Assert.Equal(0m, kpis.TotalVentas);
            Assert.Equal(0m, kpis.PromedioVenta);
            Assert.Equal(1, kpis.FacturasPendientes);
        }

        [Fact]
        public async Task ObtenerKPIsAsync_RespetaFiltrosAmbienteYSucursalYFecha()
        {
            using var ctx = NewContext();
            var inicio = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var fin = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
            ctx.Facturas.AddRange(
                Factura(1, new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc), "PROCESADO", 100m, ambiente: "00", sucursalId: 10),
                Factura(1, new DateTime(2026, 1, 6, 0, 0, 0, DateTimeKind.Utc), "PROCESADO", 200m, ambiente: "01", sucursalId: 10), // otro ambiente
                Factura(1, new DateTime(2026, 1, 7, 0, 0, 0, DateTimeKind.Utc), "PROCESADO", 400m, ambiente: "00", sucursalId: 20), // otra sucursal
                Factura(1, new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc), "PROCESADO", 800m, ambiente: "00", sucursalId: 10)  // fuera de rango
            );
            await ctx.SaveChangesAsync();
            var svc = new DashboardService(ctx);

            var kpis = await svc.ObtenerKPIsAsync(1, inicio, fin, sucursalId: 10, ambiente: "00");

            Assert.Equal(1, kpis.TotalFacturas);
            Assert.Equal(100m, kpis.TotalVentas);
        }

        // ---------- Cajero ----------

        [Fact]
        public async Task ObtenerDashboardCajeroAsync_AgrupaPorDia_YTotalizaSoloDelUsuario()
        {
            using var ctx = NewContext();
            var desde = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var hasta = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
            ctx.Facturas.AddRange(
                Factura(1, new DateTime(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc), "PROCESADO", 100m, usuarioId: 7),
                Factura(1, new DateTime(2026, 1, 5, 14, 0, 0, DateTimeKind.Utc), "PROCESADO", 150m, usuarioId: 7), // mismo día
                Factura(1, new DateTime(2026, 1, 6, 10, 0, 0, DateTimeKind.Utc), "PROCESADO", 300m, usuarioId: 7), // otro día
                Factura(1, new DateTime(2026, 1, 6, 10, 0, 0, DateTimeKind.Utc), "PROCESADO", 999m, usuarioId: 8), // otro cajero: excluido
                Factura(1, new DateTime(2026, 1, 7, 10, 0, 0, DateTimeKind.Utc), "PENDIENTE_ENVIO", 999m, usuarioId: 7) // no procesada: excluida
            );
            await ctx.SaveChangesAsync();
            var svc = new DashboardService(ctx);

            var dto = await svc.ObtenerDashboardCajeroAsync(1, usuarioId: 7, desde, hasta);

            Assert.Equal(3, dto.TotalFacturas);
            Assert.Equal(550m, dto.MontoTotalVendido);
            Assert.Equal(550m / 3, dto.PromedioVenta);
            Assert.Equal(2, dto.VentasPorDia.Count);
            var dia5 = dto.VentasPorDia.Single(v => v.Fecha.Date == new DateTime(2026, 1, 5));
            Assert.Equal(2, dia5.CantidadFacturas);
            Assert.Equal(250m, dia5.MontoTotal);
        }

        [Fact]
        public async Task ObtenerDashboardCajeroAsync_SinFacturas_DevuelveCerosYListaVacia()
        {
            using var ctx = NewContext();
            var svc = new DashboardService(ctx);

            var dto = await svc.ObtenerDashboardCajeroAsync(1, usuarioId: 7,
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc));

            Assert.Equal(0, dto.TotalFacturas);
            Assert.Equal(0m, dto.MontoTotalVendido);
            Assert.Equal(0m, dto.PromedioVenta);
            Assert.Empty(dto.VentasPorDia);
        }
    }
}
