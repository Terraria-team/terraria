using Lobby.Application.Domain;

namespace Lobby.Application.Contracts.Repositories;

public interface IServerInstanceRepository
{
    Task<List<ServerInstanceEntity>> GetAll();
    Task<ServerInstanceEntity> Create(ServerInstanceEntity entity);
    
    Task<List<ServerInstanceEntity>> GetAllNonDeleted();

    Task<int> GetAllNonDeletedCount();

    Task<ServerInstanceEntity?> GetByContainerId(string id);

    Task CreateMany(HashSet<ServerInstanceEntity> entities);
    Task UpdateMany(HashSet<ServerInstanceEntity> entities);

    Task<ServerInstanceEntity?> GetById(Guid id);

    Task<ServerInstanceEntity?> Update(ServerInstanceEntity entity);
}
