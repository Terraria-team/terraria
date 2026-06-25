using Lobby.Application.Entities;

namespace Lobby.Application.Repositories;

public interface IServerInstanceRepository
{
    Task<List<ServerInstance>> GetAll();
    Task<ServerInstance> Create(ServerInstance entity);

    Task<int> GetFreeInstancePort();
}