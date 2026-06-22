using FraFactu.API.Observability;
using FraFactu.Infrastructure.Http;
using FraFactu.Infrastructure.Observability;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Application.Common.Interfaces;
using FraFactu.Application.Common.Settings;
using FraFactu.Application.Interfaces;
using FraFactu.Infrastructure.Services;
using FraFactu.Application.Services;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IO.Compression;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// F7: Application Insights. Lee APPLICATIONINSIGHTS_CONNECTION_STRING (env var
// estandar Azure) o ApplicationInsights:ConnectionString (config). En local sin
// connection string queda inactivo (no-op silencioso).
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddSingleton<ITelemetryInitializer>(
    new CloudRoleInitializer("frafactu-backend"));
builder.Services.AddSingleton<ITelemetryService, TelemetryService>();
builder.Services.AddTransient<CorrelationIdHandler>();

// ==========================================
// CONFIGURACIÓN DE SERVICIOS
// ==========================================

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "FraFactu API",
        Version = "v1",
        Description = "API unificada de Facturación Electrónica (DTE - El Salvador) e Inventario"
    });

    // Evita conflictos de schemaId cuando hay DTOs con el mismo nombre en
    // namespaces distintos (p. ej. TributoResumenInputDto en Retorno vs
    // OperacionesEspeciales): se usa el nombre completo saneado.
    options.CustomSchemaIds(type => type.FullName?.Replace("+", ".").Replace("`", "_"));

    // Configuración JWT en Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: \"Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    // Agregar soporte para JsonElement
    options.MapType<System.Text.Json.JsonElement>(() => new Microsoft.OpenApi.Models.OpenApiSchema
    {
        Type = "object",
        AdditionalPropertiesAllowed = true
    });
});

// Base de Datos
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var dbPassword = builder.Configuration["Database:Password"];
if (!string.IsNullOrEmpty(dbPassword) && !connectionString!.Contains("Password="))
{
    connectionString = $"{connectionString};Password={dbPassword}";
}
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// AutoMapper - Configuración automática desde Assembly
builder.Services.AddAutoMapper(typeof(FraFactu.Application.Mapping.MappingProfile).Assembly);

// FluentValidation - Registro automático de todos los validadores
builder.Services.AddValidatorsFromAssemblyContaining<FraFactu.Application.Validators.Auth.LoginDtoValidator>();

// Configuración JWT
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

// Configuración Google OAuth
builder.Services.Configure<GoogleAuthSettings>(builder.Configuration.GetSection("GoogleAuth"));
builder.Services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
builder.Services.AddScoped<IGmailApiService, GmailApiService>();

// Configuración Encriptación (AES-256-GCM para campos sensibles en BD)
builder.Services.Configure<EncryptionSettings>(builder.Configuration.GetSection("Encryption"));
builder.Services.AddSingleton<IEncryptionService, AesEncryptionService>();

// Configuración Email por Defecto (fallback cuando el emisor no tiene SMTP propio)
builder.Services.Configure<DefaultEmailSettings>(builder.Configuration.GetSection("DefaultEmail"));

// Configuración Supabase (solo storage; el login vía Supabase se eliminó en F1)
builder.Services.Configure<SupabaseSettings>(builder.Configuration.GetSection("Supabase"));
builder.Services.AddHttpClient(); // Required for Supabase storage HTTP calls

// Authentication & Authorization
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings?.Issuer,
        ValidAudience = jwtSettings?.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings?.SecretKey ?? ""))
    };

    // Identidad 100% local: además de firma/issuer/audience/vida, se valida la
    // revocación local. El claim "token_version" del JWT debe coincidir con el
    // Usuario.TokenVersion actual; si el usuario hizo logout, cambió su
    // contraseña, cambió de rol o fue desactivado, su TokenVersion subió y el
    // token queda invalidado. Semántica fail-closed (ver ITokenVersionValidator).
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var principal = context.Principal;
            var subClaim = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? principal?.FindFirst("sub")?.Value;

            if (!int.TryParse(subClaim, out var usuarioId))
            {
                context.Fail("Token sin identificador de usuario válido.");
                return;
            }

            int? tokenVersionClaim =
                int.TryParse(principal?.FindFirst("token_version")?.Value, out var tv) ? tv : null;

            var validator = context.HttpContext.RequestServices
                .GetRequiredService<ITokenVersionValidator>();

            if (!await validator.IsCurrentAsync(usuarioId, tokenVersionClaim))
            {
                context.Fail("Sesión revocada: token_version no vigente.");
            }
        }
    };
});

