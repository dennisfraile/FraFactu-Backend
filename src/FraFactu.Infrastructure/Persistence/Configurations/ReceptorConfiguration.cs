using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class ReceptorConfiguration : IEntityTypeConfiguration<Receptor>
{
    public void Configure(EntityTypeBuilder<Receptor> builder)
    {
        builder.ToTable("Receptores");
        builder.HasKey(r => r.Id);

        // Propiedades básicas
        // NumeroDocumento nullable: receptor FC "Sin documento" (par natural con CatTipoDocumentoId null).
        builder.Property(r => r.NumeroDocumento)
            .HasMaxLength(20);

        builder.Property(r => r.Nrc)
            .HasMaxLength(8);

        builder.Property(r => r.NombreRazonSocial)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.CodigoActividad)
            .HasMaxLength(6);

        builder.Property(r => r.DescripcionActividad)
            .HasMaxLength(300);

        builder.Property(r => r.Direccion)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(r => r.CorreoElectronico)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Telefono)
            .IsRequired()
            .HasMaxLength(20);

        // Relaciones con Catálogos
        // TipoDocumento nullable: receptor FC "Sin documento" (MH acepta tipoDocumento+numDocumento null en FC tipo 01).
        builder.HasOne(r => r.TipoDocumento)
            .WithMany()
            .HasForeignKey(r => r.CatTipoDocumentoIdentificacionReceptorId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Departamento)
            .WithMany()
            .HasForeignKey(r => r.CatDepartamentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Municipio)
            .WithMany()
            .HasForeignKey(r => r.CatMunicipioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Distrito)
            .WithMany()
            .HasForeignKey(r => r.CatDistritoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Relación con Emisor
        builder.HasOne(r => r.Emisor)
            .WithMany(e => e.Receptores)
            .HasForeignKey(r => r.EmisorId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relación con Facturas
        builder.HasMany(r => r.Facturas)
            .WithOne(f => f.Receptor)
            .HasForeignKey(f => f.ReceptorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índices únicos filtrados - solo aplican a registros activos
        // Permite reusar NumeroDocumento/NRC si el anterior fue desactivado.
        // El filtro NumeroDocumento IS NOT NULL admite múltiples receptores "Sin documento" (FC tipo 01).
        builder.HasIndex(r => new { r.EmisorId, r.NumeroDocumento })
            .HasDatabaseName("IX_Receptor_Emisor_NumDoc_Active")
            .IsUnique()
            .HasFilter("\"Activo\" = true AND \"NumeroDocumento\" IS NOT NULL");

        builder.HasIndex(r => new { r.EmisorId, r.Nrc })
            .HasDatabaseName("IX_Receptor_Emisor_Nrc_Active")
            .IsUnique()
            .HasFilter("\"Activo\" = true AND \"Nrc\" IS NOT NULL");

        // Índice para búsquedas generales (sin unicidad)
        builder.HasIndex(r => r.EmisorId)
            .HasDatabaseName("IX_Receptor_Emisor");
    }
}
