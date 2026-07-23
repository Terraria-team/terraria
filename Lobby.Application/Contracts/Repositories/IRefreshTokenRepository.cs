using Lobby.Application.Domain;

namespace Lobby.Application.Contracts.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshTokenEntity?> GetRefreshToken(string tokenHash);
    Task<RefreshTokenEntity> AddRefreshToken(RefreshTokenEntity token);
    Task RevokeAllTokens(Guid playerId);
    Task RevokeToken(string tokenHash);
    Task DeleteAllExpiredOrRevokedTokens(CancellationToken cancellationToken);
}