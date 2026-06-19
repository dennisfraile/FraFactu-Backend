using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities;

/// <summary>
/// Evento de Operaciones Especiales (tipoEvento "17", esquema fe-eop-v1.json del MH).
/// Reporta operaciones especiales como CCI (CAT-023:97) y facturas de venta simplificada
/// (CAT-023:02). Se construye por carga manual del emisor.
/// </summary>
public class EventoOperacionEspecial : BaseEntity
{
    // ==========================================
    // IDENTIFICACIÓN
    // ==========================================

    /// <summary>Versión del esquema del evento (valor fijo: 1).</summary>
    public int Version { get; set; } = 1;

    /// <summary>Ambiente: 00 = Pruebas, 01 = Producción.</summary>
    public string Ambiente { get; set; } = "00";

    /// <summary>Modelo de facturación (valor fijo: 1 = Normal).</summary>
    public int TipoModelo { get; set; } = 1;

    /// <summary>Tipo de operación/transmisión (valor fijo: 1 = Normal).</summary>
    public int TipoOperacion { get; set; } = 1;

    /// <summary>Tipo de evento (valor fijo: "17").</summary>
    public string TipoEvento { get; set; } = "17";

    /// <summary>Tipo de moneda (valor fijo: "USD").</summary>
    public string TipoMoneda { get; set; } = "USD";

    /// <summary>Código de generación único (UUID v4) del evento.</summary>
    public string CodigoGeneracion { get; set; } = string.Empty;

    /// <summary>Fecha del evento.</summary>
    public DateTime FechaEmision { get; set; }

    /// <summary>Hora del evento (HH:mm:ss).</summary>
    public TimeSpan HoraEmision { get; set; }

    // ==========================================
    // EMISOR
    // ==========================================

    public int EmisorId { get; set; }
    public Emisor Emisor { get; set; } = null!;

    // ==========================================
    // RESUMEN (calculado a partir de los ítems)
    // ==========================================

    public decimal TotalNoSuj { get; set; }
    public decimal TotalExenta { get; set; }
    public decimal TotalGravada { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Total { get; set; }

    /// <summary>Valor total en letras.</summary>
    public string? TotalLetras { get; set; }

    /// <summary>
    /// Resumen de tributos provisto por el emisor, serializado como JSON
    /// (lista de { codigo, descripcion, valor }). Null si no aplica.
    /// </summary>
    public string? ResumenTributosJson { get; set; }

    /// <summary>
    /// Apéndice opcional serializado como JSON (lista de { campo, etiqueta, valor }). Null si no aplica.
    /// </summary>
    public string? ApendiceJson { get; set; }

    // ==========================================
    // RESPUESTA DE HACIENDA
    // ==========================================

    public DateTime? FechaTransmisionMH { get; set; }
    public string? SelloRecibido { get; set; }
    public string? EstadoHacienda { get; set; }
    public string? JsonEvento { get; set; }
    public string? JsonRespuesta { get; set; }

    // ==========================================
    // RELACIONES
    // ==========================================

    /// <summary>Cuerpo del documento: ítems reportados (1 a 2000).</summary>
    public ICollection<OperacionEspecialDetalle> Detalles { get; set; } = new List<OperacionEspecialDetalle>();
}
