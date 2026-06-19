using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class UsuarioSucursalConfiguration : IEntityTypeConfiguration<UsuarioSucursal>
{
    public void Configure(EntityTypeBuilder<UsuarioSucursal> builder)
    {
        builder.ToTable("UsuariosSucursales");

        builder.HasKey(us => new { us.UsuarioId, us.SucursalId });

        builder.HasOne(us => us.Usuario)
            .WithMany(u => u.UsuarioSucursales)
            .HasForeignKey(us => us.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(us => us.Sucursal)
            .WithMany()
            .HasForeignKey(us => us.SucursalId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(us => us.SucursalId)
            .HasDatabaseName("IX_UsuarioSucursal_SucursalId");
    }
}
