using FraFactu.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class CatMunicipioConfig : IEntityTypeConfiguration<CatMunicipio>
{
    public void Configure(EntityTypeBuilder<CatMunicipio> builder)
    {
        builder.ToTable("cat_municipio");
        
       
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Codigo).HasMaxLength(4).IsRequired(); 
        builder.Property(x => x.Valor).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CodigoDepartamento).HasMaxLength(2).IsRequired();

        // NOTA: Asignamos IDs secuenciales (1, 2, 3...) para el Seed Data
        builder.HasData(
            // --- AHUACHAPAN (01) ---
            new CatMunicipio { Id = 1, Codigo = "13", Valor = "AHUACHAPAN NORTE", CodigoDepartamento = "01" },
            new CatMunicipio { Id = 2, Codigo = "14", Valor = "AHUACHAPAN CENTRO", CodigoDepartamento = "01" },
            new CatMunicipio { Id = 3, Codigo = "15", Valor = "AHUACHAPAN SUR", CodigoDepartamento = "01" },

            // --- SANTA ANA (02) ---
            new CatMunicipio { Id = 4, Codigo = "14", Valor = "SANTA ANA NORTE", CodigoDepartamento = "02" },
            new CatMunicipio { Id = 5, Codigo = "15", Valor = "SANTA ANA CENTRO", CodigoDepartamento = "02" },
            new CatMunicipio { Id = 6, Codigo = "16", Valor = "SANTA ANA ESTE", CodigoDepartamento = "02" },
            new CatMunicipio { Id = 7, Codigo = "17", Valor = "SANTA ANA OESTE", CodigoDepartamento = "02" },

            // --- SONSONATE (03) ---
            new CatMunicipio { Id = 8, Codigo = "17", Valor = "SONSONATE NORTE", CodigoDepartamento = "03" },
            new CatMunicipio { Id = 9, Codigo = "18", Valor = "SONSONATE CENTRO", CodigoDepartamento = "03" },
            new CatMunicipio { Id = 10, Codigo = "19", Valor = "SONSONATE ESTE", CodigoDepartamento = "03" },
            new CatMunicipio { Id = 11, Codigo = "20", Valor = "SONSONATE OESTE", CodigoDepartamento = "03" },

            // --- CHALATENANGO (04) ---
            new CatMunicipio { Id = 12, Codigo = "34", Valor = "CHALATENANGO NORTE", CodigoDepartamento = "04" },
            new CatMunicipio { Id = 13, Codigo = "35", Valor = "CHALATENANGO CENTRO", CodigoDepartamento = "04" },
            new CatMunicipio { Id = 14, Codigo = "36", Valor = "CHALATENANGO SUR", CodigoDepartamento = "04" },

            // --- LA LIBERTAD (05) ---
            new CatMunicipio { Id = 15, Codigo = "23", Valor = "LA LIBERTAD NORTE", CodigoDepartamento = "05" },
            new CatMunicipio { Id = 16, Codigo = "24", Valor = "LA LIBERTAD CENTRO", CodigoDepartamento = "05" },
            new CatMunicipio { Id = 17, Codigo = "25", Valor = "LA LIBERTAD OESTE", CodigoDepartamento = "05" },
            new CatMunicipio { Id = 18, Codigo = "26", Valor = "LA LIBERTAD ESTE", CodigoDepartamento = "05" },
            new CatMunicipio { Id = 19, Codigo = "27", Valor = "LA LIBERTAD COSTA", CodigoDepartamento = "05" },
            new CatMunicipio { Id = 20, Codigo = "28", Valor = "LA LIBERTAD SUR", CodigoDepartamento = "05" },

            // --- SAN SALVADOR (06) ---
            new CatMunicipio { Id = 21, Codigo = "20", Valor = "SAN SALVADOR NORTE", CodigoDepartamento = "06" },
            new CatMunicipio { Id = 22, Codigo = "21", Valor = "SAN SALVADOR OESTE", CodigoDepartamento = "06" },
            new CatMunicipio { Id = 23, Codigo = "22", Valor = "SAN SALVADOR ESTE", CodigoDepartamento = "06" },
            new CatMunicipio { Id = 24, Codigo = "23", Valor = "SAN SALVADOR CENTRO", CodigoDepartamento = "06" },
            new CatMunicipio { Id = 25, Codigo = "24", Valor = "SAN SALVADOR SUR", CodigoDepartamento = "06" },

            // --- CUSCATLAN (07) ---
            new CatMunicipio { Id = 26, Codigo = "17", Valor = "CUSCATLAN NORTE", CodigoDepartamento = "07" },
            new CatMunicipio { Id = 27, Codigo = "18", Valor = "CUSCATLAN SUR", CodigoDepartamento = "07" },

            // --- LA PAZ (08) ---
            new CatMunicipio { Id = 28, Codigo = "23", Valor = "LA PAZ OESTE", CodigoDepartamento = "08" },
            new CatMunicipio { Id = 29, Codigo = "24", Valor = "LA PAZ CENTRO", CodigoDepartamento = "08" },
            new CatMunicipio { Id = 30, Codigo = "25", Valor = "LA PAZ ESTE", CodigoDepartamento = "08" },

            // --- CABAÑAS (09) ---
            new CatMunicipio { Id = 31, Codigo = "10", Valor = "CABAÑAS OESTE", CodigoDepartamento = "09" },
            new CatMunicipio { Id = 32, Codigo = "11", Valor = "CABAÑAS ESTE", CodigoDepartamento = "09" },

            // --- SAN VICENTE (10) ---
            new CatMunicipio { Id = 33, Codigo = "14", Valor = "SAN VICENTE NORTE", CodigoDepartamento = "10" },
            new CatMunicipio { Id = 34, Codigo = "15", Valor = "SAN VICENTE SUR", CodigoDepartamento = "10" },

            // --- USULUTAN (11) ---
            new CatMunicipio { Id = 35, Codigo = "24", Valor = "USULUTAN NORTE", CodigoDepartamento = "11" },
            new CatMunicipio { Id = 36, Codigo = "25", Valor = "USULUTAN ESTE", CodigoDepartamento = "11" },
            new CatMunicipio { Id = 37, Codigo = "26", Valor = "USULUTAN OESTE", CodigoDepartamento = "11" },

            // --- SAN MIGUEL (12) ---
            new CatMunicipio { Id = 38, Codigo = "21", Valor = "SAN MIGUEL NORTE", CodigoDepartamento = "12" },
            new CatMunicipio { Id = 39, Codigo = "22", Valor = "SAN MIGUEL CENTRO", CodigoDepartamento = "12" },
            new CatMunicipio { Id = 40, Codigo = "23", Valor = "SAN MIGUEL OESTE", CodigoDepartamento = "12" },

            // --- MORAZAN (13) ---
            new CatMunicipio { Id = 41, Codigo = "27", Valor = "MORAZAN NORTE", CodigoDepartamento = "13" },
            new CatMunicipio { Id = 42, Codigo = "28", Valor = "MORAZAN SUR", CodigoDepartamento = "13" },

            // --- LA UNION (14) ---
            new CatMunicipio { Id = 43, Codigo = "19", Valor = "LA UNION NORTE", CodigoDepartamento = "14" },
            new CatMunicipio { Id = 44, Codigo = "20", Valor = "LA UNION SUR", CodigoDepartamento = "14" },

            // --- EXTRANJEROS ---
            new CatMunicipio { Id = 45, Codigo = "00", Valor = "Otro (Para extranjeros)", CodigoDepartamento = "00" }
        );
    }
}