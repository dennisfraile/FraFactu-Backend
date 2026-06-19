using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración EF Core para FacturaElectronicaDetalle
/// </summary>
public class FacturaElectronicaDetalleConfiguration : IEntityTypeConfiguration<FacturaElectronicaDetalle>
{
    public void Configure(EntityTypeBuilder<FacturaElectronicaDetalle> builder)
    {
        // Nombre de tabla
        builder.ToTable("factura_detalles");

        // Clave primaria
        builder.HasKey(x => x.Id);

        // ==========================================
        // RELACIONES DE INVENTARIO (opcionales)
        // ==========================================

        builder.HasOne(d => d.Producto)
            .WithMany()
            .HasForeignKey(d => d.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Bodega)
            .WithMany()
            .HasForeignKey(d => d.BodegaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(d => d.CostoUnitario)
            .HasPrecision(18, 4)
            .HasDefaultValue(0);

        // ==========================================
        // NO MAPEAR PROPIEDADES CALCULADAS
        // ==========================================

        builder.Ignore(d => d.CostoTotal);
        builder.Ignore(d => d.Utilidad);
        builder.Ignore(d => d.PorcentajeUtilidad);

        // ==========================================
        // ÍNDICES
        // ==========================================

        builder.HasIndex(d => d.ProductoId)
            .HasDatabaseName("IX_FacturaDetalles_Producto");

        builder.HasIndex(d => d.BodegaId)
            .HasDatabaseName("IX_FacturaDetalles_Bodega");
    }
}
