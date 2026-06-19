namespace FraFactu.Application.DTOs.Common
{
    /// <summary>
    /// DTO para respuestas paginadas
    /// </summary>
    public class PaginatedResponse<T>
    {
        /// <summary>
        /// Lista de elementos de la página actual
        /// </summary>
        public List<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// Número de página actual
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Total de páginas
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Tamaño de página
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total de registros
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Indica si hay página anterior
        /// </summary>
        public bool HasPreviousPage => CurrentPage > 1;

        /// <summary>
        /// Indica si hay página siguiente
        /// </summary>
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}
