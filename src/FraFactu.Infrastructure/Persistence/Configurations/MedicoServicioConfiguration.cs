using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations
{
    public class MedicoServicioConfiguration : IEntityTypeConfiguration<MedicoServicio>
    {
        public void Configure(EntityTypeBuilder<MedicoServicio> builder)
        {
            builder.ToTable("MedicosServicios");

            builder.HasKey(m => m.Id);

            // Relación 1:1 con OtroDocumento
            builder.HasOne(m => m.OtroDocumento)
                .WithOne(o => o.Medico)
                .HasForeignKey<MedicoServicio>(m => m.OtroDocumentoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Nombre: max 100
            builder.Property(m => m.Nombre)
                .IsRequired()
                .HasMaxLength(100);

            // Nit: opcional, 9 o 14 dígitos
            builder.Property(m => m.Nit)
                .HasMaxLength(14);

            // DocIdentificacion: opcional, 2-25 caracteres
            builder.Property(m => m.DocIdentificacion)
                .HasMaxLength(25);

            // TipoServicio: 1-6
            builder.Property(m => m.TipoServicio)
                .IsRequired();

            // Índice por OtroDocumentoId (unique porque es 1:1)
            builder.HasIndex(m => m.OtroDocumentoId)
                .IsUnique();
        }
    }
}
