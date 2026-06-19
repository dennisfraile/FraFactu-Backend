using FraFactu.Domain.Common;
using FraFactu.Domain.Entities.Catalogos;

namespace FraFactu.Domain.Entities;

/// <summary>
/// Gasto administrativo que no afecta inventario
/// Ejemplo: papelería, servicios, mantenimiento, etc.
/// </summary>
public class GastoAdministrativo : BaseEntity
{
    // ==========================================
    // RELACIONES
    // ==========================================

    /// <summary>
    /// FK a la compra externa de donde proviene el gasto
    /// </summary>
    public int CompraExternaId { get; set; }
    public CompraExterna CompraExterna { get; set; } = null!;

    /// <summary>
    /// FK al tipo de gasto (catálogo)
    /// </summary>
    public int? CatTipoGastoId { get; set; }
    public CatTipoGasto? TipoGasto { get; set; }

    // ==========================================
    // INFORMACIÓN DEL GASTO
    // ==========================================

    /// <summary>
    /// Descripción del gasto
    /// </summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>
    /// Monto del gasto
    /// </summary>
    public decimal Monto { get; set; }

    // ==========================================
    // CONTABILIDAD (OPCIONAL)
    // ==========================================

    /// <summary>
    /// Centro de costo al que se carga el gasto
    /// </summary>
    public string? CentroCosto { get; set; }

    /// <summary>
    /// Cuenta contable asociada
    /// </summary>
    public string? CuentaContable { get; set; }
}
