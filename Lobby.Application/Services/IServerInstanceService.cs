using Lobby.Application.Entities;

namespace Lobby.Application.Services;

public interface IServerInstanceService
{
    Task<List<ServerInstance>> GetAll();

    Task<ServerInstance> Create();
}