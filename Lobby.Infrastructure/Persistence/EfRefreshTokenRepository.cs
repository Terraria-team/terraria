using Lobby.Application.Contracts.Repositories;
using Lobby.Application.Domain;
using Microsoft.EntityFrameworkCore;

namespace Lobby.Infrastructure.Persistence;

public class EfRefreshTokenRepository : IRefreshTokenRepository
{
    
    private readonly LobbyDbContext _context;

    public EfRefreshTokenRepository(LobbyDbContext context)
    {
        _context = context;
    }
    
    public async Task<RefreshTokenEntity?> GetRefreshToken(string tokenHash)
    {
        return await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);
    }

    public async Task<RefreshTokenEntity> AddRefreshToken(RefreshTokenEntity token)
    {
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();
        return token;
    }

    public async Task RevokeAllTokens(Guid playerId)
    {
        var existing = await _context.RefreshTokens.Where(rt => rt.PlayerId == playerId).ToListAsync();
        foreach (var token in existing)
        {
            _context.RefreshTokens.Remove(token);
        }
        await _context.SaveChangesAsync();
    }

    public async Task RevokeToken(string tokenHash)
    {
        var existing = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);
        if(existing is null) return;
        _context.RefreshTokens.Remove(existing);
        await _context.SaveChangesAsync();
    }
    
    public async Task DeleteAllExpiredOrRevokedTokens(CancellationToken cancellationToken)
    {
        await _context.RefreshTokens
            .Where(rt => rt.IsRevoked || rt.ExpiresAt < DateTime.UtcNow)
            .ExecuteDeleteAsync(cancellationToken);
    }
    
}