using Docker.DotNet;
using Docker.DotNet.Models;
using Lobby.Application.Contracts;
using Lobby.Application.Entities;
using Lobby.Application.Services;
using Lobby.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace Lobby.Infrastructure.Services;

public class DockerServerInstanceService : IServerInstanceSpawner
{
    private readonly IDockerClient _dockerClient;
    private readonly DockerServerSettings _settings;

    public DockerServerInstanceService(IDockerClient dockerClient, DockerServerSettings settings)
    {
        _dockerClient = dockerClient;
        _settings = settings;
    }

    public async Task<ServerInstanceSpawnInfoEntity> CreateNewServerInstance(int port, string name)
    {
        var parameters = new CreateContainerParameters
        {
            Image = _settings.ImageName,
            Name = name,
            
            HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    { 
                        "7777/udp", 
                        new List<PortBinding> { new PortBinding { HostPort = port.ToString() } } 
                    }
                },
            },
        };
        
        var response = await _dockerClient.Containers.CreateContainerAsync(parameters);
        await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());
        
        return new ServerInstanceSpawnInfoEntity(
            ContainerId: response.ID,
            Image: _settings.ImageName,
            Name: name,
            Port: port,
            Status: ServerInstanceStatus.Running
        );
    }
}