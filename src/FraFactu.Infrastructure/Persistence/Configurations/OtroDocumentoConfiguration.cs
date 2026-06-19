using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations
{
    public class OtroDocumentoConfiguration : IEntityTypeConfiguration<OtroDocumento>
    {
        public void Configure(EntityTypeBuilder<OtroDocumento> builder)
        {
            builder.ToTable("OtrosDocumentos");

            builder.HasKey(o => o.Id);

            // Relación N:1 con FacturaElectronica
            builder.HasOne(o => o.FacturaElectronica)
                .WithMany(f => f.OtrosDocumentos)
                .HasForeignKey(o => o.FacturaElectronicaId)
                .OnDelete(DeleteBehavior.Cascade);

            // CodDocAsociado: 1-4
            builder.Property(o => o.CodDocAsociado)
                .IsRequired();

            // DescDocumento: opcional, max 100
            builder.Property(o => o.DescDocumento)
                .HasMaxLength(100);

            // DetalleDocumento: opcional, max 300
            builder.Property(o => o.DetalleDocumento)
                .HasMaxLength(300);

            // Índice por FacturaElectronicaId
            builder.HasIndex(o => o.FacturaElectronicaId);
        }
    }
}
