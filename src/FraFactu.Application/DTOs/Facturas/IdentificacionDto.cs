namespace FraFactu.Application.DTOs.Facturas
{
    /// <summary>
    /// DTO para la sección de Identificación del DTE según estándar de Hacienda
    /// </summary>
    public class IdentificacionDto
    {
        /// <summary>
        /// Versión del formato del documento. Siempre debe ser 1
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Tipo de Documento Tributario Electrónico. 01 = Factura
        /// </summary>
        public string TipoDte { get; set; } = "01";

        /// <summary>
        /// Número de Control del DTE. Formato: DTE-01-XXXXXXXX-000000000000000
        /// Generado automáticamente por el sistema
        /// </summary>
        public string NumeroControl { get; set; } = string.Empty;

        /// <summary>
        /// Código de Generación único del DTE (GUID)
        /// Generado automáticamente por el sistema
        /// </summary>
        public string CodigoGeneracion { get; set; } = string.Empty;

        /// <summary>
        /// Modelo de Facturación. 1 = Previo, 2 = Diferido
        /// </summary>
        public int TipoModelo { get; set; }

        /// <summary>
        /// Tipo de Transmisión. 1 = Normal, 2 = Contingencia
        /// </summary>
        public int TipoOperacion { get; set; }

        /// <summary>
        /// Tipo de Contingencia (1-5). Obligatorio si TipoOperacion == 2
        /// </summary>
        public int? TipoContingencia { get; set; }

        /// <summary>
        /// Motivo de la Contingencia (max 150 chars). Obligatorio si TipoContingencia == 5 (Otro)
        /// </summary>
        public string? MotivoContingencia { get; set; }

        /// <summary>
        /// Si es false, NO se crea evento de contingencia automático.
        /// La factura queda en PENDIENTE_LOTE para acumular en lote manual.
        /// </summary>
        public bool CrearEventoAutomatico { get; set; } = true;

        /// <summary>
        /// Fecha de Emisión del documento
        /// </summary>
        public DateTime FechaEmision { get; set; }

        /// <summary>
        /// Hora de Emisión del documento. Formato: HH:mm:ss
        /// </summary>
        public string HoraEmision { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de Moneda. Siempre USD para El Salvador
        /// </summary>
        public string TipoMoneda { get; set; } = "USD";
    }
}
