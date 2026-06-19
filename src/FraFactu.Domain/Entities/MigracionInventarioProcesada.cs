using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities;

/// <summary>
/// Registro de migraciones de inventario ya procesadas.
/// Permite idempotencia: si llega la misma MigrationId dos veces, se ignora.
/// </summary>
public class MigracionInventarioProcesada : BaseEntity
{
    public Guid MigrationId { get; set; }

    /// <summary>"export" o "import"</summary>
    public string Tipo { get; set; } = string.Empty;

    public int HubId { get; set; }

    public int EmisorId { get; set; }
    public Emisor Emisor { get; set; } = null!;

    public DateTime ProcesadaEn { get; set; } = DateTime.UtcNow;
}
