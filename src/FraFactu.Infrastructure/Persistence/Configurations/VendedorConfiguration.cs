using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class VendedorConfiguration : IEntityTypeConfiguration<Vendedor>
{
    public void Configure(EntityTypeBuilder<Vendedor> builder)
    {
        builder.ToTable("Vendedores");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Nombre)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(v => v.Codigo)
            .IsRequired()
            .HasMaxLength(50);

        // Relación con Emisor
        builder.HasOne(v => v.Emisor)
            .WithMany()
            .HasForeignKey(v => v.EmisorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índice para garantizar unicidad por emisor (solo entre activos)
        builder.HasIndex(v => new { v.EmisorId, v.Codigo })
            .IsUnique()
            .HasFilter("\"Activo\" = true")
            .HasDatabaseName("IX_Vendedores_Emisor_Codigo");

        // Relación con Facturas (opcional configurar aquí si ya está en Factura)
        builder.HasMany(v => v.Facturas)
            .WithOne(f => f.Vendedor)
            .HasForeignKey(f => f.VendedorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
