using FraFactu.Application.DTOs.Hacienda;

namespace FraFactu.Application.Interfaces.Hacienda
{
    public interface IHaciendaApiService
    {
        /// <summary>
        /// Envía un DTE al Ministerio de Hacienda (Modo uno a uno).
        /// Orquesta el proceso: Obtener Token -> Firmar DTE -> Enviar.
        /// </summary>
        /// <param name="emisorId">ID del emisor que envía</param>
        /// <param name="dte">Objeto DTE completo</param>
        /// <returns>Respuesta de Hacienda (RecepcionResponseDto)</returns>
        Task<RecepcionResponseDto> TransmitirDteAsync(int emisorId, DteBaseDto dte);

        /// <summary>
        /// Envía un DTE al Ministerio de Hacienda usando JSON pre-construido.
        /// Evita el round-trip de serialización que puede agregar campos extra no válidos para ciertos tipos de DTE.
        /// </summary>
        /// <param name="emisorId">ID del emisor que envía</param>
        /// <param name="jsonDte">JSON del DTE ya construido según el schema correspondiente</param>
        /// <param name="version">Versión del DTE</param>
        /// <param name="tipoDte">Tipo de DTE (01=Factura, 03=CCF, etc.)</param>
        /// <param name="codigoGeneracion">Código de generación UUID del DTE</param>
        /// <returns>Respuesta de Hacienda (RecepcionResponseDto)</returns>
        Task<RecepcionResponseDto> TransmitirDteJsonAsync(int emisorId, string jsonDte, int version, string tipoDte, string codigoGeneracion);

        /// <summary>
        /// Envía múltiples DTEs al Ministerio de Hacienda en lote.
        /// </summary>
        /// <param name="emisorId">ID del emisor que envía</param>
        /// <param name="dtes">Lista de DTEs a enviar</param>
        /// <returns>Respuesta de Hacienda (RecepcionLoteResponseDto)</returns>
        Task<RecepcionLoteResponseDto> TransmitirDteLoteAsync(int emisorId, List<DteBaseDto> dtes);

        /// <summary>
        /// Envía un lote de DTEs ya firmados al Ministerio de Hacienda.
        /// </summary>
        /// <param name="emisorId">ID del emisor</param>
        /// <param name="documentosFirmados">Lista de JSONs firmados</param>
        /// <returns>Respuesta de Hacienda</returns>
        Task<RecepcionLoteResponseDto> TransmitirLoteDtesFirmadosAsync(int emisorId, List<string> documentosFirmados);

        /// <summary>
        /// Envía un Evento de Contingencia al Ministerio de Hacienda.
        /// </summary>
        /// <param name="emisorId">ID del emisor que envía</param>
        /// <param name="eventoContingencia">Evento de contingencia completo</param>
        /// <returns>Respuesta de Hacienda (ContingenciaResponseDto)</returns>
        Task<ContingenciaResponseDto> EnviarEventoContingenciaAsync(int emisorId, EventoContingenciaDto eventoContingencia);

        /// <summary>
        /// Envía un Evento de Invalidación al Ministerio de Hacienda para anular un DTE.
        /// </summary>
        /// <param name="emisorId">ID del emisor que envía</param>
        /// <param name="eventoInvalidacion">Evento de invalidación completo</param>
        /// <returns>Respuesta de Hacienda (InvalidacionResponseDto)</returns>
        Task<InvalidacionResponseDto> AnularDteAsync(int emisorId, EventoInvalidacionDto eventoInvalidacion);

        /// <summary>
        /// Envía un Evento de Operaciones Especiales (tipoEvento "17") al Ministerio de Hacienda.
        /// </summary>
        /// <param name="emisorId">ID del emisor que envía</param>
        /// <param name="evento">Evento de operaciones especiales completo</param>
        /// <returns>Respuesta de Hacienda (OperacionesEspecialesResponseDto)</returns>
        Task<OperacionesEspecialesResponseDto> EnviarEventoOperacionesEspecialesAsync(int emisorId, EventoOperacionesEspecialesDto evento);

