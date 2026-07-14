using Lobby.Application.Contracts;
using Lobby.Application.Entities;
using Lobby.Application.Models;
using Lobby.Application.Services;
using Lobby.Application.Settings;
using Moq;

namespace Lobby.Tests.Services;

/// <summary>
/// Unit tests for <see cref="AuthService.Logout"/>.
/// Covers every branch of the method: token missing, token expired,
/// token owned by another player, and the successful revocation path.
/// Dependencies are mocked (Moq); no DB, no network.
/// </summary>
public class AuthServiceLogoutTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<ITokenService> _tokenService = new();

    public AuthServiceLogoutTests()
    {
        _tokenService
            .Setup(t => t.HashToken(It.IsAny<string>()))
            .Returns((string token) => $"hash_{token}");
    }

    private AuthService CreateSut() => new(
        googleAuthService: Mock.Of<IGoogleAuthService>(),
        jwtService: Mock.Of<IJwtService>(),
        googleLoginRepository: Mock.Of<IPlayerGoogleLoginRepository>(),
        playerRepository: Mock.Of<IPlayerRepository>(),
        refreshTokenRepository: _refreshTokenRepository.Object,
        tokenService: _tokenService.Object,
        jwtSettings: new JwtSettings());

    private static RefreshTokenEntity Token(Guid playerId, DateTime expiresAt) => new()
    {
        Id = Guid.NewGuid(),
        PlayerId = playerId,
        TokenHash = "hash_tok",
        ExpiresAt = expiresAt,
        IsRevoked = false,
        CreatedByIp = "127.0.0.1"
    };

    [Fact]
    public async Task Logout_WhenTokenNotFound_ReturnsValidationErrorAndDoesNotRevoke()
    {
        _refreshTokenRepository
            .Setup(r => r.GetRefreshToken("hash_tok"))
            .ReturnsAsync((RefreshTokenEntity?)null);

        var result = await CreateSut().Logout(Guid.NewGuid(), "tok");

        Assert.False(result.IsSuccessful);
        Assert.Equal(ErrorType.Validation, result.Error!.ErrorType);
        _refreshTokenRepository.Verify(r => r.RevokeToken(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Logout_WhenTokenExpired_ReturnsValidationErrorAndDoesNotRevoke()
    {
        var playerId = Guid.NewGuid();

        _refreshTokenRepository
            .Setup(r => r.GetRefreshToken("hash_tok"))
            .ReturnsAsync(Token(playerId, DateTime.UtcNow.AddMinutes(-1)));

        var result = await CreateSut().Logout(playerId, "tok");

        Assert.False(result.IsSuccessful);
        Assert.Equal(ErrorType.Validation, result.Error!.ErrorType);
        _refreshTokenRepository.Verify(r => r.RevokeToken(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Logout_WhenTokenBelongsToAnotherPlayer_ReturnsUnauthorizedAndDoesNotRevoke()
    {
        var tokenOwner = Guid.NewGuid();
        var callerId = Guid.NewGuid();

        _refreshTokenRepository
            .Setup(r => r.GetRefreshToken("hash_tok"))
            .ReturnsAsync(Token(tokenOwner, DateTime.UtcNow.AddDays(1)));

        var result = await CreateSut().Logout(callerId, "tok");

        Assert.False(result.IsSuccessful);
        Assert.Equal(ErrorType.Unauthorized, result.Error!.ErrorType);
        _refreshTokenRepository.Verify(r => r.RevokeToken(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Logout_WithValidToken_RevokesTokenAndReturnsSuccess()
    {
        var playerId = Guid.NewGuid();
        _refreshTokenRepository
            .Setup(r => r.GetRefreshToken("hash_tok"))
            .ReturnsAsync(Token(playerId, DateTime.UtcNow.AddDays(1)));

        var result = await CreateSut().Logout(playerId, "tok");

        Assert.True(result.IsSuccessful);
        Assert.Null(result.Error);
        _refreshTokenRepository.Verify(r => r.RevokeToken("hash_tok"), Times.Once);
    }
}
