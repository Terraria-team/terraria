using Lobby.Application.Entities;
using Lobby.Application.Models;

namespace Lobby.Application.Contracts;

public interface IServerInstanceService
{
    Task<List<ServerInstanceEntity>> GetAll();

    Task<ServerInstanceEntity> Create(string? name, Guid ownerId);

    Task<ResultModel<ServerInstanceEntity>> UpdatePlayerCount(Guid id, int playerCount);
}