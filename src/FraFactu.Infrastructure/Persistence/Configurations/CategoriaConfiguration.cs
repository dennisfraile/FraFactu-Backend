using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración EF Core para Categoria
/// </summary>
public class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        // Nombre de tabla
        builder.ToTable("categorias");

        // Clave primaria
        builder.HasKey(x => x.Id);

        // ==========================================
        // PROPIEDADES
        // ==========================================

        builder.Property(x => x.Codigo)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Nombre)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Descripcion)
            .HasMaxLength(500);

        // ==========================================
        // RELACIONES
        // ==========================================

        // Auto-referencia para jerarquía
        builder.HasOne(x => x.CategoriaPadre)
            .WithMany(c => c.Subcategorias)
            .HasForeignKey(x => x.CategoriaPadreId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==========================================
        // ÍNDICES
        // ==========================================

        builder.HasIndex(x => new { x.Codigo, x.EmisorId })
            .IsUnique()
            .HasFilter("\"Activo\" = true")
            .HasDatabaseName("IX_Categorias_Codigo_EmisorId");

        builder.HasIndex(x => x.Nombre)
            .HasDatabaseName("IX_Categorias_Nombre");

        // ==========================================
        // TIMESTAMPS
        // ==========================================

        builder.Property(x => x.FechaCreacion)
            .HasDefaultValueSql("NOW()");
    }
}
