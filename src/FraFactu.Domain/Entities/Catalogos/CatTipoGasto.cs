using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities.Catalogos;

/// <summary>
/// Catálogo de tipos de gastos administrativos
/// </summary>
public class CatTipoGasto : BaseEntity
{
    /// <summary>
    /// Código único del tipo de gasto
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del tipo de gasto
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Descripción del tipo de gasto
    /// </summary>
    public string? Descripcion { get; set; }

    /// <summary>
    /// ID del Emisor al que pertenece este tipo de gasto
    /// </summary>
    public int EmisorId { get; set; }
    public Emisor? Emisor { get; set; }

    /// <summary>
    /// Indica si el tipo está activo
    /// </summary>
    public new bool Activo { get; set; } = true;
}
