using Docker.DotNet.Models;
using Lobby.Application.Domain;
using Lobby.HostedServices;
using Lobby.Settings;

namespace Lobby.Tests.HostedServices;

/// <summary>
/// Юніт-тести для ServerInstanceContainerProcessor.
/// Чисті статичні функції, що за станом Docker-контейнера й записом у БД вирішують дію
/// (створити / оновити / видалити / м'яко зупинити). Docker і БД не залучаються.
/// Покривають усі гілки Dead/Pending/Running та витягання полів у CreateNewEntity.
/// </summary>
public class ServerInstanceContainerProcessorTests
{
    private const string ContainerId = "container-abc";

    private static readonly ServerInstanceCleanupSettings Settings = new()
    {
        PendingTimeoutSeconds = 120,
        IdleTimeoutSeconds = 300
    };

    private static ContainerListResponse Container(
        string? name = "/terraria_1", int publicPort = 32770, string image = "terraria-server:latest") => new()
    {
        ID = ContainerId,
        Image = image,
        Names = name is null ? null : new List<string> { name },
        Ports = new List<Port> { new() { PublicPort = (ushort)publicPort } }
    };

    private static ServerInstanceEntity DbInfo(
        ServerInstanceStatus status,
        DateTime? pendingSince = null,
        DateTime? emptySince = null,
        int playerCount = 0) => new()
    {
        Id = Guid.NewGuid(),
        WorldId = Guid.NewGuid(),
        ContainerId = ContainerId,
        Image = "terraria-server:latest",
        Name = "terraria_1",
        Port = 32770,
        Status = status,
        PlayerCount = playerCount,
        PendingSince = pendingSince,
        EmptySince = emptySince,
        CreatedAt = DateTime.UtcNow.AddMinutes(-30)
    };

    // Мертвий контейнер без запису в БД: створюється сутність Dead і планується видалення.
    [Fact]
    public void ProcessDeadContainer_WhenNoDbInfo_CreatesDeadEntityAndFlagsRemoval()
    {
        var action = ServerInstanceContainerProcessor.ProcessDeadContainer(ContainerId, Container(), dbInfo: null);

        Assert.NotNull(action.ToCreate);
        Assert.Equal(ServerInstanceStatus.Dead, action.ToCreate!.Status);
        Assert.True(action.NeedsContainerRemoval);
        Assert.False(action.NeedsUpdate);
    }

    // Мертвий контейнер із наявним записом: запис позначається Dead, планується оновлення й видалення.
    [Fact]
    public void ProcessDeadContainer_WhenDbInfoExists_MarksDeadAndFlagsUpdateAndRemoval()
    {
        var dbInfo = DbInfo(ServerInstanceStatus.Running);

        var action = ServerInstanceContainerProcessor.ProcessDeadContainer(ContainerId, Container(), dbInfo);

        Assert.Equal(ServerInstanceStatus.Dead, dbInfo.Status);
        Assert.Null(action.ToCreate);
        Assert.True(action.NeedsUpdate);
        Assert.True(action.NeedsContainerRemoval);
    }

    // Pending-контейнер без запису в БД: створюється сутність Pending із виставленим PendingSince.
    [Fact]
    public void ProcessPendingContainer_WhenNoDbInfo_CreatesPendingEntity()
    {
        var action = ServerInstanceContainerProcessor.ProcessPendingContainer(
            ContainerId, Container(), dbInfo: null, Settings);

        Assert.NotNull(action.ToCreate);
        Assert.Equal(ServerInstanceStatus.Pending, action.ToCreate!.Status);
        Assert.NotNull(action.ToCreate.PendingSince);
        Assert.False(action.NeedsUpdate);
        Assert.False(action.NeedsContainerRemoval);
    }

    // Pending довше за таймаут: запитується м'яка зупинка.
    [Fact]
    public void ProcessPendingContainer_WhenPendingBeyondTimeout_RequestsGracefulShutdown()
    {
        var dbInfo = DbInfo(
            ServerInstanceStatus.Pending,
            pendingSince: DateTime.UtcNow.AddSeconds(-(Settings.PendingTimeoutSeconds + 60)));

        var action = ServerInstanceContainerProcessor.ProcessPendingContainer(ContainerId, Container(), dbInfo, Settings);

        Assert.True(action.NeedsGracefulShutdown);
        Assert.Null(action.ToCreate);
        Assert.False(action.NeedsUpdate);
    }

    // Pending у межах таймауту: жодної дії.
    [Fact]
    public void ProcessPendingContainer_WhenPendingWithinTimeout_ReturnsNone()
    {
        var dbInfo = DbInfo(
            ServerInstanceStatus.Pending,
            pendingSince: DateTime.UtcNow.AddSeconds(-10));

        var action = ServerInstanceContainerProcessor.ProcessPendingContainer(ContainerId, Container(), dbInfo, Settings);

        Assert.Equal(ContainerAction.None, action);
    }

    // Контейнер Pending, а запис у БД в іншому статусі: запис позначається Pending, планується оновлення.
    [Fact]
    public void ProcessPendingContainer_WhenDbStatusNotPending_MarksPendingAndFlagsUpdate()
    {
        var dbInfo = DbInfo(ServerInstanceStatus.Running);

        var action = ServerInstanceContainerProcessor.ProcessPendingContainer(ContainerId, Container(), dbInfo, Settings);

        Assert.Equal(ServerInstanceStatus.Pending, dbInfo.Status);
        Assert.True(action.NeedsUpdate);
        Assert.Null(action.ToCreate);
        Assert.False(action.NeedsGracefulShutdown);
    }

