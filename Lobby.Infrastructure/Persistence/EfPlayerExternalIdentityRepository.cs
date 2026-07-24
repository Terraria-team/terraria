using Lobby.Application.Contracts.Repositories;
using Lobby.Application.Domain;
using Microsoft.EntityFrameworkCore;

namespace Lobby.Infrastructure.Persistence;

public class EfPlayerExternalIdentityRepository : IPlayerExternalIdentityRepository
{
    private readonly LobbyDbContext _dbContext;

    public EfPlayerExternalIdentityRepository(LobbyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PlayerExternalIdentityEntity?> GetByExternalIdAsync(string provider, string externalId)
    {
        return await _dbContext.PlayerExternalIdentities
            .Include(e => e.Player)
            .FirstOrDefaultAsync(e => e.Provider == provider && e.ExternalId == externalId);
    }

    public async Task AddAsync(PlayerExternalIdentityEntity identity)
    {
        await _dbContext.PlayerExternalIdentities.AddAsync(identity);
        await _dbContext.SaveChangesAsync();
    }
}
