using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities
{
    /// <summary>
    /// F3 (G4). Registro de idempotencia para movimientos de inventario provenientes
    /// de orígenes externos reintentables (integraciones, jobs). Una misma clave
    /// (<see cref="EmisorId"/>, <see cref="MovimientoIdExterno"/>) solo se procesa
    /// una vez, evitando duplicar afectaciones de stock ante reintentos.
    /// </summary>
    public class MovimientoExternoRegistrado : BaseEntity
    {
        /// <summary>Emisor (tenant) dueño del registro.</summary>
        public int EmisorId { get; set; }

        /// <summary>
        /// Identificador externo opaco del movimiento (p.ej. "compra-123"). Único por emisor.
        /// </summary>
        public string MovimientoIdExterno { get; set; } = string.Empty;

        /// <summary>Tipo de documento de origen (informativo).</summary>
        public string? TipoDocumento { get; set; }

        /// <summary>Cantidad de ítems procesados en el movimiento (informativo).</summary>
        public int ItemsProcesados { get; set; }
    }
}
