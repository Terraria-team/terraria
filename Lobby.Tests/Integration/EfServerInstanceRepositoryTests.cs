using Lobby.Application.Domain;
using Lobby.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lobby.Tests.Integration;

/// <summary>
/// Тетси, що перевіряють логіку збереження інстансів
/// </summary>
[Collection(PostgresCollection.Name)]
public class EfServerInstanceRepositoryTests : IAsyncLifetime
{
    private const int BasePort = 7777;

    private readonly PostgresDatabaseFixture _db;

    public EfServerInstanceRepositoryTests(PostgresDatabaseFixture db) => _db = db;

    public Task InitializeAsync() => _db.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static ServerInstanceEntity Instance(int port)
    {
        var worldId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        return new ServerInstanceEntity
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            ContainerId = $"container-{port}",
            Image = "terraria-server:latest",
            Name = $"server_instance_{port}",
            Port = port,
            Status = ServerInstanceStatus.Running,
            PlayerCount = 0,
            EmptySince = null,
            PendingSince = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
            World = new TerrariaWorldEntity
            {
                Id = worldId,
                OwnerId = ownerId,
                Name = $"World_For_Port_{port}",
                StorageId = null,
                Owner = new PlayerEntity
                {
                    Id = ownerId,
                    Email = $"owner_{port}@example.com",
                    Name = $"Test Owner {port}",
                    Role = "User"
                }
            }
        };
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

    // Наявний інстанс читається за Id через окреме з'єднання.
    [DockerFact]
    public async Task GetById_WhenExists_ReturnsInstance()
    {
        var instance = Instance(BasePort + 2);
        await using (var context = _db.CreateContext())
        {
            await new EfServerInstanceRepository(context).Create(instance);
        }

        await using var readContext = _db.CreateContext();
        var found = await new EfServerInstanceRepository(readContext).GetById(instance.Id);

        Assert.NotNull(found);
        Assert.Equal(instance.Id, found.Id);
        Assert.Equal(instance.Port, found.Port);
    }

    // Пошук за неіснуючим Id повертає null.
    [DockerFact]
    public async Task GetById_WhenMissing_ReturnsNull()
    {
        await using var context = _db.CreateContext();

        var found = await new EfServerInstanceRepository(context).GetById(Guid.NewGuid());

        Assert.Null(found);
    }

    // Наявний інстанс знаходиться за ContainerId.
    [DockerFact]
    public async Task GetByContainerId_WhenExists_ReturnsInstance()
    {
        const int port = BasePort + 3;
        var instance = Instance(port);
        await using (var context = _db.CreateContext())
        {
            await new EfServerInstanceRepository(context).Create(instance);
        }

        await using var readContext = _db.CreateContext();
        var found = await new EfServerInstanceRepository(readContext).GetByContainerId($"container-{port}");

        Assert.NotNull(found);
        Assert.Equal(instance.Id, found.Id);
    }

    // Пошук за неіснуючим ContainerId повертає null.
    [DockerFact]
    public async Task GetByContainerId_WhenMissing_ReturnsNull()
    {
        await using var context = _db.CreateContext();

        var found = await new EfServerInstanceRepository(context).GetByContainerId("container-missing");

        Assert.Null(found);
    }

    // Update перезаписує скалярні поля наявного інстансу.
    [DockerFact]
    public async Task Update_WhenExists_OverwritesScalarFields()
    {
        var instance = Instance(BasePort + 4);
        await using (var context = _db.CreateContext())
        {
            await new EfServerInstanceRepository(context).Create(instance);
        }

        await using (var context = _db.CreateContext())
        {
            var toUpdate = await new EfServerInstanceRepository(context).GetById(instance.Id);
            toUpdate!.PlayerCount = 7;
            toUpdate.Status = ServerInstanceStatus.Dead;

            var updated = await new EfServerInstanceRepository(context).Update(toUpdate);
            Assert.NotNull(updated);
        }

        await using var readContext = _db.CreateContext();
        var found = await new EfServerInstanceRepository(readContext).GetById(instance.Id);
        Assert.Equal(7, found!.PlayerCount);
        Assert.Equal(ServerInstanceStatus.Dead, found.Status);
    }

    // Update неіснуючого інстансу повертає null.
    [DockerFact]
    public async Task Update_WhenMissing_ReturnsNull()
    {
        await using var context = _db.CreateContext();

        var updated = await new EfServerInstanceRepository(context).Update(Instance(BasePort + 5));

        Assert.Null(updated);
    }

    // CreateMany зберігає весь переданий набір інстансів.
    [DockerFact]
    public async Task CreateMany_PersistsAllInstances()
    {
        var instances = new HashSet<ServerInstanceEntity>
        {
            Instance(BasePort + 6),
            Instance(BasePort + 7)
        };
        await using (var context = _db.CreateContext())
        {
            await new EfServerInstanceRepository(context).CreateMany(instances);
        }

        await using var readContext = _db.CreateContext();
        var all = await new EfServerInstanceRepository(readContext).GetAll();
        Assert.Equal(2, all.Count);
    }

    // UpdateMany застосовує зміни до всіх переданих інстансів.
    [DockerFact]
    public async Task UpdateMany_PersistsAllChanges()
    {
        var first = Instance(BasePort + 8);
        var second = Instance(BasePort + 9);
        await using (var context = _db.CreateContext())
        {
            await new EfServerInstanceRepository(context).CreateMany([first, second]);
        }

        await using (var context = _db.CreateContext())
        {
            var repository = new EfServerInstanceRepository(context);
            var toUpdate = new HashSet<ServerInstanceEntity>();
            foreach (var id in new[] { first.Id, second.Id })
            {
                var entity = await repository.GetById(id);
                entity!.Status = ServerInstanceStatus.Deleted;
                toUpdate.Add(entity);
            }

            await repository.UpdateMany(toUpdate);
        }

        await using var readContext = _db.CreateContext();
        var all = await new EfServerInstanceRepository(readContext).GetAll();
        Assert.Equal(2, all.Count);
        Assert.All(all, i => Assert.Equal(ServerInstanceStatus.Deleted, i.Status));
    }
}