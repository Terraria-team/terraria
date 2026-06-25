using Lobby.Application.Entities;
using Lobby.Application.Repositories;

namespace Lobby.Application.Services;

public class ServerInstanceService : IServerInstanceService
{
    private readonly IServerInstanceRepository _repository;
    private readonly IServerInstanseManagementService _serverInstanseManagementService;

    public ServerInstanceService(IServerInstanceRepository repository, IServerInstanseManagementService serverInstanseManagementService)
    {
        _repository = repository;
        _serverInstanseManagementService = serverInstanseManagementService;
    }

    public async Task<List<ServerInstance>> GetAll()
    {
        return await _repository.GetAll();
    }

    public async Task<ServerInstance> Create()
    {
        int freePort = await _repository.GetFreeInstancePort();
        var info = await _serverInstanseManagementService
            .CreateNewServerInstance(freePort, $"server_instance_{freePort}"); 
        var newInstance = new ServerInstance
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