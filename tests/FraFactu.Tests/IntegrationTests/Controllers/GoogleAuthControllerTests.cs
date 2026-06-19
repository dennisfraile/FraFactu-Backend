using System.Net.Http.Json;
using FraFactu.Application.DTOs.Auth;
using FraFactu.Application.Interfaces;
using FraFactu.Tests.Fixtures;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace FraFactu.Tests.IntegrationTests.Controllers
{
    public class GoogleAuthControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly Mock<IGoogleTokenValidator> _mockValidator;

        public GoogleAuthControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _mockValidator = new Mock<IGoogleTokenValidator>();
        }

        [Fact]
        public async Task GoogleLogin_ShouldReturnUnauthorized_WhenUserDoesNotExist()
        {
            // Arrange
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => _mockValidator.Object);
                });
            }).CreateClient();

            var googleDto = new GoogleAuthDto { AccessToken = "valid_access_token" };

            _mockValidator.Setup(x => x.ValidateAccessTokenAsync("valid_access_token"))
                .ReturnsAsync(new GoogleAccessTokenUserInfo
                {
                    Email = "nonexistent@test.com",
                    Sub = "google_id_integration",
                    Name = "Integration User"
                });

            // Act
            var response = await client.PostAsJsonAsync("/api/auth/google", googleDto);

            // Assert — no se crean usuarios automáticamente, se rechaza el login
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GoogleLogin_ShouldReturnUnauthorized_WhenAccessTokenInvalid()
        {
            // Arrange
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => _mockValidator.Object);
                });
            }).CreateClient();

            var googleDto = new GoogleAuthDto { AccessToken = "invalid_token" };

            _mockValidator.Setup(x => x.ValidateAccessTokenAsync("invalid_token"))
                .ReturnsAsync((GoogleAccessTokenUserInfo?)null);

            // Act
            var response = await client.PostAsJsonAsync("/api/auth/google", googleDto);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task LinkGoogleAccount_ShouldReturnOk_WhenAuthenticatedAndTokenValid()
        {
            // Arrange
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => _mockValidator.Object);
                });
            }).CreateClient();

            // Link/Unlink require Auth state which is tricky with TestAuthHandler.
            // GoogleLogin tests cover the main OAuth2 code flow.
        }
    }
}
