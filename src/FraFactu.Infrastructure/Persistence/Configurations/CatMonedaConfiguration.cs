using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities.Catalogos;

namespace FraFactu.Infrastructure.Persistence.Configurations
{
    public class CatMonedaConfiguration : IEntityTypeConfiguration<CatMoneda>
    {
        public void Configure(EntityTypeBuilder<CatMoneda> builder)
        {
            builder.ToTable("CAT_Moneda");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Codigo)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(m => m.Valor)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(m => m.Simbolo)
                .HasMaxLength(5);

            // Seed Data - Monedas estándar
            builder.HasData(
                new CatMoneda
                {
                    Id = 1,
                    Codigo = "USD",
                    Valor = "Dólar estadounidense",
                    Simbolo = "$"
                },
                new CatMoneda
                {
                    Id = 2,
                    Codigo = "EUR",
                    Valor = "Euro",
                    Simbolo = "€"
                }
            );

            // Índice único por código
            builder.HasIndex(m => m.Codigo)
                .HasDatabaseName("IX_CatMoneda_Codigo")
                .IsUnique();
        }
    }
}
