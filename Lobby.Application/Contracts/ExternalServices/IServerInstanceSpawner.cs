using Lobby.Application.Domain;

namespace Lobby.Application.Contracts.ExternalServices;

public interface IServerInstanceSpawner
{
    Task<ServerInstanceSpawnInfoEntity> CreateNewServerInstance(Guid id, string name);
}