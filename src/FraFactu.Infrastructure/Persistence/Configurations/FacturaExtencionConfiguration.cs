using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations
{
    public class FacturaExtencionConfiguration : IEntityTypeConfiguration<FacturaExtencion>
    {
        public void Configure(EntityTypeBuilder<FacturaExtencion> builder)
        {
            builder.ToTable("FacturaExtensiones");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.NombEntrega)
                .HasMaxLength(100);

            builder.Property(e => e.DocuEntrega)
                .HasMaxLength(25);

            builder.Property(e => e.NombRecibe)
                .HasMaxLength(100);

            builder.Property(e => e.DocuRecibe)
                .HasMaxLength(25);

            builder.Property(e => e.PlacaVehiculo)
                .HasMaxLength(15);

            builder.Property(e => e.Observaciones)
                .HasMaxLength(3000);

            builder.HasOne(e => e.Factura)
                .WithOne(f => f.Extension)
                .HasForeignKey<FacturaExtencion>(e => e.FacturaId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
