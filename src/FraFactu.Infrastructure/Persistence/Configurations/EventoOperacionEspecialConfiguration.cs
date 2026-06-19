using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class EventoOperacionEspecialConfiguration : IEntityTypeConfiguration<EventoOperacionEspecial>
{
    public void Configure(EntityTypeBuilder<EventoOperacionEspecial> builder)
    {
        builder.ToTable("eventos_operacion_especial");

        builder.HasKey(x => x.Id);

        // Identificación
        builder.Property(x => x.Version).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.Ambiente).IsRequired().HasMaxLength(2).HasDefaultValue("00");
        builder.Property(x => x.TipoModelo).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.TipoOperacion).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.TipoEvento).IsRequired().HasMaxLength(2).HasDefaultValue("17");
        builder.Property(x => x.TipoMoneda).IsRequired().HasMaxLength(3).HasDefaultValue("USD");
        builder.Property(x => x.CodigoGeneracion).IsRequired().HasMaxLength(36);
        builder.Property(x => x.FechaEmision).IsRequired().HasColumnType("date");
        builder.Property(x => x.HoraEmision).IsRequired().HasColumnType("time");

        // Resumen
        builder.Property(x => x.TotalNoSuj).HasColumnType("numeric(18,2)");
        builder.Property(x => x.TotalExenta).HasColumnType("numeric(18,2)");
        builder.Property(x => x.TotalGravada).HasColumnType("numeric(18,2)");
        builder.Property(x => x.SubTotal).HasColumnType("numeric(18,2)");
        builder.Property(x => x.Total).HasColumnType("numeric(18,2)");
        builder.Property(x => x.TotalLetras).HasMaxLength(200);
        builder.Property(x => x.ResumenTributosJson).HasColumnType("text");
        builder.Property(x => x.ApendiceJson).HasColumnType("text");

        // Respuesta MH
        builder.Property(x => x.SelloRecibido).HasMaxLength(50);
        builder.Property(x => x.EstadoHacienda).HasMaxLength(50);
        builder.Property(x => x.JsonEvento).HasColumnType("text");
        builder.Property(x => x.JsonRespuesta).HasColumnType("text");

        // Relaciones
        builder.HasOne(x => x.Emisor)
            .WithMany()
            .HasForeignKey(x => x.EmisorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Detalles)
            .WithOne(d => d.EventoOperacionEspecial)
            .HasForeignKey(d => d.EventoOperacionEspecialId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índices
        builder.HasIndex(x => new { x.CodigoGeneracion, x.EmisorId })
            .IsUnique()
            .HasDatabaseName("IX_EventosOperacionEspecial_CodigoGeneracion_EmisorId");

        builder.HasIndex(x => x.EmisorId)
            .HasDatabaseName("IX_EventosOperacionEspecial_Emisor");

        builder.HasIndex(x => x.FechaEmision)
            .HasDatabaseName("IX_EventosOperacionEspecial_FechaEmision");

        builder.Property(x => x.FechaCreacion).HasDefaultValueSql("NOW()");
    }
}
