using FraFactu.Domain.Entities.Catalogos;

namespace FraFactu.Application.Common.Interfaces;

public interface ICatalogoService
{
    // Usamos Generics <T> para no escribir 20 métodos iguales
    Task<List<T>> GetCatalogoAsync<T>() where T : CatalogoBase;
}