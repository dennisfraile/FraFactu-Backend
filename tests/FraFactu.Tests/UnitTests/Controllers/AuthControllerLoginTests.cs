using FraFactu.API.Controllers;
using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs.Auth;
using FraFactu.Application.Interfaces;
using FraFactu.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace FraFactu.Tests.UnitTests.Controllers
{
    public class AuthControllerLoginTests
    {
        private static AuthController BuildController(Mock<IAuthService> authService)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            var context = new ApplicationDbContext(options);

            var jwt = new Mock<IOptions<JwtSettings>>();
            jwt.Setup(x => x.Value).Returns(new JwtSettings
            {
                SecretKey = "super_secret_key_for_testing_purposes_only_1234567890",
                Issuer = "I", Audience = "A", ExpirationMinutes = 60
            });

            var controller = new AuthController(authService.Object, context, jwt.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
            return controller;
        }

        [Fact]
        public async Task Login_Returns423AndRetryAfter_WhenBloqueado()
        {
            var svc = new Mock<IAuthService>();
            svc.Setup(x => x.LoginAsync(It.IsAny<LoginDto>()))
               .ReturnsAsync(LoginResultado.Bloqueado(900));
            var controller = BuildController(svc);

            var action = await controller.Login(new LoginDto { Email = "a@b.com", Password = "x" });

            var result = Assert.IsType<ObjectResult>(action.Result);
            Assert.Equal(StatusCodes.Status423Locked, result.StatusCode);
            Assert.Equal("900", controller.Response.Headers["Retry-After"].ToString());
        }

        [Fact]
        public async Task Login_Returns401_WhenCredencialesInvalidas()
        {
            var svc = new Mock<IAuthService>();
            svc.Setup(x => x.LoginAsync(It.IsAny<LoginDto>()))
               .ReturnsAsync(LoginResultado.CredencialesInvalidas());
            var controller = BuildController(svc);

            var action = await controller.Login(new LoginDto { Email = "a@b.com", Password = "x" });

            Assert.IsType<UnauthorizedObjectResult>(action.Result);
        }

        [Fact]
        public async Task Login_Returns200_WhenOk()
        {
            var svc = new Mock<IAuthService>();
            svc.Setup(x => x.LoginAsync(It.IsAny<LoginDto>()))
               .ReturnsAsync(LoginResultado.Ok(new LoginResponseDto { Token = "t", Email = "a@b.com" }));
            var controller = BuildController(svc);

            var action = await controller.Login(new LoginDto { Email = "a@b.com", Password = "x" });

            var ok = Assert.IsType<OkObjectResult>(action.Result);
            Assert.IsType<LoginResponseDto>(ok.Value);
        }
    }
}
