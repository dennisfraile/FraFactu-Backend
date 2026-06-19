using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Jobs;

/// <summary>
/// F4 (Plan inventario desde DTE): compara el StockBodega interno de
/// Smartix contra el snapshot que expone SmartInventory, persiste las
/// divergencias detectadas y marca como resueltas las que ya no aparecen.
/// Patron de ejecucion alineado con <see cref="IntegracionInventarioConsumer"/>:
/// metodo estatico testeable + bucle wrapper en <c>ReconciliacionInventarioJob</c>.
///
/// Multi-tenant: el caller (Job o test) decide a que <see cref="Emisor"/>
/// reconciliar; este servicio NO recorre toda la tabla.
/// </summary>
public static class ReconciliacionInventarioService
{
    /// <summary>
    /// Ejecuta una corrida de reconciliacion para un Emisor especifico.
    /// Devuelve los contadores de la corrida para logging y metricas.
    /// Throws si el cliente no esta configurado o si HubId del Emisor es null
    /// (no se puede consultar SmartInventory sin organizacionId).
    /// </summary>
    public static async Task<ReconciliacionResultado> EjecutarParaEmisorAsync(
        ApplicationDbContext context,
        ISmartInventoryClient client,
        Emisor emisor,
        Guid ejecucionId,
        ILogger logger,
        CancellationToken ct)
    {
        if (!client.EstaConfigurado)
            throw new InvalidOperationException(
                "ISmartInventoryClient no esta configurado; no se puede reconciliar.");
        if (emisor.HubId is null)
            throw new InvalidOperationException(
                $"Emisor {emisor.Id} no tiene HubId asignado; no hay organizacionId para SmartInventory.");

        var hubId = emisor.HubId.Value;
        var ahora = DateTime.UtcNow;

        // Excluye activos fijos y productos soft-deleted (mismo filtro que F3 al enviar
        // movimientos). El Select proyecta solo las columnas necesarias en SQL; sin
        // Include la query no carga las entidades completas.
        var smartixRaw = await context.StocksBodega
            .AsNoTracking()
            .Where(s => s.Producto.EmisorId == emisor.Id
                     && s.Producto.Activo
                     && s.Producto.TipoInventario != TipoInventario.MobiliarioEquipo)
            .Select(s => new
            {
                Codigo = s.Producto.Codigo,
                Bodega = s.Bodega.Nombre,
                Cantidad = s.CantidadDisponible
            })
            .ToListAsync(ct);

        // StockBodega es 1:1 por (producto, bodega) en BD, pero agrupamos defensivo
        // para tolerar duplicados de migraciones legacy sin fallar el job.
        var smartixDict = smartixRaw
            .GroupBy(x => (Codigo: x.Codigo, Bodega: x.Bodega))
            .ToDictionary(g => g.Key, g => (int)Math.Round(g.Sum(x => x.Cantidad)));

        // 2) Snapshot SmartInventory paginado.
        var snapshotDict = new Dictionary<(string Codigo, string Bodega), int>();
        int? token = null;
        do
        {
            var page = await client.GetSnapshotAsync(hubId, token, null, ct);
            foreach (var i in page.Items)
            {
                var key = (i.CodigoProducto, i.NombreBodega);
                snapshotDict[key] = snapshotDict.TryGetValue(key, out var prev) ? prev + i.Cantidad : i.Cantidad;
            }
            token = page.ContinuationToken;
        } while (token.HasValue);

        // 3) Cargar divergencias abiertas previas del emisor.
        var previas = await context.DivergenciasInventario
            .Where(d => d.EmisorId == emisor.Id
                     && d.Estado == EstadoDivergenciaInventario.DETECTADA)
            .ToListAsync(ct);
        var previasDict = previas.ToDictionary(d => (d.CodigoProducto, d.NombreBodega));

        // 4) Recorrer la union de claves y registrar divergencias.
        var claves = new HashSet<(string Codigo, string Bodega)>(smartixDict.Keys);
        claves.UnionWith(snapshotDict.Keys);

        var vistas = new HashSet<(string, string)>();
        var nuevas = 0;
        var reaparecidas = 0;

        foreach (var key in claves)
        {
            var smartix = smartixDict.TryGetValue(key, out var s) ? s : 0;
            var si = snapshotDict.TryGetValue(key, out var v) ? v : 0;
            var diff = smartix - si;
            if (diff == 0) continue;  // no es divergencia

            vistas.Add(key);

            if (previasDict.TryGetValue((key.Codigo, key.Bodega), out var existente))
            {
                existente.StockSmartix = smartix;
                existente.StockSmartInventory = si;
                existente.Diff = diff;
                existente.EjecucionId = ejecucionId;
                existente.UltimaDeteccion = ahora;
                reaparecidas++;
            }
            else
            {
                context.DivergenciasInventario.Add(new DivergenciaInventario
                {
                    EmisorId = emisor.Id,
                    EjecucionId = ejecucionId,
                    CodigoProducto = key.Codigo,
                    NombreBodega = key.Bodega,
                    StockSmartix = smartix,
                    StockSmartInventory = si,
                    Diff = diff,
                    Estado = EstadoDivergenciaInventario.DETECTADA,
                    UltimaDeteccion = ahora,
                    FechaCreacion = ahora
                });
                nuevas++;
            }
        }

        // 5) Resolver las divergencias previas que ya no aparecen.
        var resueltas = 0;
        foreach (var p in previas)
        {
            if (!vistas.Contains((p.CodigoProducto, p.NombreBodega)))
            {
                p.Estado = EstadoDivergenciaInventario.RESUELTA;
                p.FechaResolucion = ahora;
                p.EjecucionId = ejecucionId;
                resueltas++;
            }
        }

        await context.SaveChangesAsync(ct);

        var resultado = new ReconciliacionResultado
        {
            EjecucionId = ejecucionId,
            EmisorId = emisor.Id,
            TotalComparado = claves.Count,
            DivergenciasNuevas = nuevas,
            DivergenciasReaparecidas = reaparecidas,
            DivergenciasResueltas = resueltas,
            DivergenciasAbiertasTotal = nuevas + reaparecidas
        };

        logger.LogInformation(
            "[ReconciliacionInventario] Emisor {EmisorId} (Hub {HubId}) ejec {Ejec}: " +
            "comparados={Comp}, nuevas={Nuevas}, reaparecidas={Reap}, resueltas={Res}",
            emisor.Id, hubId, ejecucionId,
            resultado.TotalComparado, resultado.DivergenciasNuevas,
            resultado.DivergenciasReaparecidas, resultado.DivergenciasResueltas);

        return resultado;
    }
}

/// <summary>
/// Contadores agregados de una corrida del job, expuestos para logging y
/// para que los tests verifiquen el comportamiento sin depender de la BD.
/// </summary>
public class ReconciliacionResultado
{
    public Guid EjecucionId { get; init; }
    public int EmisorId { get; init; }

    /// <summary>Filas (codigo, bodega) consideradas (union de Smartix + SmartInventory).</summary>
    public int TotalComparado { get; init; }

    /// <summary>Divergencias detectadas que no existian previamente.</summary>
    public int DivergenciasNuevas { get; init; }

    /// <summary>Divergencias previas que esta corrida volvio a observar (no las duplica).</summary>
    public int DivergenciasReaparecidas { get; init; }

    /// <summary>Divergencias previas DETECTADA que esta corrida marco como RESUELTA.</summary>
    public int DivergenciasResueltas { get; init; }

    /// <summary>Total de divergencias abiertas tras la corrida (nuevas + reaparecidas).</summary>
    public int DivergenciasAbiertasTotal { get; init; }
}
