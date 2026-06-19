using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración EF Core para MovimientoInventario
/// </summary>
public class MovimientoInventarioConfiguration : IEntityTypeConfiguration<MovimientoInventario>
{
    public void Configure(EntityTypeBuilder<MovimientoInventario> builder)
    {
        // Nombre de tabla
        builder.ToTable("movimientos_inventario");

        // Clave primaria
        builder.HasKey(x => x.Id);

        // ==========================================
        // PROPIEDADES
        // ==========================================

        builder.Property(x => x.TipoMovimiento)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Cantidad)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.CostoUnitario)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.TipoDocumento)
            .HasMaxLength(50);

        builder.Property(x => x.NumeroDocumento)
            .HasMaxLength(100);

        builder.Property(x => x.Observaciones)
            .HasMaxLength(1000);

        builder.Property(x => x.UsuarioRegistro)
            .HasMaxLength(100);

        builder.Property(x => x.FechaMovimiento)
            .IsRequired();

        // Saldos para kardex
        builder.Property(x => x.SaldoAnterior)
            .HasPrecision(18, 2);

        builder.Property(x => x.NuevoSaldo)
            .HasPrecision(18, 2);

        builder.Property(x => x.CostoPromedioAnterior)
            .HasPrecision(18, 4);

        builder.Property(x => x.NuevoCostoPromedio)
            .HasPrecision(18, 4);

        // No mapear propiedades calculadas
        builder.Ignore(x => x.CostoTotal);

        // ==========================================
        // RELACIONES
        // ==========================================

        builder.HasOne(x => x.Producto)
            .WithMany(p => p.Movimientos)
            .HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Bodega)
            .WithMany(b => b.Movimientos)
            .HasForeignKey(x => x.BodegaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Bodega destino para traslados (opcional)
        builder.HasOne(x => x.BodegaDestino)
            .WithMany()
            .HasForeignKey(x => x.BodegaDestinoId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==========================================
        // ÍNDICES
        // ==========================================

        builder.HasIndex(x => x.ProductoId)
            .HasDatabaseName("IX_MovimientosInventario_Producto");

        builder.HasIndex(x => x.BodegaId)
            .HasDatabaseName("IX_MovimientosInventario_Bodega");

        builder.HasIndex(x => x.FechaMovimiento)
            .HasDatabaseName("IX_MovimientosInventario_Fecha");

        builder.HasIndex(x => x.TipoMovimiento)
            .HasDatabaseName("IX_MovimientosInventario_Tipo");

        builder.HasIndex(x => new { x.ProductoId, x.BodegaId, x.FechaMovimiento })
            .HasDatabaseName("IX_MovimientosInventario_Producto_Bodega_Fecha");

        builder.HasIndex(x => new { x.TipoDocumento, x.DocumentoId })
            .HasDatabaseName("IX_MovimientosInventario_Documento");

        // ==========================================
        // TIMESTAMPS
        // ==========================================

        builder.Property(x => x.FechaCreacion)
            .HasDefaultValueSql("NOW()");
    }
}
