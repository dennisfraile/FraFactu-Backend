namespace FraFactu.Application.DTOs.Compras;

/// <summary>
/// DTO para crear un proveedor
/// </summary>
public class CrearProveedorDto
{
    /// <summary>
    /// NIT del proveedor
    /// </summary>
    public string NIT { get; set; } = string.Empty;

    /// <summary>
    /// Nombre o razón social
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Nombre comercial (opcional)
    /// </summary>
    public string? NombreComercial { get; set; }

    /// <summary>
    /// Dirección física
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
    /// Notas u observaciones
    /// </summary>
    public string? Notas { get; set; }
}
