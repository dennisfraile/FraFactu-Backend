using FraFactu.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class CatFormaPagoConfig : IEntityTypeConfiguration<CatFormaPago>
{
    public void Configure(EntityTypeBuilder<CatFormaPago> builder)
    {
        builder.ToTable("cat_forma_pago");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Codigo).HasMaxLength(2); 
        builder.Property(x => x.Valor).HasMaxLength(100).IsRequired();

        builder.HasData(
            new CatFormaPago {Id = 1, Codigo = "01", Valor = "Billetes y monedas" },
            new CatFormaPago {Id = 2, Codigo = "02", Valor = "Tarjeta Débito" },
            new CatFormaPago {Id = 3, Codigo = "03", Valor = "Tarjeta Crédito" },
            new CatFormaPago {Id = 4, Codigo = "04", Valor = "Cheque" },
            new CatFormaPago {Id = 5, Codigo = "05", Valor = "Transferencia-Depósito Bancario" },
            new CatFormaPago {Id = 6, Codigo = "08", Valor = "Dinero electrónico" },
            new CatFormaPago {Id = 7, Codigo = "09", Valor = "Monedero electrónico" },
            new CatFormaPago {Id = 8, Codigo = "11", Valor = "Bitcoin" },
            new CatFormaPago {Id = 9, Codigo = "12", Valor = "Otras Criptomonedas" },
            new CatFormaPago {Id = 10, Codigo = "13", Valor = "Cuentas por pagar del receptor" },
            new CatFormaPago {Id = 11, Codigo = "14", Valor = "Giro bancario" },
            new CatFormaPago {Id = 12, Codigo = "99", Valor = "Otros" } 
        );
    }
}