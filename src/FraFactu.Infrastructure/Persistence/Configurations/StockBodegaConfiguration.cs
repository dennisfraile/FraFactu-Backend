using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración EF Core para StockBodega
/// </summary>
public class StockBodegaConfiguration : IEntityTypeConfiguration<StockBodega>
{
    public void Configure(EntityTypeBuilder<StockBodega> builder)
    {
        // Nombre de tabla
        builder.ToTable("stock_bodegas");

        // Clave primaria
        builder.HasKey(x => x.Id);

        // ==========================================
        // PROPIEDADES
        // ==========================================

        builder.Property(x => x.CantidadDisponible)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.CantidadReservada)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.CostoPromedio)
            .HasPrecision(18, 4)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.UltimaActualizacion)
            .IsRequired();

        // Token de concurrencia optimista (columna de sistema PostgreSQL)
        builder.Property(x => x.xmin)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // No mapear propiedades calculadas
        builder.Ignore(x => x.CantidadTotal);
        builder.Ignore(x => x.ValorInventario);

        // ==========================================
        // RELACIONES
        // ==========================================

        builder.HasOne(x => x.Producto)
            .WithMany(p => p.Stocks)
            .HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Bodega)
            .WithMany(b => b.Stocks)
            .HasForeignKey(x => x.BodegaId)
            .OnDelete(DeleteBehavior.Cascade);

        // ==========================================
        // ÍNDICES Y CONSTRAINTS
        // ==========================================

        // Constraint único: un producto solo puede tener un registro por bodega
        builder.HasIndex(x => new { x.ProductoId, x.BodegaId })
            .IsUnique()
            .HasDatabaseName("IX_StockBodega_Producto_Bodega");

        builder.HasIndex(x => x.ProductoId)
            .HasDatabaseName("IX_StockBodega_Producto");

        builder.HasIndex(x => x.BodegaId)
            .HasDatabaseName("IX_StockBodega_Bodega");

        // ==========================================
        // TIMESTAMPS
        // ==========================================

        builder.Property(x => x.FechaCreacion)
            .HasDefaultValueSql("NOW()");
    }
}
