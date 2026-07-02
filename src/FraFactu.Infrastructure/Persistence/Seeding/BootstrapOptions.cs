namespace FraFactu.Infrastructure.Persistence.Seeding;

/// <summary>
/// Credenciales del SuperAdmin inicial, bindeadas desde la sección "Bootstrap"
/// de la configuración. Sin AdminPassword no se crea el usuario (nunca hay
/// clave por defecto).
/// </summary>
public sealed class BootstrapOptions
{
    public string? AdminEmail { get; set; }
    public string? AdminPassword { get; set; }
    public string? AdminNombre { get; set; }
}
