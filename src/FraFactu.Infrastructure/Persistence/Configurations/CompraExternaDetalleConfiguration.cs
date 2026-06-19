using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración EF Core para CompraExternaDetalle
/// </summary>
public class CompraExternaDetalleConfiguration : IEntityTypeConfiguration<CompraExternaDetalle>
{
    public void Configure(EntityTypeBuilder<CompraExternaDetalle> builder)
    {
        builder.ToTable("compra_externa_detalles");

        builder.HasKey(x => x.Id);

        // Relación con CompraExterna
        builder.HasOne(x => x.CompraExterna)
            .WithMany(c => c.Detalles)
            .HasForeignKey(x => x.CompraExternaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relación con Producto
        builder.HasOne(x => x.Producto)
            .WithMany()
            .HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relación con Bodega
        builder.HasOne(x => x.Bodega)
            .WithMany()
            .HasForeignKey(x => x.BodegaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Propiedades
        builder.Property(x => x.Cantidad)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.CostoUnitario)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.Subtotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.IVA)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Total)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.EsParaInventario)
            .HasDefaultValue(true);

        builder.Property(x => x.FechaCreacion)
            .HasDefaultValueSql("NOW()");

        // Índices
        builder.HasIndex(x => x.CompraExternaId)
            .HasDatabaseName("IX_CompraExternaDetalles_CompraExterna");

        builder.HasIndex(x => x.ProductoId)
            .HasDatabaseName("IX_CompraExternaDetalles_Producto");

        builder.HasIndex(x => x.BodegaId)
            .HasDatabaseName("IX_CompraExternaDetalles_Bodega");
    }
}
