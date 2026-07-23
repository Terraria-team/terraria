using Lobby.Application.Contracts.ExternalServices;
using Lobby.Application.Contracts.Repositories;
using Lobby.Application.Domain;
using Lobby.Application.Models;

namespace Lobby.Application.UseCases;

public class ServerInstanceService : IServerInstanceService
{
    private readonly IServerInstanceRepository _repository;
    private readonly IServerInstanceSpawner _serverInstanceSpawner;
    private readonly ServerInstanceServiceSettings _settings;

    public ServerInstanceService(
        IServerInstanceRepository repository,
        IServerInstanceSpawner serverInstanceSpawner,
        ServerInstanceServiceSettings settings)
    {
        _repository = repository;
        _serverInstanceSpawner = serverInstanceSpawner;
        _settings = settings;
    }

    public async Task<List<ServerInstanceModel>> GetAll()
    {
        var entities = await _repository.GetAllNonDeleted();
        return entities.Select(ToModel).ToList();
    }

    public async Task<ResultModel<ServerInstanceModel>> Create(string? name, Guid ownerId)
    {
        if (await _repository.GetAllNonDeletedCount() > _settings.ServerInstanceMaxCount)
            return ErrorModel.ResourceExhausted("Currently running max amount of server instances. Can't spawn more.");
        
        var id = Guid.NewGuid();
        var info = await _serverInstanceSpawner.CreateNewServerInstance(id, name ?? id.ToString());
        var newInstance = new ServerInstanceEntity()
        {
            Id = id,
            WorldId = Guid.NewGuid(),
            ContainerId = info.ContainerId,
            CreatedAt = DateTime.UtcNow,
            Image = info.Image,
            Name = info.Name,
            PlayerCount = 0,
            Port = info.Port,
            Status = info.Status,
            UpdatedAt = null,
            EmptySince = null
        };

        newInstance.World = new TerrariaWorldEntity()
        {
            OwnerId = ownerId, 
            Id = newInstance.WorldId,
            Name = name ?? $"unnamed world of {ownerId}",
            StorageId = null
        };

        var created = await _repository.Create(newInstance);
        return ToModel(created);
    }

    public async Task<ResultModel<ServerInstanceModel>> UpdatePlayerCount(Guid id, int playerCount)
    {
        var serverInstance = await _repository.GetById(id);
        if (serverInstance is null) return ErrorModel.NotFound("server instance not found");

        serverInstance.UpdatedAt = DateTime.UtcNow;
        if (playerCount == 0 && serverInstance.PlayerCount != 0)
        {
            serverInstance.EmptySince = DateTime.UtcNow;
        }
        else if (playerCount > 0)
        {
            serverInstance.EmptySince = null;
        }
        
        serverInstance.PlayerCount = playerCount;

        var res = await _repository.Update(serverInstance);
        return res is null ? ErrorModel.NotFound() : ToModel(res);
    }
    
    public Task CleanupInactiveServersAsync(CancellationToken cancellationToken)
    {
        // TODO: move cleanup orchestration here in the next refactoring phase
        return Task.CompletedTask;
    }

    private static ServerInstanceModel ToModel(ServerInstanceEntity e) =>
        new(
            e.Id,
            e.ContainerId,
            e.Image,
            e.Name,
            e.Port,
            e.EmptySince,
            e.CreatedAt,
            e.PlayerCount,
            e.UpdatedAt ?? e.CreatedAt,
            e.Status
        );
}
