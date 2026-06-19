namespace FraFactu.Domain.Entities.Catalogos;

public class CatTributo : CatalogoBase
{
    // Id, Codigo y Valor heredados de CatalogoBase

    public string? DescripcionCorta { get; set; } 
    public bool EsRetencion { get; set; } = false;
    public bool EsValorPorcentual { get; set; } = true;

    // 1 = Resumen, 2 = Cuerpo, 3 = Ad-Valorem/Informativo
    public int Seccion { get; set; } 
}