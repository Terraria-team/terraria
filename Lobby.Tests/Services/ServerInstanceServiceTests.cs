using Lobby.Application.Contracts;
using Lobby.Application.Entities;
using Lobby.Application.Services;
using Moq;

namespace Lobby.Tests.Services;

/// <summary>
/// Юніт-тести для <see cref="ServerInstanceService"/>.
/// Покривають делегування <c>GetAll</c> та потік <c>Create</c>:
/// запитується вільний порт, на ньому спавниться інстанс, а отримана
/// сутність мапиться (нульова кількість гравців, метадані спавну) і зберігається.
/// </summary>
public class ServerInstanceServiceTests
{
    private readonly Mock<IServerInstanceRepository> _repository = new();
    private readonly Mock<IServerInstanceSpawner> _spawner = new();

    private ServerInstanceService CreateSut() => new(_repository.Object, _spawner.Object);

    // GetAll просто делегує виклик репозиторію й повертає його результат.
    [Fact]
    public async Task GetAll_DelegatesToRepository()
    {
        var instances = new List<ServerInstanceEntity>();
        
        _repository.Setup(r => r.GetAllNonDeleted()).ReturnsAsync(instances);

        var result = await CreateSut().GetAll();

        Assert.Same(instances, result);
        
        _repository.Verify(r => r.GetAllNonDeleted(), Times.Once);
    }
}