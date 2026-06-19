using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Persistence.Configurations
{
    public class UsuarioCajaConfiguration : IEntityTypeConfiguration<UsuarioCaja>
    {
        public void Configure(EntityTypeBuilder<UsuarioCaja> builder)
        {
            builder.ToTable("UsuariosCajas");
            builder.HasKey(uc => new { uc.UsuarioId, uc.CajaId });

            builder.HasOne(uc => uc.Usuario)
                .WithMany(u => u.UsuarioCajas)
                .HasForeignKey(uc => uc.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(uc => uc.Caja)
                .WithMany()
                .HasForeignKey(uc => uc.CajaId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
