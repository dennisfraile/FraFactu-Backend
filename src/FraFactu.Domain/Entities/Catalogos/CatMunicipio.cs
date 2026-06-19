namespace FraFactu.Domain.Entities.Catalogos
{
    public class CatMunicipio: CatalogoBase
    {
        // Mantenemos el departamento para poder filtrar en el Frontend
        public string CodigoDepartamento { get; set; } = string.Empty;
    }
}