namespace FraFactu.Application.DTOs.Facturas
{
    /// <summary>
    /// DTO para crear una nueva Factura Electrónica
    /// </summary>
    public class CreateFacturaElectronicaDto
    {
        /// <summary>
        /// Identificación del DTE
        /// </summary>
        public IdentificacionDto Identificacion { get; set; } = new();

        /// <summary>
        /// ID del Receptor/Cliente (opcional para factura a consumidor final)
        /// </summary>
        public int? ReceptorId { get; set; }

        /// <summary>
        /// Datos del receptor si no está registrado en el sistema
        /// </summary>
        public ReceptorDteDto? Receptor { get; set; }

        /// <summary>
        /// ID de la Sucursal desde donde se emite
        /// </summary>
        public int SucursalId { get; set; }

        /// <summary>
        /// ID de la Caja (opcional, si aplica)
        /// </summary>
        public int? CajaId { get; set; }

        /// <summary>
        /// ID del Vendedor que realiza la venta (opcional)
        /// </summary>
        public int? VendedorId { get; set; }

        /// <summary>
        /// Cuerpo del Documento (ítems/detalles)
        /// Mínimo 1, máximo 2000 ítems
        /// </summary>
        public List<ItemDocumentoDto> CuerpoDocumento { get; set; } = new();

        /// <summary>
        /// Resumen de totales y formas de pago
        /// </summary>
        public ResumenDto Resumen { get; set; } = new();

        /// <summary>
        /// Extensión del documento (opcional)
        /// Para información adicional no contemplada en el estándar
        /// </summary>
        public ExtensionDto? Extension { get; set; }

        /// <summary>
        /// Documentos relacionados (opcional)
        /// Ej: Notas de crédito, remisiones, etc.
        /// </summary>
        public List<DocumentoRelacionadoDto>? DocumentosRelacionados { get; set; }

        /// <summary>
        /// Ventas por cuenta de Terceros (específico de CCF)
        /// </summary>
        public VentaTerceroDto? VentaTercero { get; set; }

        /// <summary>
        /// Otros Documentos Asociados (específico de CCF)
        /// Máximo 10 documentos
        /// </summary>
        public List<OtroDocumentoDto>? OtrosDocumentos { get; set; }

        /// <summary>
        /// Apéndice - Información adicional en formato clave-valor (opcional)
        /// </summary>
        public List<ApendiceDto>? Apendices { get; set; }

        /// <summary>
        /// Observaciones generales (opcional)
        /// </summary>
        public string? Observaciones { get; set; }
    }

    /// <summary>
    /// DTO para Receptor en el contexto del DTE
    /// (puede ser diferente al Receptor ya registrado)
    /// </summary>
    public class ReceptorDteDto
    {
        /// <summary>
        /// Tipo de Documento: 36=NIT, 13=DUI, 02=Carnet Residente, 03=Pasaporte, 37=Otro
        /// </summary>
        public string? TipoDocumento { get; set; }

        /// <summary>
        /// Número de Documento
        /// </summary>
        public string? NumDocumento { get; set; }

        /// <summary>
        /// NRC del receptor (opcional, solo empresas)
        /// </summary>
        public string? Nrc { get; set; }

        /// <summary>
        /// Nombre o Razón Social del receptor
        /// </summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Nombre Comercial del receptor (opcional, incluido en schema MH)
        /// </summary>
        public string? NombreComercial { get; set; }

        /// <summary>
        /// Código de actividad económica (opcional)
        /// </summary>
        public string? CodActividad { get; set; }

        /// <summary>
        /// Descripción de actividad económica (opcional)
        /// </summary>
        public string? DescActividad { get; set; }

        /// <summary>
        /// Dirección del receptor (opcional)
        /// </summary>
        public DireccionDto? Direccion { get; set; }

        /// <summary>
        /// Teléfono del receptor (opcional)
        /// </summary>
        public string? Telefono { get; set; }

        /// <summary>
        /// Correo electrónico del receptor (opcional)
        /// </summary>
        public string? Correo { get; set; }
    }

    /// <summary>
    /// DTO para documentos relacionados
    /// </summary>
    public class DocumentoRelacionadoDto
    {
        /// <summary>
        /// Tipo de documento relacionado
        /// </summary>
        public string TipoDocumento { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de generación: 1=Propio, 2=Externo
        /// </summary>
        public int TipoGeneracion { get; set; }

        /// <summary>
        /// Número del documento relacionado
        /// </summary>
        public string NumeroDocumento { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de emisión del documento relacionado
        /// </summary>
        public DateTime FechaEmision { get; set; }
    }
}
