using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lobby.Infrastructure.ExternalServices;
using Lobby.Infrastructure.ExternalServices.Settings;
using Microsoft.IdentityModel.Tokens;

namespace Lobby.Tests.Services;

/// <summary>
/// Юніт-тести для <see cref="JwtService"/>.
/// Перевіряють, що згенерований токен містить коректні клейми,
/// правильні issuer і audience та валідується підписом із тих самих налаштувань.
/// </summary>
public class JwtServiceTests
{
    private static readonly JwtSettings Settings = new()
    {
        Secret = "super-secret-test-key-that-is-long-enough-32b",
        Issuer = "test-issuer",
        Audience = "test-audience",
        RefreshTokenExpiryDays = 7
    };

    private static JwtService CreateSut() => new(Settings);

    private static JwtSecurityToken Decode(string token) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token);

    // Токен містить коректні клейми: sub, email, name та role.
    [Fact]
    public void GenerateToken_ContainsCorrectClaims()
    {
        var playerId = Guid.NewGuid();

        var token = CreateSut().GenerateToken(playerId, "player@example.com", "User", "Player One");

        var jwt = Decode(token);
        Assert.Equal(playerId.ToString(), jwt.Subject);
        Assert.Equal("player@example.com", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("Player One", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Name).Value);
        Assert.Equal("User", jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    // Токен підписаний правильним ключем і має коректні issuer та audience.
    [Fact]
    public void GenerateToken_ValidatesWithConfiguredKeyIssuerAndAudience()
    {
        var playerId = Guid.NewGuid();

        var token = CreateSut().GenerateToken(playerId, "player@example.com", "User", "Player One");

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Settings.Issuer,
            ValidateAudience = true,
            ValidAudience = Settings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Settings.Secret)),
            ValidateLifetime = false
        };

        var principal = new JwtSecurityTokenHandler().ValidateToken(token, validationParams, out _);
        Assert.NotNull(principal);
    }
}
