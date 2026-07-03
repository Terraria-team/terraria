using Lobby.Application.Contracts;
using Lobby.Application.Entities;
using Lobby.Application.Models;
using Lobby.Application.Settings;

namespace Lobby.Application.Services;

public class AuthService : IAuthService
{
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IJwtService _jwtService;
    private readonly IPlayerGoogleLoginRepository _googleLoginRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        IGoogleAuthService googleAuthService,
        IJwtService jwtService,
        IPlayerGoogleLoginRepository googleLoginRepository,
        IPlayerRepository playerRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        JwtSettings jwtSettings)
    {
        _googleAuthService = googleAuthService;
        _jwtService = jwtService;
        _googleLoginRepository = googleLoginRepository;
        _playerRepository = playerRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings;
    }

    public async Task<ResultModel<LoginTokensModel>> LoginWithGoogle(string googleIdToken, string createdByIp)
    {
        var res = await _googleAuthService.ValidateToken(googleIdToken);

        if (!res.IsSuccessful) return res.Error!;

        var googleLogin = await _googleLoginRepository.GetById(res.Result!.GoogleId);
        PlayerEntity playerEntity;

        if (googleLogin is null)
        {
            // for now i assume that google login is the only way to create a Player account
            // so if no google login have been created the Player with this email/name and etc doesn't exist
            var playerId = Guid.NewGuid();
            playerEntity = new PlayerEntity()
            {
                Id = playerId,
                Email = res.Result!.Email,
                Name = res.Result!.Name,
                Role = res.Result!.Role,
                GoogleLogin = new PlayerGoogleLoginEntity(){GoogleId = res.Result!.GoogleId, PlayerId = playerId}
            };
            
            await _playerRepository.Create(playerEntity);
        }
        else
        {
            playerEntity = googleLogin.Player;
            await _refreshTokenRepository.RevokeAllTokens(playerEntity.Id);
        }

        var bearerToken = CreateAccessToken(playerEntity);

        var sessionToken = await CreateRefreshToken(playerEntity, createdByIp);

        return new LoginTokensModel() { accessToken = bearerToken, sessionToken = sessionToken };
    }
    
    
    public async Task<ResultModel> Logout(Guid playerId, string refreshToken)
    { 
        var refreshTokenToRevoke = await _refreshTokenRepository.GetRefreshToken(_tokenService.HashToken(refreshToken));
        
        if (refreshTokenToRevoke is null || refreshTokenToRevoke.ExpiresAt < DateTime.UtcNow)
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
        if (tokenEntity is null || tokenEntity.ExpiresAt < DateTime.UtcNow)
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
        var expiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);
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