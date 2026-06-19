using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class OperacionEspecialDetalleConfiguration : IEntityTypeConfiguration<OperacionEspecialDetalle>
{
    public void Configure(EntityTypeBuilder<OperacionEspecialDetalle> builder)
    {
        builder.ToTable("operacion_especial_detalles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NumItem).IsRequired();
        builder.Property(x => x.CodigoGeneracionRef).HasMaxLength(36);
        builder.Property(x => x.TipoDocumento).IsRequired().HasMaxLength(2);
        builder.Property(x => x.NumDocumento).HasMaxLength(36);
        builder.Property(x => x.FechaEmisionDoc).HasColumnType("date");
        builder.Property(x => x.Cantidad).IsRequired();
        builder.Property(x => x.Descripcion).IsRequired().HasMaxLength(1500);
        builder.Property(x => x.DocDel).HasMaxLength(36);
        builder.Property(x => x.DocAl).HasMaxLength(36);
        builder.Property(x => x.PrecioUni).HasColumnType("numeric(18,8)");
        builder.Property(x => x.VentaNoSuj).HasColumnType("numeric(18,8)");
        builder.Property(x => x.VentaExenta).HasColumnType("numeric(18,8)");
        builder.Property(x => x.VentaGravada).HasColumnType("numeric(18,8)");
        builder.Property(x => x.TributosJson).HasColumnType("text");

        builder.HasOne(x => x.EventoOperacionEspecial)
            .WithMany(e => e.Detalles)
            .HasForeignKey(x => x.EventoOperacionEspecialId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.EventoOperacionEspecialId, x.NumItem })
            .IsUnique()
            .HasDatabaseName("IX_OperacionEspecialDetalles_Evento_NumItem");

        builder.HasIndex(x => x.EventoOperacionEspecialId)
            .HasDatabaseName("IX_OperacionEspecialDetalles_EventoId");

        builder.Property(x => x.FechaCreacion).HasDefaultValueSql("NOW()");
    }
}
