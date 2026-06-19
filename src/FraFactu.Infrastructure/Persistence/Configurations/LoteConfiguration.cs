using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class LoteConfiguration : IEntityTypeConfiguration<Lote>
{
    public void Configure(EntityTypeBuilder<Lote> builder)
    {
        // Nombre de tabla
        builder.ToTable("lotes");

        // Clave primaria
        builder.HasKey(x => x.Id);

        // ==========================================
        // PROPIEDADES
        // ==========================================

        builder.Property(x => x.CodigoLote)
            .IsRequired();

        builder.HasIndex(x => new { x.CodigoLote, x.EmisorId })
            .IsUnique()
            .HasDatabaseName("IX_Lotes_CodigoLote_EmisorId");

        builder.Property(x => x.TotalDtes)
            .IsRequired();

        builder.Property(x => x.Ambiente)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(x => x.EsContingencia)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.Estado)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Pendiente");

        builder.Property(x => x.CodigoRespuesta)
            .HasMaxLength(10);

        builder.Property(x => x.DescripcionRespuesta)
            .HasMaxLength(500);

        builder.Property(x => x.JsonRespuesta)
            .HasColumnType("text");

        builder.Property(x => x.JsonEnviado)
            .HasColumnType("text");

        builder.Property(x => x.TotalAprobados)
            .HasDefaultValue(0);

        builder.Property(x => x.TotalRechazados)
            .HasDefaultValue(0);

        builder.Property(x => x.TotalPendientes)
            .HasDefaultValue(0);

        builder.Property(x => x.CreadoPor)
            .HasMaxLength(100);

        // ==========================================
        // RELACIONES
        // ==========================================

        builder.HasOne(x => x.Emisor)
            .WithMany()
            .HasForeignKey(x => x.EmisorId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==========================================
        // ÍNDICES
        // ==========================================

        builder.HasIndex(x => x.EmisorId)
            .HasDatabaseName("IX_Lotes_Emisor");

        builder.HasIndex(x => x.Estado)
            .HasDatabaseName("IX_Lotes_Estado");

        builder.HasIndex(x => x.FechaCreacion)
            .HasDatabaseName("IX_Lotes_FechaCreacion");

        // ==========================================
        // TIMESTAMPS
        // ==========================================

        builder.Property(x => x.FechaCreacion)
            .HasDefaultValueSql("NOW()");
    }
}
