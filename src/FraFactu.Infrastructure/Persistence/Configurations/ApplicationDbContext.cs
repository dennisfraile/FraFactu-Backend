using FraFactu.Domain.Entities; // Para Emisor, Usuario, Rol...
using FraFactu.Domain.Entities.Catalogos; // Para todos los catálogos
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace FraFactu.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // --- TABLAS DE SEGURIDAD ---
    public DbSet<Emisor> Emisores { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Rol> Roles { get; set; }
    public DbSet<Permiso> Permisos { get; set; }

    // --- TABLAS DE CATÁLOGOS (Los que ya configuramos) ---
    public DbSet<CatAmbienteDestino> CatAmbientes { get; set; }
    public DbSet<CatTipoDocumento> CatTiposDocumento { get; set; }
    public DbSet<CatModeloFacturacion> CatModelosFacturacion { get; set; }
    public DbSet<CatTipoTransmision> CatTiposTransmision { get; set; }
    public DbSet<CatTipoContingencia> CatTiposContingencia { get; set; }
    public DbSet<CatTipoGeneracionDocumento> CatTiposGeneracion { get; set; }
    public DbSet<CatTipoEstablecimiento> CatTiposEstablecimiento { get; set; }
    public DbSet<CatCodigoTipoServicio> CatTiposServicio { get; set; }
    public DbSet<CatTipoItem> CatTiposItem { get; set; }
    public DbSet<CatDepartamento> CatDepartamentos { get; set; }
    public DbSet<CatMunicipio> CatMunicipios { get; set; }
    public DbSet<CatDistrito> CatDistritos { get; set; }
    public DbSet<CatUnidadMedida> CatUnidadesMedida { get; set; }
    public DbSet<CatCondicionOperacion> CatCondicionesOperacion { get; set; }
    public DbSet<CatFormaPago> CatFormasPago { get; set; }
    public DbSet<CatPlazo> CatPlazos { get; set; }
    public DbSet<CatOtrosDocumentosAsociados> CatOtrosDocumentos { get; set; }
    public DbSet<CatTipoDocumentoIdentificacionReceptor> CatDocsIdentidadReceptor { get; set; }
    public DbSet<CatTipoDocumentoContingencia> CatDocsContingencia { get; set; }
    public DbSet<CatTipoInvalidacion> CatTiposInvalidacion { get; set; }
    public DbSet<CatTributo> CatTributos { get; set; }
    public DbSet<CatActividadEconomica> CatActividadesEconomicas { get; set; }
    public DbSet<CatMoneda> CatMonedas { get; set; } // NUEVO

    // --- TABLAS DE NEGOCIO ---
    public DbSet<Receptor> Receptores { get; set; }
    public DbSet<ProductoServicio> ProductosServicios { get; set; } // RENOMBRADO
    public DbSet<Sucursal> Sucursales { get; set; }

    // --- TABLAS DE FACTURACIÓN ELECTRÓNICA ---
    public DbSet<FacturaElectronica> Facturas { get; set; }
    public DbSet<Invalidacion> Invalidaciones { get; set; }
    public DbSet<EventoContingencia> EventosContingencia { get; set; }
    public DbSet<ContingenciaDetalle> ContingenciaDetalles { get; set; }
    public DbSet<EventoOperacionEspecial> EventosOperacionEspecial { get; set; }
    public DbSet<OperacionEspecialDetalle> OperacionEspecialDetalles { get; set; }
    public DbSet<EventoRetorno> EventosRetorno { get; set; }
    public DbSet<RetornoDetalle> RetornoDetalles { get; set; }

    // Lotes de envío
    public DbSet<Lote> Lotes { get; set; }
    public DbSet<LoteDetalle> LoteDetalles { get; set; }

    // Configuración de envío automático
    public DbSet<ConfiguracionEnvioLote> ConfiguracionEnvioLotes { get; set; }

    // Inventario
    public DbSet<Categoria> Categorias { get; set; }
    public DbSet<Marca> Marcas { get; set; }
    public DbSet<Bodega> Bodegas { get; set; }
    public DbSet<Caja> Cajas { get; set; }

    public DbSet<StockBodega> StocksBodega { get; set; }
    public DbSet<MovimientoInventario> MovimientosInventario { get; set; }

    // Compras y Gastos
    public DbSet<CatTipoGasto> CatTiposGasto { get; set; }
    public DbSet<Proveedor> Proveedores { get; set; }
    public DbSet<CompraExterna> ComprasExternas { get; set; }
    public DbSet<CompraExternaDetalle> CompraExternaDetalles { get; set; }
    public DbSet<GastoAdministrativo> GastosAdministrativos { get; set; }

    // DTEs Recibidos
    public DbSet<DteRecibido> DtesRecibidos { get; set; }
    public DbSet<LecturaCorreoJob> LecturaCorreoJobs { get; set; }

    // F3 (Plan inventario desde DTE): outbox que replica movimientos a SmartInventory
    public DbSet<IntegracionInventarioPendiente> IntegracionInventarioPendientes { get; set; }

    // F4 (Plan inventario desde DTE): registro de divergencias detectadas por el job de reconciliacion
    public DbSet<DivergenciaInventario> DivergenciasInventario { get; set; }

    // Vendedores y Rastreo
    public DbSet<Vendedor> Vendedores { get; set; }
    public DbSet<VendedorSucursal> VendedoresSucursales { get; set; }
    public DbSet<ProductoServicioSucursal> ProductosServiciosSucursales { get; set; }
    public DbSet<ProductoServicioTributo> ProductosServiciosTributos { get; set; }
    public DbSet<HistorialUsuarioCaja> HistorialUsuariosCajas { get; set; }
    public DbSet<HistorialUsuarioSucursal> HistorialUsuariosSucursales { get; set; }
    public DbSet<UsuarioSucursal> UsuariosSucursales { get; set; }
    public DbSet<UsuarioCaja> UsuariosCajas { get; set; }


    // ==========================================
    public DbSet<FacturaElectronicaDetalle> FacturaDetalles { get; set; }
    public DbSet<Pago> FacturaPagos { get; set; }

    // --- VENTAS A CRÉDITO POR CUOTAS ---
    public DbSet<PlanCuotas> PlanesCuotas { get; set; }
    public DbSet<Cuota> Cuotas { get; set; }
    public DbSet<ConfiguracionCuotas> ConfiguracionesCuotas { get; set; }

    // --- NOTIFICACIONES IN-APP ---
    public DbSet<Notificacion> Notificaciones { get; set; }
    public DbSet<NotificacionLeida> NotificacionesLeidas { get; set; }
    public DbSet<HistorialRefinanciamiento> HistorialesRefinanciamiento { get; set; }
    public DbSet<FacturaTributo> FacturaTributos { get; set; }
    public DbSet<FacturaDocumentoRelacionado> FacturaDocumentosRelacionados { get; set; }
    public DbSet<FacturaApendice> FacturaApendices { get; set; }
    public DbSet<FacturaExtencion> FacturaExtenciones { get; set; }
    public DbSet<RolPermiso> RolPermisos { get; set; } // Added for RolPermiso

    // Saldos DTE para Notas de Crédito
    public DbSet<SaldoDte> SaldosDte { get; set; }

    // Suscripciones
    public DbSet<Suscripcion> Suscripciones { get; set; }
    public DbSet<ConfiguracionProveedor> ConfiguracionProveedor { get; set; }
    public DbSet<FacturaSuscripcion> FacturasSuscripcion { get; set; }

    // Correlativos de migración desde sistemas externos
    public DbSet<CorrelativoInicial> CorrelativosIniciales { get; set; }

    // Integración SmartHub
    public DbSet<MigracionInventarioProcesada> MigracionesInventarioProcesadas { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // ESTA LÍNEA ES MÁGICA:
        // Busca automáticamente todos los archivos "Config.cs" que creamos hoy
        // y aplica sus reglas y sus datos de carga (Seed Data).
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(builder);
    }
}