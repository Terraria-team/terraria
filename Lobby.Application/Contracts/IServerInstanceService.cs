using Lobby.Application.Entities;

namespace Lobby.Application.Contracts;

public interface IServerInstanceService
{
    Task<List<ServerInstanceEntity>> GetAll();

    Task<ServerInstanceEntity> Create();
}