// Authorization con Permisos
builder.Services.AddAuthorization(options =>
{
    // Aquí se pueden agregar políticas específicas si es necesario
    // Las políticas dinámicas se crean en tiempo de ejecución por el handler
});

// Registrar el handler de permisos
builder.Services.AddSingleton<IAuthorizationHandler, FraFactu.API.Authorization.PermissionAuthorizationHandler>();

// HttpContextAccessor para acceder al contexto HTTP
builder.Services.AddHttpContextAccessor();

// Servicio para obtener usuario actual desde JWT claims
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Servicios de Aplicación
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenVersionValidator, FraFactu.Infrastructure.Services.TokenVersionValidator>();
builder.Services.AddScoped<IUsuarioService, FraFactu.Infrastructure.Services.UsuarioService>();
builder.Services.AddScoped<ICatalogoService, FraFactu.Infrastructure.Services.CatalogoService>();
builder.Services.AddSingleton<IProveedorSmtpResolver, FraFactu.Infrastructure.Services.ProveedorSmtpResolver>();
builder.Services.AddScoped<IEmisorService, FraFactu.Infrastructure.Services.EmisorService>();
builder.Services.AddScoped<IProductoServicioService, FraFactu.Infrastructure.Services.ProductoServicioService>();
builder.Services.AddScoped<ISucursalService, FraFactu.Infrastructure.Services.SucursalService>();
builder.Services.AddScoped<IReceptorService, FraFactu.Infrastructure.Services.ReceptorService>();
builder.Services.AddSingleton<ICrossDbCorrelativoService, FraFactu.Infrastructure.Services.CrossDbCorrelativoService>();
builder.Services.AddScoped<IFacturaService, FraFactu.Infrastructure.Services.FacturaService>();
builder.Services.AddScoped<FraFactu.Application.Interfaces.IFacturacionCuotaStrategy, FraFactu.Application.Services.FacturacionCuotaStrategy>();
builder.Services.AddScoped<FraFactu.Application.Interfaces.IMoraService, FraFactu.Application.Services.MoraService>();
builder.Services.AddScoped<FraFactu.Application.Interfaces.IConfiguracionCuotasService, FraFactu.Infrastructure.Services.ConfiguracionCuotasService>();
builder.Services.AddScoped<FraFactu.Application.Interfaces.IRecordatorioCuotasService, FraFactu.Infrastructure.Services.RecordatorioCuotasService>();
builder.Services.AddScoped<FraFactu.Application.Interfaces.IPlanCuotasService, FraFactu.Infrastructure.Services.PlanCuotasService>();
builder.Services.AddScoped<FraFactu.Application.Interfaces.IAgingService, FraFactu.Infrastructure.Services.AgingService>();
builder.Services.AddScoped<FraFactu.Application.Interfaces.IAgingExcelExporter, FraFactu.Infrastructure.Services.AgingExcelExporter>();
builder.Services.AddScoped<FraFactu.Application.Interfaces.IAgingPdfExporter, FraFactu.Infrastructure.Services.AgingPdfExporter>();
builder.Services.AddScoped<FraFactu.Application.Interfaces.IEstadoCuentaService, FraFactu.Infrastructure.Services.EstadoCuentaService>();
builder.Services.AddScoped<FraFactu.Application.Interfaces.IEstadoCuentaExcelExporter, FraFactu.Infrastructure.Services.EstadoCuentaExcelExporter>();
builder.Services.AddScoped<FraFactu.Application.Interfaces.IEstadoCuentaPdfExporter, FraFactu.Infrastructure.Services.EstadoCuentaPdfExporter>();
builder.Services.AddScoped<FraFactu.Application.Interfaces.INotificacionService, FraFactu.Infrastructure.Services.NotificacionService>();
builder.Services.AddScoped<ISaldoDteService, FraFactu.Infrastructure.Services.SaldoDteService>();
builder.Services.AddScoped<ILoteService, FraFactu.Infrastructure.Services.LoteService>();
builder.Services.AddScoped<IConfiguracionEnvioLoteService, FraFactu.Infrastructure.Services.ConfiguracionEnvioLoteService>();
builder.Services.AddScoped<IVendedorService, FraFactu.Infrastructure.Services.VendedorService>();
builder.Services.AddScoped<ICajaService, FraFactu.Infrastructure.Services.CajaService>();

