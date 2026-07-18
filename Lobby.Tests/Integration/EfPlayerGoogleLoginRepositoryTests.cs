using Lobby.Application.Entities;
using Lobby.Infrastructure.Repositories;

namespace Lobby.Tests.Integration;

/// <summary>
/// Тести, що перевіряють пошук за GoogleId разом із завантаженням пов'язаного гравця (Include).
/// </summary>
[Collection(PostgresCollection.Name)]
public class EfPlayerGoogleLoginRepositoryTests : IAsyncLifetime
{
    private readonly PostgresDatabaseFixture _db;

    public EfPlayerGoogleLoginRepositoryTests(PostgresDatabaseFixture db) => _db = db;

    public Task InitializeAsync() => _db.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // Пошук за GoogleId повертає логін разом із приєднаним гравцем.
    [DockerFact]
    public async Task GetById_ReturnsLoginWithPlayerIncluded()
    {
        var player = new PlayerEntity
        {
            Id = Guid.NewGuid(),
            Email = "google@example.com",
            Name = "Google Player",
            Role = "User",
            GoogleLogin = null
        };
        player.GoogleLogin = new PlayerGoogleLoginEntity { PlayerId = player.Id, GoogleId = "google-7" };
        await using (var context = _db.CreateContext())
        {
            context.Players.Add(player);
            await context.SaveChangesAsync();
        }

        await using var readContext = _db.CreateContext();
        var found = await new EfPlayerGoogleLoginRepository(readContext).GetById("google-7");

        Assert.NotNull(found);
        Assert.Equal(player.Id, found.PlayerId);
        Assert.NotNull(found.Player);
        Assert.Equal("Google Player", found.Player.Name);
    }

    // Пошук за неіснуючим GoogleId повертає null.
    [DockerFact]
    public async Task GetById_WhenGoogleIdUnknown_ReturnsNull()
    {
        await using var context = _db.CreateContext();

        var found = await new EfPlayerGoogleLoginRepository(context).GetById("missing");

        Assert.Null(found);
    }
}
