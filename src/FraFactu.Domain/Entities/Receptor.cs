using FraFactu.Domain.Common;
using FraFactu.Domain.Entities.Catalogos;

namespace FraFactu.Domain.Entities;

public class Receptor : BaseEntity
{
    public int EmisorId { get; set; }
    public Emisor Emisor { get; set; } = null!;

    /// <summary>
    /// FK al catálogo de tipo de documento de identificación del receptor (CAT-04).
    /// 36=NIT, 13=DUI, 02=Carnet, 03=Pasaporte, 37=Otro.
    /// Nullable: receptor FC tipo 01 puede ir "Sin documento" (MH acepta
    /// tipoDocumento + numDocumento ambos null en Factura de Consumidor Final).
    /// </summary>
    public int? CatTipoDocumentoIdentificacionReceptorId { get; set; }
    public CatTipoDocumentoIdentificacionReceptor? TipoDocumento { get; set; }

    /// <summary>
    /// Nullable: receptor FC "Sin documento" (par natural con CatTipoDocumentoIdentificacionReceptorId).
    /// </summary>
    public string? NumeroDocumento { get; set; }
    public string? Nrc { get; set; } // NRC (opcional, solo para empresas)
    public string NombreRazonSocial { get; set; } = string.Empty;

    // ACTIVIDAD ECONÓMICA (opcional para receptores empresariales)
    public string? CodigoActividad { get; set; }
    public string? DescripcionActividad { get; set; }

    // DIRECCIÓN (opcional pero recomendado)
    /// <summary>
    /// FK al catálogo de departamentos (CAT-12) - Opcional
    /// </summary>
    public int? CatDepartamentoId { get; set; }
    public CatDepartamento? Departamento { get; set; }

    /// <summary>
    /// FK al catálogo de municipios (CAT-13) - Opcional
    /// </summary>
    public int? CatMunicipioId { get; set; }
    public CatMunicipio? Municipio { get; set; }

    /// <summary>
    /// FK al catálogo de distritos (CAT-008). Obligatorio en la Normativa DTE V2.0
    /// cuando el receptor lleva dirección. Nullable hasta poblar el catálogo y migrar datos.
    /// </summary>
    public int? CatDistritoId { get; set; }
    public CatDistrito? Distrito { get; set; }

    public string Direccion { get; set; } = string.Empty; // Complemento de dirección

    // CONTACTO
    public string CorreoElectronico { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;

    // Relación: Un cliente tiene muchas facturas históricas
    public ICollection<FacturaElectronica> Facturas { get; set; } = new List<FacturaElectronica>();
}