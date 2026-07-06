using Lobby.Application.Contracts;
using Lobby.Application.Entities;
using Lobby.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lobby.Infrastructure.Repositories;

public class EfPlayerRepository : IPlayerRepository
{
    private readonly LobbyDbContext _context;

    public EfPlayerRepository(LobbyDbContext context)
    {
        _context = context;
    }
    public async Task<PlayerEntity?> GetById(Guid id)
    {
        return await _context.Players.FindAsync(id);
    }
    
    public async Task<bool> IsEmailTakenAsync(string email, Guid currentId)
    {
        return await _context.Players.AnyAsync(p => p.Email == email && p.Id != currentId);
    }
    public async Task<PlayerEntity?> GetByEmail(string email)
    {
        return await _context.Players
            .Include(p => p.GoogleLogin)
            .FirstOrDefaultAsync(l => l.Email == email);
    }

    public async Task<bool> PlayerExists(Guid id)
    {
        return await _context.Players.AnyAsync(p => p.Id == id);
    }

    public async Task<List<PlayerEntity>> GetAll(int skip, int take)
    {
        return await _context.Players
            .OrderBy(p => p.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }
    

    public async Task<int> CountAll()
    {
        return await _context.Players.CountAsync();
    }

    public async Task<bool> Delete(Guid id)
    {
        var existing = await _context.Players.FindAsync(id);
        if (existing is null)
        {
            return false;
        }

        _context.Players.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<PlayerEntity?> Update(PlayerEntity player)
    {
        var existing = await _context.Players.FindAsync(player.Id);
        if (existing is null)
        {
            return null;
        }
        
        _context.Players.Entry(existing).CurrentValues.SetValues(player);
        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<PlayerEntity> Create(PlayerEntity player)
    {
        _context.Players.Add(player);
        await _context.SaveChangesAsync();
        return player;
    }
}