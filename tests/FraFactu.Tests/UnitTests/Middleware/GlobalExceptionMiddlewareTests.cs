using System.Text.Json;
using FluentAssertions;
using FraFactu.API.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;

namespace FraFactu.Tests.UnitTests.Middleware
{
    public class GlobalExceptionMiddlewareTests
    {
        // Ejecuta el middleware con un 'next' que lanza 'thrown' y devuelve status + errorCode + detail.
        private static async Task<(int status, string errorCode, string detail)> RunAsync(
            Exception thrown, string environmentName)
        {
            var envMock = new Mock<IWebHostEnvironment>();
            envMock.SetupGet(e => e.EnvironmentName).Returns(environmentName);

            RequestDelegate next = _ => throw thrown;
            var middleware = new GlobalExceptionMiddleware(
                next, NullLogger<GlobalExceptionMiddleware>.Instance, envMock.Object);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            await middleware.InvokeAsync(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var errorCode = root.GetProperty("errorCode").GetString() ?? "";
            var detail = root.TryGetProperty("detail", out var d) ? d.GetString() ?? "" : "";
            return (context.Response.StatusCode, errorCode, detail);
        }

        // Construye un PostgresException real con el SqlState dado (constructor público de Npgsql 8).
        private static PostgresException Pg(string sqlState, string message) =>
            new PostgresException(message, "ERROR", "ERROR", sqlState);

        [Fact] // caracterización: pasa en el código actual y en el nuevo
        public async Task UniqueViolation_Returns409_DuplicateEntry()
        {
            var (status, code, _) = await RunAsync(
                new DbUpdateException("update failed", Pg(PostgresErrorCodes.UniqueViolation, "llave duplicada")),
                "Development");
            status.Should().Be(409);
            code.Should().Be("DUPLICATE_ENTRY");
        }

        [Fact] // caracterización
        public async Task CheckViolation_Returns409_StockConstraint()
        {
            var (status, code, _) = await RunAsync(
                new DbUpdateException("update failed", Pg(PostgresErrorCodes.CheckViolation, "check violation")),
                "Development");
            status.Should().Be(409);
            code.Should().Be("STOCK_CONSTRAINT_VIOLATION");
        }

        [Fact] // DIFERENCIADOR: falla en el código actual (da 409 por string-match), pasa con el nuevo
        public async Task NonPostgresDuplicateKeyText_Returns400_NotFalsePositive()
        {
            var inner = new InvalidOperationException("duplicate key value violates something");
            var (status, code, _) = await RunAsync(
                new DbUpdateException("update failed", inner), "Development");
            status.Should().Be(400);
            code.Should().Be("DATABASE_ERROR");
        }

        [Fact] // saneo de detail: el caso Production falla en el código actual (filtra el mensaje crudo)
        public async Task UniqueViolation_InProduction_DoesNotLeakRawMessage()
        {
            var raw = "llave duplicada viola restriccion unica xyz";
            var (status, code, detail) = await RunAsync(
                new DbUpdateException("update failed", Pg(PostgresErrorCodes.UniqueViolation, raw)),
                "Production");
            status.Should().Be(409);
            code.Should().Be("DUPLICATE_ENTRY");
            detail.Should().NotContain("xyz");
        }

        [Fact] // caracterización: en Development sí se incluye el mensaje crudo
        public async Task UniqueViolation_InDevelopment_IncludesRawMessage()
        {
            var raw = "llave duplicada viola restriccion unica xyz";
            var (_, _, detail) = await RunAsync(
                new DbUpdateException("update failed", Pg(PostgresErrorCodes.UniqueViolation, raw)),
                "Development");
            detail.Should().Contain("xyz");
        }
    }
}