        /// <summary>
        /// Envía un Evento de Retorno (tipoEvento "18") al Ministerio de Hacienda.
        /// </summary>
        /// <param name="emisorId">ID del emisor que envía</param>
        /// <param name="evento">Evento de retorno completo</param>
        /// <returns>Respuesta de Hacienda (RetornoResponseDto)</returns>
        Task<RetornoResponseDto> EnviarEventoRetornoAsync(int emisorId, EventoRetornoDto evento);

        /// <summary>
        /// Consulta el estado de un lote de facturas enviado previamente.
        /// GET /fesv/recepcion/consultadtelote/{codigoLote}
        /// </summary>
        /// <param name="emisorId">ID del emisor consultante</param>
        /// <param name="codigoGeneracionLote">Código de generación del lote a consultar</param>
        /// <returns>Estado actual del lote en MH</returns>
        Task<ConsultaLoteResponseDto> ConsultarEstadoLoteAsync(int emisorId, string codigoGeneracionLote);

        /// <summary>
        /// Consulta el estado de un DTE individual.
        /// POST /fesv/recepcion/consultadte/
        /// </summary>
        /// <param name="emisorId">ID del emisor consultante</param>
        /// <param name="codigoGeneracion">Código de generación del DTE a consultar</param>
        /// <param name="tipoDte">Tipo de DTE (01=Factura, 03=CCF, etc.)</param>
        /// <returns>Estado actual del DTE en MH</returns>
        Task<ConsultaEstadoDteResponseDto> ConsultarEstadoDteAsync(int emisorId, string codigoGeneracion, string tipoDte);
    }

    public class RecepcionResponseDto
    {
        public int Version { get; set; }
        public string Ambiente { get; set; } = string.Empty;
        public int VersionApp { get; set; }
        public string Estado { get; set; } = string.Empty; // "PROCESADO", "RECHAZADO"
        public string CodigoGeneracion { get; set; } = string.Empty;
        public string SelloRecibido { get; set; } = string.Empty;
        public string FhProcesamiento { get; set; } = string.Empty;
        public string ClasificaMsg { get; set; } = string.Empty;
        public string CodigoMsg { get; set; } = string.Empty;
        public string DescripcionMsg { get; set; } = string.Empty;
        public List<string>? Observaciones { get; set; }
        public string? DocumentoFirmado { get; set; }  // El JSON firmado (JWS)
    }

    public class RecepcionLoteResponseDto
    {
        public int Version { get; set; }
        public string Ambiente { get; set; } = string.Empty;
        public int VersionApp { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string IdEnvio { get; set; } = string.Empty;
        public string FhProcesamiento { get; set; } = string.Empty;
        public string CodigoLote { get; set; } = string.Empty;
        public string CodigoMsg { get; set; } = string.Empty;
        public string DescripcionMsg { get; set; } = string.Empty;
    }

    public class ContingenciaResponseDto
    {
        public string Estado { get; set; } = string.Empty; // "RECIBIDO", "RECHAZADO"
        public string FechaHora { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string? SelloRecibido { get; set; }
        public List<string>? Observaciones { get; set; }
    }

    public class OperacionesEspecialesResponseDto
    {
        public string Estado { get; set; } = string.Empty; // "PROCESADO"/"RECIBIDO", "RECHAZADO"
        public string FechaHora { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string? SelloRecibido { get; set; }
        public List<string>? Observaciones { get; set; }
    }

    public class RetornoResponseDto
    {
        public string Estado { get; set; } = string.Empty; // "PROCESADO"/"RECIBIDO", "RECHAZADO"
        public string FechaHora { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string? SelloRecibido { get; set; }
        public List<string>? Observaciones { get; set; }
    }



    public class ConsultaEstadoDteResponseDto
    {
        public int Version { get; set; }
        public string Ambiente { get; set; } = string.Empty;
        public int VersionApp { get; set; }
        public string Estado { get; set; } = string.Empty; // "PROCESADO", "RECHAZADO", "NO EXISTE"
        public string CodigoGeneracion { get; set; } = string.Empty;
        public string? SelloRecibido { get; set; }
        public string FhProcesamiento { get; set; } = string.Empty;
        public string CodigoMsg { get; set; } = string.Empty;
        public string DescripcionMsg { get; set; } = string.Empty;
        public List<string>? Observaciones { get; set; }
    }
}
