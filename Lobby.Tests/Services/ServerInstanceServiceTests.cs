using Lobby.Application.Contracts.ExternalServices;
using Lobby.Application.Contracts.Repositories;
using Lobby.Application.Domain;
using Lobby.Application.Models;
using Lobby.Application.UseCases;
using Moq;

namespace Lobby.Tests.Services;

/// <summary>
/// Юніт-тести для ServerInstanceService.
/// Покривають GetAll (вибірка невидалених інстансів мапиться в моделі)
/// та потік Create: інстанс спавниться зовнішнім спавнером, його метадані
/// переносяться в сутність (нульова кількість гравців, світ власника) і зберігаються,
/// а при досягненні ліміту повертається ResourceExhausted без звернення до спавнера.
/// </summary>
public class ServerInstanceServiceTests
{
    private readonly Mock<IServerInstanceRepository> _repository = new();
    private readonly Mock<IServerInstanceSpawner> _spawner = new();
    private readonly ServerInstanceServiceSettings _settings = new();

    private ServerInstanceService CreateSut() => new(_repository.Object, _spawner.Object, _settings);

    private static ServerInstanceEntity Instance(Guid id, int port) => new()
    {
        Id = id,
        WorldId = Guid.NewGuid(),
        ContainerId = $"container-{port}",
        Image = "terraria-server:latest",
        Name = $"server_instance_{port}",
        Port = port,
        Status = ServerInstanceStatus.Running,
        PlayerCount = 3,
        CreatedAt = DateTime.UtcNow
    };

    // GetAll бере невидалені інстанси з репозиторію й віддає їх як моделі.
    [Fact]
    public async Task GetAll_MapsNonDeletedInstancesToModels()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetAllNonDeleted()).ReturnsAsync([Instance(id, 7777)]);

        var result = await CreateSut().GetAll();

        var model = Assert.Single(result);
        Assert.Equal(id, model.Id);
        Assert.Equal("container-7777", model.ContainerId);
        Assert.Equal(7777, model.Port);
        Assert.Equal(3, model.PlayerCount);
        Assert.Equal(ServerInstanceStatus.Running, model.Status);
        _repository.Verify(r => r.GetAllNonDeleted(), Times.Once);
    }

    // Create спавнить інстанс і зберігає його з метаданими спавнера та світом власника.
    [Fact]
    public async Task Create_SpawnsInstanceAndPersistsItWithOwnerWorld()
    {
        var ownerId = Guid.NewGuid();
        _repository.Setup(r => r.GetAllNonDeletedCount()).ReturnsAsync(0);
        _spawner
            .Setup(s => s.CreateNewServerInstance(It.IsAny<Guid>(), "my-world"))
            .ReturnsAsync(new ServerInstanceSpawnInfoEntity(
                ContainerId: "container-42",
                Image: "fake/terraria-server:test",
                Name: "my-world",
                Port: 7777,
                Status: ServerInstanceStatus.Running));

        ServerInstanceEntity? persisted = null;
        _repository
            .Setup(r => r.Create(It.IsAny<ServerInstanceEntity>()))
            .Callback<ServerInstanceEntity>(e => persisted = e)
            .ReturnsAsync((ServerInstanceEntity e) => e);

        var result = await CreateSut().Create("my-world", ownerId);

        Assert.True(result.IsSuccessful);
        Assert.Equal("container-42", result.Result!.ContainerId);
        Assert.Equal(7777, result.Result.Port);
        Assert.Equal(0, result.Result.PlayerCount);

        Assert.NotNull(persisted);
        Assert.Equal("container-42", persisted.ContainerId);
        Assert.Equal("fake/terraria-server:test", persisted.Image);
        Assert.Equal(ServerInstanceStatus.Running, persisted.Status);
        Assert.Equal(0, persisted.PlayerCount);
        // Спавнеру передається той самий Id, під яким інстанс зберігається.
        _spawner.Verify(s => s.CreateNewServerInstance(persisted.Id, "my-world"), Times.Once);

        Assert.NotNull(persisted.World);
        Assert.Equal(ownerId, persisted.World.OwnerId);
        Assert.Equal(persisted.WorldId, persisted.World.Id);
    }

    // Ліміт інстансів вичерпано → ResourceExhausted, спавнер не викликається.
    [Fact]
    public async Task Create_WhenMaxCountReached_ReturnsResourceExhaustedAndDoesNotSpawn()
    {
        _settings.ServerInstanceMaxCount = 2;
        _repository.Setup(r => r.GetAllNonDeletedCount()).ReturnsAsync(3);

        var result = await CreateSut().Create("my-world", Guid.NewGuid());

        Assert.False(result.IsSuccessful);
        Assert.Equal(ErrorType.ResourceExhausted, result.Error!.ErrorType);
        _spawner.Verify(s => s.CreateNewServerInstance(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        _repository.Verify(r => r.Create(It.IsAny<ServerInstanceEntity>()), Times.Never);
    }

    // Кількість гравців падає до нуля (з ненульової) → проставляється EmptySince.
    [Fact]
    public async Task UpdatePlayerCount_WhenDropsToZero_SetsEmptySince()
    {
        var id = Guid.NewGuid();
        var instance = Instance(id, 7777); // PlayerCount = 3, EmptySince = null
        _repository.Setup(r => r.GetById(id)).ReturnsAsync(instance);
        _repository.Setup(r => r.Update(It.IsAny<ServerInstanceEntity>()))
            .ReturnsAsync((ServerInstanceEntity e) => e);

        var result = await CreateSut().UpdatePlayerCount(id, 0);

        Assert.True(result.IsSuccessful);
        Assert.Equal(0, result.Result!.PlayerCount);
        Assert.NotNull(instance.EmptySince);
    }

    // Кількість гравців стає додатною → EmptySince скидається в null.
    [Fact]
    public async Task UpdatePlayerCount_WhenPositive_ClearsEmptySince()
    {
        var id = Guid.NewGuid();
        var instance = Instance(id, 7777);
        instance.PlayerCount = 0;
        instance.EmptySince = DateTime.UtcNow.AddMinutes(-5);
        _repository.Setup(r => r.GetById(id)).ReturnsAsync(instance);
        _repository.Setup(r => r.Update(It.IsAny<ServerInstanceEntity>()))
            .ReturnsAsync((ServerInstanceEntity e) => e);

        var result = await CreateSut().UpdatePlayerCount(id, 3);

        Assert.True(result.IsSuccessful);
        Assert.Equal(3, result.Result!.PlayerCount);
        Assert.Null(instance.EmptySince);
    }

    // Інстанс не знайдено → NotFound, оновлення не викликається.
    [Fact]
    public async Task UpdatePlayerCount_WhenInstanceMissing_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetById(id)).ReturnsAsync((ServerInstanceEntity?)null);

        var result = await CreateSut().UpdatePlayerCount(id, 5);

        Assert.False(result.IsSuccessful);
        Assert.Equal(ErrorType.NotFound, result.Error!.ErrorType);
        _repository.Verify(r => r.Update(It.IsAny<ServerInstanceEntity>()), Times.Never);
    }
}
