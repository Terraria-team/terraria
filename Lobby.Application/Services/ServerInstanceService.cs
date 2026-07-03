using Lobby.Application.Contracts;
using Lobby.Application.Entities;

namespace Lobby.Application.Services;

public class ServerInstanceService : IServerInstanceService
{
    private readonly IServerInstanceRepository _repository;
    private readonly IServerInstanceSpawner _serverInstanceSpawner;

    public ServerInstanceService(IServerInstanceRepository repository, IServerInstanceSpawner serverInstanceSpawner)
    {
        _repository = repository;
        _serverInstanceSpawner = serverInstanceSpawner;
    }

    public async Task<List<ServerInstanceEntity>> GetAll()
    {
        return await _repository.GetAll();
    }

    public async Task<ServerInstanceEntity> Create()
    {
        int freePort = await _repository.GetFreeInstancePort();
        var info = await _serverInstanceSpawner
            .CreateNewServerInstance(freePort, $"server_instance_{freePort}"); 
        var newInstance = new ServerInstanceEntity
        (
            Id: Guid.NewGuid(),
            ContainerId: info.ContainerId,
            CreatedAt: DateTime.UtcNow,
            Image: info.Image,
            Name: info.Name,
            PlayerCount: 0,
            Port: info.Port,
            Status: info.Status,
            UpdatedAt: null,
            EmptySince: null
        );

        return await _repository.Create(newInstance);
    }
    
    
}