using Lobby.Application.Contracts.ExternalServices;
using Lobby.Application.Contracts.Repositories;
using Lobby.Application.Domain;
using Lobby.Application.Models;
using Lobby.Application.UseCases;
using Moq;

namespace Lobby.Tests.Services;

/// <summary>
/// Юніт-тести для <see cref="AuthService.Refresh"/> (ротація refresh-токена).
/// Покривають гілки: токен відсутній, токен протермінований, а також
/// happy-path, де старий токен відкликається й видається нова пара
/// access+refresh.
/// </summary>
public class AuthServiceRefreshTests
{
    private readonly Mock<IJwtService> _jwtService = new();
    private readonly Mock<IPlayerRepository> _playerRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<ITokenService> _tokenService = new();

    public AuthServiceRefreshTests()
    {
        _tokenService.Setup(t => t.HashToken(It.IsAny<string>())).Returns((string token) => $"hash_{token}");
        _tokenService.Setup(t => t.GenerateRandomToken()).Returns("new_session_token");
    }

    private AuthService CreateSut() => new(
        externalAuthProvider: Mock.Of<IExternalAuthProvider>(),
        jwtService: _jwtService.Object,
        identityRepository: Mock.Of<IPlayerExternalIdentityRepository>(),
        playerRepository: _playerRepository.Object,
        refreshTokenRepository: _refreshTokenRepository.Object,
        tokenService: _tokenService.Object,
        refreshTokenSettings: new RefreshTokenSettings { RefreshTokenExpiryDays = 7 });

    private static RefreshTokenEntity Token(Guid playerId, DateTime expiresAt) => new()
    {
        Id = Guid.NewGuid(),
        PlayerId = playerId,
        TokenHash = "hash_old",
        ExpiresAt = expiresAt,
        IsRevoked = false,
        CreatedByIp = "127.0.0.1"
    };

    private static PlayerEntity Player(Guid id) => new()
    {
        Id = id,
        Email = "player@example.com",
        Name = "Player",
        Role = "User"
    };

    // Токена немає в репозиторії → помилка валідації, ротація не відбувається.
    [Fact]
    public async Task Refresh_WhenTokenNotFound_ReturnsValidationErrorAndDoesNotRotate()
    {
        _refreshTokenRepository.Setup(r => r.GetRefreshToken("hash_old"))
            .ReturnsAsync((RefreshTokenEntity?)null);

        var result = await CreateSut().Refresh("old", "127.0.0.1");

        Assert.False(result.IsSuccessful);
        Assert.Equal(ErrorType.Validation, result.Error!.ErrorType);
        _refreshTokenRepository.Verify(r => r.AddRefreshToken(It.IsAny<RefreshTokenEntity>()), Times.Never);
        _refreshTokenRepository.Verify(r => r.RevokeToken(It.IsAny<string>()), Times.Never);
    }

    // Токен протермінований → помилка валідації, новий токен не створюється.
    [Fact]
    public async Task Refresh_WhenTokenExpired_ReturnsValidationErrorAndDoesNotRotate()
    {
        _refreshTokenRepository.Setup(r => r.GetRefreshToken("hash_old"))
            .ReturnsAsync(Token(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-1)));

        var result = await CreateSut().Refresh("old", "127.0.0.1");

        Assert.False(result.IsSuccessful);
        Assert.Equal(ErrorType.Validation, result.Error!.ErrorType);
        _refreshTokenRepository.Verify(r => r.AddRefreshToken(It.IsAny<RefreshTokenEntity>()), Times.Never);
    }

    // Валідний токен → старий відкликається, а для гравця зберігається новий хешований токен і повертається нова пара.
    [Fact]
    public async Task Refresh_WithValidToken_RevokesOldTokenAndIssuesNewPair()
    {
        var playerId = Guid.NewGuid();
        _refreshTokenRepository.Setup(r => r.GetRefreshToken("hash_old"))
            .ReturnsAsync(Token(playerId, DateTime.UtcNow.AddDays(1)));
        _playerRepository.Setup(p => p.GetById(playerId)).ReturnsAsync(Player(playerId));
        _jwtService
            .Setup(j => j.GenerateToken(playerId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("new_access_token");

        var result = await CreateSut().Refresh("old", "10.0.0.1");

        Assert.True(result.IsSuccessful);
        Assert.Equal("new_access_token", result.Result!.accessToken);
        Assert.Equal("new_session_token", result.Result!.sessionToken);

        _refreshTokenRepository.Verify(r => r.RevokeToken("hash_old"), Times.Once);
        _refreshTokenRepository.Verify(r => r.AddRefreshToken(It.Is<RefreshTokenEntity>(
            t => t.PlayerId == playerId
                 && t.TokenHash == "hash_new_session_token"
                 && !t.IsRevoked
                 && t.CreatedByIp == "10.0.0.1")), Times.Once);
    }
}
