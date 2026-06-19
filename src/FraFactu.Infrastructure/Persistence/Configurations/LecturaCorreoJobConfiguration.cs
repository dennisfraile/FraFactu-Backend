using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class LecturaCorreoJobConfiguration : IEntityTypeConfiguration<LecturaCorreoJob>
{
    public void Configure(EntityTypeBuilder<LecturaCorreoJob> builder)
    {
        builder.ToTable("lectura_correo_jobs");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Emisor)
            .WithMany()
            .HasForeignKey(x => x.EmisorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Estado)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>()
            .HasDefaultValue(EstadoLecturaCorreoJob.ENCOLADO);

        builder.Property(x => x.MensajeError)
            .HasMaxLength(1000);

        builder.Property(x => x.FechaCreacion)
            .HasDefaultValueSql("NOW()");

        // El consumidor consulta por (Estado=ENCOLADO) ordenado por FechaCreacion.
        builder.HasIndex(x => x.Estado)
            .HasDatabaseName("IX_LecturaCorreoJobs_Estado");

        // La UI busca el job más reciente del emisor.
        builder.HasIndex(x => new { x.EmisorId, x.FechaCreacion })
            .HasDatabaseName("IX_LecturaCorreoJobs_Emisor_FechaCreacion");

        // Regla de negocio: como mucho un job activo por emisor a la vez.
        // El backend lo valida explícitamente al encolar; este índice es para
        // diagnóstico/lookups por emisor + estado.
        builder.HasIndex(x => new { x.EmisorId, x.Estado })
            .HasDatabaseName("IX_LecturaCorreoJobs_Emisor_Estado");
    }
}
