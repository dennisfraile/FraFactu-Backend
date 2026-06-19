using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations
{
    public class VentaTerceroConfiguration : IEntityTypeConfiguration<VentaTercero>
    {
        public void Configure(EntityTypeBuilder<VentaTercero> builder)
        {
            builder.ToTable("VentaTerceros");

            builder.HasKey(v => v.Id);

            // Relación 1:1 con FacturaElectronica
            builder.HasOne(v => v.FacturaElectronica)
                .WithOne(f => f.VentaTercero)
                .HasForeignKey<VentaTercero>(v => v.FacturaElectronicaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Nit: 9 o 14 dígitos
            builder.Property(v => v.Nit)
                .IsRequired()
                .HasMaxLength(14);

            // Nombre: 3-200 caracteres
            builder.Property(v => v.Nombre)
                .IsRequired()
                .HasMaxLength(200);

            // Índice por FacturaElectronicaId (unique porque es 1:1)
            builder.HasIndex(v => v.FacturaElectronicaId)
                .IsUnique();
        }
    }
}
