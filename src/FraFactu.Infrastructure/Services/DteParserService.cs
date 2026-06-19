using System.Text;
using System.Text.Json;
using FraFactu.Application.DTOs.DtesRecibidos;
using FraFactu.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Services;

/// <summary>
/// Servicio para parsear DTEs desde JSON o JWT
/// </summary>
public class DteParserService : IDteParserService
{
    private readonly ILogger<DteParserService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DteParserService(ILogger<DteParserService> logger)
    {
        _logger = logger;
    }

    public DteRecibidoParsedDto ParsearDteJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("El contenido JSON del DTE está vacío");

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return ExtraerDteDesdeJson(root, json);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"El contenido no es un JSON válido: {ex.Message}", ex);
        }
    }

    public DteRecibidoParsedDto ParsearDteJwt(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
            throw new ArgumentException("El contenido JWT del DTE está vacío");

        try
        {
            // JWT format: header.payload.signature
            var parts = jwt.Trim().Split('.');
            if (parts.Length != 3)
                throw new InvalidOperationException("El formato JWT no es válido (se esperan 3 partes separadas por punto)");

            // Decode the payload (second part)
            var payload = Base64UrlDecode(parts[1]);
            var payloadJson = Encoding.UTF8.GetString(payload);

            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;

            return ExtraerDteDesdeJson(root, payloadJson);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"El payload del JWT no es un JSON válido: {ex.Message}", ex);
        }
    }

    public bool EsDteValido(string contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido))
            return false;

        try
        {
            contenido = contenido.Trim();

            // Try as JSON
            if (contenido.StartsWith('{'))
            {
                using var doc = JsonDocument.Parse(contenido);
                return doc.RootElement.TryGetProperty("identificacion", out _);
            }

            // Try as JWT
            if (contenido.Contains('.'))
            {
                var parts = contenido.Split('.');
                if (parts.Length == 3)
                {
                    var payload = Base64UrlDecode(parts[1]);
                    var json = Encoding.UTF8.GetString(payload);
                    using var doc = JsonDocument.Parse(json);
                    return doc.RootElement.TryGetProperty("identificacion", out _);
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private DteRecibidoParsedDto ExtraerDteDesdeJson(JsonElement root, string jsonOriginal)
    {
        // Validar secciones requeridas
        if (!root.TryGetProperty("identificacion", out var identificacion))
            throw new InvalidOperationException("El DTE no contiene la sección 'identificacion'");

        if (!root.TryGetProperty("emisor", out var emisor))
            throw new InvalidOperationException("El DTE no contiene la sección 'emisor'");

        var result = new DteRecibidoParsedDto
        {
            JsonOriginal = jsonOriginal,
            CodigoGeneracion = GetStringProperty(identificacion, "codigoGeneracion"),
            TipoDte = GetStringProperty(identificacion, "tipoDte"),
            NumeroControl = GetStringPropertyOrNull(identificacion, "numeroControl"),
            FechaEmision = ParseFechaEmision(identificacion),
            EmisorNit = GetStringProperty(emisor, "nit"),
            EmisorNombre = GetStringProperty(emisor, "nombre"),
            EmisorNrc = GetStringPropertyOrNull(emisor, "nrc"),
        };

        // Receptor (opcional en algunos tipos)
        if (root.TryGetProperty("receptor", out var receptor))
        {
            result.ReceptorNit = GetStringPropertyOrNull(receptor, "nit");
            result.ReceptorNombre = GetStringPropertyOrNull(receptor, "nombre");
        }

        // Resumen
        if (root.TryGetProperty("resumen", out var resumen))
        {
            result.MontoGravado = GetDecimalProperty(resumen, "totalGravada");
            result.MontoExento = GetDecimalProperty(resumen, "totalExenta");
            result.MontoNoSujeto = GetDecimalProperty(resumen, "totalNoSuj");
            result.SubTotal = GetDecimalProperty(resumen, "subTotal", GetDecimalProperty(resumen, "subTotalVentas"));
            result.Total = GetDecimalProperty(resumen, "totalPagar", GetDecimalProperty(resumen, "montoTotalOperacion"));

            // IVA desde tributos
            if (resumen.TryGetProperty("tributos", out var tributos) && tributos.ValueKind == JsonValueKind.Array)
            {
                foreach (var tributo in tributos.EnumerateArray())
                {
                    var codigo = GetStringPropertyOrNull(tributo, "codigo");
                    if (codigo == "20") // IVA
                    {
                        result.IVA = GetDecimalProperty(tributo, "valor");
                        break;
                    }
                }
            }

            // Si no hay tributos, calcular IVA como diferencia
            if (result.IVA == 0 && result.MontoGravado > 0)
            {
                result.IVA = Math.Round(result.MontoGravado * 0.13m, 2);
            }
        }

        // Cuerpo del documento (items)
        if (root.TryGetProperty("cuerpoDocumento", out var cuerpo) && cuerpo.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in cuerpo.EnumerateArray())
            {
                result.Items.Add(new DteItemParsedDto
                {
                    NumItem = GetIntProperty(item, "numItem"),
                    Codigo = GetStringPropertyOrNull(item, "codigo"),
                    Descripcion = GetStringProperty(item, "descripcion", "Sin descripción"),
                    Cantidad = GetDecimalProperty(item, "cantidad", 1),
                    PrecioUnitario = GetDecimalProperty(item, "precioUni"),
                    MontoDescuento = GetDecimalProperty(item, "montoDescu"),
                    VentaGravada = GetDecimalProperty(item, "ventaGravada"),
                    VentaExenta = GetDecimalProperty(item, "ventaExenta"),
                    VentaNoSujeta = GetDecimalProperty(item, "ventaNoSuj"),
                    UnidadMedida = GetIntPropertyOrNull(item, "uniMedida"),
                });
            }
        }

        return result;
    }

    #region Helpers

    private static string GetStringProperty(JsonElement element, string name, string defaultValue = "")
    {
        if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString() ?? defaultValue;
        return defaultValue;
    }

    private static string? GetStringPropertyOrNull(JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }

    private static decimal GetDecimalProperty(JsonElement element, string name, decimal defaultValue = 0)
    {
        if (!element.TryGetProperty(name, out var prop))
            return defaultValue;

        return prop.ValueKind switch
        {
            JsonValueKind.Number => prop.GetDecimal(),
            JsonValueKind.String when decimal.TryParse(prop.GetString(), out var val) => val,
            _ => defaultValue
        };
    }

    private static int GetIntProperty(JsonElement element, string name, int defaultValue = 0)
    {
        if (!element.TryGetProperty(name, out var prop))
            return defaultValue;

        return prop.ValueKind switch
        {
            JsonValueKind.Number => prop.GetInt32(),
            JsonValueKind.String when int.TryParse(prop.GetString(), out var val) => val,
            _ => defaultValue
        };
    }

    private static int? GetIntPropertyOrNull(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var prop))
            return null;

        return prop.ValueKind switch
        {
            JsonValueKind.Number => prop.GetInt32(),
            JsonValueKind.String when int.TryParse(prop.GetString(), out var val) => val,
            _ => null
        };
    }

    private static DateTime ParseFechaEmision(JsonElement identificacion)
    {
        var fecha = GetStringPropertyOrNull(identificacion, "fecEmi");
        if (fecha != null && DateTime.TryParse(fecha, out var dt))
            return dt;
        return DateTime.UtcNow;
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var output = input.Replace('-', '+').Replace('_', '/');
        switch (output.Length % 4)
        {
            case 2: output += "=="; break;
            case 3: output += "="; break;
        }
        return Convert.FromBase64String(output);
    }

    #endregion
}
