using Lobby.Application.Domain;
using Lobby.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lobby.Tests.Integration;

/// <summary>
/// Тести, що перевіряють пошук зовнішньої ідентичності за парою (provider, externalId)
/// разом із завантаженням пов'язаного гравця (Include), а також збереження нової ідентичності.
/// </summary>
[Collection(PostgresCollection.Name)]
public class EfPlayerExternalIdentityRepositoryTests : IAsyncLifetime
{
    private readonly PostgresDatabaseFixture _db;

    public EfPlayerExternalIdentityRepositoryTests(PostgresDatabaseFixture db) => _db = db;

    public Task InitializeAsync() => _db.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static PlayerEntity Player(string email = "google@example.com") => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        Name = "Google Player",
        Role = "User"
    };

    // Пошук за (provider, externalId) повертає ідентичність разом із приєднаним гравцем.
    [DockerFact]
    public async Task GetByExternalIdAsync_ReturnsIdentityWithPlayerIncluded()
    {
        var player = Player();
        player.ExternalIdentities.Add(new PlayerExternalIdentityEntity
        {
            PlayerId = player.Id, Provider = "Google", ExternalId = "google-7"
        });
        await using (var context = _db.CreateContext())
        {
            context.Players.Add(player);
            await context.SaveChangesAsync();
        }

        await using var readContext = _db.CreateContext();
        var found = await new EfPlayerExternalIdentityRepository(readContext)
            .GetByExternalIdAsync("Google", "google-7");

        Assert.NotNull(found);
        Assert.Equal(player.Id, found.PlayerId);
        Assert.NotNull(found.Player);
        Assert.Equal("Google Player", found.Player.Name);
    }

    // Пошук за неіснуючим externalId повертає null.
    [DockerFact]
    public async Task GetByExternalIdAsync_WhenExternalIdUnknown_ReturnsNull()
    {
        await using var context = _db.CreateContext();

        var found = await new EfPlayerExternalIdentityRepository(context)
            .GetByExternalIdAsync("Google", "missing");

        Assert.Null(found);
    }

    // Той самий externalId в іншого провайдера не вважається збігом.
    [DockerFact]
    public async Task GetByExternalIdAsync_WhenProviderDiffers_ReturnsNull()
    {
        var player = Player();
        player.ExternalIdentities.Add(new PlayerExternalIdentityEntity
        {
            PlayerId = player.Id, Provider = "Google", ExternalId = "shared-id"
        });
        await using (var context = _db.CreateContext())
        {
            context.Players.Add(player);
            await context.SaveChangesAsync();
        }

        await using var readContext = _db.CreateContext();
        var found = await new EfPlayerExternalIdentityRepository(readContext)
            .GetByExternalIdAsync("Discord", "shared-id");

        Assert.Null(found);
    }

    // Додана ідентичність зберігається й читається назад через окреме з'єднання.
    [DockerFact]
    public async Task AddAsync_PersistsIdentity()
    {
        var player = Player("added@example.com");
        await using (var context = _db.CreateContext())
        {
            context.Players.Add(player);
            await context.SaveChangesAsync();
        }

        await using (var context = _db.CreateContext())
        {
            await new EfPlayerExternalIdentityRepository(context).AddAsync(new PlayerExternalIdentityEntity
            {
                PlayerId = player.Id, Provider = "Google", ExternalId = "google-added"
            });
        }

        await using var readContext = _db.CreateContext();
        var stored = await readContext.PlayerExternalIdentities.SingleAsync();
        Assert.Equal(player.Id, stored.PlayerId);
        Assert.Equal("Google", stored.Provider);
        Assert.Equal("google-added", stored.ExternalId);
    }
}
