using FraFactu.Application.DTOs.Facturas;
using FraFactu.Application.Interfaces;

namespace FraFactu.Application.Services;

/// <summary>
/// Construye el DTE de cada cuota: intermedias por su monto (una línea),
/// final por el total con descuento global de lo ya facturado.
/// Cada cuota se emite como Contado (1) con su forma de pago.
/// <para>
/// <b>Limitación MVP:</b> solo admite ventas 100% gravadas. Si la venta original
/// contiene ítems con VentaExenta &gt; 0 o VentaNoSuj &gt; 0, se lanza
/// <see cref="NotSupportedException"/>.
/// </para>
/// </summary>
public class FacturacionCuotaStrategy : IFacturacionCuotaStrategy
{
    private const decimal TasaIva = 0.13m;

    public CreateFacturaElectronicaDto ConstruirDteCuota(
        CreateFacturaElectronicaDto ventaOriginal,
        decimal montoCuota,
        int numeroCuota,
        int totalCuotas,
        decimal montoNetoFacturadoPrevio,
        int catFormaPagoId,
        string? referenciaPago,
        bool esCuotaFinal,
        decimal interesMora = 0m)
    {
        if (ventaOriginal.CuerpoDocumento.Any(i => i.VentaExenta > 0 || i.VentaNoSuj > 0))
            throw new NotSupportedException(
                "El módulo de ventas a crédito por cuotas solo admite ventas 100% gravadas en esta versión.");

        // Usar el flag explícito en vez de numeroCuota == totalCuotas:
        // tras un refinanciamiento la numeración es continua y ese cálculo produce falsos positivos/negativos.
        return esCuotaFinal
            ? ConstruirCuotaFinal(ventaOriginal, montoCuota, montoNetoFacturadoPrevio, catFormaPagoId, referenciaPago, interesMora)
            : ConstruirCuotaIntermedia(ventaOriginal, montoCuota, numeroCuota, totalCuotas, catFormaPagoId, referenciaPago, interesMora);
    }

    private CreateFacturaElectronicaDto ConstruirCuotaIntermedia(
        CreateFacturaElectronicaDto venta, decimal montoCuota, int numero, int total,
        int catFormaPagoId, string? referencia, decimal interesMora)
    {
        decimal baseGravada = decimal.Round(montoCuota / (1 + TasaIva), 2);
        decimal iva = montoCuota - baseGravada;

        // UniMedida es el Id del catálogo cat_uni_medida (FK), NO el código MH. Reutilizamos
        // la unidad del primer ítem de la venta (un Id válido ya resuelto por el frontend) para
        // las líneas sintéticas de la cuota; usar un literal como 99 viola la FK CatUnidadMedidaId.
        int uniMedidaId = UnidadDeVenta(venta);

        var dte = ClonarCabecera(venta);
        dte.CuerpoDocumento = new List<ItemDocumentoDto>
        {
            new ItemDocumentoDto
            {
                NumItem = 1,
                TipoItem = 2, // Servicio (pago a cuenta)
                Cantidad = 1,
                UniMedida = uniMedidaId,
                Descripcion = $"Cuota {numero}/{total} - venta a credito",
                PrecioUni = baseGravada,
                VentaGravada = baseGravada,
                IvaItem = iva
            }
        };

        AgregarLineaMoraYResumen(dte, baseGravada, 0m, montoCuota, interesMora, catFormaPagoId, referencia, uniMedidaId);
        return dte;
    }

