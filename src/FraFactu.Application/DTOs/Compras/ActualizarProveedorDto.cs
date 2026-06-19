namespace FraFactu.Application.DTOs.Compras;

/// <summary>
/// DTO para actualizar un proveedor existente
/// </summary>
public class ActualizarProveedorDto
{
    /// <summary>
    /// NIT del proveedor (14 dígitos sin guiones)
    /// </summary>
    public string NIT { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Contacto { get; set; }
    public string? SitioWeb { get; set; }
    public string? Notas { get; set; }
    public bool Activo { get; set; } = true;
}
