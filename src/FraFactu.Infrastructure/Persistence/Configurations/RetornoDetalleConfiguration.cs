using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class RetornoDetalleConfiguration : IEntityTypeConfiguration<RetornoDetalle>
{
    public void Configure(EntityTypeBuilder<RetornoDetalle> builder)
    {
        builder.ToTable("retorno_detalles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NumItem).IsRequired();
        builder.Property(x => x.TipoItem).IsRequired();
        builder.Property(x => x.CodigoGeneracion).IsRequired().HasMaxLength(36);
        builder.Property(x => x.Cantidad).HasColumnType("numeric(18,8)");
        builder.Property(x => x.PrecioUni).HasColumnType("numeric(18,8)");
        builder.Property(x => x.Descripcion).IsRequired().HasMaxLength(1500);
        builder.Property(x => x.Codigo).HasMaxLength(25);
        builder.Property(x => x.UniMedida).IsRequired();
        builder.Property(x => x.MontoDescu).HasColumnType("numeric(18,8)");
        builder.Property(x => x.CodTributo).HasMaxLength(2);
        builder.Property(x => x.VentaNoSuj).HasColumnType("numeric(18,8)");
        builder.Property(x => x.VentaExenta).HasColumnType("numeric(18,8)");
        builder.Property(x => x.VentaGravada).HasColumnType("numeric(18,8)");
        builder.Property(x => x.Compra).HasColumnType("numeric(18,8)");
        builder.Property(x => x.TributosJson).HasColumnType("text");
        builder.Property(x => x.Psv).HasColumnType("numeric(18,8)");
        builder.Property(x => x.IvaItem).HasColumnType("numeric(18,8)");
        builder.Property(x => x.NoGravado).HasColumnType("numeric(18,8)");
        builder.Property(x => x.Seguro).HasColumnType("numeric(18,8)");
        builder.Property(x => x.Flete).HasColumnType("numeric(18,8)");
        builder.Property(x => x.IvaRete).HasColumnType("numeric(18,2)");
        builder.Property(x => x.ReteRenta).HasColumnType("numeric(18,2)");

        builder.HasOne(x => x.EventoRetorno)
            .WithMany(e => e.Detalles)
            .HasForeignKey(x => x.EventoRetornoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.EventoRetornoId, x.NumItem })
            .IsUnique()
            .HasDatabaseName("IX_RetornoDetalles_Evento_NumItem");

        builder.HasIndex(x => x.EventoRetornoId)
            .HasDatabaseName("IX_RetornoDetalles_EventoId");

        builder.Property(x => x.FechaCreacion).HasDefaultValueSql("NOW()");
    }
}
