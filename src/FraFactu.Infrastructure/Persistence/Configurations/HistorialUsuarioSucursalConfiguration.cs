using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class HistorialUsuarioSucursalConfiguration : IEntityTypeConfiguration<HistorialUsuarioSucursal>
{
    public void Configure(EntityTypeBuilder<HistorialUsuarioSucursal> builder)
    {
        builder.ToTable("HistorialUsuariosSucursales");

        builder.HasKey(h => h.Id);

        builder.HasOne(h => h.Usuario)
            .WithMany()
            .HasForeignKey(h => h.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Sucursal)
            .WithMany()
            .HasForeignKey(h => h.SucursalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
