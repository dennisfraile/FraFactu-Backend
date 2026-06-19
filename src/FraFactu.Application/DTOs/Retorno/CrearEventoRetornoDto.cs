namespace FraFactu.Application.DTOs.Retorno;

/// <summary>
/// Request para crear un Evento de Retorno (18) por carga manual.
/// El backend calcula los totales del resumen y el total en letras a partir de los ítems;
/// el resumen de tributos y los campos de política los provee el emisor.
/// ventaTercero y compraTercero son mutuamente excluyentes.
/// </summary>
public class CrearEventoRetornoDto
{
    // Identificación (campos editables; el resto los fija el backend)
    public int TipoModelo { get; set; } = 1;
    public int TipoOperacion { get; set; } = 1;
    public int? TipoContingencia { get; set; }
    public string? MotivoContin { get; set; }
    public string? Fusion { get; set; }

    // Emisor (campos de exportación)
    public string? CodEstableMH { get; set; }
    public string? CodEstable { get; set; }
    public string? CodPuntoVentaMH { get; set; }
    public string? CodPuntoVenta { get; set; }
    public string? RecintoFiscal { get; set; }
    public string? TipoRegimen { get; set; }
    public string? Regimen { get; set; }
    public int? TipoItemExpor { get; set; }

    /// <summary>Documentos relacionados (1 a 50).</summary>
    public List<DocumentoRelacionadoInputDto> DocumentoRelacionado { get; set; } = new();

    /// <summary>Receptor/documento (opcional, nullable en el esquema).</summary>
    public DocumentoReceptorInputDto? Documento { get; set; }

    /// <summary>Venta por cuenta de terceros (excluyente con CompraTercero).</summary>
    public VentaTerceroInputDto? VentaTercero { get; set; }

    /// <summary>Compra por cuenta de terceros (excluyente con VentaTercero).</summary>
    public CompraTerceroInputDto? CompraTercero { get; set; }

    /// <summary>Cuerpo del documento: ítems (1 a 2000).</summary>
    public List<ItemRetornoDto> Items { get; set; } = new();

    /// <summary>Resumen de tributos (código + descripción + valor). Opcional.</summary>
    public List<TributoResumenInputDto>? Tributos { get; set; }

    /// <summary>Campos de política del resumen no derivables de los ítems.</summary>
    public decimal TotalCompraExcluidos { get; set; }
    public decimal TotalNoOnerosas { get; set; }
    public decimal SaldoFavor { get; set; }

    /// <summary>Apéndice opcional (1 a 10 entradas).</summary>
    public List<ApendiceInputDto>? Apendice { get; set; }
}

public class DocumentoRelacionadoInputDto
{
    public string TipoDocumento { get; set; } = string.Empty;
    public string CodigoGeneracion { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
}

public class DocumentoReceptorInputDto
{
    public string? TipoDocumento { get; set; }
    public string? NumDocumento { get; set; }
    public string? Nombre { get; set; }
    public string? CodPais { get; set; }
    public string? NombrePais { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
}

public class VentaTerceroInputDto
{
    public string? Nit { get; set; }
    public string? Nombre { get; set; }
    public int? CodDomiciliado { get; set; }
}

public class CompraTerceroInputDto
{
    public string? NumDocumento { get; set; }
    public string? Nombre { get; set; }
}

public class ItemRetornoDto
{
    public int TipoItem { get; set; }
    public string CodigoGeneracion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal PrecioUni { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? Codigo { get; set; }
    public int UniMedida { get; set; }
    public decimal MontoDescu { get; set; }
    public string? CodTributo { get; set; }
    public decimal VentaNoSuj { get; set; }
    public decimal VentaExenta { get; set; }
    public decimal VentaGravada { get; set; }
    public decimal Compra { get; set; }
    public List<string>? Tributos { get; set; }
    public decimal Psv { get; set; }
    public decimal IvaItem { get; set; }
    public decimal NoGravado { get; set; }
    public decimal Seguro { get; set; }
    public decimal Flete { get; set; }
    public decimal IvaRete { get; set; }
    public decimal ReteRenta { get; set; }
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
