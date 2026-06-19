using FraFactu.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FraFactu.Infrastructure.Services;

/// <summary>
/// Consulta el máximo correlativo de NumeroControl en todas las BDs configuradas
/// para evitar colisiones entre ambientes (producción, test, demo) al enviar a MH.
/// </summary>
public class CrossDbCorrelativoService : ICrossDbCorrelativoService
{
    private readonly string[] _connectionStrings;
    private readonly ILogger<CrossDbCorrelativoService> _logger;

    public CrossDbCorrelativoService(
        IConfiguration configuration,
        ILogger<CrossDbCorrelativoService> logger)
    {
        _logger = logger;
        _connectionStrings = configuration.GetSection("CrossDatabaseConnections").Get<string[]>() ?? Array.Empty<string>();

        if (_connectionStrings.Length == 0)
            _logger.LogWarning("[CROSS-DB] No hay CrossDatabaseConnections configuradas. Se usará solo la BD local.");
    }

    public async Task<int> ObtenerMaxCorrelativoAsync(int emisorId, string tipoDte, int anio, string ambiente)
    {
        if (_connectionStrings.Length == 0)
            return 0;

        var maxGlobal = 0;

        // Consultar en paralelo todas las BDs
        var tasks = _connectionStrings.Select(cs => ConsultarMaxEnBdAsync(cs, emisorId, tipoDte, anio, ambiente));
        var resultados = await Task.WhenAll(tasks);

        foreach (var resultado in resultados)
        {
            if (resultado > maxGlobal)
                maxGlobal = resultado;
        }

        _logger.LogInformation(
            "[CROSS-DB] Max correlativo global para EmisorId={EmisorId}, TipoDte='{TipoDte}', Anio={Anio}, Ambiente='{Ambiente}': {Max} (consultadas {Count} BDs)",
            emisorId, tipoDte, anio, ambiente, maxGlobal, _connectionStrings.Length);

        return maxGlobal;
    }

    private async Task<int> ConsultarMaxEnBdAsync(string connectionString, int emisorId, string tipoDte, int anio, string ambiente)
    {
        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            // Buscar el máximo correlativo para el emisor, tipo de DTE, año y ambiente.
            // NumeroControl formato: DTE-01-M001P001-000000000000399
            // El correlativo es la parte después del último guión (15 dígitos)
            const string sql = @"
                SELECT MAX(CAST(SPLIT_PART(""NumeroControl"", '-', 4) AS INTEGER))
                FROM ""Facturas""
                WHERE ""EmisorId"" = @emisorId
                  AND ""NumeroControl"" LIKE 'DTE-' || @tipoDte || '-%'
                  AND CAST(EXTRACT(YEAR FROM ""FechaEmision"") AS INTEGER) = @anio
                  AND ""Ambiente"" = @ambiente";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@emisorId", emisorId);
            cmd.Parameters.AddWithValue("@tipoDte", tipoDte);
            cmd.Parameters.AddWithValue("@anio", anio);
            cmd.Parameters.AddWithValue("@ambiente", ambiente);

            var result = await cmd.ExecuteScalarAsync();

            if (result is int maxVal)
                return maxVal;
            if (result is long maxLong)
                return (int)maxLong;

            return 0;
        }
        catch (Exception ex)
        {
            // No fallar si una BD no está disponible — log y continuar
            var hostInfo = ExtraerHost(connectionString);
            _logger.LogWarning(ex,
                "[CROSS-DB] Error consultando BD {Host} para EmisorId={EmisorId}, TipoDte='{TipoDte}'. Se ignora esta BD.",
                hostInfo, emisorId, tipoDte);
            return 0;
        }
    }

    private static string ExtraerHost(string connectionString)
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            return builder.Host ?? "desconocido";
        }
        catch
        {
            return "desconocido";
        }
    }
}
