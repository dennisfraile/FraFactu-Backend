using FraFactu.Application.DTOs.DtesRecibidos;

namespace FraFactu.Application.Interfaces;

/// <summary>
/// Servicio para parsear DTEs desde JSON o JWT
/// </summary>
public interface IDteParserService
{
    /// <summary>
    /// Parsea un JSON de DTE y extrae la información relevante
    /// </summary>
    DteRecibidoParsedDto ParsearDteJson(string json);

    /// <summary>
    /// Parsea un DTE en formato JWT, decodifica el payload y extrae la información
    /// </summary>
    DteRecibidoParsedDto ParsearDteJwt(string jwt);

    /// <summary>
    /// Determina si un string es un DTE válido (JSON o JWT)
    /// </summary>
    bool EsDteValido(string contenido);
}
