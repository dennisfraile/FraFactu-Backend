using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class CajaConfiguration : IEntityTypeConfiguration<Caja>
{
    public void Configure(EntityTypeBuilder<Caja> builder)
    {
        builder.ToTable("Cajas");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nombre)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.Codigo)
            .HasMaxLength(50);

        // Relación con Sucursal
        builder.HasOne(c => c.Sucursal)
            .WithMany() // O WithMany(s => s.Cajas) si agregamos la colección en Sucursal
            .HasForeignKey(c => c.SucursalId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relación con Facturas
        builder.HasMany(c => c.Facturas)
            .WithOne(f => f.Caja)
            .HasForeignKey(f => f.CajaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índice para garantizar unicidad por sucursal
        builder.HasIndex(c => new { c.SucursalId, c.Codigo })
            .IsUnique()
            .HasFilter("\"Activo\" = true")
            .HasDatabaseName("IX_Cajas_Sucursal_Codigo");

        // Índice único de CodPuntoVenta por sucursal (excluye vacíos)
        builder.HasIndex(c => new { c.SucursalId, c.CodPuntoVenta })
            .IsUnique()
            .HasFilter("\"CodPuntoVenta\" != '' AND \"Activo\" = true")
            .HasDatabaseName("IX_Cajas_Sucursal_CodPuntoVenta");

        // Índice único de CodPuntoVentaMH por sucursal (excluye vacíos/nulos)
        builder.HasIndex(c => new { c.SucursalId, c.CodPuntoVentaMH })
            .IsUnique()
            .HasFilter("\"CodPuntoVentaMH\" IS NOT NULL AND \"CodPuntoVentaMH\" != '' AND \"Activo\" = true")
            .HasDatabaseName("IX_Cajas_Sucursal_CodPuntoVentaMH");
    }
}
