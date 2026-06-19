using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class ProductoServicioSucursalConfiguration : IEntityTypeConfiguration<ProductoServicioSucursal>
{
    public void Configure(EntityTypeBuilder<ProductoServicioSucursal> builder)
    {
        builder.ToTable("ProductosServiciosSucursales");

        builder.HasKey(ps => new { ps.ProductoServicioId, ps.SucursalId });

        builder.HasOne(ps => ps.ProductoServicio)
            .WithMany(p => p.ProductoServicioSucursales)
            .HasForeignKey(ps => ps.ProductoServicioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ps => ps.Sucursal)
            .WithMany()
            .HasForeignKey(ps => ps.SucursalId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(ps => ps.SucursalId)
            .HasDatabaseName("IX_ProductoServicioSucursal_SucursalId");
    }
}
