using System.Net;
using System.Net.Http;
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

        [Fact]
        public async Task Login_RateLimitPartitionsByForwardedFor()
        {
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

            var dto = new LoginDto { Email = "fwd_nobody@test.com", Password = "whatever" };

            // Agotar el cupo para la IP 203.0.113.10
            for (int i = 0; i < 4; i++)
            {
                var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
                { Content = JsonContent.Create(dto) };
                req.Headers.Add("X-Forwarded-For", "203.0.113.10");
                await client.SendAsync(req);
            }

            // La IP A ya está limitada
            var reqA = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
            { Content = JsonContent.Create(dto) };
            reqA.Headers.Add("X-Forwarded-For", "203.0.113.10");
            var respA = await client.SendAsync(reqA);
            Assert.Equal(HttpStatusCode.TooManyRequests, respA.StatusCode);

            // Una IP distinta tiene su propio bucket → NO limitada (prueba que
            // el limiter particiona por la IP de X-Forwarded-For, no por la del proxy).
            var reqB = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
            { Content = JsonContent.Create(dto) };
            reqB.Headers.Add("X-Forwarded-For", "203.0.113.20");
            var respB = await client.SendAsync(reqB);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, respB.StatusCode);
        }
    }
}
