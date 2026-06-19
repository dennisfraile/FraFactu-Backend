using FraFactu.Application.Interfaces.Repositories;
using FraFactu.Infrastructure.Persistence;

namespace FraFactu.Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio de catálogos
/// </summary>
public class CatalogRepository : ICatalogRepository
{
    private readonly ApplicationDbContext _context;

    public CatalogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public string GetFormaPagoCodigo(int id)
    {
        var catalog = _context.CatFormasPago.Find(id);
        return catalog?.Codigo ?? throw new InvalidOperationException(
            $"No se encontró CatFormaPago con Id={id}");
    }

    public string? GetPlazoCodigo(int? id)
    {
        if (!id.HasValue) return null;

        var catalog = _context.CatPlazos.Find(id.Value);
        return catalog?.Codigo;
    }

    public string GetCondicionOperacionCodigo(int id)
    {
        var catalog = _context.CatCondicionesOperacion.Find(id);
        return catalog?.Codigo ?? throw new InvalidOperationException(
            $"No se encontró CatCondicionOperacion con Id={id}");
    }

    public int GetFormaPagoId(string codigo)
    {
        var catalog = _context.CatFormasPago.FirstOrDefault(c => c.Codigo == codigo);
        return catalog?.Id ?? throw new InvalidOperationException(
            $"No se encontró CatFormaPago con Codigo='{codigo}'");
    }

    public int? GetPlazoId(string? codigo)
    {
        if (string.IsNullOrEmpty(codigo)) return null;

        var catalog = _context.CatPlazos.FirstOrDefault(c => c.Codigo == codigo);
        return catalog?.Id;
    }

    public int GetCondicionOperacionId(string codigo)
    {
        var catalog = _context.CatCondicionesOperacion.FirstOrDefault(c => c.Codigo == codigo);
        return catalog?.Id ?? throw new InvalidOperationException(
            $"No se encontró CatCondicionOperacion con Codigo='{codigo}'");
    }
}
