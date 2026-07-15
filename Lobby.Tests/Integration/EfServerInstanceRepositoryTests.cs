using Lobby.Application.Entities;
using Lobby.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lobby.Tests.Integration;

/// <summary>
/// Тетси, що перевіряють логіку вибору вільного порту (max+1, fallback на порожній таблиці),
/// збереження інстансів та унікальний індекс на порту.
/// </summary>
[Collection(PostgresCollection.Name)]
public class EfServerInstanceRepositoryTests : IAsyncLifetime
{
    // Дублює fallback-порт із EfServerInstanceRepository.GetFreeInstancePort().
    private const int BasePort = 7777;

    private readonly PostgresDatabaseFixture _db;

    public EfServerInstanceRepositoryTests(PostgresDatabaseFixture db) => _db = db;

    public Task InitializeAsync() => _db.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static ServerInstanceEntity Instance(int port) => new(
        Id: Guid.NewGuid(),
        ContainerId: $"container-{port}",
        Image: "terraria-server:latest",
        Name: $"server_instance_{port}",
        Port: port,
        PlayerCount: 0,
        EmptySince: null,
        CreatedAt: DateTime.UtcNow,
        UpdatedAt: null,
        Status: ServerInstanceStatus.Running);

    // На порожній таблиці вільний порт — це fallback BasePort + 1.
    [DockerFact]
    public async Task GetFreeInstancePort_WhenTableEmpty_ReturnsBasePortPlusOne()
    {
        await using var context = _db.CreateContext();

        var port = await new EfServerInstanceRepository(context).GetFreeInstancePort();

        Assert.Equal(BasePort + 1, port);
    }

    // За наявності інстансів вільний порт — максимальний зайнятий + 1.
    [DockerFact]
    public async Task GetFreeInstancePort_ReturnsMaxPortPlusOne()
    {
        const int highestPort = BasePort + 24;
        await using (var context = _db.CreateContext())
        {
            context.ServerInstances.AddRange(
                Instance(BasePort + 1),
                Instance(highestPort),
                Instance(BasePort + 3));
            await context.SaveChangesAsync();
        }

        await using var readContext = _db.CreateContext();
        var port = await new EfServerInstanceRepository(readContext).GetFreeInstancePort();

        Assert.Equal(highestPort + 1, port);
    }

    // Створений інстанс читається назад через GetAll з усіма полями.
    [DockerFact]
    public async Task Create_ThenGetAll_ReturnsPersistedInstance()
    {
        const int port = BasePort + 1;
        await using (var context = _db.CreateContext())
        {
            await new EfServerInstanceRepository(context).Create(Instance(port));
        }

        await using var readContext = _db.CreateContext();
        var all = await new EfServerInstanceRepository(readContext).GetAll();

        var instance = Assert.Single(all);
        Assert.Equal($"container-{port}", instance.ContainerId);
        Assert.Equal(port, instance.Port);
        Assert.Equal(ServerInstanceStatus.Running, instance.Status);
    }

    // Унікальний індекс на Port не дозволяє два інстанси на одному порту.
    [DockerFact]
    public async Task Create_WithDuplicatePort_ThrowsDbUpdateException()
    {
        const int port = BasePort + 1;
        await using (var context = _db.CreateContext())
        {
            await new EfServerInstanceRepository(context).Create(Instance(port));
        }

        await using var secondContext = _db.CreateContext();
        var repository = new EfServerInstanceRepository(secondContext);

        await Assert.ThrowsAsync<DbUpdateException>(() => repository.Create(Instance(port)));
    }
}
