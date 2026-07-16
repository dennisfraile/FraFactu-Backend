using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FraFactu.API.Swagger;

/// <summary>
/// Genera un doc de Swagger por cada versión de API que describe el ApiExplorer,
/// en vez de clavar un único "v1". Así, cuando se añada un controller con
/// [ApiVersion("2.0")], su doc aparece automáticamente en Swagger.
/// </summary>
public class ConfigureSwaggerOptions : IConfigureNamedOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
        => _provider = provider;

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var desc in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(desc.GroupName, CreateInfoForApiVersion(desc));
        }
    }

    public void Configure(string? name, SwaggerGenOptions options) => Configure(options);

    private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription desc)
    {
        var description = "API unificada de Facturación Electrónica (DTE - El Salvador) e Inventario";
        if (desc.IsDeprecated)
        {
            description += " — ⚠️ Esta versión de API está obsoleta.";
        }

        return new OpenApiInfo
        {
            Title = "FraFactu API",
            Version = desc.GroupName,
            Description = description
        };
    }
}
