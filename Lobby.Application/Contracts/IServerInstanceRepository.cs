using Lobby.Application.Entities;

namespace Lobby.Application.Contracts;

public interface IServerInstanceRepository
{
    Task<List<ServerInstanceEntity>> GetAll();
    Task<ServerInstanceEntity> Create(ServerInstanceEntity entity);

    Task<int> GetFreeInstancePort();
}