// Servicios de Compras y Gastos
builder.Services.AddScoped<IProveedorService, FraFactu.Infrastructure.Services.ProveedorService>();
builder.Services.AddScoped<ICompraExternaService, FraFactu.Infrastructure.Services.CompraExternaService>();
builder.Services.AddScoped<IGastoAdministrativoService, FraFactu.Infrastructure.Services.GastoAdministrativoService>();

// Servicios de DTEs Recibidos
builder.Services.Configure<FraFactu.Application.Common.Settings.DtesRecibidosSettings>(
    builder.Configuration.GetSection("DtesRecibidos"));
builder.Services.AddScoped<FraFactu.Application.Interfaces.IDteParserService, FraFactu.Infrastructure.Services.DteParserService>();
builder.Services.AddScoped<FraFactu.Application.Interfaces.IDteIngestaService, FraFactu.Infrastructure.Services.DteIngestaService>();
builder.Services.AddScoped<FraFactu.Application.Interfaces.IEmailReaderService, FraFactu.Infrastructure.Services.EmailReaderService>();
builder.Services.AddScoped<FraFactu.Application.Interfaces.IEmailLectorWorker, FraFactu.Infrastructure.Services.EmailLectorWorker>();
builder.Services.AddScoped<FraFactu.Application.Services.IDteRecibidoService, FraFactu.Infrastructure.Services.DteRecibidoService>();

builder.Services.AddHostedService<FraFactu.Infrastructure.Jobs.LecturaCorreoConsumer>();
builder.Services.AddHostedService<FraFactu.Infrastructure.Jobs.LecturaCorreoAutomaticaScheduler>();

// Servicios de Reportes de Compras
builder.Services.AddScoped<FraFactu.Application.Interfaces.IReporteComprasService, FraFactu.Infrastructure.Services.ReporteComprasService>();

// Servicios de Catálogos (Categorías, Marcas, Tipos de Gasto)
builder.Services.AddScoped<ICategoriaService, FraFactu.Infrastructure.Services.CategoriaService>();
builder.Services.AddScoped<IMarcaService, FraFactu.Infrastructure.Services.MarcaService>();
builder.Services.AddScoped<ITipoGastoService, FraFactu.Infrastructure.Services.TipoGastoService>();

// Servicios de Integración Inventario
builder.Services.AddScoped<IInventarioIntegrationService, FraFactu.Infrastructure.Services.InventarioIntegrationService>();
builder.Services.AddScoped<IInventarioReporteService, FraFactu.Infrastructure.Services.InventarioReporteService>();
builder.Services.AddScoped<IIdempotenciaMovimientosService, FraFactu.Infrastructure.Services.IdempotenciaMovimientosService>();

// Servicio de Correlativos Iniciales (migración desde otros sistemas)
builder.Services.AddScoped<FraFactu.Application.Interfaces.ICorrelativoInicialService, FraFactu.Infrastructure.Services.CorrelativoInicialService>();

// Servicio de Eventos de Contingencia
builder.Services.AddScoped<FraFactu.Application.Interfaces.IEventoContingenciaService, FraFactu.Infrastructure.Services.EventoContingenciaService>();

// Servicio de Eventos de Operaciones Especiales (17)
builder.Services.AddScoped<FraFactu.Application.Interfaces.IEventoOperacionEspecialService, FraFactu.Infrastructure.Services.EventoOperacionEspecialService>();

