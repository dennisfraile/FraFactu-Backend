namespace FraFactu.Application.DTOs.Auth
{
    /// <summary>
    /// DTO para cambio de contexto de SuperAdmin
    /// </summary>
    public class CambiarContextoDto
    {
        /// <summary>
        /// ID del emisor al cual cambiar contexto
        /// </summary>
        public int EmisorId { get; set; }

        /// <summary>
        /// ID de la sucursal (opcional)
        /// </summary>
        public int? SucursalId { get; set; }
    }
}
