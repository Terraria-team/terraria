using Docker.DotNet.Models;
using Lobby.Application.Domain;
using Lobby.Settings;

namespace Lobby.HostedServices;

internal static class ServerInstanceContainerProcessor
{
    public static ContainerAction ProcessDeadContainer(
        string containerId, ContainerListResponse containerData, ServerInstanceEntity? dbInfo)
    {
        if (dbInfo is null)
        {
            var newEntity = CreateNewEntity(containerId, containerData, ServerInstanceStatus.Dead, null, null);
            return ContainerAction.Create(newEntity) with { NeedsContainerRemoval = true };
        }

        dbInfo.MarkAsDead();
        return ContainerAction.UpdateAndRemove();
    }

    public static ContainerAction ProcessPendingContainer(
        string containerId, ContainerListResponse containerData, ServerInstanceEntity? dbInfo, ServerInstanceCleanupSettings settings)
    {
        if (dbInfo is null)
        {
            var newEntity = CreateNewEntity(containerId, containerData, ServerInstanceStatus.Pending, DateTime.UtcNow, null);
            return ContainerAction.Create(newEntity);
        }

        if (dbInfo.Status == ServerInstanceStatus.Pending && dbInfo.PendingSince.HasValue)
        {
            var pendingTime = DateTime.UtcNow - dbInfo.PendingSince.Value;
            var timeoutLimit = TimeSpan.FromSeconds(settings.PendingTimeoutSeconds);

            if (pendingTime > timeoutLimit)
            {
                return ContainerAction.GracefulShutdown();
            }
        }
        else
        {
            dbInfo.MarkAsPending();
            return ContainerAction.Update();
        }

        return ContainerAction.None;
    }

    public static ContainerAction ProcessRunningContainer(
        string containerId, ContainerListResponse containerData, ServerInstanceEntity? dbInfo, ServerInstanceCleanupSettings settings)
    {
        if (dbInfo is null)
        {
            var newEntity = CreateNewEntity(containerId, containerData, ServerInstanceStatus.Running, null, DateTime.UtcNow);
            return ContainerAction.Create(newEntity);
        }

        bool needsUpdate = false;
        bool needsGracefulShutdown = false;

        if (dbInfo.Status != ServerInstanceStatus.Running)
        {
            dbInfo.MarkAsRunning();
            dbInfo.PlayerCount = 0;
            needsUpdate = true;
        }
        else if (dbInfo.EmptySince == null && dbInfo.PlayerCount == 0)
        {
            dbInfo.EmptySince = DateTime.UtcNow;
            dbInfo.UpdatedAt = DateTime.UtcNow;
            needsUpdate = true;
        }
        else if (dbInfo.IsIdleFor(TimeSpan.FromSeconds(settings.IdleTimeoutSeconds)))
        {
            needsGracefulShutdown = true;
        }

        return new ContainerAction(NeedsUpdate: needsUpdate, NeedsGracefulShutdown: needsGracefulShutdown);
    }

    public static ServerInstanceEntity CreateNewEntity(
        string containerId,
        ContainerListResponse containerData,
        ServerInstanceStatus initialStatus,
        DateTime? pendingSince,
        DateTime? emptySince)
    {
        var containerName = containerData.Names?.FirstOrDefault() ?? "Unnamed";
        var hostPort = containerData.Ports?.FirstOrDefault()?.PublicPort ?? 0;

        return new ServerInstanceEntity
        {
            Id = Guid.NewGuid(),
            WorldId = Guid.Empty, // empty guid = default world guid?
            ContainerId = containerId,
            Image = containerData.Image,
            Name = containerName,
            Port = hostPort,
            PlayerCount = 0,
            EmptySince = emptySince,
            PendingSince = pendingSince,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = initialStatus
        };
    }
}
