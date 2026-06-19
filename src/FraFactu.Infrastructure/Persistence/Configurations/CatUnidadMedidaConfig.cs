using FraFactu.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class CatUnidadMedidaConfig : IEntityTypeConfiguration<CatUnidadMedida>
{
    public void Configure(EntityTypeBuilder<CatUnidadMedida> builder)
    {
        builder.ToTable("cat_uni_medida");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Codigo).HasMaxLength(2); 
        builder.Property(x => x.Valor).HasMaxLength(100).IsRequired();

        builder.HasData(
            // Longitud
            new CatUnidadMedida {Id = 1, Codigo = "01", Valor = "Metro" },
            new CatUnidadMedida {Id = 2, Codigo = "02", Valor = "Yarda" },
            new CatUnidadMedida {Id = 3, Codigo = "06", Valor = "Milímetro" }, 
            
            // Área
            new CatUnidadMedida {Id = 4, Codigo = "09", Valor = "Kilómetro cuadrado" },
            new CatUnidadMedida {Id = 5, Codigo = "10", Valor = "Hectárea" },
            new CatUnidadMedida {Id = 6, Codigo = "13", Valor = "Metro cuadrado" },
            new CatUnidadMedida {Id = 7, Codigo = "15", Valor = "Vara cuadrada" },
            
            // Volumen
            new CatUnidadMedida {Id = 8, Codigo = "18", Valor = "Metro cúbico" },
            new CatUnidadMedida {Id = 9, Codigo = "20", Valor = "Barril" },
            new CatUnidadMedida {Id = 10, Codigo = "22", Valor = "Galón" },
            new CatUnidadMedida {Id = 11, Codigo = "23", Valor = "Litro" },
            new CatUnidadMedida {Id = 12, Codigo = "24", Valor = "Botella" },
            new CatUnidadMedida {Id = 13, Codigo = "26", Valor = "Mililitro" },
            
            // Peso / Masa
            new CatUnidadMedida {Id = 14, Codigo = "30", Valor = "Tonelada" },
            new CatUnidadMedida {Id = 15, Codigo = "32", Valor = "Quintal" },
            new CatUnidadMedida {Id = 16, Codigo = "33", Valor = "Arroba" },
            new CatUnidadMedida {Id = 17, Codigo = "34", Valor = "Kilogramo" },
            new CatUnidadMedida {Id = 18, Codigo = "36", Valor = "Libra" },
            new CatUnidadMedida {Id = 19, Codigo = "37", Valor = "Onza troy" },
            new CatUnidadMedida {Id = 20, Codigo = "38", Valor = "Onza" },
            new CatUnidadMedida {Id = 21, Codigo = "39", Valor = "Gramo" },
            new CatUnidadMedida {Id = 22, Codigo = "40", Valor = "Miligramo" },
            
            // Electricidad y Energía
            new CatUnidadMedida {Id = 23, Codigo = "42", Valor = "Megawatt" },
            new CatUnidadMedida {Id = 24, Codigo = "43", Valor = "Kilowatt" },
            new CatUnidadMedida {Id = 25, Codigo = "44", Valor = "Watt" },
            new CatUnidadMedida {Id = 26, Codigo = "45", Valor = "Megavoltio-amperio" },
            new CatUnidadMedida {Id = 27, Codigo = "46", Valor = "Kilovoltio-amperio" },
            new CatUnidadMedida {Id = 28, Codigo = "47", Valor = "Voltio-amperio" },
            new CatUnidadMedida {Id = 29, Codigo = "49", Valor = "Gigawatt-hora" },
            new CatUnidadMedida {Id = 30, Codigo = "50", Valor = "Megawatt-hora" },
            new CatUnidadMedida {Id = 31, Codigo = "51", Valor = "Kilowatt-hora" },
            new CatUnidadMedida {Id = 32, Codigo = "52", Valor = "Watt-hora" },
            new CatUnidadMedida {Id = 33, Codigo = "53", Valor = "Kilovoltio" },
            new CatUnidadMedida {Id = 34, Codigo = "54", Valor = "Voltio" },
            
            // Conteo / Unidades
            new CatUnidadMedida {Id = 35, Codigo = "55", Valor = "Millar" },
            new CatUnidadMedida {Id = 36, Codigo = "56", Valor = "Medio millar" },
            new CatUnidadMedida {Id = 37, Codigo = "57", Valor = "Ciento" },
            new CatUnidadMedida {Id = 38, Codigo = "58", Valor = "Docena" },
            new CatUnidadMedida {Id = 39, Codigo = "59", Valor = "Unidad" },
            
            // Otros
            new CatUnidadMedida {Id = 40, Codigo = "99", Valor = "Otra" }
        );
    }
}