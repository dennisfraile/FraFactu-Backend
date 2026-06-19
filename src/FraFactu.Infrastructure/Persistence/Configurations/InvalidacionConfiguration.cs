using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations
{
    public class InvalidacionConfiguration : IEntityTypeConfiguration<Invalidacion>
    {
        public void Configure(EntityTypeBuilder<Invalidacion> builder)
        {
            builder.ToTable("invalidaciones");

            builder.HasKey(x => x.Id);

            // Código de generación único
            builder.Property(x => x.CodigoGeneracion)
                .IsRequired()
                .HasMaxLength(36);

            builder.HasIndex(x => new { x.CodigoGeneracion, x.EmisorId })
                .IsUnique()
                .HasDatabaseName("IX_Invalidaciones_CodigoGeneracion_EmisorId");

            // Fechas
            builder.Property(x => x.FechaAnulacion)
                .IsRequired();

            builder.Property(x => x.HoraAnulacion)
                .IsRequired();

            builder.Property(x => x.Ambiente)
                .IsRequired()
                .HasMaxLength(2);

            // Tipo y motivo
            builder.Property(x => x.TipoAnulacion)
                .IsRequired();

            builder.Property(x => x.MotivoAnulacion)
                .HasMaxLength(250);

            builder.Property(x => x.CodigoGeneracionReemplazo)
                .HasMaxLength(36);

            // Responsables
            builder.Property(x => x.NombreResponsable)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.NumDocResponsable)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.NombreSolicita)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.NumDocSolicita)
                .IsRequired()
                .HasMaxLength(20);

            // Receptor
            builder.Property(x => x.NumDocReceptor)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.NombreReceptor)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.TelefonoReceptor)
                .HasMaxLength(50);

            builder.Property(x => x.CorreoReceptor)
                .HasMaxLength(100);

            // Monto
            builder.Property(x => x.MontoIva)
                .HasPrecision(18, 2);

            // Respuesta MH
            builder.Property(x => x.SelloRecibido)
                .HasMaxLength(40);

            builder.Property(x => x.EstadoHacienda)
                .HasMaxLength(50);

            builder.Property(x => x.JsonEvento)
                .HasColumnType("text");

            builder.Property(x => x.JsonRespuesta)
                .HasColumnType("text");

            // Relación con FacturaElectronica (a invalidar)
            builder.HasOne(x => x.FacturaElectronica)
                .WithMany()
                .HasForeignKey(x => x.FacturaElectronicaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación con FacturaReemplazo (opcional)
            builder.HasOne(x => x.FacturaReemplazo)
                .WithMany()
                .HasForeignKey(x => x.FacturaReemplazoId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // Relación con Emisor
            builder.HasOne(x => x.Emisor)
                .WithMany()
                .HasForeignKey(x => x.EmisorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relaciones con Catálogos de Tipo Documento
            builder.HasOne(x => x.TipoDocResponsable)
                .WithMany()
                .HasForeignKey(x => x.CatTipoDocResponsableId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.TipoDocSolicita)
                .WithMany()
                .HasForeignKey(x => x.CatTipoDocSolicitaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.TipoDocReceptor)
                .WithMany()
                .HasForeignKey(x => x.CatTipoDocReceptorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Gestión de Inventario
            builder.Property(x => x.TipoInvalidacion)
                .HasMaxLength(30);

            builder.Property(x => x.RevirtiInventario)
                .HasDefaultValue(true);

            // Índices
            builder.HasIndex(x => x.FacturaElectronicaId)
                .HasDatabaseName("IX_Invalidaciones_FacturaElectronicaId");

            builder.HasIndex(x => x.FechaAnulacion)
                .HasDatabaseName("IX_Invalidaciones_FechaAnulacion");

            builder.HasIndex(x => new { x.EmisorId, x.FechaAnulacion })
                .HasDatabaseName("IX_Invalidaciones_Emisor_Fecha");

        }
    }
}
