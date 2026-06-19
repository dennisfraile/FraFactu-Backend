using FraFactu.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class CatTributoConfig : IEntityTypeConfiguration<CatTributo>
{
    public void Configure(EntityTypeBuilder<CatTributo> builder)
    {
        builder.ToTable("cat_tributos");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Codigo).HasMaxLength(4).IsRequired();
        builder.Property(x => x.Valor).HasMaxLength(250).IsRequired();
        builder.Property(x => x.DescripcionCorta).HasMaxLength(50);
        builder.Property(x => x.Seccion).IsRequired(); 

        builder.HasData(
            // ==========================================================
            // SECCIÓN 1: TRIBUTOS EN RESUMEN DEL DTE
            // ==========================================================
            new CatTributo { Id = 1, Seccion = 1, Codigo = "20", Valor = "Impuesto al Valor Agregado 13%", DescripcionCorta = "IVA", EsValorPorcentual = true },
            new CatTributo { Id = 2, Seccion = 1, Codigo = "C3", Valor = "Impuesto al Valor Agregado (exportaciones) 0%", DescripcionCorta = "IVA Exp.", EsValorPorcentual = true },
            new CatTributo { Id = 3, Seccion = 1, Codigo = "59", Valor = "Turismo: por alojamiento (5%)", DescripcionCorta = "Turismo", EsValorPorcentual = true },
            new CatTributo { Id = 4, Seccion = 1, Codigo = "71", Valor = "Turismo: salida del país por vía aérea $7.00", DescripcionCorta = "Turismo Salida", EsValorPorcentual = false },
            new CatTributo { Id = 5, Seccion = 1, Codigo = "D1", Valor = "FOVIAL ($0.20 Ctvs. por galón)", DescripcionCorta = "FOVIAL", EsValorPorcentual = false },
            new CatTributo { Id = 6, Seccion = 1, Codigo = "C8", Valor = "COTRANS ($0.10 Ctvs. por galón)", DescripcionCorta = "COTRANS", EsValorPorcentual = false },
            new CatTributo { Id = 7, Seccion = 1, Codigo = "D5", Valor = "Otras tasas casos especiales", DescripcionCorta = "Otras Tasas", EsValorPorcentual = false },
            new CatTributo { Id = 8, Seccion = 1, Codigo = "D4", Valor = "Otros impuestos casos especiales", DescripcionCorta = "Otros Impuestos", EsValorPorcentual = false },

            // ==========================================================
            // SECCIÓN 2: TRIBUTOS EN CUERPO DEL DOCUMENTO
            // ==========================================================
            new CatTributo { Id = 9, Seccion = 2, Codigo = "A8", Valor = "Impuesto Especial al Combustible (0%, 0.5%, 1%)", DescripcionCorta = "IEC", EsValorPorcentual = true },
            new CatTributo { Id = 10, Seccion = 2, Codigo = "57", Valor = "Impuesto industria de Cemento", DescripcionCorta = "Imp. Cemento", EsValorPorcentual = false },
            new CatTributo { Id = 11, Seccion = 2, Codigo = "90", Valor = "Impuesto especial a la primera matrícula", DescripcionCorta = "1ra Matrícula", EsValorPorcentual = false },
            new CatTributo { Id = 48, Seccion = 2, Codigo = "D4", Valor = "Otros impuestos casos especiales", DescripcionCorta = "Otros Impuestos", EsValorPorcentual = false },
            new CatTributo { Id = 49, Seccion = 2, Codigo = "D5", Valor = "Otras tasas casos especiales", DescripcionCorta = "Otras Tasas", EsValorPorcentual = false },

            new CatTributo { Id = 12, Seccion = 2, Codigo = "A6", Valor = "Impuesto ad-valorem, armas de fuego, municiones explosivas", DescripcionCorta = "Ad-Valorem Armas", EsValorPorcentual = true },

            // ==========================================================
            // SECCIÓN 3: IMPUESTO AD-VALOREM / INFORMATIVO
            // ==========================================================
            new CatTributo { Id = 13, Seccion = 3, Codigo = "C5", Valor = "Impuesto ad-valorem por diferencial de precios de bebidas alcohólicas (8%)", DescripcionCorta = "Ad-Valorem Alcohol", EsValorPorcentual = true },
            new CatTributo { Id = 14, Seccion = 3, Codigo = "C6", Valor = "Impuesto ad-valorem por diferencial de precios al tabaco cigarrillos (39%)", DescripcionCorta = "Ad-Valorem Cigarros", EsValorPorcentual = true },
            new CatTributo { Id = 15, Seccion = 3, Codigo = "C7", Valor = "Impuesto ad-valorem por diferencial de precios al tabaco cigarros (100%)", DescripcionCorta = "Ad-Valorem Tabaco", EsValorPorcentual = true },
            new CatTributo { Id = 16, Seccion = 3, Codigo = "19", Valor = "Fabricante de Bebidas Gaseosas...", DescripcionCorta = "Fab. Bebidas", EsValorPorcentual = false },
            new CatTributo { Id = 17, Seccion = 3, Codigo = "28", Valor = "Importador de Bebidas Gaseosas...", DescripcionCorta = "Imp. Bebidas", EsValorPorcentual = false },
            new CatTributo { Id = 18, Seccion = 3, Codigo = "31", Valor = "Detallistas o Expendedores de Bebidas Alcohólicas", DescripcionCorta = "Exp. Alcohol", EsValorPorcentual = false },
            new CatTributo { Id = 19, Seccion = 3, Codigo = "32", Valor = "Fabricante de Cerveza", DescripcionCorta = "Fab. Cerveza", EsValorPorcentual = false },
            new CatTributo { Id = 20, Seccion = 3, Codigo = "33", Valor = "Importador de Cerveza", DescripcionCorta = "Imp. Cerveza", EsValorPorcentual = false },
            new CatTributo { Id = 21, Seccion = 3, Codigo = "34", Valor = "Fabricante de Productos de Tabaco", DescripcionCorta = "Fab. Tabaco", EsValorPorcentual = false },
            new CatTributo { Id = 22, Seccion = 3, Codigo = "35", Valor = "Importador de Productos de Tabaco", DescripcionCorta = "Imp. Tabaco", EsValorPorcentual = false },
            new CatTributo { Id = 23, Seccion = 3, Codigo = "36", Valor = "Fabricante de Armas de Fuego y Municiones", DescripcionCorta = "Fab. Armas", EsValorPorcentual = false },
            new CatTributo { Id = 24, Seccion = 3, Codigo = "37", Valor = "Importador de Arma de Fuego y Municiones", DescripcionCorta = "Imp. Armas", EsValorPorcentual = false },
            new CatTributo { Id = 25, Seccion = 3, Codigo = "38", Valor = "Fabricante de Explosivos", DescripcionCorta = "Fab. Explosivos", EsValorPorcentual = false },
            new CatTributo { Id = 26, Seccion = 3, Codigo = "39", Valor = "Importador de Explosivos", DescripcionCorta = "Imp. Explosivos", EsValorPorcentual = false },
            new CatTributo { Id = 27, Seccion = 3, Codigo = "42", Valor = "Fabricante de Productos Pirotécnicos", DescripcionCorta = "Fab. Pirotecnia", EsValorPorcentual = false },
            new CatTributo { Id = 28, Seccion = 3, Codigo = "43", Valor = "Importador de Productos Pirotécnicos", DescripcionCorta = "Imp. Pirotecnia", EsValorPorcentual = false },
            new CatTributo { Id = 29, Seccion = 3, Codigo = "44", Valor = "Productor de Tabaco", DescripcionCorta = "Prod. Tabaco", EsValorPorcentual = false },
            new CatTributo { Id = 30, Seccion = 3, Codigo = "50", Valor = "Distribuidor de Bebidas Gaseosas...", DescripcionCorta = "Dist. Bebidas", EsValorPorcentual = false },
            new CatTributo { Id = 31, Seccion = 3, Codigo = "51", Valor = "Bebidas Alcohólicas", DescripcionCorta = "Bebidas Alc.", EsValorPorcentual = false },
            new CatTributo { Id = 32, Seccion = 3, Codigo = "52", Valor = "Cerveza", DescripcionCorta = "Cerveza", EsValorPorcentual = false },
            new CatTributo { Id = 33, Seccion = 3, Codigo = "53", Valor = "Productos del Tabaco", DescripcionCorta = "Tabaco", EsValorPorcentual = false },
            new CatTributo { Id = 34, Seccion = 3, Codigo = "54", Valor = "Bebidas Carbonatadas o Gaseosas", DescripcionCorta = "Gaseosas", EsValorPorcentual = false },
            new CatTributo { Id = 35, Seccion = 3, Codigo = "55", Valor = "Otros Específicos", DescripcionCorta = "Otros Específicos", EsValorPorcentual = false },
            new CatTributo { Id = 36, Seccion = 3, Codigo = "58", Valor = "Alcohol", DescripcionCorta = "Alcohol", EsValorPorcentual = false },
            new CatTributo { Id = 37, Seccion = 3, Codigo = "77", Valor = "Importador de Jugos y Refrescos", DescripcionCorta = "Imp. Jugos", EsValorPorcentual = false },
            new CatTributo { Id = 38, Seccion = 3, Codigo = "78", Valor = "Distribuidor de Jugos y Refrescos", DescripcionCorta = "Dist. Jugos", EsValorPorcentual = false },
            new CatTributo { Id = 39, Seccion = 3, Codigo = "79", Valor = "Sobre Llamadas Telefónicas Provenientes del Ext.", DescripcionCorta = "Llamadas Ext.", EsValorPorcentual = false },
            new CatTributo { Id = 40, Seccion = 3, Codigo = "85", Valor = "Detallista de Jugos y Refrescos", DescripcionCorta = "Det. Jugos", EsValorPorcentual = false },
            new CatTributo { Id = 41, Seccion = 3, Codigo = "86", Valor = "Fabricante de Preparaciones para Bebidas", DescripcionCorta = "Fab. Prep. Bebidas", EsValorPorcentual = false },
            new CatTributo { Id = 42, Seccion = 3, Codigo = "91", Valor = "Fabricante de Jugos y Refrescos", DescripcionCorta = "Fab. Jugos", EsValorPorcentual = false },
            new CatTributo { Id = 43, Seccion = 3, Codigo = "92", Valor = "Importador de Preparaciones para Bebidas", DescripcionCorta = "Imp. Prep. Bebidas", EsValorPorcentual = false },
            new CatTributo { Id = 44, Seccion = 3, Codigo = "A1", Valor = "Específicos y Ad-Valorem", DescripcionCorta = "Esp. y Ad-Valorem", EsValorPorcentual = false },
            new CatTributo { Id = 45, Seccion = 3, Codigo = "A5", Valor = "Bebidas Gaseosas y Energizantes", DescripcionCorta = "Bebidas Energ.", EsValorPorcentual = false },
            new CatTributo { Id = 46, Seccion = 3, Codigo = "A7", Valor = "Alcohol Etílico", DescripcionCorta = "Alcohol Etílico", EsValorPorcentual = false },
            new CatTributo { Id = 47, Seccion = 3, Codigo = "A9", Valor = "Sacos Sintéticos", DescripcionCorta = "Sacos Sintéticos", EsValorPorcentual = false }
        );
    }
}