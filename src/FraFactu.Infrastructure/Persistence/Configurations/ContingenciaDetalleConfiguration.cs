using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class ContingenciaDetalleConfiguration : IEntityTypeConfiguration<ContingenciaDetalle>
{
    public void Configure(EntityTypeBuilder<ContingenciaDetalle> builder)
    {
        // Nombre de tabla
        builder.ToTable("contingencia_detalles");

        // Clave primaria
        builder.HasKey(x => x.Id);

        // ==========================================
        // PROPIEDADES
        // ==========================================

        builder.Property(x => x.NoItem)
            .IsRequired();

        builder.Property(x => x.CodigoGeneracion)
            .IsRequired()
            .HasMaxLength(36);

        // ==========================================
        // RELACIONES
        // ==========================================

        // Relación con EventoContingencia (ya configurada desde el otro lado)
        builder.HasOne(x => x.EventoContingencia)
            .WithMany(e => e.Detalles)
            .HasForeignKey(x => x.EventoContingenciaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relación con FacturaElectronica
        builder.HasOne(x => x.FacturaElectronica)
            .WithMany()
            .HasForeignKey(x => x.FacturaElectronicaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relación con CatTipoDocumento
        builder.HasOne(x => x.TipoDocumento)
            .WithMany()
            .HasForeignKey(x => x.CatTipoDocumentoId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==========================================
        // ÍNDICES Y CONSTRAINTS
        // ==========================================

        // Constraint único: un evento no puede tener el mismo NoItem dos veces
        builder.HasIndex(x => new { x.EventoContingenciaId, x.NoItem })
            .IsUnique()
            .HasDatabaseName("IX_ContingenciaDetalles_Evento_NoItem");

        // Constraint único: una factura no puede estar en el mismo evento dos veces
        builder.HasIndex(x => new { x.EventoContingenciaId, x.FacturaElectronicaId })
            .IsUnique()
            .HasDatabaseName("IX_ContingenciaDetalles_Evento_Factura");

        // Índice en EventoContingenciaId para búsquedas
        builder.HasIndex(x => x.EventoContingenciaId)
            .HasDatabaseName("IX_ContingenciaDetalles_EventoId");

        // Índice en FacturaElectronicaId para búsquedas
        builder.HasIndex(x => x.FacturaElectronicaId)
            .HasDatabaseName("IX_ContingenciaDetalles_FacturaId");

        // ==========================================
        // TIMESTAMPS
        // ==========================================

        builder.Property(x => x.FechaCreacion)
            .HasDefaultValueSql("NOW()");
    }
}
