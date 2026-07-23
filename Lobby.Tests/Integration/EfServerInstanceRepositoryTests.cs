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
}