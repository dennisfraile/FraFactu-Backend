using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class EventoContingenciaConfiguration : IEntityTypeConfiguration<EventoContingencia>
{
    public void Configure(EntityTypeBuilder<EventoContingencia> builder)
    {
        // Nombre de tabla
        builder.ToTable("eventos_contingencia");

        // Clave primaria
        builder.HasKey(x => x.Id);

        // ==========================================
        // PROPIEDADES - IDENTIFICACIÓN
        // ==========================================

        builder.Property(x => x.Version)
            .IsRequired()
            .HasDefaultValue(3);

        builder.Property(x => x.Ambiente)
            .IsRequired()
            .HasMaxLength(2)
            .HasDefaultValue("00");

        builder.Property(x => x.CodigoGeneracion)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(x => x.FechaTransmision)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.HoraTransmision)
            .IsRequired()
            .HasColumnType("time");

        // ==========================================
        // PROPIEDADES - RESPONSABLE
        // ==========================================

        builder.Property(x => x.NombreResponsable)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.NumeroDocResponsable)
            .IsRequired()
            .HasMaxLength(25);

        builder.Property(x => x.CodigoEstablecimientoMH)
            .HasMaxLength(4);

        builder.Property(x => x.CodigoPuntoVenta)
            .HasMaxLength(15);

        // ==========================================
        // PROPIEDADES - MOTIVO
        // ==========================================

        builder.Property(x => x.FechaInicioContingencia)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.FechaFinContingencia)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.HoraInicioContingencia)
            .IsRequired()
            .HasColumnType("time");

        builder.Property(x => x.HoraFinContingencia)
            .IsRequired()
            .HasColumnType("time");

        builder.Property(x => x.TipoContingencia)
            .IsRequired();

        builder.Property(x => x.MotivoContingencia)
            .HasMaxLength(500);

        // ==========================================
        // PROPIEDADES - RESPUESTA MH
        // ==========================================

        builder.Property(x => x.SelloRecibido)
            .HasMaxLength(50);

        builder.Property(x => x.EstadoHacienda)
            .HasMaxLength(50);

        builder.Property(x => x.JsonEvento)
            .HasColumnType("text");

        builder.Property(x => x.JsonRespuesta)
            .HasColumnType("text");

        // ==========================================
        // RELACIONES
        // ==========================================

        // Relación con Emisor
        builder.HasOne(x => x.Emisor)
            .WithMany()
            .HasForeignKey(x => x.EmisorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relación con CatTipoDocumento (TipoDocResponsable)
        builder.HasOne(x => x.TipoDocResponsable)
            .WithMany()
            .HasForeignKey(x => x.CatTipoDocResponsableId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relación con CatTipoEstablecimiento
        builder.HasOne(x => x.TipoEstablecimiento)
            .WithMany()
            .HasForeignKey(x => x.CatTipoEstablecimientoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relación con Detalles (uno a muchos)
        builder.HasMany(x => x.Detalles)
            .WithOne(d => d.EventoContingencia)
            .HasForeignKey(d => d.EventoContingenciaId)
            .OnDelete(DeleteBehavior.Cascade);

        // ==========================================
        // ÍNDICES
        // ==========================================

        // Índice único en CodigoGeneracion
        builder.HasIndex(x => new { x.CodigoGeneracion, x.EmisorId })
            .IsUnique()
            .HasDatabaseName("IX_EventosContingencia_CodigoGeneracion_EmisorId");

        // Índice en EmisorId para búsquedas
        builder.HasIndex(x => x.EmisorId)
            .HasDatabaseName("IX_EventosContingencia_Emisor");

        // Índice en FechaTransmision para reportes
        builder.HasIndex(x => x.FechaTransmision)
            .HasDatabaseName("IX_EventosContingencia_FechaTransmision");

        // Índice compuesto Emisor + Fecha para consultas optimizadas
        builder.HasIndex(x => new { x.EmisorId, x.FechaTransmision })
            .HasDatabaseName("IX_EventosContingencia_Emisor_Fecha");

        // ==========================================
        // TIMESTAMPS
        // ==========================================

        builder.Property(x => x.FechaCreacion)
            .HasDefaultValueSql("NOW()");
    }
}
