using FraFactu.Domain.Entities;

namespace FraFactu.Application.Interfaces;

public interface IVendedorService
{
    Task<List<Vendedor>> GetAllAsync(int emisorId, bool activeOnly = true, int? sucursalId = null, List<int>? sucursalIds = null);
    Task<Vendedor?> GetByIdAsync(int id);
    Task<Vendedor> CreateAsync(Vendedor vendedor, bool accesoTodasSucursales, List<int> sucursalIds);
    Task<Vendedor> UpdateAsync(int id, Vendedor vendedor, bool accesoTodasSucursales, List<int> sucursalIds);
    Task ToggleActiveAsync(int id);
}
