namespace FraFactu.Domain.Entities.Catalogos
{
    public class CatalogoBase
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty; 
        public string Valor { get; set; } = string.Empty; 
        }
}