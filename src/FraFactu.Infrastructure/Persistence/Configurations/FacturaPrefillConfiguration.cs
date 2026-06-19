using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuración para la entidad <see cref="FacturaPrefill"/>.
    /// Tabla "FacturaPrefills" — borrador temporal que SmartCare deposita para que
    /// la UI de Smartix abra el wizard pre-cargado.
    /// </summary>
    public class FacturaPrefillConfiguration : IEntityTypeConfiguration<FacturaPrefill>
    {
        public void Configure(EntityTypeBuilder<FacturaPrefill> builder)
        {
            builder.ToTable("FacturaPrefills");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.CorrelationId)
                .IsRequired()
                .HasMaxLength(64);

            builder.HasIndex(p => p.CorrelationId)
                .IsUnique()
                .HasDatabaseName("IX_FacturaPrefills_CorrelationId");

            builder.Property(p => p.TipoDte)
                .IsRequired()
                .HasMaxLength(2);

            // Snapshots JSON — text para máxima compatibilidad (Postgres + InMemory).
            builder.Property(p => p.ReceptorJson)
                .HasColumnType("text")
                .IsRequired();

            builder.Property(p => p.LineasJson)
                .HasColumnType("text")
                .IsRequired();

            builder.Property(p => p.Observaciones)
                .HasMaxLength(3000);

            builder.Property(p => p.FormaPagoSugerida)
                .HasMaxLength(8);

            builder.Property(p => p.SmartCareWebhookUrl)
                .HasMaxLength(500)
                .IsRequired();

            // Ciclo de vida
            builder.Property(p => p.ExpiresAt).IsRequired();

            // Relaciones
            builder.HasOne(p => p.Emisor)
                .WithMany()
                .HasForeignKey(p => p.EmisorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.ConsumedFactura)
                .WithMany()
                .HasForeignKey(p => p.ConsumedFacturaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Índices auxiliares
            builder.HasIndex(p => p.ExpiresAt)
                .HasDatabaseName("IX_FacturaPrefills_ExpiresAt");
        }
    }
}
