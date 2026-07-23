using Lobby.Application.Models;

namespace Lobby.Application.UseCases;

public interface IServerInstanceService
{
    Task<List<ServerInstanceModel>> GetAll();

    Task<ResultModel<ServerInstanceModel>> Create(string? name, Guid ownerId);

    Task<ResultModel<ServerInstanceModel>> UpdatePlayerCount(Guid id, int playerCount);

    Task CleanupInactiveServersAsync(CancellationToken cancellationToken);
}
