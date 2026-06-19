using FraFactu.Application.DTOs.Hacienda;
using FraFactu.Application.Services;
using System.Text.Json;
using System.Text;

namespace FraFactu.Application.Examples;

/// <summary>
/// Ejemplo de uso del DteJsonMapperService para serializar DTEs al formato JSON de MH
/// </summary>
public class HaciendaJsonSerializationExample
{
    private readonly DteJsonMapperService _mapper;

    public HaciendaJsonSerializationExample(DteJsonMapperService mapper)
    {
        _mapper = mapper;
    }

    /// <summary>
    /// Ejemplo: Serializar un DTE para enviar a la API de Hacienda
    /// </summary>
    public string SerializeDteForMh(DteBaseDto dte)
    {
        // 1. Convertir DTO interno a formato MH (FK int → string Codigo)
        var mhJsonObject = _mapper.MapToMhJson(dte);

        // 2. Configurar opciones de serialización JSON
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false // Para producción usar false, para debug usar true
        };

        // 3. Serializar a JSON string
        var jsonString = JsonSerializer.Serialize(mhJsonObject, jsonOptions);

        return jsonString;
    }

    /// <summary>
    /// Ejemplo: Crear HttpContent listo para enviar a MH
    /// </summary>
    public HttpContent CreateMhHttpContent(DteBaseDto dte)
    {
        var jsonString = SerializeDteForMh(dte);
        return new StringContent(jsonString, Encoding.UTF8, "application/json");
    }

    /// <summary>
    /// Ejemplo de uso en HaciendaApiService.TransmitirDte
    /// </summary>
    public async Task<string> ExampleTransmitirDte(DteBaseDto dte, HttpClient httpClient)
    {
        // Crear el contenido HTTP con JSON serializado
        var content = CreateMhHttpContent(dte);

        // Enviar a la API del MH
        var response = await httpClient.PostAsync("/dte/transmitir", content);

        // Procesar respuesta
        var responseContent = await response.Content.ReadAsStringAsync();
        return responseContent;
    }

    /// <summary>
    /// Ejemplo: Ver el JSON resultante (útil para debugging)
    /// </summary>
    public string DebugDteJson(DteBaseDto dte)
    {
        var mhJsonObject = _mapper.MapToMhJson(dte);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true // Formato legible para debug
        };

        return JsonSerializer.Serialize(mhJsonObject, jsonOptions);
    }
}

/*
 * EJEMPLO DE SALIDA JSON:
 * 
 * {
 *   "identificacion": { ... },
 *   "emisor": { ... },
 *   "receptor": { ... },
 *   "cuerpoDocumento": [ ... ],
 *   "resumen": {
 *     "totalNoSuj": 0.0,
 *     "totalGravada": 100.0,
 *     "totalPagar": 113.0,
 *     "condicionOperacion": "1",  ← Convertido de int 1 a string "1"
 *     "pagos": [
 *       {
 *         "codigo": "01",          ← Convertido de int 1 a string "01"
 *         "montoPago": 113.0,
 *         "plazo": "02",           ← Convertido de int 2 a string "02"
 *         "periodo": 30
 *       }
 *     ]
 *   }
 * }
 * 
 * NOTA: Los campos con FK (int) se convirtieron automáticamente a códigos string del catálogo.
 */
