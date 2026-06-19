using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class EmisorConfiguration : IEntityTypeConfiguration<Emisor>
{
    public void Configure(EntityTypeBuilder<Emisor> builder)
    {
        builder.ToTable("TBL_Emisores");
        builder.HasKey(e => e.Id);

        // Propiedades básicas
        builder.Property(e => e.Nit).IsRequired().HasMaxLength(14);
        builder.Property(e => e.Nrc).IsRequired().HasMaxLength(8);
        builder.Property(e => e.NombreRazonSocial).IsRequired().HasMaxLength(200);
        builder.Property(e => e.NombreComercial).HasMaxLength(200);
        builder.Property(e => e.CodigoActividad).IsRequired().HasMaxLength(6);
        builder.Property(e => e.DescripcionActividad).IsRequired().HasMaxLength(500);
        builder.Property(e => e.CorreoElectronico).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Telefono).IsRequired().HasMaxLength(20);
        builder.Property(e => e.Direccion).IsRequired().HasMaxLength(500);

        // Relaciones con Catálogos
        builder.HasOne(e => e.TipoEstablecimiento)
            .WithMany()
            .HasForeignKey(e => e.CatTipoEstablecimientoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Departamento)
            .WithMany()
            .HasForeignKey(e => e.CatDepartamentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Municipio)
            .WithMany()
            .HasForeignKey(e => e.CatMunicipioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Distrito)
            .WithMany()
            .HasForeignKey(e => e.CatDistritoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AmbienteDestino)
            .WithMany()
            .HasForeignKey(e => e.CatAmbienteDestinoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relaciones 1:N
        builder.HasMany(e => e.Usuarios)
            .WithOne(u => u.Emisor)
            .HasForeignKey(u => u.EmisorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Facturas)
            .WithOne(f => f.Emisor)
            .HasForeignKey(f => f.EmisorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Sucursales)
            .WithOne(s => s.Emisor)
            .HasForeignKey(s => s.EmisorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.ProductosServicios)
            .WithOne(p => p.Emisor)
            .HasForeignKey(p => p.EmisorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Receptores)
            .WithOne(r => r.Emisor)
            .HasForeignKey(r => r.EmisorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configuracion SMTP
        builder.Property(e => e.SmtpHost).HasMaxLength(200);
        builder.Property(e => e.SmtpUser).HasMaxLength(200);
        builder.Property(e => e.SmtpPassword).HasMaxLength(500);
        builder.Property(e => e.EmailRemitente).HasMaxLength(200);
        builder.Property(e => e.EmailHabilitado).HasDefaultValue(false);

        // Configuracion Gmail OAuth2
        builder.Property(e => e.GmailRefreshToken).HasMaxLength(1000);
        builder.Property(e => e.GmailEmail).HasMaxLength(200);
        builder.Property(e => e.GmailConectado).HasDefaultValue(false);

        // Índices
        builder.HasIndex(e => e.Nit).IsUnique();
        builder.HasIndex(e => e.Nrc);
    }
}
