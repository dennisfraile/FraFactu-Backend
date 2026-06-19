namespace FraFactu.Application.Interfaces.Repositories;

/// <summary>
/// Repositorio para acceso a catálogos del MH
/// </summary>
public interface ICatalogRepository
{
    /// <summary>
    /// Obtiene el código string de CatFormaPago por su ID
    /// </summary>
    string GetFormaPagoCodigo(int id);

    /// <summary>
    /// Obtiene el código string de CatPlazo por su ID (nullable)
    /// </summary>
    string? GetPlazoCodigo(int? id);

    /// <summary>
    /// Obtiene el código string de CatCondicionOperacion por su ID
    /// </summary>
    string GetCondicionOperacionCodigo(int id);

    /// <summary>
    /// Obtiene el ID de CatFormaPago por su código string
    /// </summary>
    int GetFormaPagoId(string codigo);

    /// <summary>
    /// Obtiene el ID de CatPlazo por su código string (nullable)
    /// </summary>
    int? GetPlazoId(string? codigo);

    /// <summary>
    /// Obtiene el ID de CatCondicionOperacion por su código string
    /// </summary>
    int GetCondicionOperacionId(string codigo);
}
