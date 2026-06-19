using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities.Catalogos;

namespace FraFactu.Infrastructure.Persistence.Configurations.Catalogos;

/// <summary>
/// Configuración EF Core para CatTipoGasto
/// </summary>
public class CatTipoGastoConfiguration : IEntityTypeConfiguration<CatTipoGasto>
{
    public void Configure(EntityTypeBuilder<CatTipoGasto> builder)
    {
        builder.ToTable("cat_tipo_gasto");

        builder.HasKey(x => x.Id);

        // Propiedades
        builder.Property(x => x.Codigo)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Nombre)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Descripcion)
            .HasMaxLength(500);

        builder.Property(x => x.Activo)
            .HasDefaultValue(true);

        builder.Property(x => x.FechaCreacion)
            .HasDefaultValueSql("NOW()");

        // Índices - único por emisor, solo activos
        builder.HasIndex(x => new { x.EmisorId, x.Codigo })
            .IsUnique()
            .HasFilter("\"Activo\" = true")
            .HasDatabaseName("IX_CatTipoGasto_Emisor_Codigo");

        builder.HasIndex(x => x.Nombre)
            .HasDatabaseName("IX_CatTipoGasto_Nombre");
    }
}
