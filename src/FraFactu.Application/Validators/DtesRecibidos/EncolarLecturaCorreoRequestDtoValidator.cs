using System.Globalization;
using FluentValidation;
using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs.DtesRecibidos;
using Microsoft.Extensions.Options;

namespace FraFactu.Application.Validators.DtesRecibidos;

/// <summary>
/// F4: valida el body opcional de <c>POST /leer-correo</c>. Reglas:
/// - Ambos meses se mandan juntos o ninguno (omitir = mes actual).
/// - Formato estricto <c>YYYY-MM</c> (sin ambiguedad de cultura).
/// - <c>MesFin</c> no puede ser anterior a <c>MesInicio</c>.
/// - El rango no puede superar <c>DtesRecibidos:LimiteMesesManual</c>
///   (default 12, configurable por entorno).
/// </summary>
public class EncolarLecturaCorreoRequestDtoValidator : AbstractValidator<EncolarLecturaCorreoRequestDto>
{
    private readonly int _limiteMeses;

    public EncolarLecturaCorreoRequestDtoValidator(IOptions<DtesRecibidosSettings> settings)
    {
        _limiteMeses = settings.Value.LimiteMesesManual;

        RuleFor(x => x).Custom((dto, ctx) =>
        {
            var sinDatos = string.IsNullOrWhiteSpace(dto.MesInicio)
                && string.IsNullOrWhiteSpace(dto.MesFin);
            if (sinDatos) return; // default = mes actual (lo resuelve el servicio).

            if (string.IsNullOrWhiteSpace(dto.MesInicio) || string.IsNullOrWhiteSpace(dto.MesFin))
            {
                ctx.AddFailure("Debe especificar mesInicio y mesFin juntos, o ninguno (mes actual).");
                return;
            }

            if (!TryParseMes(dto.MesInicio!, out var inicio))
            {
                ctx.AddFailure($"mesInicio debe tener formato YYYY-MM (recibido: {dto.MesInicio}).");
                return;
            }

            if (!TryParseMes(dto.MesFin!, out var fin))
            {
                ctx.AddFailure($"mesFin debe tener formato YYYY-MM (recibido: {dto.MesFin}).");
                return;
            }

            if (fin < inicio)
            {
                ctx.AddFailure("mesFin no puede ser anterior a mesInicio.");
                return;
            }

            var meses = ((fin.Year - inicio.Year) * 12) + (fin.Month - inicio.Month) + 1;
            if (meses > _limiteMeses)
                ctx.AddFailure($"El rango no puede superar {_limiteMeses} meses (recibidos {meses}).");
        });
    }

    /// <summary>
    /// Convierte el DTO ya validado a (RangoDesde, RangoHasta) UTC.
    /// Si los meses vienen vacios, devuelve (null, null) y el servicio aplica
    /// el default de mes en curso. <c>RangoHasta</c> es exclusivo: primer dia
    /// del mes posterior al <c>MesFin</c> ingresado.
    /// </summary>
    public static (DateTime? desde, DateTime? hasta) AMesesUtc(EncolarLecturaCorreoRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.MesInicio) || string.IsNullOrWhiteSpace(dto.MesFin))
            return (null, null);

        TryParseMes(dto.MesInicio!, out var inicio);
        TryParseMes(dto.MesFin!, out var fin);
        return (inicio, fin.AddMonths(1));
    }

    private static bool TryParseMes(string valor, out DateTime mesUtc)
    {
        if (DateTime.TryParseExact(valor, "yyyy-MM",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            mesUtc = new DateTime(parsed.Year, parsed.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            return true;
        }
        mesUtc = default;
        return false;
    }
}
