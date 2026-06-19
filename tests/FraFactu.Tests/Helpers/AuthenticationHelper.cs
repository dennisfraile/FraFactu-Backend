using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace FraFactu.Tests.Helpers;

/// <summary>
/// Helper para generar tokens JWT válidos para tests
/// </summary>
public static class AuthenticationHelper
{
    private const string SecretKey = "test-secret-key-minimum-32-characters-long-for-security";
    private const string Issuer = "TestIssuer";
    private const string Audience = "TestAudience";

    public static string GenerateJwtToken(
        string userId = "test-user-id",
        string email = "test@example.com",
        string role = "Admin")
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static HttpClient CreateAuthenticatedClient(
        HttpClient client,
        string userId = "test-user-id",
        string email = "test@example.com",
        string role = "Admin")
    {
        var token = GenerateJwtToken(userId, email, role);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return client;
    }
}
