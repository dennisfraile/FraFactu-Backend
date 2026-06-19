using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations
{
    public class CuotaConfiguration : IEntityTypeConfiguration<Cuota>
    {
        public void Configure(EntityTypeBuilder<Cuota> builder)
        {
            builder.ToTable("cuotas");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Monto).HasColumnType("numeric(18,2)");
            builder.Property(c => c.Porcentaje).HasColumnType("numeric(7,4)");
            builder.Property(c => c.InteresMora).HasColumnType("numeric(18,2)");
            builder.Property(c => c.Estado).HasConversion<int>();

            builder.HasOne(c => c.Factura)
                .WithMany()
                .HasForeignKey(c => c.FacturaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(c => new { c.PlanCuotasId, c.Numero })
                .IsUnique()
                .HasDatabaseName("IX_cuotas_Plan_Numero");

            builder.HasIndex(c => new { c.Estado, c.FechaPactada })
                .HasDatabaseName("IX_cuotas_Estado_FechaPactada");
        }
    }
}
