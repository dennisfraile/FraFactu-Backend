namespace FraFactu.Application.DTOs.Facturas
{
    /// <summary>
    /// DTO de respuesta para Factura Electrónica
    /// </summary>
    public class FacturaElectronicaResponseDto
    {
        /// <summary>
        /// ID de la factura en el sistema
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// ID de la sucursal (opcional)
        /// </summary>
        public int? SucursalId { get; set; }

        /// <summary>
        /// ID del vendedor que realizó la venta (opcional)
        /// </summary>
        public int? VendedorId { get; set; }

        /// <summary>
        /// Nombre del vendedor que realizó la venta (opcional)
        /// </summary>
        public string? VendedorNombre { get; set; }

        /// <summary>
        /// Código del vendedor
        /// </summary>
        public string? VendedorCodigo { get; set; }

        /// <summary>
        /// ID de la caja donde se realizó la venta (opcional)
        /// </summary>
        public int? CajaId { get; set; }

        /// <summary>
        /// Código de la caja
        /// </summary>
        public string? CajaCodigo { get; set; }

        /// <summary>
        /// Código de Generación del DTE (GUID)
        /// </summary>
        public string CodigoGeneracion { get; set; } = string.Empty;

        /// <summary>
        /// Número de Control del DTE
        /// </summary>
        public string NumeroControl { get; set; } = string.Empty;

        /// <summary>
        /// Sello de Recepción de Hacienda (cuando se envía)
        /// </summary>
        public string? SelloRecepcion { get; set; }

        /// <summary>
        /// Fecha y hora de emisión
        /// </summary>
        public DateTime FechaEmision { get; set; }
        public TimeSpan HoraEmision { get; set; }

        /// <summary>
        /// Fecha y hora de transmisión a Hacienda
        /// </summary>
        public DateTime? FechaTransmision { get; set; }
        public TimeSpan? HoraTransmision { get; set; }

        /// <summary>
        /// Identificación del DTE
        /// </summary>
        public IdentificacionDto Identificacion { get; set; } = new();

        /// <summary>
        /// Datos del Emisor
        /// </summary>
        public EmisorDteDto Emisor { get; set; } = new();

        /// <summary>
        /// Datos del Receptor (puede ser null para consumidor final)
        /// </summary>
        public ReceptorDteDto? Receptor { get; set; }

        /// <summary>
        /// Ítems de la factura
        /// </summary>
        public List<ItemDocumentoDto> CuerpoDocumento { get; set; } = new();

        /// <summary>
        /// Resumen de totales
        /// </summary>
        public ResumenDto Resumen { get; set; } = new();

        /// <summary>
        /// Estado del DTE: Generado, Enviado, Aprobado, Rechazado
        /// </summary>
        public string Estado { get; set; } = string.Empty;

        /// <summary>
        /// JSON del DTE generado
        /// </summary>
        public string? JsonDte { get; set; }

        /// <summary>
        /// Respuesta de Hacienda
        /// </summary>
        public string? RespuestaHacienda { get; set; }

        /// <summary>
        /// Fecha de creación en el sistema
        /// </summary>
        public DateTime FechaCreacion { get; set; }

        /// <summary>
        /// Venta a cuenta de terceros (CCF)
        /// </summary>
        public VentaTerceroDto? VentaTercero { get; set; }

        /// <summary>
        /// Documentos asociados (CCF)
        /// </summary>
        public List<OtroDocumentoDto>? OtrosDocumentos { get; set; }

        /// <summary>
        /// Documentos relacionados (notas de crédito, remisiones, etc.)
        /// </summary>
        public List<DocumentoRelacionadoResponseDto>? DocumentosRelacionados { get; set; }

        /// <summary>
        /// Extensión del DTE (datos de entrega/recepción)
        /// </summary>
        public ExtensionDto? Extension { get; set; }

        /// <summary>
        /// Apéndices del DTE (información adicional clave-valor)
        /// </summary>
        public List<ApendiceDto>? Apendice { get; set; }
    }

    /// <summary>
    /// DTO de respuesta para documentos relacionados
    /// </summary>
    public class DocumentoRelacionadoResponseDto
    {
        public string TipoDocumento { get; set; } = string.Empty;
        public string NumeroDocumento { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; }
    }

    /// <summary>
    /// DTO del Emisor para DTE
    /// </summary>
    public class EmisorDteDto
    {
        /// <summary>
        /// NIT del emisor (9 o 14 dígitos)
        /// </summary>
        public string Nit { get; set; } = string.Empty;

        /// <summary>
        /// NRC del emisor (1-8 dígitos)
        /// </summary>
        public string Nrc { get; set; } = string.Empty;

        /// <summary>
        /// Nombre o Razón Social
        /// </summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Código de Actividad Económica (5-6 dígitos)
        /// </summary>
        public string CodActividad { get; set; } = string.Empty;

        /// <summary>
        /// Descripción de Actividad Económica
        /// </summary>
        public string DescActividad { get; set; } = string.Empty;

        /// <summary>
        /// Nombre Comercial (opcional)
        /// </summary>
        public string? NombreComercial { get; set; }

        /// <summary>
        /// Tipo de Establecimiento: 01=Casa Matriz, 02=Sucursal, etc.
        /// </summary>
        public string TipoEstablecimiento { get; set; } = string.Empty;

        /// <summary>
        /// Dirección del establecimiento
        /// </summary>
        public DireccionDto Direccion { get; set; } = new();

        /// <summary>
        /// Teléfono
        /// </summary>
        public string Telefono { get; set; } = string.Empty;

        /// <summary>
        /// Correo electrónico
        /// </summary>
        public string Correo { get; set; } = string.Empty;

        /// <summary>
        /// Código del establecimiento (opcional)
        /// </summary>
        public string? CodEstablecimiento { get; set; }

        /// <summary>
        /// Código del punto de venta (opcional)
        /// </summary>
        public string? CodPuntoVenta { get; set; }
    }
}
