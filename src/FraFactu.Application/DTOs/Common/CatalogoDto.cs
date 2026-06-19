namespace FraFactu.Application.DTOs.Common
{
    /// <summary>
    /// DTO genérico para todos los catálogos del sistema
    /// </summary>
    public class CatalogoDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
    }
}
