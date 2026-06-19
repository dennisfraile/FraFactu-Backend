using FraFactu.Application.DTOs.Facturas;

namespace FraFactu.Application.Interfaces;

/// <summary>
/// Construye el CreateFacturaElectronicaDto del DTE de una cuota concreta,
/// a partir de la venta original y el número de cuota.
/// </summary>
public interface IFacturacionCuotaStrategy
{
    /// <param name="ventaOriginal">DTO de la venta completa (snapshot).</param>
    /// <param name="montoCuota">Monto (con IVA) de la cuota a emitir.</param>
    /// <param name="numeroCuota">Número de cuota (1..N).</param>
    /// <param name="totalCuotas">Total de cuotas N.</param>
    /// <param name="montoNetoFacturadoPrevio">Suma NETA (sin IVA) facturada en cuotas previas.</param>
    /// <param name="catFormaPagoId">Forma de pago del DTE de esta cuota.</param>
    /// <param name="referenciaPago">Referencia opcional del pago.</param>
    /// <param name="esCuotaFinal">
    /// <see langword="true"/> si esta cuota es la última del plan (usa el flag <c>Cuota.EsCuotaFinal</c>
    /// de la entidad). NO derivar de <c>numeroCuota == totalCuotas</c> porque tras un
    /// refinanciamiento la numeración es continua y ese cálculo es incorrecto.
    /// </param>
    /// <param name="interesMora">Monto total de mora (IVA incluido); 0 = sin mora.</param>
    CreateFacturaElectronicaDto ConstruirDteCuota(
        CreateFacturaElectronicaDto ventaOriginal,
        decimal montoCuota,
        int numeroCuota,
        int totalCuotas,
        decimal montoNetoFacturadoPrevio,
        int catFormaPagoId,
        string? referenciaPago,
        bool esCuotaFinal,
        decimal interesMora = 0m);
}
