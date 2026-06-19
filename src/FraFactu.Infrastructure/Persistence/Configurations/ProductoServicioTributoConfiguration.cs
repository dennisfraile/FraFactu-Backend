using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class ProductoServicioTributoConfiguration : IEntityTypeConfiguration<ProductoServicioTributo>
{
    public void Configure(EntityTypeBuilder<ProductoServicioTributo> builder)
    {
        builder.ToTable("producto_servicio_tributos");

        builder.HasKey(pt => new { pt.ProductoServicioId, pt.CatTributoId });

        builder.Property(pt => pt.TipoCalculo).HasMaxLength(20);
        builder.Property(pt => pt.Valor).HasPrecision(18, 8);

        builder.HasOne(pt => pt.ProductoServicio)
            .WithMany(p => p.TributosAdicionales)
            .HasForeignKey(pt => pt.ProductoServicioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pt => pt.CatTributo)
            .WithMany()
            .HasForeignKey(pt => pt.CatTributoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(pt => pt.CatTributoId)
            .HasDatabaseName("IX_ProductoServicioTributo_CatTributoId");
    }
}
