using Docker.DotNet;
using Docker.DotNet.Models;
using Lobby.Application.Contracts.Repositories;
using Lobby.Application.Domain;
using Lobby.Settings;
using Microsoft.Extensions.Options;

namespace Lobby.HostedServices;

public class ServerInstanceCleanupService : BackgroundService
{
    private readonly ILogger<ServerInstanceCleanupService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDockerClient _dockerClient;

    public ServerInstanceCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<ServerInstanceCleanupService> logger,
        IDockerClient dockerClient)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _dockerClient = dockerClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ServerInstanceCleanupService: starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var settings = scope.ServiceProvider
                    .GetRequiredService<IOptionsSnapshot<ServerInstanceCleanupSettings>>().Value;
                var repository = scope.ServiceProvider.GetRequiredService<IServerInstanceRepository>();

                await Task.Delay(TimeSpan.FromSeconds(settings.CleanupIntervalSeconds), stoppingToken);

                try
                {
                    var dockerContainers = await _dockerClient.Containers.ListContainersAsync(
                        new ContainersListParameters { All = true },
                        stoppingToken);

                    var serverInstanceContainers = dockerContainers
                        .Where(c => c.Image.Contains(settings.ServerInstanceImageName))
                        .ToDictionary(c => c.ID);

                    var allNonDeletedContainersInDb =
                        (await repository.GetAllNonDeleted()).ToDictionary(c => c.ContainerId);

                    List<string> containersToRemove = new();
                    List<string> containersToGracefullyShutdown = new();
                    HashSet<ServerInstanceEntity> dbEntitiesToUpdate = new();
                    HashSet<ServerInstanceEntity> dbEntitiesToCreate = new();
                    
                    foreach (var serverInstanceEntity in allNonDeletedContainersInDb)
                    {
                        if (serverInstanceContainers.ContainsKey(serverInstanceEntity.Value.ContainerId)) continue;

                        var dbEntity = serverInstanceEntity.Value;
                        dbEntity.MarkAsDeleted();

                        dbEntitiesToUpdate.Add(dbEntity);
                    }

                    foreach (var serverInstanceContainer in serverInstanceContainers)
                    {
                        var containerId = serverInstanceContainer.Key;
                        var containerData = serverInstanceContainer.Value;
                        var dockerState = containerData.State.ToLower();

                        allNonDeletedContainersInDb.TryGetValue(containerId, out var dbInfo);

                        // If it's not in non-deleted list try get info of it as it was deleted, could still get null
                        if (dbInfo is null)
                        {
                            dbInfo = await repository.GetByContainerId(containerId);
                        }

                        ContainerAction action = dockerState switch
                        {
                            "exited" or "dead" => ServerInstanceContainerProcessor.ProcessDeadContainer(containerId, containerData, dbInfo),
                            "paused" or "created" or "restarting" or "removing" => ServerInstanceContainerProcessor.ProcessPendingContainer(containerId, containerData, dbInfo, settings),
                            "running" => ServerInstanceContainerProcessor.ProcessRunningContainer(containerId, containerData, dbInfo, settings),
                            _ => ContainerAction.None
                        };

                        if (action.ToCreate != null) dbEntitiesToCreate.Add(action.ToCreate);
                        if (action.NeedsUpdate && dbInfo != null) dbEntitiesToUpdate.Add(dbInfo);
                        if (action.NeedsContainerRemoval) containersToRemove.Add(containerId);
                        if (action.NeedsGracefulShutdown) containersToGracefullyShutdown.Add(containerId);
                    }

                    // 1. Graceful Shutdowns
                    if (containersToGracefullyShutdown.Any())
                    {
                        var stopTasks = containersToGracefullyShutdown.Select(async containerId =>
                        {
                            try
                            {
                                await _dockerClient.Containers.StopContainerAsync(
                                    containerId,
                                    new ContainerStopParameters { WaitBeforeKillSeconds = 20 },
                                    stoppingToken);
                            }
                            catch (DockerContainerNotFoundException)
                            {
                                // ignore already deleted
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to gracefully stop container {ContainerId}", containerId);
                            }
                        });

                        await Task.WhenAll(stopTasks);
                    }

                    // 2. Delete
                    var allToDelete = containersToRemove.Concat(containersToGracefullyShutdown).Distinct().ToList();

                    if (allToDelete.Any())
                    {
                        var deleteTasks = allToDelete.Select(async containerId =>
                        {
                            try
                            {
                                await _dockerClient.Containers.RemoveContainerAsync(
                                    containerId,
                                    new ContainerRemoveParameters { Force = true },
                                    stoppingToken);

                                return containerId; // Deletion successful
                            }
                            catch (DockerContainerNotFoundException)
                            {
                                return containerId; // Already gone, which counts as a success
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to forcefully remove container {ContainerId}", containerId);
                                return null; // Deletion failed!
                            }
                        });

                        var results = await Task.WhenAll(deleteTasks);

                        var successfullyDeleted = results.Where(id => id != null).ToList();

                        // DB states should update/create with Deleted status where container was successfully deleted
                        foreach (var containerId in successfullyDeleted)
                        {
                            var plannedCreation = dbEntitiesToCreate.FirstOrDefault(c => c.ContainerId == containerId);
                            if (plannedCreation != null)
                            {
                                plannedCreation.MarkAsDeleted();
                                continue;
                            }

                            var plannedUpdate = dbEntitiesToUpdate.FirstOrDefault(c => c.ContainerId == containerId);
                            if (plannedUpdate != null)
                            {
                                plannedUpdate.MarkAsDeleted();
                                dbEntitiesToUpdate.Add(plannedUpdate);
                            }
                        }
                    }

                    // 3. Update DB
                    if (dbEntitiesToCreate.Any())
                    {
                        await repository.CreateMany(dbEntitiesToCreate);
                    }

                    if (dbEntitiesToUpdate.Any())
                    {
                        await repository.UpdateMany(dbEntitiesToUpdate);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to cleanup and sync server instances");
                }
            }
        }
    }
}