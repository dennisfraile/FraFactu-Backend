using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations
{
    public class FacturaDocumentoRelacionadoConfiguration : IEntityTypeConfiguration<FacturaDocumentoRelacionado>
    {
        public void Configure(EntityTypeBuilder<FacturaDocumentoRelacionado> builder)
        {
            builder.ToTable("FacturaDocumentosRelacionados");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.NumeroDocumento)
                .IsRequired()
                .HasMaxLength(36);

            builder.Property(d => d.FechaEmision)
                .IsRequired();

            builder.HasOne(d => d.Factura)
                .WithMany(f => f.DocumentosRelacionados)
                .HasForeignKey(d => d.FacturaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.TipoDocumento)
                .WithMany()
                .HasForeignKey(d => d.CatTipoDocumentoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(d => d.TipoGeneracion)
                .WithMany()
                .HasForeignKey(d => d.CatTipoGeneracionDocumentoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
