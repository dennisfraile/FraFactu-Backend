namespace FraFactu.Application.DTOs.OperacionesEspeciales;

/// <summary>
/// Request para crear un Evento de Operaciones Especiales (17) por carga manual.
/// El backend calcula los totales del resumen y el total en letras a partir de los ítems;
/// el resumen de tributos lo provee el emisor (descripción y valor por código de tributo).
/// </summary>
public class CrearEventoOperacionEspecialDto
{
    /// <summary>Cuerpo del documento: ítems a reportar (1 a 2000).</summary>
    public List<ItemOperacionEspecialDto> Items { get; set; } = new();

    /// <summary>Resumen de tributos (código + descripción + valor). Opcional.</summary>
    public List<TributoResumenInputDto>? Tributos { get; set; }

    /// <summary>Apéndice opcional (1 a 10 entradas).</summary>
    public List<ApendiceInputDto>? Apendice { get; set; }
}

public class ItemOperacionEspecialDto
{
    public string? CodigoGeneracionRef { get; set; }
    public string TipoDocumento { get; set; } = string.Empty;
    public string? NumDocumento { get; set; }
    public DateTime? FechaEmision { get; set; }
    public int Cantidad { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? DocDel { get; set; }
    public string? DocAl { get; set; }
    public decimal PrecioUni { get; set; }
    public decimal VentaNoSuj { get; set; }
    public decimal VentaExenta { get; set; }
    public decimal VentaGravada { get; set; }
    public List<string>? Tributos { get; set; }
}

public class TributoResumenInputDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal Valor { get; set; }
}

public class ApendiceInputDto
{
    public string Campo { get; set; } = string.Empty;
    public string Etiqueta { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
}
