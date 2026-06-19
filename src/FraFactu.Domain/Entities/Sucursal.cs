using FraFactu.Domain.Common;
using FraFactu.Domain.Entities.Catalogos;

namespace FraFactu.Domain.Entities
{
    /// <summary>
    /// Sucursales o puntos de venta del emisor
    /// </summary>
    public class Sucursal : BaseEntity
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? CorreoElectronico { get; set; }

        // UBICACIÓN SEGÚN MINISTERIO DE HACIENDA
        /// <summary>
        /// FK al catálogo de departamentos (CAT-12)
        /// </summary>
        public int CatDepartamentoId { get; set; }
        public CatDepartamento Departamento { get; set; } = null!;

        /// <summary>
        /// FK al catálogo de municipios (CAT-13)
        /// </summary>
        public int CatMunicipioId { get; set; }
        public CatMunicipio Municipio { get; set; } = null!;

        /// <summary>
        /// FK al catálogo de distritos (CAT-008). Obligatorio en la Normativa DTE V2.0.
        /// Nullable a nivel de BD hasta poblar el catálogo y migrar las direcciones existentes.
        /// </summary>
        public int? CatDistritoId { get; set; }
        public CatDistrito? Distrito { get; set; }

        /// <summary>
        /// FK al catálogo de tipo de establecimiento (CAT-09)
        /// 01=Casa Matriz, 02=Sucursal, 04=Agencia, 07=Depósito, 20=Otro
        /// </summary>
        public int CatTipoEstablecimientoId { get; set; }
        public CatTipoEstablecimiento TipoEstablecimiento { get; set; } = null!;

        // CÓDIGOS OFICIALES MINISTERIO DE HACIENDA
        /// <summary>
        /// Código del establecimiento según MH (4 dígitos, ej: "0001")
        /// </summary>
        public string? CodigoEstablecimiento { get; set; }



        // ==========================================
        // DATOS DEL RESPONSABLE PARA CONTINGENCIA
        // Según schema de Hacienda contingencia-schema-v3
        // ==========================================

        /// <summary>
        /// Nombre del responsable del establecimiento (min 5, max 100 chars).
        /// Requerido por Hacienda para eventos de contingencia.
        /// </summary>
        public string? ContingenciaNombreResponsable { get; set; }

        /// <summary>
        /// Tipo de documento del responsable (CAT-22).
        /// Valores: "36"=NIT, "13"=DUI, "02"=Carnet Residente, "03"=Pasaporte, "37"=Otro
        /// </summary>
        public string? ContingenciaTipoDocResponsable { get; set; }

        /// <summary>
        /// Número de documento del responsable (min 5, max 25 chars).
        /// </summary>
        public string? ContingenciaNumeroDocResponsable { get; set; }

        // Relación con Emisor
        public int EmisorId { get; set; }
        public Emisor Emisor { get; set; } = null!;

        // ============================================
        // INTEGRACIÓN SMARTHUB (Plan B Hub-as-Emisor — D14)
        // ============================================

        /// <summary>
        /// ID de la Sucursal en SmartHub que corresponde a esta Sucursal de Smartix (1:1).
        /// Null si la sucursal aún no está vinculada a SmartHub. Unique cuando no es null
        /// (una Sucursal Hub se vincula a lo sumo a una Sucursal Smartix).
        /// </summary>
        public int? HubSucursalId { get; set; }

        // Facturas emitidas desde esta sucursal
        public ICollection<FacturaElectronica> Facturas { get; set; } = new List<FacturaElectronica>();
    }
}
