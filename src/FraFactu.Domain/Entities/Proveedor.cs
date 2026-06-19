using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities;

/// <summary>
/// Proveedor de productos o servicios
/// </summary>
public class Proveedor : BaseEntity
{
    // ==========================================
    // INFORMACIÓN BÁSICA (OBLIGATORIA)
    // ==========================================

    /// <summary>
    /// NIT del proveedor
    /// </summary>
    public string NIT { get; set; } = string.Empty;

    /// <summary>
    /// Nombre o razón social del proveedor
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    // ==========================================
    // INFORMACIÓN ADICIONAL (OPCIONAL)
    // ==========================================

    /// <summary>
    /// Nombre comercial del proveedor
    /// </summary>
    public string? NombreComercial { get; set; }

    /// <summary>
    /// Dirección física del proveedor
    /// </summary>
    public string? Direccion { get; set; }

    /// <summary>
    /// Teléfono de contacto
    /// </summary>
    public string? Telefono { get; set; }

    /// <summary>
    /// Email de contacto
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Nombre de la persona de contacto
    /// </summary>
    public string? Contacto { get; set; }

    /// <summary>
    /// Sitio web del proveedor
    /// </summary>
    public string? SitioWeb { get; set; }

    /// <summary>
    /// Notas u observaciones del proveedor
    /// </summary>
    public string? Notas { get; set; }

    // ==========================================
    // ESTADO
    // ==========================================

    /// <summary>
    /// Indica si el proveedor está activo
    /// </summary>
    public new bool Activo { get; set; } = true;

    // ==========================================
    // RELACIÓN CON EMISOR (MULTI-TENANT)
    // ==========================================

    /// <summary>
    /// ID del emisor al que pertenece este proveedor
    /// </summary>
    public int EmisorId { get; set; }

    /// <summary>
    /// Emisor propietario del proveedor
    /// </summary>
    public Emisor Emisor { get; set; } = null!;

    // ==========================================
    // NAVEGACIÓN
    // ==========================================

    /// <summary>
    /// Compras realizadas a este proveedor
    /// </summary>
    public ICollection<CompraExterna> Compras { get; set; } = new List<CompraExterna>();
}
