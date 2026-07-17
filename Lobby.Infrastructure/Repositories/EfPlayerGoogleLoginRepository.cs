using Lobby.Application.Contracts;
using Lobby.Application.Entities;
using Lobby.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lobby.Infrastructure.Repositories;

public class EfPlayerGoogleLoginRepository : IPlayerGoogleLoginRepository
{
    private readonly LobbyDbContext _dbContext;

    public EfPlayerGoogleLoginRepository(LobbyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PlayerGoogleLoginEntity?> GetById(string googleId)
    {
        return await _dbContext.PlayerGoogleLogins
            .Include(gl => gl.Player)
            .FirstOrDefaultAsync(gl => gl.GoogleId == googleId);
    }
}