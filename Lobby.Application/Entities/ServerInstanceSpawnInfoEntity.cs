namespace Lobby.Application.Entities;

public record ServerInstanceSpawnInfoEntity(
    string ContainerId,
    string Image,
    string Name,
    int Port,
    ServerInstanceStatus Status
);
