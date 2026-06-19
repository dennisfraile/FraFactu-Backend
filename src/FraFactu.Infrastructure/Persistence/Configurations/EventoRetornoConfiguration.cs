using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class EventoRetornoConfiguration : IEntityTypeConfiguration<EventoRetorno>
{
    public void Configure(EntityTypeBuilder<EventoRetorno> builder)
    {
        builder.ToTable("eventos_retorno");

        builder.HasKey(x => x.Id);

        // Identificación
        builder.Property(x => x.Version).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.Ambiente).IsRequired().HasMaxLength(2).HasDefaultValue("00");
        builder.Property(x => x.TipoModelo).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.TipoOperacion).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.TipoEvento).IsRequired().HasMaxLength(2).HasDefaultValue("18");
        builder.Property(x => x.MotivoContin).HasMaxLength(500);
        builder.Property(x => x.CodigoGeneracion).IsRequired().HasMaxLength(36);
        builder.Property(x => x.FechaEmision).IsRequired().HasColumnType("date");
        builder.Property(x => x.HoraEmision).IsRequired().HasColumnType("time");
        builder.Property(x => x.Fusion).HasMaxLength(14);
        builder.Property(x => x.TipoMoneda).IsRequired().HasMaxLength(3).HasDefaultValue("USD");

        // Emisor (exportación)
        builder.Property(x => x.CodEstableMH).HasMaxLength(4);
        builder.Property(x => x.CodEstable).HasMaxLength(10);
        builder.Property(x => x.CodPuntoVentaMH).HasMaxLength(4);
        builder.Property(x => x.CodPuntoVenta).HasMaxLength(15);
        builder.Property(x => x.RecintoFiscal).HasMaxLength(2);
        builder.Property(x => x.TipoRegimen).HasMaxLength(4);
        builder.Property(x => x.Regimen).HasMaxLength(13);

        // Sub-estructuras JSON
        builder.Property(x => x.DocumentoRelacionadoJson).HasColumnType("text");
        builder.Property(x => x.DocumentoReceptorJson).HasColumnType("text");
        builder.Property(x => x.VentaTerceroJson).HasColumnType("text");
        builder.Property(x => x.CompraTerceroJson).HasColumnType("text");
        builder.Property(x => x.ResumenTributosJson).HasColumnType("text");
        builder.Property(x => x.ApendiceJson).HasColumnType("text");

        // Resumen (totales)
        builder.Property(x => x.TotalNoSuj).HasColumnType("numeric(18,2)");
        builder.Property(x => x.TotalExenta).HasColumnType("numeric(18,2)");
        builder.Property(x => x.TotalGravada).HasColumnType("numeric(18,2)");
        builder.Property(x => x.TotalCompraExcluidos).HasColumnType("numeric(18,2)");
        builder.Property(x => x.SubTotalVentas).HasColumnType("numeric(18,2)");
        builder.Property(x => x.TotalSeguro).HasColumnType("numeric(18,2)");
        builder.Property(x => x.TotalFlete).HasColumnType("numeric(18,2)");
        builder.Property(x => x.MontoTotalOperacion).HasColumnType("numeric(18,2)");
        builder.Property(x => x.IvaRete).HasColumnType("numeric(18,2)");
        builder.Property(x => x.ReteRenta).HasColumnType("numeric(18,2)");
        builder.Property(x => x.TotalNoGravado).HasColumnType("numeric(18,2)");
        builder.Property(x => x.TotalPagar).HasColumnType("numeric(18,2)");
        builder.Property(x => x.TotalLetras).HasMaxLength(200);
        builder.Property(x => x.TotalNoOnerosas).HasColumnType("numeric(18,2)");
        builder.Property(x => x.TotalIva).HasColumnType("numeric(18,2)");
        builder.Property(x => x.SaldoFavor).HasColumnType("numeric(18,2)");

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
            .WithOne(d => d.EventoRetorno)
            .HasForeignKey(d => d.EventoRetornoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índices
        builder.HasIndex(x => new { x.CodigoGeneracion, x.EmisorId })
            .IsUnique()
            .HasDatabaseName("IX_EventosRetorno_CodigoGeneracion_EmisorId");

        builder.HasIndex(x => x.EmisorId)
            .HasDatabaseName("IX_EventosRetorno_Emisor");

        builder.HasIndex(x => x.FechaEmision)
            .HasDatabaseName("IX_EventosRetorno_FechaEmision");

        builder.Property(x => x.FechaCreacion).HasDefaultValueSql("NOW()");
    }
}