    // Running-контейнер без запису в БД: створюється сутність Running із виставленим EmptySince.
    [Fact]
    public void ProcessRunningContainer_WhenNoDbInfo_CreatesRunningEntity()
    {
        var action = ServerInstanceContainerProcessor.ProcessRunningContainer(
            ContainerId, Container(), dbInfo: null, Settings);

        Assert.NotNull(action.ToCreate);
        Assert.Equal(ServerInstanceStatus.Running, action.ToCreate!.Status);
        Assert.NotNull(action.ToCreate.EmptySince);
    }

    // Контейнер Running, а запис у БД в іншому статусі: позначається Running, лічильник гравців скидається.
    [Fact]
    public void ProcessRunningContainer_WhenDbStatusNotRunning_MarksRunningResetsPlayersAndFlagsUpdate()
    {
        var dbInfo = DbInfo(ServerInstanceStatus.Pending, pendingSince: DateTime.UtcNow, playerCount: 5);

        var action = ServerInstanceContainerProcessor.ProcessRunningContainer(ContainerId, Container(), dbInfo, Settings);

        Assert.Equal(ServerInstanceStatus.Running, dbInfo.Status);
        Assert.Equal(0, dbInfo.PlayerCount);
        Assert.True(action.NeedsUpdate);
        Assert.False(action.NeedsGracefulShutdown);
    }

    // Running і EmptySince ще не виставлено: проставляється EmptySince, планується оновлення.
    [Fact]
    public void ProcessRunningContainer_WhenRunningAndEmptySinceNull_SetsEmptySinceAndFlagsUpdate()
    {
        var dbInfo = DbInfo(ServerInstanceStatus.Running, emptySince: null);

        var action = ServerInstanceContainerProcessor.ProcessRunningContainer(ContainerId, Container(), dbInfo, Settings);

        Assert.NotNull(dbInfo.EmptySince);
        Assert.True(action.NeedsUpdate);
        Assert.False(action.NeedsGracefulShutdown);
    }

    // Порожній довше за таймаут простою: запитується м'яка зупинка.
    [Fact]
    public void ProcessRunningContainer_WhenIdleBeyondTimeout_RequestsGracefulShutdown()
    {
        var dbInfo = DbInfo(
            ServerInstanceStatus.Running,
            emptySince: DateTime.UtcNow.AddSeconds(-(Settings.IdleTimeoutSeconds + 60)),
            playerCount: 0);

        var action = ServerInstanceContainerProcessor.ProcessRunningContainer(ContainerId, Container(), dbInfo, Settings);

        Assert.True(action.NeedsGracefulShutdown);
        Assert.False(action.NeedsUpdate);
    }

    // Running із гравцями (не простоює): жодної дії.
    [Fact]
    public void ProcessRunningContainer_WhenActiveOrRecentlyEmpty_ReturnsNone()
    {
        var dbInfo = DbInfo(
            ServerInstanceStatus.Running,
            emptySince: DateTime.UtcNow.AddSeconds(-(Settings.IdleTimeoutSeconds + 60)),
            playerCount: 3);

        var action = ServerInstanceContainerProcessor.ProcessRunningContainer(ContainerId, Container(), dbInfo, Settings);

        Assert.Equal(ContainerAction.None, action);
    }

    // CreateNewEntity тягне ім'я та зовнішній порт із даних контейнера, WorldId лишається порожнім.
    [Fact]
    public void CreateNewEntity_ExtractsNameAndHostPortFromContainerData()
    {
        var entity = ServerInstanceContainerProcessor.CreateNewEntity(
            ContainerId, Container(name: "/terraria_42", publicPort: 40000),
            ServerInstanceStatus.Running, pendingSince: null, emptySince: DateTime.UtcNow);

        Assert.Equal(ContainerId, entity.ContainerId);
        Assert.Equal("/terraria_42", entity.Name);
        Assert.Equal(40000, entity.Port);
        Assert.Equal(Guid.Empty, entity.WorldId);
    }

    // Без імені та портів у контейнера CreateNewEntity дає "Unnamed" і порт 0.
    [Fact]
    public void CreateNewEntity_WhenNamesAndPortsNull_FallsBackToUnnamedAndZeroPort()
    {
        var container = new ContainerListResponse
        {
            ID = ContainerId,
            Image = "terraria-server:latest",
            Names = null,
            Ports = null
        };

        var entity = ServerInstanceContainerProcessor.CreateNewEntity(
            ContainerId, container, ServerInstanceStatus.Dead, pendingSince: null, emptySince: null);

        Assert.Equal("Unnamed", entity.Name);
        Assert.Equal(0, entity.Port);
    }

    // Порожні (не null) списки Names/Ports так само дають "Unnamed" і порт 0.
    [Fact]
    public void CreateNewEntity_WhenNamesAndPortsEmpty_FallsBackToUnnamedAndZeroPort()
    {
        var container = new ContainerListResponse
        {
            ID = ContainerId,
            Image = "terraria-server:latest",
            Names = new List<string>(),
            Ports = new List<Port>()
        };

        var entity = ServerInstanceContainerProcessor.CreateNewEntity(
            ContainerId, container, ServerInstanceStatus.Dead, pendingSince: null, emptySince: null);

        Assert.Equal("Unnamed", entity.Name);
        Assert.Equal(0, entity.Port);
    }
}
