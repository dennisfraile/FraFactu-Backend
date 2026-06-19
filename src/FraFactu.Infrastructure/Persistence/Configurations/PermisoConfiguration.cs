using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations
{
    public class PermisoConfiguration : IEntityTypeConfiguration<Permiso>
    {
        public void Configure(EntityTypeBuilder<Permiso> builder)
        {
            builder.ToTable("Permisos");

            builder.HasKey(p => p.Id);

            // Propiedades
            builder.Property(p => p.Codigo)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.Nombre)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.Slug)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.Descripcion)
                .HasMaxLength(300);

            builder.Property(p => p.Modulo)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.FechaCreacion)
                .IsRequired();

            builder.Property(p => p.Activo)
                .IsRequired()
                .HasDefaultValue(true);

            // Índices
            builder.HasIndex(p => p.Codigo)
                .IsUnique()
                .HasDatabaseName("IX_Permisos_Codigo");

            builder.HasIndex(p => p.Slug)
                .IsUnique()
                .HasDatabaseName("IX_Permisos_Slug");

            builder.HasIndex(p => p.Modulo)
                .HasDatabaseName("IX_Permisos_Modulo");
        }
    }
}
