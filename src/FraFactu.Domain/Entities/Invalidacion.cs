using FraFactu.Domain.Common;
using FraFactu.Domain.Entities.Catalogos;

namespace FraFactu.Domain.Entities
{
    /// <summary>
    /// Evento de invalidación de un DTE
    /// NO es un DTE, es una estructura JSON que informa a MH sobre la invalidación
    /// </summary>
    public class Invalidacion : BaseEntity
    {
        // IDENTIFICACIÓN
        /// <summary>
        /// Código de generación único del evento (UUID)
        /// </summary>
        public string CodigoGeneracion { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de invalidación
        /// </summary>
        public DateTime FechaAnulacion { get; set; }

        /// <summary>
        /// Hora de invalidación
        /// </summary>
        public TimeSpan HoraAnulacion { get; set; }

        /// <summary>
        /// Ambiente: 00=Pruebas, 01=Producción
        /// </summary>
        public string Ambiente { get; set; } = "00";

        // RELACIÓN CON DTE A INVALIDAR
        /// <summary>
        /// Factura electrónica que se invalida
        /// </summary>
        public int FacturaElectronicaId { get; set; }
        public FacturaElectronica FacturaElectronica { get; set; } = null!;

        // DOCUMENTO DE REEMPLAZO (OPCIONAL)
        /// <summary>
        /// Factura de reemplazo (si tipo invalidación 1 o 3)
        /// </summary>
        public int? FacturaReemplazoId { get; set; }
        public FacturaElectronica? FacturaReemplazo { get; set; }

        /// <summary>
        /// Código de generación del documento de reemplazo
        /// </summary>
        public string? CodigoGeneracionReemplazo { get; set; }

        // MOTIVO
        /// <summary>
        /// Tipo de invalidación: 1=Error info, 2=Rescisión, 3=Otro
        /// </summary>
        public int TipoAnulacion { get; set; }

        /// <summary>
        /// Descripción del motivo
        /// </summary>
        public string? MotivoAnulacion { get; set; }

        // RESPONSABLES
        /// <summary>
        /// Nombre del responsable de invalidar
        /// </summary>
        public string NombreResponsable { get; set; } = string.Empty;

        /// <summary>
        /// FK al catálogo de tipo documento responsable (CAT-22)
        /// 36=NIT, 13=DUI, 02=Carnet residente, 03=Pasaporte, 37=Otro
        /// </summary>
        public int CatTipoDocResponsableId { get; set; }
        public CatTipoDocumento TipoDocResponsable { get; set; } = null!;

        /// <summary>
        /// Número documento responsable
        /// </summary>
        public string NumDocResponsable { get; set; } = string.Empty;

        /// <summary>
        /// Nombre de quien solicita invalidación
        /// </summary>
        public string NombreSolicita { get; set; } = string.Empty;

        /// <summary>
        /// FK al catálogo de tipo documento solicitante (CAT-22)
        /// </summary>
        public int CatTipoDocSolicitaId { get; set; }
        public CatTipoDocumento TipoDocSolicita { get; set; } = null!;

        /// <summary>
        /// Número documento solicitante
        /// </summary>
        public string NumDocSolicita { get; set; } = string.Empty;

        // INFORMACIÓN DEL RECEPTOR (del DTE original)
        /// <summary>
        /// FK al catálogo de tipo documento receptor (CAT-22)
        /// </summary>
        public int CatTipoDocReceptorId { get; set; }
        public CatTipoDocumento TipoDocReceptor { get; set; } = null!;

        /// <summary>
        /// Número documento receptor
        /// </summary>
        public string NumDocReceptor { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del receptor
        /// </summary>
        public string NombreReceptor { get; set; } = string.Empty;

        /// <summary>
        /// Teléfono receptor (opcional)
        /// </summary>
        public string? TelefonoReceptor { get; set; }

        /// <summary>
        /// Correo receptor (opcional)
        /// </summary>
        public string? CorreoReceptor { get; set; }

        // DATOS DEL DTE ORIGINAL
        /// <summary>
        /// Monto IVA del documento original
        /// </summary>
        public decimal? MontoIva { get; set; }

        // TRANSMISIÓN A MH
        /// <summary>
        /// Fecha de transmisión a MH
        /// </summary>
        public DateTime? FechaTransmision { get; set; }

        /// <summary>
        /// Sello de recepción otorgado por MH
        /// </summary>
        public string? SelloRecibido { get; set; }

        /// <summary>
        /// Estado en Hacienda
        /// </summary>
        public string? EstadoHacienda { get; set; }

        /// <summary>
        /// JSON del evento enviado
        /// </summary>
        public string? JsonEvento { get; set; }

        /// <summary>
        /// JSON de respuesta de MH
        /// </summary>
        public string? JsonRespuesta { get; set; }

        // GESTIÓN DE INVENTARIO
        /// <summary>
        /// Tipo de invalidación para gestión de inventario
        /// ERROR_FACTURA: Error en factura, se emitirá una nueva
        /// DEVOLUCION_SIMPLE: Cliente devuelve producto
        /// CAMBIO_PRODUCTO: Cliente devuelve producto A y se le entrega producto B
        /// </summary>
        public string? TipoInvalidacion { get; set; }

        /// <summary>
        /// Indica si la invalidación debe revertir el movimiento de inventario
        /// true = El producto regresa al inventario
        /// false = El producto NO regresa (por ejemplo, producto dañado)
        /// </summary>
        public bool RevirtiInventario { get; set; } = true;

        // RELACIÓN CON EMISOR
        public int EmisorId { get; set; }
        public Emisor Emisor { get; set; } = null!;

    }
}
