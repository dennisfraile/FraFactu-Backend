namespace FraFactu.Application.DTOs.Facturas
{
    /// <summary>
    /// DTO para Otros Documentos Asociados (específico de CCF)
    /// </summary>
    public class OtroDocumentoDto
    {
        /// <summary>
        /// Documento asociado
        /// Valores: 1-4
        /// 1 = Otro
        /// 2 = Otro  
        /// 3 = Médico (requiere campo Medico)
        /// 4 = Otro
        /// </summary>
        public int CodDocAsociado { get; set; }

        /// <summary>
        /// Identificación del documento asociado
        /// MaxLength: 100
        /// Requerido si CodDocAsociado != 3
        /// </summary>
        public string? DescDocumento { get; set; }

        /// <summary>
        /// Descripción de documento asociado
        /// MaxLength: 300
        /// Requerido si CodDocAsociado != 3
        /// </summary>
        public string? DetalleDocumento { get; set; }

        /// <summary>
        /// Información del médico
        /// Requerido si CodDocAsociado = 3
        /// </summary>
        public MedicoDto? Medico { get; set; }
    }
}
