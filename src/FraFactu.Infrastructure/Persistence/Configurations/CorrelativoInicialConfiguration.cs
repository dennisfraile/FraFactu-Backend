using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class CorrelativoInicialConfiguration : IEntityTypeConfiguration<CorrelativoInicial>
{
    public void Configure(EntityTypeBuilder<CorrelativoInicial> builder)
    {
        builder.ToTable("correlativos_iniciales");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.TipoDte).HasMaxLength(2).IsRequired();
        builder.Property(c => c.Ambiente).HasMaxLength(2).IsRequired();
        builder.HasOne(c => c.Emisor)
            .WithMany()
            .HasForeignKey(c => c.EmisorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(c => new { c.EmisorId, c.TipoDte, c.Anio, c.Ambiente }).IsUnique();
    }
}
