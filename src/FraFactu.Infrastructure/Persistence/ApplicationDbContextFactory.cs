using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FraFactu.Infrastructure.Persistence;

/// <summary>
/// Fábrica de diseño para las herramientas de EF Core (dotnet ef).
///
/// Cuando existe esta fábrica, 'dotnet ef' la usa para crear el DbContext y
/// NO ejecuta Program.cs del proyecto API. Esto evita que el bloque de arranque
/// de la API (que llama a <c>Database.MigrateAsync()</c>) se dispare al generar
/// migraciones y aplique cambios contra la base de datos configurada.
///
/// Para 'migrations add' no se abre ninguna conexión, por lo que el placeholder
/// de abajo es suficiente. Para 'database update' debe exportarse la cadena real
/// en la variable de entorno <c>ConnectionStrings__DefaultConnection</c>.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=smartix_designtime;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }
}