    private CreateFacturaElectronicaDto ConstruirCuotaFinal(
        CreateFacturaElectronicaDto venta, decimal montoCuota, decimal netoPrevio,
        int catFormaPagoId, string? referencia, decimal interesMora)
    {
        var dte = ClonarCabecera(venta);
        // Detalle completo de la venta original (copia de ítems)
        dte.CuerpoDocumento = venta.CuerpoDocumento
            .Select(i => ClonarItem(i))
            .ToList();

        decimal gravadaBruta = dte.CuerpoDocumento.Sum(i => i.VentaGravada);
        // Se deriva la base neta desde el monto de la cuota para garantizar cuadre exacto.
        // El IVA absorbe el residuo de centavo: gravadaNeta + iva == montoCuota siempre.
        decimal gravadaNeta = decimal.Round(montoCuota / (1 + TasaIva), 2);
        // El descuento lleva de la bruta total a la neta de esta cuota final.
        decimal descuento = decimal.Round(gravadaBruta - gravadaNeta, 2);

        AgregarLineaMoraYResumen(dte, gravadaNeta, descuento, montoCuota, interesMora, catFormaPagoId, referencia, UnidadDeVenta(venta));
        return dte;
    }

    // Id de catálogo cat_uni_medida a usar en las líneas sintéticas (cuota/mora): se toma del
    // primer ítem de la venta, que el frontend ya resolvió a un Id válido de la FK.
    private static int UnidadDeVenta(CreateFacturaElectronicaDto venta) =>
        venta.CuerpoDocumento[0].UniMedida;

    private static void AgregarLineaMoraYResumen(
        CreateFacturaElectronicaDto dte, decimal gravadaNetaBase, decimal descuento,
        decimal montoCuota, decimal interesMora, int catFormaPagoId, string? referencia, int uniMedidaId)
    {
        decimal gravadaTotal = gravadaNetaBase;
        decimal totalPagar = montoCuota;

        if (interesMora > 0)
        {
            decimal baseMora = decimal.Round(interesMora / (1 + TasaIva), 2);
            decimal ivaMora = interesMora - baseMora;
            int numItem = dte.CuerpoDocumento.Count + 1;
            dte.CuerpoDocumento.Add(new ItemDocumentoDto
            {
                NumItem = numItem,
                TipoItem = 2,
                Cantidad = 1,
                UniMedida = uniMedidaId,
                Descripcion = "Interés por mora",
                PrecioUni = baseMora,
                VentaGravada = baseMora,
                IvaItem = ivaMora
            });
            gravadaTotal = decimal.Round(gravadaNetaBase + baseMora, 2);
            totalPagar = montoCuota + interesMora;
        }

        // IVA exacto: total - base (absorbe el residuo de redondeo)
        decimal ivaTotal = decimal.Round(totalPagar - gravadaTotal, 2);

        dte.Resumen = new ResumenDto
        {
            CondicionOperacion = 1, // Contado
            TotalGravada = gravadaTotal,
            DescuGravada = descuento,
            TotalDescu = descuento,
            TotalIva = ivaTotal,
            SubTotal = gravadaTotal,
            TotalPagar = totalPagar,
            Pagos = new List<PagoDto>
            {
                new PagoDto { CatFormaPagoId = catFormaPagoId, Monto = totalPagar, Referencia = referencia }
            }
        };
    }

    private static CreateFacturaElectronicaDto ClonarCabecera(CreateFacturaElectronicaDto v) => new()
    {
        Identificacion = v.Identificacion,
        ReceptorId = v.ReceptorId,
        Receptor = v.Receptor,
        SucursalId = v.SucursalId,
        CajaId = v.CajaId,
        VendedorId = v.VendedorId,
        Extension = v.Extension,
        Observaciones = v.Observaciones
    };

    private static ItemDocumentoDto ClonarItem(ItemDocumentoDto i) => new()
    {
        NumItem = i.NumItem, TipoItem = i.TipoItem, NumeroDocumento = i.NumeroDocumento,
        Cantidad = i.Cantidad, Codigo = i.Codigo, CodTributo = i.CodTributo,
        UniMedida = i.UniMedida, Descripcion = i.Descripcion, PrecioUni = i.PrecioUni,
        MontoDescuento = i.MontoDescuento, VentaGravada = i.VentaGravada,
        VentaExenta = i.VentaExenta, VentaNoSuj = i.VentaNoSuj, IvaItem = i.IvaItem,
        ProductoId = i.ProductoId, BodegaId = i.BodegaId, PrecioIncluyeIva = i.PrecioIncluyeIva
    };
}
