using Lobby.Application.Entities;
using Lobby.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lobby.Tests.Integration;

/// <summary>
/// Тетси, що перевіряють логіку збереження інстансів
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

    // Оновлено для використання ініціалізатора об'єкта (class) замість позиційного конструктора (record)
    private static ServerInstanceEntity Instance(int port) => new ServerInstanceEntity
    {
        Id = Guid.NewGuid(),
        WorldId = Guid.NewGuid(), // Додано нове поле з сутності
        ContainerId = $"container-{port}",
        Image = "terraria-server:latest",
        Name = $"server_instance_{port}",
        Port = port,
        Status = ServerInstanceStatus.Running,
        PlayerCount = 0,
        EmptySince = null,
        PendingSince = null, // Додано нове поле з сутності
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = null
    };

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