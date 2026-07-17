using Lobby.Application.Entities;

namespace Lobby.Application.Contracts;

public interface IRefreshTokenRepository
{
    Task<RefreshTokenEntity?> GetRefreshToken(string tokenHash);
    Task<RefreshTokenEntity> AddRefreshToken(RefreshTokenEntity token);
    Task RevokeAllTokens(Guid playerId);
    Task RevokeToken(string tokenHash);
    Task DeleteAllExpiredOrRevokedTokens(CancellationToken cancellationToken);
}