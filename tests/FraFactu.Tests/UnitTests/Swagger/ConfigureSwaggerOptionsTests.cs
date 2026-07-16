using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using FraFactu.API.Swagger;
using Moq;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

namespace FraFactu.Tests.UnitTests.Swagger;

public class ConfigureSwaggerOptionsTests
{
    private static ConfigureSwaggerOptions BuildSut(params ApiVersionDescription[] descriptions)
    {
        var provider = new Mock<IApiVersionDescriptionProvider>();
        provider.SetupGet(p => p.ApiVersionDescriptions).Returns(descriptions);
        return new ConfigureSwaggerOptions(provider.Object);
    }

    [Fact]
    public void Configure_SingleActiveVersion_CreatesOneDocWithExpectedInfo()
    {
        // Tercer arg posicional = deprecated (bool)
        var sut = BuildSut(new ApiVersionDescription(new ApiVersion(1, 0), "v1", false));
        var options = new SwaggerGenOptions();

        sut.Configure(options);

        var docs = options.SwaggerGeneratorOptions.SwaggerDocs;
        Assert.Single(docs);
        Assert.True(docs.ContainsKey("v1"));
        var info = docs["v1"];
        Assert.Equal("FraFactu API", info.Title);
        Assert.Equal("v1", info.Version);
        Assert.DoesNotContain("obsoleta", info.Description);
    }

    [Fact]
    public void Configure_DeprecatedVersion_AppendsObsoleteNoticeToDescription()
    {
        var sut = BuildSut(
            new ApiVersionDescription(new ApiVersion(1, 0), "v1", false),
            new ApiVersionDescription(new ApiVersion(2, 0), "v2", true));
        var options = new SwaggerGenOptions();

        sut.Configure(options);

        var docs = options.SwaggerGeneratorOptions.SwaggerDocs;
        Assert.Equal(2, docs.Count);
        Assert.DoesNotContain("obsoleta", docs["v1"].Description);
        Assert.Contains("obsoleta", docs["v2"].Description);
    }
}
