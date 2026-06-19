using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities;

/// <summary>
/// Representa un DTE individual incluido en un lote
/// Permite tracking del estado individual de cada documento
/// </summary>
public class LoteDetalle : BaseEntity
{
    /// <summary>
    /// FK al lote padre
    /// </summary>
    public int LoteId { get; set; }
    public Lote Lote { get; set; } = null!;

    /// <summary>
    /// FK a la factura electrónica incluida
    /// </summary>
    public int FacturaElectronicaId { get; set; }
    public FacturaElectronica FacturaElectronica { get; set; } = null!;

    /// <summary>
    /// Número de item dentro del lote (1 a 100)
    /// Orden en el que aparece en el JSON enviado
    /// </summary>
    public int NumeroItem { get; set; }

    // ==========================================
    // ESTADO INDIVIDUAL DEL DTE
    // ==========================================

    /// <summary>
    /// Estado individual del DTE después de consulta:
    /// Pendiente, Aprobado, Rechazado, Error
    /// </summary>
    public string? EstadoDte { get; set; }

    /// <summary>
    /// Sello de recepción asignado por MH (si aprobado)
    /// </summary>
    public string? SelloRecibido { get; set; }

    /// <summary>
    /// Fecha y hora de la consulta individual del estado
    /// </summary>
    public DateTime? FechaConsulta { get; set; }

    // ==========================================
    // INFORMACIÓN DE RECHAZO (si aplica)
    // ==========================================

    /// <summary>
    /// Código de rechazo asignado por MH
    /// </summary>
    public string? CodigoRechazo { get; set; }

    /// <summary>
    /// Observaciones o motivos del rechazo
    /// </summary>
    public string? ObservacionesRechazo { get; set; }

    /// <summary>
    /// JSON completo de la respuesta individual (para auditoría)
    /// </summary>
    public string? JsonRespuestaIndividual { get; set; }
}
