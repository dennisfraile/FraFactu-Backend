using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class HistorialRefinanciamientoConfiguration : IEntityTypeConfiguration<HistorialRefinanciamiento>
{
    public void Configure(EntityTypeBuilder<HistorialRefinanciamiento> builder)
    {
        builder.ToTable("historial_refinanciamiento");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Motivo).HasMaxLength(1000);
        builder.Property(h => h.PlanAnteriorJson).HasColumnType("jsonb");
        builder.Property(h => h.PlanNuevoJson).HasColumnType("jsonb");
        builder.HasOne(h => h.PlanCuotas)
            .WithMany()
            .HasForeignKey(h => h.PlanCuotasId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(h => h.Usuario)
            .WithMany()
            .HasForeignKey(h => h.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(h => h.PlanCuotasId);
    }
}
