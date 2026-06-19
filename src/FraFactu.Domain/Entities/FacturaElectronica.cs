using FraFactu.Domain.Entities.Catalogos;
using FraFactu.Domain.Common;
namespace FraFactu.Domain.Entities
{
    public class FacturaElectronica : BaseEntity
    {
        // ==========================================
        // 1. RELACIONES DE NEGOCIO (SaaS)
        // ==========================================
        public int EmisorId { get; set; }
        public Emisor Emisor { get; set; } = null!;

        /// <summary>
        /// FK al lote en el que fue enviada esta factura (si se envió por lotes)
        /// </summary>
        public int? LoteId { get; set; }
        public Lote? Lote { get; set; }

        // Receptor es OPCIONAL si monto total < $1,095.00 según MH
        public int? ReceptorId { get; set; }
        public Receptor? Receptor { get; set; }

        // Sucursal desde donde se emite (opcional)
        public int? SucursalId { get; set; }
        public Sucursal? Sucursal { get; set; }

        /// <summary>
        /// Usuario (cajero) que creó la factura
        /// </summary>
        public int? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        // ==========================================
        // 2. SECCIÓN: IDENTIFICACIÓN (DTE)
        // ==========================================
        public int Version { get; set; } = 1; // Versión del formato (siempre 1)

        /// <summary>
        /// Ambiente en que fue emitida: "00" = Pruebas, "01" = Produccion
        /// </summary>
        public string Ambiente { get; set; } = string.Empty;

        // FK a CatTipoDocumento (01=Factura, 03=CCF)
        public int CatTipoDocumentoId { get; set; }
        public CatTipoDocumento TipoDocumento { get; set; } = null!;

        public string NumeroControl { get; set; } = string.Empty; // DTE-01-000...
        public string CodigoGeneracion { get; set; } = string.Empty; // UUID

        public int CatModeloFacturacionId { get; set; } = 1; // 1=Previo, 2=Diferido
        public int CatTipoTransmisionId { get; set; } = 1; // 1=Normal, 2=Contingencia

        // Fecha y Hora son obligatorias por separado
        public DateTime FechaEmision { get; set; }
        public TimeSpan HoraEmision { get; set; }

        // Fecha y Hora de Transmisión a Hacienda (para calcular plazo de invalidación)
        public DateTime? FechaTransmision { get; set; }
        public TimeSpan? HoraTransmision { get; set; }

        public int CatMonedaId { get; set; } = 1; // Por defecto USD

        // CAMPOS DE CONTINGENCIA (Opcionales)
        public int? CatTipoContingenciaId { get; set; }
        public string? MotivoContingencia { get; set; }

        // Campos para Contingencia Automática
        public int? TipoContingenciaSugerido { get; set; } // CAT-005: 1, 3, o null
        public string? DetalleErrorEnvio { get; set; }
        public DateTime? FechaErrorEnvio { get; set; }
        public int IntentosEnvio { get; set; } = 0;

        public int? EventoContingenciaId { get; set; }
        public EventoContingencia? EventoContingencia { get; set; }

        // ==========================================
        // 3. SECCIÓN: RESUMEN (TOTALES)
        // ==========================================
        public decimal TotalNoSujeto { get; set; }
        public decimal TotalExento { get; set; }
        public decimal TotalGravado { get; set; }
        public decimal SubTotalVentas { get; set; } // Suma de las 3 anteriores

        public decimal DescuentoNoSujeto { get; set; }
        public decimal DescuentoExento { get; set; }
        public decimal DescuentoGravado { get; set; }
        public decimal PorcentajeDescuento { get; set; } // Global
        public decimal TotalDescuento { get; set; }

        public decimal SubTotal { get; set; } // Ventas - Descuentos

        // IMPUESTOS
        public decimal TotalIva { get; set; } // IVA 13% sobre gravado
        public decimal IvaPercibido { get; set; } // +
        public decimal IvaRetenido { get; set; }  // -
        public decimal RetencionRenta { get; set; } // -

        public decimal MontoTotalOperacion { get; set; } // SubTotal + Impuestos

        public decimal TotalNoGravado { get; set; } // Cargos extra
        public decimal TotalPagar { get; set; }

