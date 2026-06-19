using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración EF Core para Marca
/// </summary>
public class MarcaConfiguration : IEntityTypeConfiguration<Marca>
{
    public void Configure(EntityTypeBuilder<Marca> builder)
    {
        // Nombre de tabla
        builder.ToTable("marcas");

        // Clave primaria
        builder.HasKey(x => x.Id);

        // ==========================================
        // PROPIEDADES
        // ==========================================

        builder.Property(x => x.Nombre)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Descripcion)
            .HasMaxLength(500);

        builder.Property(x => x.Activa)
            .IsRequired()
            .HasDefaultValue(true);

        // ==========================================
        // ÍNDICES
        // ==========================================

        builder.HasIndex(x => new { x.Nombre, x.EmisorId })
            .IsUnique()
            .HasFilter("\"Activa\" = true")
            .HasDatabaseName("IX_Marcas_Nombre_EmisorId");

        // ==========================================
        // TIMESTAMPS
        // ==========================================

        builder.Property(x => x.FechaCreacion)
            .HasDefaultValueSql("NOW()");
    }
}
