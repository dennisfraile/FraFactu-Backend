using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FraFactu.Infrastructure.Persistence;

namespace FraFactu.Tests.Fixtures;

/// <summary>
/// Factory para crear aplicación de prueba
/// Desactiva todos los filtros de autorización
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Proveer configuración válida para el entorno de tests
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:MasterKey"] = Convert.ToBase64String(new byte[32]),
                ["JwtSettings:SecretKey"] = "SuperSecretTestKeyThatIsLongEnoughForHmacSha256Validation!",
                ["JwtSettings:Issuer"] = "FacturacionDteAPI",
                ["JwtSettings:Audience"] = "FacturacionDteClients",
                ["JwtSettings:ExpirationMinutes"] = "60",
                ["DefaultEmail:Habilitado"] = "false",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remover DbContext de producción
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbDescriptor != null) services.Remove(dbDescriptor);

            // Agregar DbContext con InMemory (nombre fijo para que seed y app usen la misma BD)
            var dbName = $"TestDb_{Guid.NewGuid()}";
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
                options.EnableSensitiveDataLogging();
            });

            // Remover background hosted services que no se necesitan en tests
            var backgroundServiceTypes = new[]
            {
                typeof(FraFactu.Infrastructure.Jobs.EnvioAutomaticoBackgroundService),
                typeof(FraFactu.Infrastructure.Jobs.ContingenciaAutoReportService),
                typeof(FraFactu.Infrastructure.Jobs.ConsultaEstadosLotesBackgroundService),
            };
            foreach (var type in backgroundServiceTypes)
            {
                var descriptor = services.SingleOrDefault(d => d.ImplementationType == type);
                if (descriptor != null) services.Remove(descriptor);
            }

            // CLAVE: Remover TODOS los filtros de autorización
            services.AddMvc(options =>
            {
                options.Filters.Clear();
            });

            // Configurar autenticación de prueba con TestAuthHandler
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

            // Agregar política que permite todo
            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder("Test")
                    .RequireAssertion(_ => true)
                    .Build();
            });

            // Seed de datos
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Database.EnsureCreated();
            TestDatabaseHelper.SeedTestData(context);
        });

        builder.UseEnvironment("Testing");
    }
}
