using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;

namespace FraFactu.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración EF Core para DteRecibido
/// </summary>
public class DteRecibidoConfiguration : IEntityTypeConfiguration<DteRecibido>
{
    public void Configure(EntityTypeBuilder<DteRecibido> builder)
    {
        builder.ToTable("dtes_recibidos");

        builder.HasKey(x => x.Id);

        // Relaciones
        builder.HasOne(x => x.Emisor)
            .WithMany()
            .HasForeignKey(x => x.EmisorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CompraExterna)
            .WithMany()
            .HasForeignKey(x => x.CompraExternaId)
            .OnDelete(DeleteBehavior.SetNull);

        // Propiedades
        builder.Property(x => x.CodigoGeneracion)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(x => x.SelloRecibido)
            .HasMaxLength(100);

        builder.Property(x => x.TipoDte)
            .IsRequired()
            .HasMaxLength(5);

        builder.Property(x => x.NumeroControl)
            .HasMaxLength(31);

        builder.Property(x => x.FechaEmision)
            .IsRequired();

        builder.Property(x => x.EmisorNit)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.EmisorNombre)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.EmisorNrc)
            .HasMaxLength(20);

        builder.Property(x => x.ReceptorNit)
            .HasMaxLength(20);

        builder.Property(x => x.ReceptorNombre)
            .HasMaxLength(300);

        builder.Property(x => x.JsonDte)
            .IsRequired();

        // Montos
        builder.Property(x => x.MontoGravado).HasPrecision(18, 2);
        builder.Property(x => x.MontoExento).HasPrecision(18, 2);
        builder.Property(x => x.MontoNoSujeto).HasPrecision(18, 2);
        builder.Property(x => x.SubTotal).HasPrecision(18, 2);
        builder.Property(x => x.IVA).HasPrecision(18, 2);
        builder.Property(x => x.Total).HasPrecision(18, 2);

        // Estado: enum persistido como string. La columna sigue siendo
        // varchar(20) y conserva los valores existentes
        // "PENDIENTE"/"VINCULADO"/"DESCARTADO" sin migrar datos.
        builder.Property(x => x.Estado)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>()
            .HasDefaultValue(EstadoDteRecibido.PENDIENTE);

        builder.Property(x => x.MotivoDescarte)
            .HasMaxLength(500);

        // Fuente de recepción: enum persistido como string. Default CORREO
        // para que las filas existentes (todas vienen de Gmail) queden correctas.
        builder.Property(x => x.FuenteRecepcion)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>()
            .HasDefaultValue(FuenteRecepcionDte.CORREO);

        // Correo
        builder.Property(x => x.EmailOrigen)
            .HasMaxLength(200);

        builder.Property(x => x.FechaCreacion)
            .HasDefaultValueSql("NOW()");

        // Índices
        builder.HasIndex(x => new { x.EmisorId, x.CodigoGeneracion })
            .IsUnique()
            .HasDatabaseName("IX_DtesRecibidos_Emisor_CodigoGeneracion");

        builder.HasIndex(x => x.EmisorNit)
            .HasDatabaseName("IX_DtesRecibidos_EmisorNit");

        builder.HasIndex(x => x.Estado)
            .HasDatabaseName("IX_DtesRecibidos_Estado");

        builder.HasIndex(x => x.FechaEmision)
            .HasDatabaseName("IX_DtesRecibidos_FechaEmision");

        builder.HasIndex(x => x.CompraExternaId)
            .HasDatabaseName("IX_DtesRecibidos_CompraExterna");
    }
}