        public string TotalLetras { get; set; } = string.Empty; // "CIEN DOLARES..."
        /// <summary>
        /// Número de Pago Electrónico - Número de transacción electrónica
        /// Máximo 100 caracteres (opcional)
        /// </summary>
        public string? NumPagoElectronico { get; set; }
        public decimal SaldoFavor { get; set; }

        public int CatCondicionOperacionId { get; set; }
        public CatCondicionOperacion CondicionOperacion { get; set; } = null!;

        // ==========================================
        // 4. ESTADO DEL PROCESO
        // ==========================================
        public string EstadoHacienda { get; set; } = "BORRADOR"; // PROCESADO, RECHAZADO
        public string? SelloRecibido { get; set; }

        /// <summary>
        /// JSON del documento firmado electrónicamente (JWS - JSON Web Signature)
        /// Se genera después de firmar con la llave privada del emisor
        /// Requerido para envío en lotes a MH
        /// </summary>
        public string? JsonFirmado { get; set; }

        public string? Observaciones { get; set; }

        /// <summary>
        /// Indica si el correo con el DTE fue enviado al cliente
        /// </summary>
        public bool CorreoEnviado { get; set; } = false;

        /// <summary>
        /// Fecha/hora del envío del correo al cliente
        /// </summary>
        public DateTime? FechaEnvioCorreo { get; set; }

        /// <summary>
        /// Email del receptor al que se envió el DTE (cache para no depender de Receptor)
        /// </summary>
        public string? EmailReceptor { get; set; }

        // ==========================================
        // 5. NAVEGACIÓN A CATÁLOGOS
        // ==========================================
        public CatTipoContingencia? TipoContingencia { get; set; }

        // ==========================================
        // 6. LISTAS (DETALLES)
        // ==========================================
        public ICollection<FacturaElectronicaDetalle> Detalles { get; set; } = new List<FacturaElectronicaDetalle>();
        public ICollection<FacturaTributo> Tributos { get; set; } = new List<FacturaTributo>(); // Resumen de impuestos
        public ICollection<Pago> Pagos { get; set; } = new List<Pago>(); // Formas de pago
        public ICollection<FacturaDocumentoRelacionado> DocumentosRelacionados { get; set; } = new List<FacturaDocumentoRelacionado>();
        public ICollection<FacturaApendice> Apendices { get; set; } = new List<FacturaApendice>();
        public FacturaExtencion? Extension { get; set; } // Relación 1 a 1 (Opcional)

        // ==========================================
        // 6. CAMPOS ESPECÍFICOS DE CCF
        // ==========================================

        /// <summary>
        /// Venta por cuenta de Terceros (opcional, relación 1:1)
        /// Disponible en: Factura (DTE-01) y CCF (DTE-03)
        /// </summary>
        public VentaTercero? VentaTercero { get; set; }

        /// <summary>
        /// Otros Documentos Asociados (máximo 10)
        /// Disponible en: Factura (DTE-01), CCF (DTE-03), y otros tipos de DTE
        /// codDocAsociado: 1=Mandato, 2=Certificado, 3=Servicio médico, 4=Donación
        /// </summary>
        /// <summary>
        /// Otros Documentos Asociados (máximo 10)
        /// Disponible en: Factura (DTE-01), CCF (DTE-03), y otros tipos de DTE
        /// codDocAsociado: 1=Mandato, 2=Certificado, 3=Servicio médico, 4=Donación
        /// </summary>
        public ICollection<OtroDocumento> OtrosDocumentos { get; set; } = new List<OtroDocumento>();

        // ==========================================
        // 7. RELACIONES DE VENTA
        // ==========================================
        public int? VendedorId { get; set; }
        public Vendedor? Vendedor { get; set; }

        public string? CodigoVendedor { get; set; } // Por si el vendedor se borra, mantener histórico

        public int? CajaId { get; set; }
        public Caja? Caja { get; set; }

        /// <summary>
        /// Snapshot point-in-time del payload fiscal (Emisor + Sucursal) tal como
        /// existía al emitir la factura. Permite auditoría histórica: si luego se
        /// cambian datos fiscales del Emisor, las facturas anteriores conservan el
        /// dato usado al emitirlas. Null si no se capturó snapshot.
        /// </summary>
        public string? SnapshotFiscalJson { get; set; }
    }
}