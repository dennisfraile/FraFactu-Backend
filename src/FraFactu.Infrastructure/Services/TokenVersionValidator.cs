using FraFactu.Application.Interfaces;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FraFactu.Infrastructure.Services
{
    /// <inheritdoc cref="ITokenVersionValidator" />
    public class TokenVersionValidator : ITokenVersionValidator
    {
        private readonly ApplicationDbContext _context;

        public TokenVersionValidator(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsCurrentAsync(int usuarioId, int? tokenVersionClaim)
        {
            // Fail-closed: sin claim no se puede comprobar revocación → se rechaza.
            if (tokenVersionClaim is null)
                return false;

            var tokenVersionActual = await _context.Usuarios
                .Where(u => u.Id == usuarioId)
                .Select(u => (int?)u.TokenVersion)
                .FirstOrDefaultAsync();

            // Usuario inexistente → se rechaza.
            if (tokenVersionActual is null)
                return false;

            return tokenVersionActual.Value == tokenVersionClaim.Value;
        }
    }
}
