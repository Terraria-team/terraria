namespace Lobby.Application.Entities;

public record ServerInstance(
    Guid Id,
    string ContainerId,
    string Image,
    string Name,
    int Port,
    int PlayerCount,
    DateTime? EmptySince,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    ServerInstanceStatus Status
);