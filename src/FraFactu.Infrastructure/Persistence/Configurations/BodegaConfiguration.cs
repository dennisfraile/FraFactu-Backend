using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración EF Core para Bodega
/// </summary>
public class BodegaConfiguration : IEntityTypeConfiguration<Bodega>
{
    public void Configure(EntityTypeBuilder<Bodega> builder)
    {
        // Nombre de tabla
        builder.ToTable("bodegas");

        // Clave primaria
        builder.HasKey(x => x.Id);

        // ==========================================
        // PROPIEDADES
        // ==========================================

        builder.Property(x => x.Codigo)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Nombre)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Direccion)
            .HasMaxLength(500);

        builder.Property(x => x.EsPrincipal)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.Activa)
            .IsRequired()
            .HasDefaultValue(true);

        // ==========================================
        // RELACIONES
        // ==========================================

        builder.HasOne(x => x.Sucursal)
            .WithMany()
            .HasForeignKey(x => x.SucursalId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==========================================
        // ÍNDICES
        // ==========================================

        // Índice compuesto para permitir códigos repetidos en diferentes sucursales/emisores
        builder.HasIndex(x => new { x.SucursalId, x.Codigo })
            .IsUnique()
            .HasFilter("\"Activa\" = true")
            .HasDatabaseName("IX_Bodegas_Sucursal_Codigo");

        builder.HasIndex(x => x.Nombre)
            .HasDatabaseName("IX_Bodegas_Nombre");

        builder.HasIndex(x => x.SucursalId)
            .HasDatabaseName("IX_Bodegas_Sucursal");

        // ==========================================
        // TIMESTAMPS
        // ==========================================

        builder.Property(x => x.FechaCreacion)
            .HasDefaultValueSql("NOW()");
    }
}
