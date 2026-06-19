using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class LoteDetalleConfiguration : IEntityTypeConfiguration<LoteDetalle>
{
    public void Configure(EntityTypeBuilder<LoteDetalle> builder)
    {
        // Nombre de tabla
        builder.ToTable("lote_detalles");

        // Clave primaria
        builder.HasKey(x => x.Id);

        // ==========================================
        // PROPIEDADES
        // ==========================================

        builder.Property(x => x.NumeroItem)
            .IsRequired();

        builder.Property(x => x.EstadoDte)
            .HasMaxLength(20);

        builder.Property(x => x.SelloRecibido)
            .HasMaxLength(100);

        builder.Property(x => x.CodigoRechazo)
            .HasMaxLength(10);

        builder.Property(x => x.ObservacionesRechazo)
            .HasColumnType("text");

        builder.Property(x => x.JsonRespuestaIndividual)
            .HasColumnType("text");

        // ==========================================
        // RELACIONES
        // ==========================================

        builder.HasOne(x => x.Lote)
            .WithMany(l => l.Detalles)
            .HasForeignKey(x => x.LoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.FacturaElectronica)
            .WithMany()
            .HasForeignKey(x => x.FacturaElectronicaId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==========================================
        // ÍNDICES Y CONSTRAINTS
        // ==========================================

        // Constraint único: una factura no puede estar dos veces en el mismo lote
        builder.HasIndex(x => new { x.LoteId, x.FacturaElectronicaId })
            .IsUnique()
            .HasDatabaseName("IX_LoteDetalle_Lote_Factura");

        builder.HasIndex(x => x.LoteId)
            .HasDatabaseName("IX_LoteDetalle_Lote");

        builder.HasIndex(x => x.FacturaElectronicaId)
            .HasDatabaseName("IX_LoteDetalle_Factura");

        builder.HasIndex(x => x.EstadoDte)
            .HasDatabaseName("IX_LoteDetalle_Estado");

        // ==========================================
        // TIMESTAMPS
        // ==========================================

        builder.Property(x => x.FechaCreacion)
            .HasDefaultValueSql("NOW()");
    }
}
