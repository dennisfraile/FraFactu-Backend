namespace FraFactu.Application.DTOs;

/// <summary>
/// Item del catalogo de productos/servicios de una Sucursal Smartix que SmartCare
/// muestra al admin para que mapee con un servicio SmartCare (ej. "Consulta")
/// en el modulo de Facturacion electronica.
///
/// Incluye productos y servicios (no se filtra por CatTipoItemId) porque
/// algunas clinicas cobran insumos junto con la consulta. El admin decide
/// que items son facturables para SmartCare.
/// </summary>
public class ServicioParaMapeoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal PrecioUnitario { get; set; }
    /// <summary>1 = Producto fisico, 2 = Servicio.</summary>
    public int? CatTipoItemId { get; set; }
}
