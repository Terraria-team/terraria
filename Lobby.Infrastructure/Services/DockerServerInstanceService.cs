using Docker.DotNet;
using Docker.DotNet.Models;
using Lobby.Application.Contracts;
using Lobby.Application.Entities;
using Lobby.Infrastructure.Settings;
using LobbyUnityShared;
namespace Lobby.Infrastructure.Services;

public class DockerServerInstanceService : IServerInstanceSpawner
{
    private readonly IDockerClient _dockerClient;
    private readonly DockerServerSettings _settings;
    private readonly ServerToLobbyAuthSettings _authSettings;

    public DockerServerInstanceService(IDockerClient dockerClient, DockerServerSettings settings, ServerToLobbyAuthSettings authSettings)
    {
        _dockerClient = dockerClient;
        _settings = settings;
        _authSettings = authSettings;
    }

    public async Task<ServerInstanceSpawnInfoEntity> CreateNewServerInstance(Guid id, string name)
    {
        var targetPort = "7777/udp";
        var parameters = new CreateContainerParameters
        {
            Image = _settings.ImageName,
            Name = name,
            Env = new List<string> 
            { 
                $"{EnvVariables.ServerInstanceId}={id}",
                $"{EnvVariables.ServerApiKey}={_authSettings.ApiKey}"
            },
            HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    { 
                       targetPort, 
                        new List<PortBinding> { new PortBinding { HostPort = "" } }
                    }
                },
            },
        };
        
        var response = await _dockerClient.Containers.CreateContainerAsync(parameters);
        await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());
        
        var inspect = await _dockerClient.Containers.InspectContainerAsync(response.ID);
        int hostPort = 0;
        
        if (inspect.NetworkSettings.Ports != null && 
            inspect.NetworkSettings.Ports.TryGetValue(targetPort, out var bindings) && 
            bindings != null)
        {
            var portString = bindings.FirstOrDefault()?.HostPort;
            if (!string.IsNullOrEmpty(portString)) hostPort = int.Parse(portString);
        }
    
        return new ServerInstanceSpawnInfoEntity(
            ContainerId: response.ID,
            Image: _settings.ImageName,
            Name: name,
            Port: hostPort,
            Status: ServerInstanceStatus.Running
        );
    }
}