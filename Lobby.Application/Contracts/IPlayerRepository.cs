using Lobby.Application.Entities;

namespace Lobby.Application.Contracts;

public interface IPlayerRepository
{
    Task<PlayerEntity?> GetById(Guid id);
    Task<bool> IsEmailTakenAsync(string email, Guid currentId);

    Task<bool> PlayerExists(Guid id);
    
    Task<PlayerEntity?> GetByEmail(string email);
    Task<List<PlayerEntity>> GetAll(int skip, int take);
    Task<int> CountAll();
    Task<bool> Delete(Guid id);
    
    Task<PlayerEntity?> Update(PlayerEntity player);
    
    Task<PlayerEntity> Create(PlayerEntity player);
}