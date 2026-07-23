using Lobby.Application.Contracts.ExternalServices;
using Lobby.Application.Contracts.Repositories;
using Lobby.Application.Domain;
using Lobby.Application.Models;
using Lobby.Application.UseCases;
using Moq;

namespace Lobby.Tests.Services;

/// <summary>
/// Юніт-тести для <see cref="AuthService.LoginWithGoogle"/>.
/// Покривають гілки: невдала верифікація коду зовнішнім провайдером, перший вхід
/// (створюється новий гравець разом із зовнішньою ідентичністю) і повторний вхід
/// (ідентичність уже є, старі сесії відкликаються). Перевіряють і повернуті токени,
/// і побічні ефекти (створення гравця чи відкликання токенів).
/// </summary>
public class AuthServiceLoginWithGoogleTests
{
    private const string Provider = "Google";

    private readonly Mock<IExternalAuthProvider> _externalAuthProvider = new();
    private readonly Mock<IJwtService> _jwtService = new();
    private readonly Mock<IPlayerExternalIdentityRepository> _identityRepository = new();
    private readonly Mock<IPlayerRepository> _playerRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<ITokenService> _tokenService = new();

    public AuthServiceLoginWithGoogleTests()
    {
        _externalAuthProvider.Setup(p => p.ProviderName).Returns(Provider);
        _tokenService.Setup(t => t.HashToken(It.IsAny<string>())).Returns((string s) => $"hash_{s}");
        _tokenService.Setup(t => t.GenerateRandomToken()).Returns("session");
        _jwtService
            .Setup(j => j.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("access");
    }

    private AuthService CreateSut() => new(
        externalAuthProvider: _externalAuthProvider.Object,
        jwtService: _jwtService.Object,
        identityRepository: _identityRepository.Object,
        playerRepository: _playerRepository.Object,
        refreshTokenRepository: _refreshTokenRepository.Object,
        tokenService: _tokenService.Object,
        refreshTokenSettings: new RefreshTokenSettings { RefreshTokenExpiryDays = 7 });

    private static PlayerExternalIdentityModel IdentityModel() => new()
    {
        ExternalId = "google-123",
        Provider = Provider,
        Email = "new@example.com",
        Name = "New Player",
        Role = "User"
    };

    // Верифікація коду не вдалася → повертається помилка, гравець не створюється.
    [Fact]
    public async Task LoginWithGoogle_WhenExternalVerificationFails_ReturnsErrorAndCreatesNoPlayer()
    {
        ResultModel<PlayerExternalIdentityModel> failure = ErrorModel.Unauthorized("bad code");
        _externalAuthProvider.Setup(g => g.VerifyIdentityAsync("code", "uri")).ReturnsAsync(failure);

        var result = await CreateSut().LoginWithGoogle("code", "uri", "127.0.0.1");

        Assert.False(result.IsSuccessful);
        Assert.Equal(ErrorType.Unauthorized, result.Error!.ErrorType);
        _playerRepository.Verify(p => p.Create(It.IsAny<PlayerEntity>()), Times.Never);
        _refreshTokenRepository.Verify(r => r.AddRefreshToken(It.IsAny<RefreshTokenEntity>()), Times.Never);
    }

    // Зовнішньої ідентичності ще немає → створюється новий гравець із профілю Google,
    // до нього прив'язується ідентичність і повертаються токени.
    [Fact]
    public async Task LoginWithGoogle_WhenNoExistingIdentity_CreatesPlayerWithIdentityAndReturnsTokens()
    {
        ResultModel<PlayerExternalIdentityModel> success = IdentityModel();
        _externalAuthProvider.Setup(g => g.VerifyIdentityAsync("code", "uri")).ReturnsAsync(success);
        _identityRepository
            .Setup(r => r.GetByExternalIdAsync(Provider, "google-123"))
            .ReturnsAsync((PlayerExternalIdentityEntity?)null);

        PlayerEntity? createdPlayer = null;
        _playerRepository
            .Setup(p => p.Create(It.IsAny<PlayerEntity>()))
            .Callback<PlayerEntity>(p => createdPlayer = p)
            .ReturnsAsync((PlayerEntity p) => p);

        PlayerExternalIdentityEntity? createdIdentity = null;
        _identityRepository
            .Setup(r => r.AddAsync(It.IsAny<PlayerExternalIdentityEntity>()))
            .Callback<PlayerExternalIdentityEntity>(i => createdIdentity = i)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().LoginWithGoogle("code", "uri", "127.0.0.1");

        Assert.True(result.IsSuccessful);
        Assert.Equal("access", result.Result!.accessToken);
        Assert.Equal("session", result.Result!.sessionToken);

        Assert.NotNull(createdPlayer);
        Assert.Equal("new@example.com", createdPlayer.Email);
        Assert.Equal("New Player", createdPlayer.Name);
        Assert.Equal("User", createdPlayer.Role);

        // Ідентичність має вказувати саме на щойно створеного гравця.
        Assert.NotNull(createdIdentity);
        Assert.Equal(createdPlayer.Id, createdIdentity.PlayerId);
        Assert.Equal(Provider, createdIdentity.Provider);
        Assert.Equal("google-123", createdIdentity.ExternalId);

        _refreshTokenRepository.Verify(r => r.RevokeAllTokens(It.IsAny<Guid>()), Times.Never);
    }

    // Зовнішня ідентичність уже існує → новий гравець не створюється,
    // усі попередні сесії відкликаються.
    [Fact]
    public async Task LoginWithGoogle_WhenExistingIdentity_RevokesOldSessionsAndCreatesNoPlayer()
    {
        var playerId = Guid.NewGuid();
        var existingPlayer = new PlayerEntity
        {
            Id = playerId, Email = "existing@example.com", Name = "Existing", Role = "User"
        };
        var existingIdentity = new PlayerExternalIdentityEntity
        {
            PlayerId = playerId, Provider = Provider, ExternalId = "google-123", Player = existingPlayer
        };

        ResultModel<PlayerExternalIdentityModel> success = IdentityModel();
        _externalAuthProvider.Setup(g => g.VerifyIdentityAsync("code", "uri")).ReturnsAsync(success);
        _identityRepository
            .Setup(r => r.GetByExternalIdAsync(Provider, "google-123"))
            .ReturnsAsync(existingIdentity);

        var result = await CreateSut().LoginWithGoogle("code", "uri", "127.0.0.1");

        Assert.True(result.IsSuccessful);
        _playerRepository.Verify(p => p.Create(It.IsAny<PlayerEntity>()), Times.Never);
        _identityRepository.Verify(r => r.AddAsync(It.IsAny<PlayerExternalIdentityEntity>()), Times.Never);
        _refreshTokenRepository.Verify(r => r.RevokeAllTokens(playerId), Times.Once);
    }
}
