using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class MigracionInventarioProcesadaConfig : IEntityTypeConfiguration<MigracionInventarioProcesada>
{
    public void Configure(EntityTypeBuilder<MigracionInventarioProcesada> builder)
    {
        builder.ToTable("migraciones_inventario_procesadas");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MigrationId).IsRequired();
        builder.Property(x => x.Tipo).HasMaxLength(20).IsRequired();

        builder.HasIndex(x => new { x.MigrationId, x.Tipo }).IsUnique();

        builder.HasOne(x => x.Emisor)
            .WithMany()
            .HasForeignKey(x => x.EmisorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
