using System.Net;
using System.Net.Http.Json;
using FraFactu.Application.DTOs.Auth;
using FraFactu.Tests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FraFactu.Tests.IntegrationTests.Controllers
{
    public class LoginRateLimitTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public LoginRateLimitTests(CustomWebApplicationFactory factory) => _factory = factory;

        [Fact]
        public async Task Login_Returns429_AfterExceedingIpLimit()
        {
            // Límite bajo y ventana amplia para determinismo: los 4 requests
            // caen en la misma ventana y comparten la IP de loopback.
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["LoginSecurity:IpPermitLimit"] = "3",
                        ["LoginSecurity:IpWindowSeconds"] = "60"
                    });
                });
            }).CreateClient();

            // Email inexistente → siempre 401 hasta que el limiter corte con 429
            // (nunca dispara lockout de cuenta porque no existe la cuenta).
            var dto = new LoginDto { Email = "ratelimit_nobody@test.com", Password = "whatever" };

            HttpResponseMessage? last = null;
            for (int i = 0; i < 4; i++)
                last = await client.PostAsJsonAsync("/api/auth/login", dto);

            Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
        }
    }
}
