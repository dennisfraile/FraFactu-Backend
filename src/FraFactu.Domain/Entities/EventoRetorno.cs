using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities;

/// <summary>
/// Evento de Retorno (tipoEvento "18", esquema fe-eret-v1.json del MH).
/// Permite afectar/devolver bienes sin invalidar el DTE original. Estructura de exportación
/// (recinto fiscal, régimen, país) y terceros mutuamente excluyentes (ventaTercero | compraTercero).
/// Se construye por carga manual del emisor. NO reconcilia saldos vivos de los DTE relacionados.
/// </summary>
public class EventoRetorno : BaseEntity
{
    // ==========================================
    // IDENTIFICACIÓN
    // ==========================================

    public int Version { get; set; } = 1;
    public string Ambiente { get; set; } = "00";

    /// <summary>Modelo de facturación: 1 = Normal, 2 = Diferido.</summary>
    public int TipoModelo { get; set; } = 1;

    /// <summary>Tipo de transmisión: 1 = Normal, 2 = Contingencia.</summary>
    public int TipoOperacion { get; set; } = 1;

    public string TipoEvento { get; set; } = "18";

    /// <summary>Tipo de contingencia (CAT-005: 1-5). Null si no aplica.</summary>
    public int? TipoContingencia { get; set; }

    /// <summary>Motivo de contingencia (máx. 500). Null si no aplica.</summary>
    public string? MotivoContin { get; set; }

    public string CodigoGeneracion { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public TimeSpan HoraEmision { get; set; }

    /// <summary>Fusiones y otros (NIT 9-14). Null si no aplica.</summary>
    public string? Fusion { get; set; }

    public string TipoMoneda { get; set; } = "USD";

    // ==========================================
    // EMISOR (campos de exportación)
    // ==========================================

    public int EmisorId { get; set; }
    public Emisor Emisor { get; set; } = null!;

    public string? CodEstableMH { get; set; }
    public string? CodEstable { get; set; }
    public string? CodPuntoVentaMH { get; set; }
    public string? CodPuntoVenta { get; set; }

    /// <summary>Recinto fiscal (CAT-027, 2 chars). Null si no aplica.</summary>
    public string? RecintoFiscal { get; set; }

    /// <summary>Tipo de régimen (CAT-033). Null si no aplica.</summary>
    public string? TipoRegimen { get; set; }

    /// <summary>Régimen de exportación (CAT-028). Null si no aplica.</summary>
    public string? Regimen { get; set; }

    /// <summary>Tipo de ítem de exportación. Null si no aplica.</summary>
    public int? TipoItemExpor { get; set; }

    // ==========================================
    // SUB-ESTRUCTURAS SERIALIZADAS COMO JSON
    // ==========================================

    /// <summary>Documentos relacionados (lista de { tipoDocumento, codigoGeneracion, fechaEmision }). JSON.</summary>
    public string? DocumentoRelacionadoJson { get; set; }

    /// <summary>Receptor/documento (objeto nullable del esquema). JSON o null.</summary>
    public string? DocumentoReceptorJson { get; set; }

    /// <summary>Venta por cuenta de terceros (excluyente con compraTercero). JSON o null.</summary>
    public string? VentaTerceroJson { get; set; }

    /// <summary>Compra por cuenta de terceros (excluyente con ventaTercero). JSON o null.</summary>
    public string? CompraTerceroJson { get; set; }

    /// <summary>Resumen de tributos provisto por el emisor (lista de { codigo, descripcion, valor }). JSON o null.</summary>
    public string? ResumenTributosJson { get; set; }

    /// <summary>Apéndice opcional (lista de { campo, etiqueta, valor }). JSON o null.</summary>
    public string? ApendiceJson { get; set; }

    // ==========================================
    // RESUMEN (totales)
    // ==========================================

    public decimal TotalNoSuj { get; set; }
    public decimal TotalExenta { get; set; }
    public decimal TotalGravada { get; set; }
    public decimal TotalCompraExcluidos { get; set; }
    public decimal SubTotalVentas { get; set; }
    public decimal? TotalSeguro { get; set; }
    public decimal? TotalFlete { get; set; }
    public decimal MontoTotalOperacion { get; set; }
    public decimal IvaRete { get; set; }
    public decimal? ReteRenta { get; set; }
    public decimal TotalNoGravado { get; set; }
    public decimal TotalPagar { get; set; }
    public string? TotalLetras { get; set; }
    public decimal TotalNoOnerosas { get; set; }
    public decimal TotalIva { get; set; }
    public decimal SaldoFavor { get; set; }

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

    /// <summary>Cuerpo del documento: ítems retornados (1 a 2000).</summary>
    public ICollection<RetornoDetalle> Detalles { get; set; } = new List<RetornoDetalle>();
}
