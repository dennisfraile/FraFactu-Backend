using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations
{
    public class SaldoDteConfiguration : IEntityTypeConfiguration<SaldoDte>
    {
        public void Configure(EntityTypeBuilder<SaldoDte> builder)
        {
            builder.ToTable("saldo_dte");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.DteCodigoGeneracion)
                .IsRequired()
                .HasMaxLength(36);

            builder.Property(s => s.TipoDte)
                .IsRequired()
                .HasMaxLength(2);

            builder.Property(s => s.MontoOriginal)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(s => s.MontoAcreditado)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0m);

            builder.Property(s => s.NceCount)
                .HasDefaultValue(0);

            // Ignorar propiedad calculada
            builder.Ignore(s => s.SaldoDisponible);

            // Índice único en CodigoGeneracion del DTE original
            builder.HasIndex(s => s.DteCodigoGeneracion).IsUnique();

            // Índice por emisor para búsquedas
            builder.HasIndex(s => s.EmisorId);

            // Relación con Emisor
            builder.HasOne(s => s.Emisor)
                .WithMany()
                .HasForeignKey(s => s.EmisorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
