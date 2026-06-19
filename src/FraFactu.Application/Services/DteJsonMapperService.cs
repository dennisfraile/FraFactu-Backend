using FraFactu.Application.DTOs.Hacienda;
using FraFactu.Application.Interfaces.Repositories;

namespace FraFactu.Application.Services;

/// <summary>
/// Servicio para mapear DTOs internos a formato JSON compatible con API del MH
/// Convierte FKs int a códigos string de catálogos
/// </summary>
public class DteJsonMapperService
{
    private readonly ICatalogRepository _catalogRepo;

    public DteJsonMapperService(ICatalogRepository catalogRepo)
    {
        _catalogRepo = catalogRepo;
    }

    /// <summary>
    /// Mapea DteBaseDto a un objeto anónimo listo para serializar a JSON del MH
    /// </summary>
    public object MapToMhJson(DteBaseDto dto)
    {
        return new
        {
            identificacion = dto.Identificacion,
            documentoRelacionado = dto.DocumentoRelacionado,
            emisor = dto.Emisor,
            receptor = dto.Receptor,
            otrosDocumentos = dto.OtrosDocumentos,
            ventaTercero = dto.VentaTercero,
            cuerpoDocumento = dto.CuerpoDocumento,
            resumen = MapResumenToMhJson(dto.Resumen),
            extension = dto.Extension,
            apendice = dto.Apendice
        };
    }

    /// <summary>
    /// Mapea ResumenDto convirtiendo FKs a códigos de catálogo
    /// </summary>
    private object MapResumenToMhJson(ResumenDto resumen)
    {
        return new
        {
            totalNoSuj = resumen.TotalNoSuj,
            totalExenta = resumen.TotalExenta,
            totalGravada = resumen.TotalGravada,
            subTotalVentas = resumen.SubTotalVentas,
            descuNoSuj = resumen.DescuNoSuj,
            descuExenta = resumen.DescuExenta,
            descuGravada = resumen.DescuGravada,
            porcentajeDescuento = resumen.PorcentajeDescuento,
            totalDescu = resumen.TotalDescu,
            tributos = resumen.Tributos,
            subTotal = resumen.SubTotal,
            ivaPerci1 = resumen.IvaPerci1,
            ivaRete1 = resumen.IvaRete1,
            reteRenta = resumen.ReteRenta,
            montoTotalOperacion = resumen.MontoTotalOperacion,
            totalNoGravado = resumen.TotalNoGravado,
            totalPagar = resumen.TotalPagar,
            totalLetras = resumen.TotalLetras,
            saldoFavor = resumen.SaldoFavor,
            // FK converted to string
            condicionOperacion = _catalogRepo.GetCondicionOperacionCodigo(resumen.CondicionOperacion),
            pagos = resumen.Pagos?.Select(MapPagoToMhJson).ToList(),
            // Custom field - not sent to MH
            // numPagoElectronico = resumen.NumPagoElectronico
        };
    }

    /// <summary>
    /// Mapea PagoDto convirtiendo FK plazo a código de catálogo
    /// Codigo ya viene en formato string "01", "02", etc. del DTO
    /// </summary>
    private object MapPagoToMhJson(PagoDto pago)
    {
        return new
        {
            // El código ya viene formateado como string "01", "02", etc.
            codigo = pago.Codigo,
            montoPago = pago.MontoPago,
            referencia = pago.Referencia,
            // FK converted to string (nullable)
            plazo = _catalogRepo.GetPlazoCodigo(pago.Plazo),
            periodo = pago.Periodo
        };
    }
}
