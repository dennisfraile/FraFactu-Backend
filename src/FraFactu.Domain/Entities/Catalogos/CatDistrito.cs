namespace FraFactu.Domain.Entities.Catalogos
{
    public class CatDistrito : CatalogoBase
    {
        // Los códigos de distrito (CAT-008) no son únicos globalmente: se reinician
        // por departamento/municipio. La llave real es la triada
        // Departamento → Municipio → Distrito, por eso guardamos ambos códigos padre.
        public string CodigoDepartamento { get; set; } = string.Empty;
        public string CodigoMunicipio { get; set; } = string.Empty;
    }
}
