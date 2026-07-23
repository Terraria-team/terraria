using Lobby.Application.Domain;

namespace Lobby.HostedServices;

internal readonly record struct ContainerAction(
    ServerInstanceEntity? ToCreate = null,
    bool NeedsUpdate = false,
    bool NeedsContainerRemoval = false,
    bool NeedsGracefulShutdown = false)
{
    public static readonly ContainerAction None = new();

    public static ContainerAction Create(ServerInstanceEntity entity) => new(ToCreate: entity);

    public static ContainerAction Update() => new(NeedsUpdate: true);

    public static ContainerAction UpdateAndRemove() => new(NeedsUpdate: true, NeedsContainerRemoval: true);

    public static ContainerAction Remove() => new(NeedsContainerRemoval: true);

    public static ContainerAction GracefulShutdown() => new(NeedsGracefulShutdown: true);
}
