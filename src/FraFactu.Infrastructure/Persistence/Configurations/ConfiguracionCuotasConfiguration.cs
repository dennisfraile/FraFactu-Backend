using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class ConfiguracionCuotasConfiguration : IEntityTypeConfiguration<ConfiguracionCuotas>
{
    public void Configure(EntityTypeBuilder<ConfiguracionCuotas> builder)
    {
        builder.ToTable("configuracion_cuotas");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.TasaMoraMensual).HasColumnType("numeric(7,4)");
        builder.HasOne(c => c.Emisor)
            .WithMany()
            .HasForeignKey(c => c.EmisorId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(c => c.EmisorId).IsUnique();
    }
}
