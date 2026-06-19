using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations
{
    public class ProductoServicioConfiguration : IEntityTypeConfiguration<ProductoServicio>
    {
        public void Configure(EntityTypeBuilder<ProductoServicio> builder)
        {
            builder.ToTable("TBL_ProductosServicios");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Codigo)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.Nombre)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(p => p.Descripcion)
                .HasMaxLength(500);

            builder.Property(p => p.PrecioVenta)
                .HasPrecision(18, 8);

            // Relación con Emisor
            builder.HasOne(p => p.Emisor)
                .WithMany(e => e.ProductosServicios)
                .HasForeignKey(p => p.EmisorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación con CatUnidadMedida
            builder.HasOne(p => p.UnidadMedida)
                .WithMany()
                .HasForeignKey(p => p.CatUnidadMedidaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación con CatTipoItem
            builder.HasOne(p => p.TipoItem)
                .WithMany()
                .HasForeignKey(p => p.CatTipoItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // ==========================================
            // CONFIGURACIÓN MERGEADA DE PRODUCTO
            // ==========================================

            builder.Property(p => p.CodigoBarras)
                .HasMaxLength(50);

            // Precios y Costos
            builder.Property(p => p.PrecioCosto)
                .HasPrecision(18, 2);

            // Stock
            builder.Property(p => p.StockMinimo)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            builder.Property(p => p.StockMaximo)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            builder.Property(p => p.PuntoReorden)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            // Impuestos
            builder.Property(p => p.TipoImpuesto)
                .HasConversion<int>() // Store enum as int
                .HasDefaultValue(Domain.Enums.TipoImpuesto.Gravado)
                .HasSentinel((Domain.Enums.TipoImpuesto)0)
                .IsRequired();

            // PorcentajeIVA sin HasDefaultValue: con default a nivel SQL, EF Core
            // trata null (CLR default para nullable decimal) como sentinel value
            // y aplica el SQL default 13 al INSERT en vez de respetar el null
            // explicito que UpsertByCodigoAsync asigna para productos Exento/NoSujeto.
            // El default 13 ya esta cubierto a nivel C# en la entidad (= 13m).
            builder.Property(p => p.PorcentajeIVA)
                .HasPrecision(5, 2);

            builder.Property(p => p.PrecioIncluyeIva)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(p => p.CostoIncluyeIva)
                .IsRequired()
                .HasDefaultValue(false);

            // Configuración
            builder.Property(p => p.PermiteVentaSinStock)
                .HasDefaultValue(false);

            // F2: clasificacion contable. Se persiste como int (mismo patron
            // que TipoImpuesto). Default Ventas para que el catalogo existente
            // no rompa al migrar.
            builder.Property(p => p.TipoInventario)
                .HasConversion<int>()
                .HasDefaultValue(Domain.Enums.TipoInventario.Ventas)
                .HasSentinel((Domain.Enums.TipoInventario)(-1))
                .IsRequired();

            // F2: campos de activo fijo (solo aplican a MobiliarioEquipo).
            builder.Property(p => p.ValorActual).HasPrecision(18, 2);
            builder.Property(p => p.ValorResidual).HasPrecision(18, 2);

            // Relaciones Nuevas (Opcionales)
            builder.HasOne(p => p.Categoria)
                .WithMany(c => c.Productos)
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Marca)
                .WithMany(m => m.Productos)
                .HasForeignKey(p => p.MarcaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Índices
            builder.HasIndex(p => new { p.EmisorId, p.Codigo })
                .HasDatabaseName("IX_Producto_Emisor_Codigo")
                .IsUnique()
                .HasFilter("\"Activo\" = true");

            builder.HasIndex(p => p.EmisorId)
                .HasDatabaseName("IX_Producto_Emisor");

            builder.HasIndex(p => p.CodigoBarras)
                .HasDatabaseName("IX_Producto_CodigoBarras");

            builder.HasIndex(p => p.CategoriaId)
                .HasDatabaseName("IX_Producto_Categoria");

            builder.HasIndex(p => p.MarcaId)
                .HasDatabaseName("IX_Producto_Marca");
        }
    }
}
