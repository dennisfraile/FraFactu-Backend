using Microsoft.EntityFrameworkCore;

namespace FraFactu.Infrastructure.Helpers.Pagination
{
    public static class PaginationExtensions
    {
        public static Task<PaginatedList<T>> ToPaginatedListAsync<T>(this IQueryable<T> source, int pageNumber, int pageSize)
        {
            return PaginatedList<T>.CreateAsync(source, pageNumber, pageSize);
        }
    }
}
