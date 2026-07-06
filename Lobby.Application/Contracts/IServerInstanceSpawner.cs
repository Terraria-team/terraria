using Lobby.Application.Entities;

namespace Lobby.Application.Contracts;

public interface IServerInstanceSpawner
{
    Task<ServerInstanceSpawnInfoEntity> CreateNewServerInstance(int port, string name);
}