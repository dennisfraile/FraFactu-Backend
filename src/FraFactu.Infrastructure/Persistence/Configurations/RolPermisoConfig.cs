using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class RolPermisoConfig : IEntityTypeConfiguration<RolPermiso>
{
    public void Configure(EntityTypeBuilder<RolPermiso> builder)
    {
        builder.ToTable("roles_permisos");

        // 🔑 LLAVE PRIMARIA COMPUESTA (N:M)
        builder.HasKey(x => new { x.RolId, x.PermisoId });

        // Relaciones
        builder.HasOne(x => x.Rol)
            .WithMany(x => x.RolesPermisos)
            .HasForeignKey(x => x.RolId);

        builder.HasOne(x => x.Permiso)
            .WithMany(x => x.RolesPermisos)
            .HasForeignKey(x => x.PermisoId);
    }
}