namespace FraFactu.Application.Interfaces;

/// <summary>
/// Servicio para generar reportes de compras (Libro de Compras, Resumen, Cruce DTEs)
/// </summary>
public interface IReporteComprasService
{
    /// <summary>
    /// Genera el Libro de Compras en formato Excel (requerido por Hacienda)
    /// Columnas: Fecha, Nº Doc, NIT proveedor, Nombre, Exento, Gravado, IVA, Total
    /// </summary>
    Task<byte[]> GenerarLibroComprasExcelAsync(int emisorId, DateTime desde, DateTime hasta);

    /// <summary>
    /// Genera el Libro de Compras en formato CSV
    /// </summary>
    Task<byte[]> GenerarLibroComprasCsvAsync(int emisorId, DateTime desde, DateTime hasta);

    /// <summary>
    /// Genera un resumen mensual de compras agrupado por proveedor
    /// </summary>
    Task<byte[]> GenerarResumenMensualExcelAsync(int emisorId, int mes, int anio);

    /// <summary>
    /// Genera detalle de compras por proveedor específico o todos
    /// </summary>
    Task<byte[]> GenerarDetallePorProveedorExcelAsync(int emisorId, DateTime desde, DateTime hasta, int? proveedorId = null);

    /// <summary>
    /// Genera cruce de compras registradas vs DTEs recibidos para identificar discrepancias
    /// </summary>
    Task<byte[]> GenerarCruceComprasVsDtesExcelAsync(int emisorId, DateTime desde, DateTime hasta);
}