// Servicio de Eventos de Retorno (18)
builder.Services.AddScoped<FraFactu.Application.Interfaces.IEventoRetornoService, FraFactu.Infrastructure.Services.EventoRetornoService>();

// Servicios de Firma y Criptografía
builder.Services.AddScoped<FraFactu.Application.Interfaces.Hacienda.IDteSignerService, FraFactu.Infrastructure.Services.DteSignerService>();

// Memory Cache para tokens de Hacienda
builder.Services.AddMemoryCache();

// Servicios de Hacienda (Auth)
builder.Services.AddHttpClient<FraFactu.Application.Interfaces.Hacienda.IHaciendaAuthService, FraFactu.Infrastructure.Services.HaciendaAuthService>();

// Servicios de Hacienda (API)
builder.Services.AddHttpClient<FraFactu.Application.Interfaces.Hacienda.IHaciendaApiService, FraFactu.Infrastructure.Services.HaciendaApiService>();

// Repositorio de Catálogos y Mapper para JSON de MH
builder.Services.AddScoped<FraFactu.Application.Interfaces.Repositories.ICatalogRepository, FraFactu.Infrastructure.Repositories.CatalogRepository>();
// Repositorio de Catálogos y Mapper para JSON de MH
builder.Services.AddScoped<FraFactu.Application.Interfaces.Repositories.ICatalogRepository, FraFactu.Infrastructure.Repositories.CatalogRepository>();
builder.Services.AddScoped<FraFactu.Application.Services.DteJsonMapperService>();

// Servicios de Diagnóstico y Contingencia
builder.Services.AddScoped<IContingenciaDiagnosticoService, ContingenciaDiagnosticoService>();
builder.Services.AddScoped<IHaciendaRetryService, HaciendaRetryService>();

//Services de Features (Dashboard, Email, Export)
builder.Services.AddScoped<IDashboardService, FraFactu.Infrastructure.Services.DashboardService>();
builder.Services.AddScoped<IEmailService, FraFactu.Infrastructure.Services.EmailService>();
builder.Services.AddScoped<IAuthEmailService, FraFactu.Infrastructure.Services.AuthEmailService>();
builder.Services.AddScoped<IExportService, FraFactu.Infrastructure.Services.ExportService>();
builder.Services.AddScoped<IImportService, FraFactu.Infrastructure.Services.ImportService>();
builder.Services.AddScoped<ISupabaseStorageService, FraFactu.Infrastructure.Services.SupabaseStorageService>();

// Servicio de Suscripciones
builder.Services.AddScoped<ISuscripcionService, FraFactu.Infrastructure.Services.SuscripcionService>();

// Background Jobs - Envío automático de lotes
builder.Services.AddScoped<FraFactu.Infrastructure.Jobs.EnvioAutomaticoLotesJob>();
builder.Services.AddHostedService<FraFactu.Infrastructure.Jobs.EnvioAutomaticoBackgroundService>();
builder.Services.AddHostedService<FraFactu.Infrastructure.Jobs.ContingenciaAutoReportService>();
builder.Services.AddHostedService<FraFactu.Infrastructure.Jobs.ConsultaEstadosLotesBackgroundService>();
// Reintento automático de facturas en ERROR (≥15 min, regla 13.2.1)
builder.Services.AddHostedService<FraFactu.Infrastructure.Jobs.ReintentoEnvioErrorBackgroundService>();

// Background Job - Recordatorios de suscripción
builder.Services.AddHostedService<FraFactu.Infrastructure.Jobs.SuscripcionReminderBackgroundService>();

// Background Job - Recordatorios de cuotas (vencidas / por vencer)
builder.Services.AddHostedService<FraFactu.Infrastructure.Jobs.RecordatorioCuotasBackgroundService>();

// Background Job - Devaluación anual de mobiliario/equipo (F3 G1)
builder.Services.AddHostedService<FraFactu.Infrastructure.Jobs.DevaluacionAnualBackgroundService>();

// HttpClient para integraciones con MH
builder.Services.AddHttpClient("MinisterioHacienda", client =>
{
    // TODO: Configurar URL base desde appsettings
    client.BaseAddress = new Uri("https://api.ministerio-hacienda.gob.sv/");
    client.Timeout = TimeSpan.FromMinutes(5);
});

