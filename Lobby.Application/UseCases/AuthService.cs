using Lobby.Application.Contracts.ExternalServices;
using Lobby.Application.Contracts.Repositories;
using Lobby.Application.Domain;
using Lobby.Application.Models;

namespace Lobby.Application.UseCases;

public class AuthService : IAuthService
{
    private readonly IExternalAuthProvider _externalAuthProvider;
    private readonly IJwtService _jwtService;
    private readonly IPlayerExternalIdentityRepository _identityRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly RefreshTokenSettings _refreshTokenSettings;

    public AuthService(
        IExternalAuthProvider externalAuthProvider,
        IJwtService jwtService,
        IPlayerExternalIdentityRepository identityRepository,
        IPlayerRepository playerRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        RefreshTokenSettings refreshTokenSettings)
    {
        _externalAuthProvider = externalAuthProvider;
        _jwtService = jwtService;
        _identityRepository = identityRepository;
        _playerRepository = playerRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _refreshTokenSettings = refreshTokenSettings;
    }

    public async Task<ResultModel<LoginTokensModel>> LoginWithGoogle(string code, string redirectUri, string createdByIp)
    {
        var res = await _externalAuthProvider.VerifyIdentityAsync(code, redirectUri);

        if (!res.IsSuccessful) return res.Error!;

        var identity = await _identityRepository.GetByExternalIdAsync(
            _externalAuthProvider.ProviderName,
            res.Result!.ExternalId);

        PlayerEntity playerEntity;

        if (identity is null)
        {
            // for now i assume that external login is the only way to create a Player account
            // so if no external identity exists the Player with this email/name doesn't exist yet
            var playerId = Guid.NewGuid();
            playerEntity = new PlayerEntity()
            {
                Id = playerId,
                Email = res.Result!.Email,
                Name = res.Result!.Name,
                Role = res.Result!.Role,
            };

            var newIdentity = new PlayerExternalIdentityEntity()
            {
                PlayerId = playerId,
                Provider = _externalAuthProvider.ProviderName,
                ExternalId = res.Result!.ExternalId
            };
            
            await _playerRepository.Create(playerEntity);
            await _identityRepository.AddAsync(newIdentity);
        }
        else
        {
            playerEntity = identity.Player;
            await _refreshTokenRepository.RevokeAllTokens(playerEntity.Id);
        }

        var bearerToken = CreateAccessToken(playerEntity);

        var sessionToken = await CreateRefreshToken(playerEntity, createdByIp);

        return new LoginTokensModel() { accessToken = bearerToken, sessionToken = sessionToken };
    }
    
    
    public async Task<ResultModel> Logout(Guid playerId, string refreshToken)
    { 
        var refreshTokenToRevoke = await _refreshTokenRepository.GetRefreshToken(_tokenService.HashToken(refreshToken));
        
        if (refreshTokenToRevoke is null || refreshTokenToRevoke.IsExpired)
            return ErrorModel.Validation("Invalid or expired refresh token");
        
        if (refreshTokenToRevoke.PlayerId != playerId) return ErrorModel.Unauthorized("");
        
        await _refreshTokenRepository.RevokeToken(_tokenService.HashToken(refreshToken));
        return ResultModel.Success();
    }
    
    public async Task LogoutAll(Guid playerId)
    {
        await _refreshTokenRepository.RevokeAllTokens(playerId);
    }
    
    public async Task<ResultModel<LoginTokensModel>> Refresh(string refreshToken, string createByIp)
    {
        var hash = _tokenService.HashToken(refreshToken);
        var tokenEntity = await _refreshTokenRepository.GetRefreshToken(hash);
        if (tokenEntity is null || tokenEntity.IsExpired)
            return ErrorModel.Validation("Invalid or expired refresh token");
        
        // not sure if i should immediately kickout user just playing from different ip
        // if (createByIp != tokenEntity.CreatedByIp)
        // {
        //     return ErrorModel.Unauthorized("ip mismatch");
        // }

        var player = await _playerRepository.GetById(tokenEntity.PlayerId);
        var sessionToken = await CreateRefreshToken(player!, createByIp);
        
        await _refreshTokenRepository.RevokeToken(hash);
        
        var newAccessToken =  CreateAccessToken(player!);
        return new LoginTokensModel() { accessToken = newAccessToken, sessionToken = sessionToken };
    }
    
    private string CreateAccessToken(PlayerEntity playerEntity)
    {
        return  _jwtService.GenerateToken(playerEntity.Id, playerEntity.Email, playerEntity.Role, playerEntity.Name);
    }
    
    private async Task<string> CreateRefreshToken(PlayerEntity player, string createdByIp)
    {
        var expiry = DateTime.UtcNow.AddDays(_refreshTokenSettings.RefreshTokenExpiryDays);
        var sessionToken = _tokenService.GenerateRandomToken();
        
        var newRefreshToken = new RefreshTokenEntity()
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            TokenHash = _tokenService.HashToken(sessionToken),
            ExpiresAt = expiry,
            CreatedByIp = createdByIp,
            IsRevoked = false
        };
        await _refreshTokenRepository.AddRefreshToken(newRefreshToken);
        return sessionToken;
    }
}
