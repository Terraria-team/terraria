using Lobby.Application.Contracts;
using Lobby.Application.Entities;
using Lobby.Application.Models;
using Lobby.Application.Services;
using Lobby.Application.Settings;
using Moq;

namespace Lobby.Tests.Services;

/// <summary>
/// Юніт-тести для <see cref="AuthService.LoginWithGoogle"/>.
/// Покривають гілки: невдалий обмін коду Google, перший вхід
/// (створюється новий гравець) і повторний вхід (наявний гравець,
/// старі сесії відкликаються). Перевіряють і повернуті токени, і
/// побічні ефекти (створення гравця чи відкликання токенів).
/// </summary>
public class AuthServiceLoginWithGoogleTests
{
    private readonly Mock<IGoogleAuthService> _googleAuthService = new();
    private readonly Mock<IJwtService> _jwtService = new();
    private readonly Mock<IPlayerGoogleLoginRepository> _googleLoginRepository = new();
    private readonly Mock<IPlayerRepository> _playerRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<ITokenService> _tokenService = new();

    public AuthServiceLoginWithGoogleTests()
    {
        _tokenService.Setup(t => t.HashToken(It.IsAny<string>())).Returns((string s) => $"hash_{s}");
        _tokenService.Setup(t => t.GenerateRandomToken()).Returns("session");
        _jwtService
            .Setup(j => j.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("access");
    }

    private AuthService CreateSut() => new(
        googleAuthService: _googleAuthService.Object,
        jwtService: _jwtService.Object,
        googleLoginRepository: _googleLoginRepository.Object,
        playerRepository: _playerRepository.Object,
        refreshTokenRepository: _refreshTokenRepository.Object,
        tokenService: _tokenService.Object,
        jwtSettings: new JwtSettings { RefreshTokenExpiryDays = 7 });

    private static PlayerGoogleLoginModel GoogleModel() => new()
    {
        GoogleId = "google-123",
        Email = "new@example.com",
        Name = "New Player",
        Role = "User"
    };

    // Обмін коду Google не вдався → повертається помилка, гравець не створюється.
    [Fact]
    public async Task LoginWithGoogle_WhenGoogleExchangeFails_ReturnsErrorAndCreatesNoPlayer()
    {
        ResultModel<PlayerGoogleLoginModel> failure = ErrorModel.Unauthorized("bad code");
        _googleAuthService.Setup(g => g.ExchangeCode("code", "uri")).ReturnsAsync(failure);

        var result = await CreateSut().LoginWithGoogle("code", "uri", "127.0.0.1");

        Assert.False(result.IsSuccessful);
        Assert.Equal(ErrorType.Unauthorized, result.Error!.ErrorType);
        _playerRepository.Verify(p => p.Create(It.IsAny<PlayerEntity>()), Times.Never);
        _refreshTokenRepository.Verify(r => r.AddRefreshToken(It.IsAny<RefreshTokenEntity>()), Times.Never);
    }

    // Google-логіна ще немає → створюється новий гравець із профілю Google і повертаються токени.
    [Fact]
    public async Task LoginWithGoogle_WhenNoExistingLogin_CreatesPlayerAndReturnsTokens()
    {
        ResultModel<PlayerGoogleLoginModel> success = GoogleModel();
        _googleAuthService.Setup(g => g.ExchangeCode("code", "uri")).ReturnsAsync(success);
        _googleLoginRepository.Setup(r => r.GetById("google-123")).ReturnsAsync((PlayerGoogleLoginEntity?)null);

        var result = await CreateSut().LoginWithGoogle("code", "uri", "127.0.0.1");

        Assert.True(result.IsSuccessful);
        Assert.Equal("access", result.Result!.accessToken);
        Assert.Equal("session", result.Result!.sessionToken);

        _playerRepository.Verify(p => p.Create(It.Is<PlayerEntity>(
            pe => pe.Email == "new@example.com"
                  && pe.Name == "New Player"
                  && pe.GoogleLogin != null
                  && pe.GoogleLogin.GoogleId == "google-123")), Times.Once);
        _refreshTokenRepository.Verify(r => r.RevokeAllTokens(It.IsAny<Guid>()), Times.Never);
    }

    // Google-логін уже існує → новий гравець не створюється, усі попередні сесії відкликаються.
    [Fact]
    public async Task LoginWithGoogle_WhenExistingLogin_RevokesOldSessionsAndCreatesNoPlayer()
    {
        var playerId = Guid.NewGuid();
        var existingPlayer = new PlayerEntity
        {
            Id = playerId, Email = "existing@example.com", Name = "Existing", Role = "User"
        };
        var existingLogin = new PlayerGoogleLoginEntity
        {
            GoogleId = "google-123", PlayerId = playerId, Player = existingPlayer
        };

        ResultModel<PlayerGoogleLoginModel> success = GoogleModel();
        _googleAuthService.Setup(g => g.ExchangeCode("code", "uri")).ReturnsAsync(success);
        _googleLoginRepository.Setup(r => r.GetById("google-123")).ReturnsAsync(existingLogin);

        var result = await CreateSut().LoginWithGoogle("code", "uri", "127.0.0.1");

        Assert.True(result.IsSuccessful);
        _playerRepository.Verify(p => p.Create(It.IsAny<PlayerEntity>()), Times.Never);
        _refreshTokenRepository.Verify(r => r.RevokeAllTokens(playerId), Times.Once);
    }
}
