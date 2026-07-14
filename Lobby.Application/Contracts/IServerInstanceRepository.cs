using Lobby.Application.Entities;

namespace Lobby.Application.Contracts;

public interface IServerInstanceRepository
{
    Task<List<ServerInstanceEntity>> GetAll();
    Task<ServerInstanceEntity> Create(ServerInstanceEntity entity);
    
    Task<List<ServerInstanceEntity>> GetAllNonDeleted();

    Task<ServerInstanceEntity?> GetByContainerId(string id);

    Task CreateMany(HashSet<ServerInstanceEntity> entities);
    Task UpdateMany(HashSet<ServerInstanceEntity> entities);

    Task<ServerInstanceEntity?> GetById(Guid id);

    Task<ServerInstanceEntity?> Update(ServerInstanceEntity entity);
}