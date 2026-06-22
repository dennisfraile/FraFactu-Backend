using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations
{
    public class MovimientoExternoRegistradoConfiguration : IEntityTypeConfiguration<MovimientoExternoRegistrado>
    {
        public void Configure(EntityTypeBuilder<MovimientoExternoRegistrado> builder)
        {
            builder.ToTable("movimientos_externos_registrados");

            builder.Property(m => m.MovimientoIdExterno)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(m => m.TipoDocumento)
                .HasMaxLength(50);

            // Idempotencia: una clave externa por emisor.
            builder.HasIndex(m => new { m.EmisorId, m.MovimientoIdExterno })
                .HasDatabaseName("IX_MovimientoExterno_Emisor_IdExterno")
                .IsUnique();
        }
    }
}
