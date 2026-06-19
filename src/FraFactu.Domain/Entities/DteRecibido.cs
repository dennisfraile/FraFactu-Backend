using FraFactu.Domain.Common;
using FraFactu.Domain.Enums;

namespace FraFactu.Domain.Entities;

/// <summary>
/// DTE (Documento Tributario Electrónico) recibido desde correo electrónico.
/// Representa un documento fiscal recibido de un proveedor.
/// </summary>
public class DteRecibido : BaseEntity
{
    // ==========================================
    // EMISOR (dueño del registro)
    // ==========================================

    public int EmisorId { get; set; }
    public Emisor Emisor { get; set; } = null!;

    // ==========================================
    // IDENTIFICACIÓN DEL DTE
    // ==========================================

    /// <summary>
    /// Código de generación único del DTE (UUID)
    /// </summary>
    public string CodigoGeneracion { get; set; } = string.Empty;

    /// <summary>
    /// Sello de recibido del Ministerio de Hacienda
    /// </summary>
    public string? SelloRecibido { get; set; }

    /// <summary>
    /// Tipo de DTE (01=Factura, 03=CCF, 05=NC, 06=ND, etc.)
    /// </summary>
    public string TipoDte { get; set; } = string.Empty;

    /// <summary>
    /// Número de control del DTE
    /// </summary>
    public string? NumeroControl { get; set; }

    /// <summary>
    /// Fecha de emisión del DTE
    /// </summary>
    public DateTime FechaEmision { get; set; }

    // ==========================================
    // DATOS DEL EMISOR DEL DTE (proveedor)
    // ==========================================

    public string EmisorNit { get; set; } = string.Empty;
    public string EmisorNombre { get; set; } = string.Empty;
    public string? EmisorNrc { get; set; }

    // ==========================================
    // DATOS DEL RECEPTOR DEL DTE (nosotros)
    // ==========================================

    public string? ReceptorNit { get; set; }
    public string? ReceptorNombre { get; set; }

    // ==========================================
    // CONTENIDO COMPLETO
    // ==========================================

    /// <summary>
    /// JSON completo del DTE
    /// </summary>
    public string JsonDte { get; set; } = string.Empty;

    // ==========================================
    // MONTOS
    // ==========================================

    public decimal MontoGravado { get; set; }
    public decimal MontoExento { get; set; }
    public decimal MontoNoSujeto { get; set; }
    public decimal SubTotal { get; set; }
    public decimal IVA { get; set; }
    public decimal Total { get; set; }

    // ==========================================
    // ESTADO Y TRAZABILIDAD
    // ==========================================

    /// <summary>
    /// Estado: PENDIENTE, VINCULADO, DESCARTADO
    /// </summary>
    public EstadoDteRecibido Estado { get; set; } = EstadoDteRecibido.PENDIENTE;

    /// <summary>
    /// FK a la compra vinculada (null si no vinculada)
    /// </summary>
    public int? CompraExternaId { get; set; }
    public CompraExterna? CompraExterna { get; set; }

    /// <summary>
    /// Motivo de descarte (si fue descartado)
    /// </summary>
    public string? MotivoDescarte { get; set; }

    // ==========================================
    // ORIGEN / TRAZABILIDAD
    // ==========================================

    /// <summary>
    /// Fuente por la que entró el DTE: correo (Gmail) o carga manual.
    /// </summary>
    public FuenteRecepcionDte FuenteRecepcion { get; set; } = FuenteRecepcionDte.CORREO;

    /// <summary>
    /// Usuario que subió el DTE cuando la fuente es carga manual (null si vino por correo).
    /// </summary>
    public int? CargadoPorUsuarioId { get; set; }

    /// <summary>
    /// Dirección de email de donde se extrajo (solo si la fuente es correo)
    /// </summary>
    public string? EmailOrigen { get; set; }

    /// <summary>
    /// Fecha de recepción del correo
    /// </summary>
    public DateTime? FechaRecepcionEmail { get; set; }

    /// <summary>
    /// Fecha de última actualización
    /// </summary>
    public DateTime? FechaActualizacion { get; set; }
}
