using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities;

/// <summary>
/// Ítem del cuerpo del documento de un Evento de Operaciones Especiales (17).
/// </summary>
public class OperacionEspecialDetalle : BaseEntity
{
    public int EventoOperacionEspecialId { get; set; }
    public EventoOperacionEspecial EventoOperacionEspecial { get; set; } = null!;

    /// <summary>Número de ítem (1 a 2000).</summary>
    public int NumItem { get; set; }

    /// <summary>Código de generación del EOE de referencia (UUID v4). Opcional.</summary>
    public string? CodigoGeneracionRef { get; set; }

    /// <summary>Tipo de documento (CAT-023: 97=CCI, 02=Factura venta simplificada, etc.).</summary>
    public string TipoDocumento { get; set; } = string.Empty;

    /// <summary>Número de documento origen. Opcional.</summary>
    public string? NumDocumento { get; set; }

    /// <summary>Fecha del documento origen. Opcional.</summary>
    public DateTime? FechaEmisionDoc { get; set; }

    /// <summary>Cantidad (>= 1).</summary>
    public int Cantidad { get; set; }

    /// <summary>Descripción del ítem (1 a 1500 caracteres).</summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Documento "Del" (rango). Opcional.</summary>
    public string? DocDel { get; set; }

    /// <summary>Documento "Al" (rango). Opcional.</summary>
    public string? DocAl { get; set; }

    /// <summary>Precio unitario.</summary>
    public decimal PrecioUni { get; set; }

    /// <summary>Ventas no sujetas.</summary>
    public decimal VentaNoSuj { get; set; }

    /// <summary>Ventas exentas.</summary>
    public decimal VentaExenta { get; set; }

    /// <summary>Ventas gravadas.</summary>
    public decimal VentaGravada { get; set; }

    /// <summary>
    /// Códigos de tributo del ítem (CAT-015) serializados como JSON (lista de strings de 2 chars).
    /// Null si el ítem no lleva tributos.
    /// </summary>
    public string? TributosJson { get; set; }
}
