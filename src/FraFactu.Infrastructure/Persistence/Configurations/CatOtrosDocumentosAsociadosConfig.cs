using FraFactu.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class CatOtrosDocumentosAsociadosConfig : IEntityTypeConfiguration<CatOtrosDocumentosAsociados>
{
    public void Configure(EntityTypeBuilder<CatOtrosDocumentosAsociados> builder)
    {
        builder.ToTable("cat_otros_documentos_asociados");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Codigo).HasMaxLength(2); 
        builder.Property(x => x.Valor).HasMaxLength(200).IsRequired();

        builder.HasData(
            new CatOtrosDocumentosAsociados {Id = 1, Codigo = "1", Valor = "Emisor" },
            new CatOtrosDocumentosAsociados {Id = 2, Codigo = "2", Valor = "Receptor" },
            new CatOtrosDocumentosAsociados {Id = 3, Codigo = "3", Valor = "Médico (solo aplica para contribuyentes obligados a la presentación de F-958)" },
            new CatOtrosDocumentosAsociados {Id = 4, Codigo = "4", Valor = "Transporte (solo aplica para Factura de exportación)" }
        );
    }
}