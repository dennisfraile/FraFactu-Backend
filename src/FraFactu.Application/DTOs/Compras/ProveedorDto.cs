namespace FraFactu.Application.DTOs.Compras;

/// <summary>
/// DTO para mostrar información de un proveedor
/// </summary>
public class ProveedorDto
{
    public int Id { get; set; }
    public string NIT { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del tipo de documento (siempre "NIT" para proveedores)
    /// </summary>
    public string TipoDocumentoNombre { get; set; } = "NIT";

    public string Nombre { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Contacto { get; set; }
    public string? SitioWeb { get; set; }
    public string? Notas { get; set; }
    public bool Activo { get; set; }

    // Información agregada
    public int TotalCompras { get; set; }
    public decimal MontoTotalCompras { get; set; }
    public DateTime? UltimaCompra { get; set; }

    public DateTime FechaCreacion { get; set; }
}
