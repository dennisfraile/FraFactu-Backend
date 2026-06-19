using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class VendedorSucursalConfiguration : IEntityTypeConfiguration<VendedorSucursal>
{
    public void Configure(EntityTypeBuilder<VendedorSucursal> builder)
    {
        builder.ToTable("VendedoresSucursales");

        builder.HasKey(vs => new { vs.VendedorId, vs.SucursalId });

        builder.HasOne(vs => vs.Vendedor)
            .WithMany(v => v.VendedorSucursales)
            .HasForeignKey(vs => vs.VendedorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(vs => vs.Sucursal)
            .WithMany()
            .HasForeignKey(vs => vs.SucursalId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(vs => vs.SucursalId)
            .HasDatabaseName("IX_VendedorSucursal_SucursalId");
    }
}
