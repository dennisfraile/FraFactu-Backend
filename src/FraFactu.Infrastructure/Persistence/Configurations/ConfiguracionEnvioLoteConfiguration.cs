using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class ConfiguracionEnvioLoteConfiguration : IEntityTypeConfiguration<ConfiguracionEnvioLote>
{
    public void Configure(EntityTypeBuilder<ConfiguracionEnvioLote> builder)
    {
        builder.ToTable("ConfiguracionEnvioLotes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.HoraEnvioAutomatico)
            .IsRequired();

        builder.Property(c => c.ZonaHoraria)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.EnvioAutomaticoHabilitado)
            .IsRequired();

        builder.Property(c => c.EnviarRecordatorio)
            .IsRequired();

        builder.Property(c => c.MinutosAnticipacionRecordatorio)
            .IsRequired();

        // Relación con Emisor
        builder.HasOne(c => c.Emisor)
            .WithMany()
            .HasForeignKey(c => c.EmisorId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índice único: un emisor solo puede tener una configuración
        builder.HasIndex(c => c.EmisorId)
            .IsUnique();
    }
}
