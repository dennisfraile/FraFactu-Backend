using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class NotificacionConfiguration : IEntityTypeConfiguration<Notificacion>
{
    public void Configure(EntityTypeBuilder<Notificacion> builder)
    {
        builder.ToTable("notificaciones");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Tipo).HasMaxLength(80).IsRequired();
        builder.Property(n => n.Nivel).HasMaxLength(20).IsRequired();
        builder.Property(n => n.Titulo).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Mensaje).HasMaxLength(1000).IsRequired();
        builder.Property(n => n.Ruta).HasMaxLength(300);
        builder.HasOne(n => n.Emisor)
            .WithMany()
            .HasForeignKey(n => n.EmisorId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(n => n.EmisorId);
    }
}
