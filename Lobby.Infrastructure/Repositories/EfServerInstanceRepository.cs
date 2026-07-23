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

    public async Task<List<ServerInstanceEntity>> GetAllNonDeleted()
    {
        return await _context.ServerInstances.Where(s => s.Status != ServerInstanceStatus.Deleted).ToListAsync();
    }

    public async Task<int> GetAllNonDeletedCount()
    {
        return await _context.ServerInstances.Where(s => s.Status != ServerInstanceStatus.Deleted).CountAsync();
    }

    public async Task<ServerInstanceEntity?> GetByContainerId(string id)
    {
        return await _context.ServerInstances.FirstOrDefaultAsync(s => s.ContainerId == id);
    }

    public async Task CreateMany(HashSet<ServerInstanceEntity> entities)
    {
        _context.ServerInstances.AddRange(entities);
    
        await _context.SaveChangesAsync();
    }

    public async Task UpdateMany(HashSet<ServerInstanceEntity> entities)
    {
        _context.ServerInstances.UpdateRange(entities);
    
        await _context.SaveChangesAsync();
    }

    public async Task<ServerInstanceEntity?> GetById(Guid id)
    {
        return await _context.ServerInstances.FindAsync(id);
    }

    public async Task<ServerInstanceEntity?> Update(ServerInstanceEntity entity)
    {
        var existing = await _context.ServerInstances.FindAsync(entity.Id);
        if (existing is null)
        {
            return null;
        }
        
        _context.ServerInstances.Entry(existing).CurrentValues.SetValues(entity);
        await _context.SaveChangesAsync();
        return existing;
    }
}