using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuración para la entidad FacturaElectronica
    /// </summary>
    public class FacturaElectronicaConfiguration : IEntityTypeConfiguration<FacturaElectronica>
    {
        public void Configure(EntityTypeBuilder<FacturaElectronica> builder)
        {
            // Mapear a la tabla "Facturas" en PostgreSQL
            builder.ToTable("Facturas");

            // Configurar clave primaria
            builder.HasKey(f => f.Id);

            // Propiedades requeridas
            builder.Property(f => f.CodigoGeneracion)
                .IsRequired()
                .HasMaxLength(36);

            builder.Property(f => f.NumeroControl)
                .IsRequired()
                .HasMaxLength(31);

            builder.Property(f => f.Ambiente)
                .IsRequired()
                .HasMaxLength(2)
                .HasDefaultValue("00");

            // Propiedades opcionales
            builder.Property(f => f.Observaciones)
                .HasMaxLength(3000);

            // Relaciones
            builder.HasOne(f => f.Emisor)
                .WithMany(e => e.Facturas) // ✅ Referencia colección Emisor.Facturas
                .HasForeignKey(f => f.EmisorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación opcional con Lote (si se envió por lotes)
            builder.HasOne(x => x.Lote)
                .WithMany()
                .HasForeignKey(x => x.LoteId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(f => f.Sucursal)
                .WithMany(s => s.Facturas) // ✅ Referencia colección Sucursal.Facturas
                .HasForeignKey(f => f.SucursalId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.Receptor)
                .WithMany(r => r.Facturas) // ✅ Referencia colección Receptor.Facturas
                .HasForeignKey(f => f.ReceptorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Índices para búsquedas
            builder.HasIndex(f => new { f.CodigoGeneracion, f.EmisorId })
                .IsUnique()
                .HasDatabaseName("IX_Facturas_CodigoGeneracion_EmisorId");

            builder.HasIndex(f => f.NumeroControl);

            builder.HasIndex(f => f.FechaEmision);

            builder.HasIndex(f => new { f.EmisorId, f.EstadoHacienda });

            builder.HasIndex(f => new { f.EmisorId, f.Ambiente })
                .HasDatabaseName("IX_Facturas_EmisorId_Ambiente");
        }
    }
}
