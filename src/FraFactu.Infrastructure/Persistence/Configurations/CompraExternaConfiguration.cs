using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración EF Core para CompraExterna
/// </summary>
public class CompraExternaConfiguration : IEntityTypeConfiguration<CompraExterna>
{
    public void Configure(EntityTypeBuilder<CompraExterna> builder)
    {
        builder.ToTable("compras_externas");

        builder.HasKey(x => x.Id);

        // Relaciones
        builder.HasOne(c => c.Proveedor)
            .WithMany(p => p.Compras)
            .HasForeignKey(c => c.ProveedorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Sucursal)
            .WithMany()
            .HasForeignKey(c => c.SucursalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Detalles)
            .WithOne(d => d.CompraExterna)
            .HasForeignKey(d => d.CompraExternaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Gastos)
            .WithOne(g => g.CompraExterna)
            .HasForeignKey(g => g.CompraExternaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Propiedades
        builder.Property(x => x.NumeroFactura)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.FechaEmision)
            .IsRequired();

        builder.Property(x => x.FechaRegistro)
            .HasDefaultValueSql("NOW()");

        builder.Property(x => x.Subtotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.IVA)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Total)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Estado)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("BORRADOR");

        builder.Property(x => x.Observaciones)
            .HasMaxLength(1000);

        // Origen y trazabilidad DTE
        builder.Property(x => x.Origen)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("MANUAL");

        builder.Property(x => x.CodigoGeneracionDte)
            .HasMaxLength(36);

        builder.Property(x => x.SelloRecibidoDte)
            .HasMaxLength(100);

        builder.Property(x => x.TipoDte)
            .HasMaxLength(5);

        builder.Property(x => x.NumeroControlDte)
            .HasMaxLength(31);

        builder.Property(x => x.FechaCreacion)
            .HasDefaultValueSql("NOW()");

        // Índices
        builder.HasIndex(x => x.ProveedorId)
            .HasDatabaseName("IX_ComprasExternas_Proveedor");

        builder.HasIndex(x => x.NumeroFactura)
            .HasDatabaseName("IX_ComprasExternas_NumeroFactura");

        builder.HasIndex(x => x.FechaEmision)
            .HasDatabaseName("IX_ComprasExternas_FechaEmision");

        builder.HasIndex(x => x.Estado)
            .HasDatabaseName("IX_ComprasExternas_Estado");

        builder.HasIndex(x => new { x.ProveedorId, x.NumeroFactura })
            .IsUnique()
            .HasDatabaseName("IX_ComprasExternas_Proveedor_NumeroFactura");
    }
}
