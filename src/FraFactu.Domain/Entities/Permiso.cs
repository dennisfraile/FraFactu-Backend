using FraFactu.Domain.Common;
namespace FraFactu.Domain.Entities
{
    public class Permiso : BaseEntity
    {
        /// <summary>
        /// Código único del permiso (ej: "factura.crear", "receptor.editar")
        /// </summary>
        public string Codigo { get; set; } = string.Empty;

        /// <summary>
        /// Nombre descriptivo del permiso
        /// </summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Slug para URLs (deprecado, usar Codigo)
        /// </summary>
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// Descripción detallada del permiso (opcional)
        /// </summary>
        public string? Descripcion { get; set; }

        /// <summary>
        /// Módulo al que pertenece (ej: "Facturas", "Reportes", "Sistema")
        /// </summary>
        public string Modulo { get; set; } = string.Empty;

        // Muchos a Muchos: Un permiso está en varios roles
        public ICollection<RolPermiso> RolesPermisos { get; set; } = new List<RolPermiso>();

    }
}