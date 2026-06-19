using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

/// <summary>
/// F3 (Plan inventario desde DTE): configuracion del outbox de
/// integracion con SmartInventory.
/// </summary>
public class IntegracionInventarioPendienteConfiguration : IEntityTypeConfiguration<IntegracionInventarioPendiente>
{
    public void Configure(EntityTypeBuilder<IntegracionInventarioPendiente> builder)
    {
        builder.ToTable("TBL_IntegracionInventarioPendiente");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.TipoEvento)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.PayloadJson)
            .IsRequired();

        builder.Property(p => p.Estado)
            .HasConversion<int>()
            .HasDefaultValue(Domain.Enums.EstadoIntegracionInventario.ENCOLADO)
            .HasSentinel((Domain.Enums.EstadoIntegracionInventario)(-1))
            .IsRequired();

        builder.Property(p => p.MovimientoIdExterno)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.UltimoError)
            .HasMaxLength(1000);

        builder.HasOne(p => p.Emisor)
            .WithMany()
            .HasForeignKey(p => p.EmisorId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indice del consumidor: barre ENCOLADO/FALLIDO con FechaProximoIntento <= ahora
        // ordenados por FechaCreacion. Sin este indice el polling escanea tabla completa.
        builder.HasIndex(p => new { p.Estado, p.FechaProximoIntento, p.FechaCreacion })
            .HasDatabaseName("IX_IntegracionInventario_Estado_ProximoIntento");

        // Para auditoria + dedupe debugging desde la UI/queries del admin.
        builder.HasIndex(p => p.MovimientoIdExterno)
            .HasDatabaseName("IX_IntegracionInventario_MovimientoIdExterno");
    }
}
