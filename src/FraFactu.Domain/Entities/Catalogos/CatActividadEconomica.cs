namespace FraFactu.Domain.Entities.Catalogos;

public class CatActividadEconomica : CatalogoBase
{
    // TRUE = Código final (seleccionable). FALSE = Título de sección.
    public bool EsSeleccionable { get; set; }

    // Relación Jerárquica: Apunta al ID de su sección padre
    public int? PadreId { get; set; }
    public CatActividadEconomica? Padre { get; set; }
}