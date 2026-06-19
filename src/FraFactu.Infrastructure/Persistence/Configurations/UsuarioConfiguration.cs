using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations
{
    public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("Usuarios");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.PasswordHash)
                .IsRequired(false) // Nullable para usuarios OAuth
                .HasMaxLength(255);

            builder.Property(u => u.NombreCompleto)
                .IsRequired()
                .HasMaxLength(200);

            // Configuración de autenticación externa (OAuth)
            builder.Property(u => u.ProveedorAuth)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(u => u.ProveedorExternoId)
                .HasMaxLength(255)
                .IsRequired(false);

            builder.Property(u => u.ProveedorExternoAccessToken)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(u => u.ProveedorExternoTokenExpiracion)
                .IsRequired(false);

            // Estado del usuario
            builder.Property(u => u.Estado)
                .HasConversion<int>()
                .IsRequired();

            // Índice único en Email
            builder.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_Usuario_Email");

            // Índice en ProveedorExternoId para búsquedas rápidas
            builder.HasIndex(u => u.ProveedorExternoId)
                .HasDatabaseName("IX_Usuario_ProveedorExternoId");

            // Índice compuesto Emisor + Email
            builder.HasIndex(u => new { u.EmisorId, u.Email })
                .HasDatabaseName("IX_Usuario_Emisor_Email");

            // Relación con Emisor
            builder.HasOne(u => u.Emisor)
                .WithMany(e => e.Usuarios) // ✅ Referencia a la colección
                .HasForeignKey(u => u.EmisorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación con Rol
            builder.HasOne(u => u.Rol)
                .WithMany(r => r.Usuarios)
                .HasForeignKey(u => u.RolId)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
