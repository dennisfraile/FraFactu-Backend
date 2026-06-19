using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class NotificacionLeidaConfiguration : IEntityTypeConfiguration<NotificacionLeida>
{
    public void Configure(EntityTypeBuilder<NotificacionLeida> builder)
    {
        builder.ToTable("notificacion_leida");
        builder.HasKey(l => l.Id);
        builder.HasOne(l => l.Notificacion)
            .WithMany()
            .HasForeignKey(l => l.NotificacionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(l => l.Usuario)
            .WithMany()
            .HasForeignKey(l => l.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(l => new { l.NotificacionId, l.UsuarioId }).IsUnique();
    }
}
