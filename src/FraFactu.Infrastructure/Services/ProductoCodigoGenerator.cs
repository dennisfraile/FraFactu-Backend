using System.Text.RegularExpressions;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FraFactu.Infrastructure.Services;

/// <summary>
/// Genera el siguiente codigo correlativo de ProductoServicio por emisor:
/// <c>PROD-00001</c> para bienes/productos, <c>SERV-00001</c> para servicios
/// (<c>CatTipoItemId == 2</c>). Logica compartida entre la creacion normal
/// (<see cref="ProductoServicioService"/>) y el wizard de mapeo de DTEs recibidos
/// (<c>DteRecibidoService.MapearYCrearCompraAsync</c>) para que ambos flujos
/// autogeneren el codigo de la misma forma.
/// </summary>
public static class ProductoCodigoGenerator
{
    /// <summary>
    /// Devuelve el siguiente codigo libre para el emisor segun el tipo de item.
    /// Si el ultimo codigo del prefijo no es numerico (SKU manual), cae al conteo
    /// total + 1. El llamador debe verificar colisiones si guarda varios sin
    /// <c>SaveChanges</c> intermedio.
    /// </summary>
    public static async Task<string> GenerarSiguienteCodigoAsync(
        ApplicationDbContext context, int emisorId, int catTipoItemId, CancellationToken ct = default)
    {
        // 2 = Servicio; cualquier otro tipo se trata como Producto/Bien.
        var prefix = catTipoItemId == 2 ? "SERV" : "PROD";

        var lastInEmisor = await context.ProductosServicios
            .Where(p => p.EmisorId == emisorId && p.Codigo.StartsWith(prefix + "-"))
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync(ct);

        int nextNumber = 1;
        if (lastInEmisor != null)
        {
            var match = Regex.Match(lastInEmisor.Codigo, @$"{prefix}-(\d+)$");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
            else
            {
                var total = await context.ProductosServicios
                    .Where(p => p.EmisorId == emisorId && p.Codigo.StartsWith(prefix + "-"))
                    .CountAsync(ct);
                nextNumber = total + 1;
            }
        }

        return $"{prefix}-{nextNumber:D5}"; // PROD-00001 o SERV-00001
    }
}
