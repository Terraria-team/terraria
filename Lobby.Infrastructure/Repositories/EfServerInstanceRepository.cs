using Lobby.Application.Contracts;
using Lobby.Application.Entities;
using Lobby.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lobby.Infrastructure.Repositories;

public class EfServerInstanceRepository : IServerInstanceRepository
{
    private readonly LobbyDbContext _context;

    public EfServerInstanceRepository(LobbyDbContext context)
    {
        _context = context;
    }

    public async Task<List<ServerInstanceEntity>> GetAll()
    {
        return await _context.ServerInstances.ToListAsync();
    }

    public async Task<ServerInstanceEntity> Create(ServerInstanceEntity entity)
    {
        _context.ServerInstances.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<int> GetFreeInstancePort()
    {
        int? maxPort = await _context.ServerInstances.MaxAsync(si => (int?)si.Port);
        return (maxPort ?? 7777) + 1;
    }
}