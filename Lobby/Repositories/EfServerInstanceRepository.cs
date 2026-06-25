using Lobby.Application.Entities;
using Lobby.Application.Repositories;
using Lobby.Data;
using Microsoft.EntityFrameworkCore;

namespace Lobby.Repositories;

public class EfServerInstanceRepository : IServerInstanceRepository
{
    private readonly LobbyDbContext _context;

    public EfServerInstanceRepository(LobbyDbContext context)
    {
        _context = context;
    }

    public async Task<List<ServerInstance>> GetAll()
    {
        return await _context.ServerInstances.ToListAsync();
    }

    public async Task<ServerInstance> Create(ServerInstance entity)
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