using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations
{
    public class SucursalConfiguration : IEntityTypeConfiguration<Sucursal>
    {
        public void Configure(EntityTypeBuilder<Sucursal> builder)
        {
            builder.ToTable("TBL_Sucursales");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Codigo)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(s => s.Nombre)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(s => s.Direccion)
                .HasMaxLength(500);

            builder.Property(s => s.Telefono)
                .HasMaxLength(20);

            builder.Property(s => s.CorreoElectronico)
                .HasMaxLength(100);

            // Relaciones con Catálogos
            builder.HasOne(s => s.Departamento)
                .WithMany()
                .HasForeignKey(s => s.CatDepartamentoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.Municipio)
                .WithMany()
                .HasForeignKey(s => s.CatMunicipioId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.Distrito)
                .WithMany()
                .HasForeignKey(s => s.CatDistritoId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.TipoEstablecimiento)
                .WithMany()
                .HasForeignKey(s => s.CatTipoEstablecimientoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(s => s.CodigoEstablecimiento)
                .HasMaxLength(4);


            // Relación con Emisor
            builder.HasOne(s => s.Emisor)
                .WithMany(e => e.Sucursales)
                .HasForeignKey(s => s.EmisorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Índices
            builder.HasIndex(s => new { s.EmisorId, s.Codigo })
                .HasDatabaseName("IX_Sucursal_Emisor_Codigo")
                .IsUnique()
                .HasFilter("\"Activo\" = true");

            builder.HasIndex(s => s.EmisorId)
                .HasDatabaseName("IX_Sucursal_Emisor");

            builder.HasIndex(s => new { s.EmisorId, s.CodigoEstablecimiento })
                .IsUnique()
                .HasFilter("\"Activo\" = true")
                .HasDatabaseName("IX_Sucursal_Emisor_CodigoEstablecimiento");

            // Plan B Hub-as-Emisor (D14): 1 Sucursal Smartix ↔ 1 Sucursal Hub.
            // Filtered unique: multiples sucursales sin vincular conviven, pero
            // no se puede vincular dos sucursales Smartix al mismo Hub Sucursal.
            builder.HasIndex(s => s.HubSucursalId)
                .IsUnique()
                .HasFilter("\"HubSucursalId\" IS NOT NULL")
                .HasDatabaseName("IX_Sucursal_HubSucursalId");
        }
    }
}
