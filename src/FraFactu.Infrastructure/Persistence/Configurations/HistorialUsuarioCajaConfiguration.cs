using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class HistorialUsuarioCajaConfiguration : IEntityTypeConfiguration<HistorialUsuarioCaja>
{
    public void Configure(EntityTypeBuilder<HistorialUsuarioCaja> builder)
    {
        builder.ToTable("HistorialUsuariosCajas");

        builder.HasKey(h => h.Id);

        builder.HasOne(h => h.Usuario)
            .WithMany()
            .HasForeignKey(h => h.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Caja)
            .WithMany()
            .HasForeignKey(h => h.CajaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.UsuarioAsignador)
            .WithMany()
            .HasForeignKey(h => h.UsuarioAsignadorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
