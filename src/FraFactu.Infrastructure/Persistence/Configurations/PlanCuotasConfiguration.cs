using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations
{
    public class PlanCuotasConfiguration : IEntityTypeConfiguration<PlanCuotas>
    {
        public void Configure(EntityTypeBuilder<PlanCuotas> builder)
        {
            builder.ToTable("plan_cuotas");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.MontoTotal).HasColumnType("numeric(18,2)");
            builder.Property(p => p.MontoPagado).HasColumnType("numeric(18,2)");
            builder.Property(p => p.SaldoAdeudado).HasColumnType("numeric(18,2)");
            builder.Property(p => p.EstadoCobro).HasConversion<int>();
            builder.Property(p => p.VentaSnapshotJson).HasColumnType("jsonb");
            builder.Property(p => p.Observaciones).HasMaxLength(1000);

            builder.HasOne(p => p.Emisor)
                .WithMany()
                .HasForeignKey(p => p.EmisorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Receptor)
                .WithMany()
                .HasForeignKey(p => p.ReceptorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(p => p.Cuotas)
                .WithOne(c => c.PlanCuotas)
                .HasForeignKey(c => c.PlanCuotasId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(p => new { p.EmisorId, p.EstadoCobro })
                .HasDatabaseName("IX_plan_cuotas_Emisor_Estado");
        }
    }
}
