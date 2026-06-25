namespace Lobby.Application.Entities;

public record DockerContainerInfo(
    string ContainerId,
    string Image,
    string Name,
    int Port,
    ServerInstanceStatus Status
);
