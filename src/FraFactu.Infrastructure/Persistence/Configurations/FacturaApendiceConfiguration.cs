using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations
{
    public class FacturaApendiceConfiguration : IEntityTypeConfiguration<FacturaApendice>
    {
        public void Configure(EntityTypeBuilder<FacturaApendice> builder)
        {
            builder.ToTable("FacturaApendices");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Campo)
                .IsRequired()
                .HasMaxLength(25);

            builder.Property(a => a.Etiqueta)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(a => a.Valor)
                .IsRequired()
                .HasMaxLength(150);

            builder.HasOne(a => a.Factura)
                .WithMany(f => f.Apendices)
                .HasForeignKey(a => a.FacturaId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
