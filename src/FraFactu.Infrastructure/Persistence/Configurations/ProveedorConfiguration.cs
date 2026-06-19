using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración EF Core para Proveedor
/// </summary>
public class ProveedorConfiguration : IEntityTypeConfiguration<Proveedor>
{
    public void Configure(EntityTypeBuilder<Proveedor> builder)
    {
        builder.ToTable("proveedores");

        builder.HasKey(x => x.Id);

        // Propiedades obligatorias
        builder.Property(x => x.NIT)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Nombre)
            .IsRequired()
            .HasMaxLength(300);

        // Propiedades opcionales
        builder.Property(x => x.NombreComercial)
            .HasMaxLength(300);

        builder.Property(x => x.Direccion)
            .HasMaxLength(500);

        builder.Property(x => x.Telefono)
            .HasMaxLength(50);

        builder.Property(x => x.Email)
            .HasMaxLength(200);

        builder.Property(x => x.Contacto)
            .HasMaxLength(200);

        builder.Property(x => x.SitioWeb)
            .HasMaxLength(300);

        builder.Property(x => x.Notas)
            .HasMaxLength(1000);

        builder.Property(x => x.Activo)
            .HasDefaultValue(true);

        builder.Property(x => x.FechaCreacion)
            .HasDefaultValueSql("NOW()");

        // Índices
        builder.HasIndex(x => new { x.NIT, x.EmisorId })
            .IsUnique()
            .HasFilter("\"Activo\" = true")
            .HasDatabaseName("IX_Proveedores_NIT_EmisorId");

        builder.HasIndex(x => x.Nombre)
            .HasDatabaseName("IX_Proveedores_Nombre");

        builder.HasIndex(x => x.Activo)
            .HasDatabaseName("IX_Proveedores_Activo");
    }
}
