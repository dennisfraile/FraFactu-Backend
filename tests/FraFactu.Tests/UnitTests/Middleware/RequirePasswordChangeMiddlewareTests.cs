using FraFactu.API.Middleware;
using Microsoft.AspNetCore.Http;

namespace FraFactu.Tests.UnitTests.Middleware
{
    public class RequirePasswordChangeMiddlewareTests
    {
        [Theory]
        [InlineData("/api/auth/change-password-first-login")]
        [InlineData("/API/Auth/Change-Password-First-Login")]
        [InlineData("/api/auth/logout")]
        public void IsAllowed_ReturnsTrue_ForAllowedEndpoints(string path)
        {
            Assert.True(RequirePasswordChangeMiddleware.IsAllowedDuringPasswordChange(new PathString(path)));
        }

        [Theory]
        [InlineData("/api/facturas")]
        [InlineData("/api/auth/me")]
        [InlineData("/api/usuarios")]
        [InlineData("/api/auth/change-password")]
        public void IsAllowed_ReturnsFalse_ForEverythingElse(string path)
        {
            Assert.False(RequirePasswordChangeMiddleware.IsAllowedDuringPasswordChange(new PathString(path)));
        }
    }
}
