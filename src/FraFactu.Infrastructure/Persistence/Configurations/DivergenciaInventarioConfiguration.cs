using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

/// <summary>
/// F4 (Plan inventario desde DTE): mapeo de la tabla que persiste las
/// divergencias detectadas entre Smartix.StockBodega y el snapshot de
/// SmartInventory.
/// </summary>
public class DivergenciaInventarioConfiguration : IEntityTypeConfiguration<DivergenciaInventario>
{
    public void Configure(EntityTypeBuilder<DivergenciaInventario> builder)
    {
        builder.ToTable("TBL_DivergenciaInventario");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.CodigoProducto)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.NombreBodega)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.Estado)
            .HasConversion<int>()
            .HasDefaultValue(Domain.Enums.EstadoDivergenciaInventario.DETECTADA)
            .HasSentinel((Domain.Enums.EstadoDivergenciaInventario)(-1))
            .IsRequired();

        builder.HasOne(d => d.Emisor)
            .WithMany()
            .HasForeignKey(d => d.EmisorId)
            .OnDelete(DeleteBehavior.Cascade);

        // Lookup por (emisor, codigo, bodega): el job busca filas
        // DETECTADA existentes en cada corrida para actualizar en vez
        // de duplicar.
        builder.HasIndex(d => new { d.EmisorId, d.CodigoProducto, d.NombreBodega, d.Estado })
            .HasDatabaseName("IX_DivergenciaInventario_Emisor_Producto_Bodega_Estado");

        // Para listar el reporte de una corrida puntual desde el endpoint.
        builder.HasIndex(d => d.EjecucionId)
            .HasDatabaseName("IX_DivergenciaInventario_EjecucionId");

        // Vista temporal: divergencias abiertas mas recientes primero.
        builder.HasIndex(d => new { d.Estado, d.UltimaDeteccion })
            .HasDatabaseName("IX_DivergenciaInventario_Estado_UltimaDeteccion");
    }
}
