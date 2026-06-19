using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración EF Core para GastoAdministrativo
/// </summary>
public class GastoAdministrativoConfiguration : IEntityTypeConfiguration<GastoAdministrativo>
{
    public void Configure(EntityTypeBuilder<GastoAdministrativo> builder)
    {
        builder.ToTable("gastos_administrativos");

        builder.HasKey(x => x.Id);

        // Relación con CompraExterna
        builder.HasOne(x => x.CompraExterna)
            .WithMany(c => c.Gastos)
            .HasForeignKey(x => x.CompraExternaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relación con CatTipoGasto (opcional)
        builder.HasOne(x => x.TipoGasto)
            .WithMany()
            .HasForeignKey(x => x.CatTipoGastoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Propiedades
        builder.Property(x => x.Descripcion)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Monto)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.CentroCosto)
            .HasMaxLength(100);

        builder.Property(x => x.CuentaContable)
            .HasMaxLength(50);

        builder.Property(x => x.FechaCreacion)
            .HasDefaultValueSql("NOW()");

        // Índices
        builder.HasIndex(x => x.CompraExternaId)
            .HasDatabaseName("IX_GastosAdministrativos_CompraExterna");

        builder.HasIndex(x => x.CatTipoGastoId)
            .HasDatabaseName("IX_GastosAdministrativos_TipoGasto");

        builder.HasIndex(x => x.CentroCosto)
            .HasDatabaseName("IX_GastosAdministrativos_CentroCosto");
    }
}