// Controllers y FluentValidation
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddValidatorsFromAssemblyContaining<FraFactu.Application.Validators.CrearFacturaValidator>();

// Health Checks
builder.Services.AddHealthChecks();

// CORS Configuration
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                  ?? new[] { "http://localhost:3000", "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Response Caching - Para mejorar performance de endpoints
builder.Services.AddResponseCaching();

// Compresion HTTP: respuestas JSON de listados (facturas, dtes recibidos,
// catalogos) son las que mas pesan. Brotli es la mejor compresion para texto,
// gzip queda como fallback. EnableForHttps porque Azure App Service termina
// TLS en el front-door; sin esto Kestrel salta la compresion.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes
        .Concat(new[] { "application/json", "application/json; charset=utf-8" });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

var app = builder.Build();

// Apply pending EF migrations automatically on startup. Skip when the
// provider is not relational (eg. tests con WebApplicationFactory que
// inyectan InMemoryDatabase) — MigrateAsync solo aplica a relational.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FraFactu.Infrastructure.Persistence.ApplicationDbContext>();
    if (db.Database.IsRelational())
    {
        await db.Database.MigrateAsync();
    }
}

// Global Exception Handling
app.UseMiddleware<FraFactu.API.Middleware.GlobalExceptionMiddleware>();

// F7: correlationId middleware (debe ir despues del exception handler para
// que las excepciones tambien queden con correlationId, pero antes del
// resto del pipeline para que tracking en controllers tenga el id resuelto).
app.UseMiddleware<FraFactu.API.Middleware.CorrelationIdMiddleware>();

// ==========================================
// CONFIGURACIÓN DEL PIPELINE HTTP
// ==========================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Facturación Electrónica API v1");
        //options.RoutePrefix = string.Empty; // Swagger en la raíz
    });
}

// CORS - Debe estar lo más temprano posible en el pipeline
app.UseCors("DefaultCorsPolicy");

app.UseHttpsRedirection();

// Request Logging Middleware - Para debugging de autenticación
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("🌐 [REQUEST] =================================================");
    logger.LogInformation("🌐 [REQUEST] Incoming HTTP Request");
    logger.LogInformation("🌐 [REQUEST] Method: {Method}", context.Request.Method);
    logger.LogInformation("🌐 [REQUEST] Path: {Path}", context.Request.Path);
    logger.LogInformation("🌐 [REQUEST] QueryString: {QueryString}", context.Request.QueryString);
    logger.LogInformation("🌐 [REQUEST] Origin: {Origin}", context.Request.Headers.Origin.FirstOrDefault() ?? "Not set");

    // Log Authorization header (sin exponer el token completo)
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault() ?? "";
        if (authHeader.StartsWith("Bearer "))
        {
            var tokenPrefix = authHeader.Substring(0, Math.Min(40, authHeader.Length));
            logger.LogInformation("✅ [REQUEST] Authorization header present: {TokenPrefix}...", tokenPrefix);
        }
        else
        {
            logger.LogWarning("⚠️ [REQUEST] Authorization header present but not Bearer token: {AuthHeader}", authHeader);
        }
    }
    else
    {
        logger.LogWarning("⚠️ [REQUEST] NO Authorization header present");
    }

    logger.LogInformation("🌐 [REQUEST] =================================================");

    await next();
});

// Compresion HTTP - antes de Caching para que el cache guarde la version comprimida.
app.UseResponseCompression();

// Response Caching - Debe estar después de CORS
app.UseResponseCaching();

// Authentication & Authorization DEBE estar antes de MapControllers
app.UseAuthentication();
app.UseAuthorization();

// Restricción de primer ingreso: tras autenticar, si el usuario tiene un cambio
// de contraseña obligatorio pendiente, solo se le permite el endpoint de cambio.
app.UseMiddleware<FraFactu.API.Middleware.RequirePasswordChangeMiddleware>();

// Mapear controladores
app.MapControllers();

// Health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");

app.Run();

// Make Program accessible to tests
public partial class Program { }

