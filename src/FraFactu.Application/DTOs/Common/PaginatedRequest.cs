namespace FraFactu.Application.DTOs.Common
{
    /// <summary>
    /// DTO para solicitudes paginadas
    /// </summary>
    public class PaginatedRequest
    {
        /// <summary>
        /// Número de página (1-indexed)
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Cantidad de elementos por página
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Campo por el cual ordenar
        /// </summary>
        public string? OrderBy { get; set; }

        /// <summary>
        /// Dirección del ordenamiento (asc/desc)
        /// </summary>
        public string OrderDirection { get; set; } = "asc";

        /// <summary>
        /// Término de búsqueda general
        /// </summary>
        public string? SearchTerm { get; set; }
    }
}
