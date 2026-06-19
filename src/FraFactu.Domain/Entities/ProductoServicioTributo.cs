using FraFactu.Domain.Entities.Catalogos;

namespace FraFactu.Domain.Entities;

/// <summary>
/// Tabla join: impuestos adicionales (Sección 1 configurables + Sección 3 informativos)
/// asignados a un producto/servicio. El IVA (código "20") NO se guarda aquí.
/// </summary>
public class ProductoServicioTributo
{
    public int ProductoServicioId { get; set; }
    public ProductoServicio ProductoServicio { get; set; } = null!;

    public int CatTributoId { get; set; }
    public CatTributo CatTributo { get; set; } = null!;

    /// <summary>"monto_fijo" | "porcentaje" para Sección 1; null para Sección 3.</summary>
    public string? TipoCalculo { get; set; }

    /// <summary>Monto o porcentaje para Sección 1; null para Sección 3.</summary>
    public decimal? Valor { get; set; }
}
