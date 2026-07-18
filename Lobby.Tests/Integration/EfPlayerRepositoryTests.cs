using Lobby.Application.Entities;
using Lobby.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lobby.Tests.Integration;

/// <summary>
/// Перевірки для CRUD, зайнятості email, пагінації,
/// завантаження Google-логіна та каскадне видалення пов'язаних сутностей.
/// </summary>
[Collection(PostgresCollection.Name)]
public class EfPlayerRepositoryTests : IAsyncLifetime
{
    private readonly PostgresDatabaseFixture _db;

    public EfPlayerRepositoryTests(PostgresDatabaseFixture db) => _db = db;

    public Task InitializeAsync() => _db.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static PlayerEntity Player(string email = "player@example.com", Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Email = email,
        Name = "Player",
        Role = "User"
    };

    // Створений гравець читається назад за Id через окреме з'єднання.
    [DockerFact]
    public async Task Create_ThenGetById_ReturnsPersistedPlayer()
    {
        var player = Player("roundtrip@example.com");
        await using (var context = _db.CreateContext())
        {
            await new EfPlayerRepository(context).Create(player);
        }

        await using var readContext = _db.CreateContext();
        var found = await new EfPlayerRepository(readContext).GetById(player.Id);

        Assert.NotNull(found);
        Assert.Equal("roundtrip@example.com", found.Email);
        Assert.Equal("User", found.Role);
    }

    // Email зайнятий іншим гравцем → true; власний email того ж гравця → false.
    [DockerFact]
    public async Task IsEmailTakenAsync_ExcludesCurrentPlayer()
    {
        var owner = Player("taken@example.com");
        await using (var context = _db.CreateContext())
        {
            context.Players.Add(owner);
            await context.SaveChangesAsync();
        }

        await using var readContext = _db.CreateContext();
        var repository = new EfPlayerRepository(readContext);

        Assert.True(await repository.IsEmailTakenAsync("taken@example.com", Guid.NewGuid()));
        Assert.False(await repository.IsEmailTakenAsync("taken@example.com", owner.Id));
        Assert.False(await repository.IsEmailTakenAsync("free@example.com", Guid.NewGuid()));
    }

    // GetByEmail повертає гравця разом із приєднаним Google-логіном (Include).
    [DockerFact]
    public async Task GetByEmail_IncludesGoogleLogin()
    {
        var player = Player("google@example.com");
        player.GoogleLogin = new PlayerGoogleLoginEntity { PlayerId = player.Id, GoogleId = "google-42" };
        await using (var context = _db.CreateContext())
        {
            context.Players.Add(player);
            await context.SaveChangesAsync();
        }

        await using var readContext = _db.CreateContext();
        var found = await new EfPlayerRepository(readContext).GetByEmail("google@example.com");

        Assert.NotNull(found);
        Assert.NotNull(found.GoogleLogin);
        Assert.Equal("google-42", found.GoogleLogin.GoogleId);
    }

    // GetAll сортує за Id та коректно застосовує skip/take.
    [DockerFact]
    public async Task GetAll_OrdersByIdAndAppliesPaging()
    {
        var first = Player("p1@example.com", Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var second = Player("p2@example.com", Guid.Parse("00000000-0000-0000-0000-000000000002"));
        var third = Player("p3@example.com", Guid.Parse("00000000-0000-0000-0000-000000000003"));
        await using (var context = _db.CreateContext())
        {
            context.Players.AddRange(third, first, second);
            await context.SaveChangesAsync();
        }

        await using var readContext = _db.CreateContext();
        var page = await new EfPlayerRepository(readContext).GetAll(skip: 1, take: 1);

        Assert.Single(page);
        Assert.Equal(second.Id, page[0].Id);
    }

    // CountAll повертає фактичну кількість гравців у таблиці.
    [DockerFact]
    public async Task CountAll_ReturnsNumberOfPlayers()
    {
        await using (var context = _db.CreateContext())
        {
            context.Players.AddRange(Player("c1@example.com"), Player("c2@example.com"));
            await context.SaveChangesAsync();
        }

        await using var readContext = _db.CreateContext();
        var count = await new EfPlayerRepository(readContext).CountAll();

        Assert.Equal(2, count);
    }

    // Update перезаписує скалярні поля наявного гравця, а для неіснуючого повертає null.
    [DockerFact]
    public async Task Update_OverwritesExistingAndReturnsNullForMissing()
    {
        var player = Player("before@example.com");
        await using (var context = _db.CreateContext())
        {
            context.Players.Add(player);
            await context.SaveChangesAsync();
        }

        await using (var context = _db.CreateContext())
        {
            var updated = await new EfPlayerRepository(context)
                .Update(Player("after@example.com", player.Id));
            Assert.NotNull(updated);
        }

        await using var readContext = _db.CreateContext();
        var repository = new EfPlayerRepository(readContext);
        var found = await repository.GetById(player.Id);
        Assert.Equal("after@example.com", found!.Email);
        Assert.Null(await repository.Update(Player("ghost@example.com")));
    }

    // Видалення гравця каскадно видаляє його refresh-токени та Google-логін.
    [DockerFact]
    public async Task Delete_CascadesToTokensAndGoogleLogin()
    {
        var player = Player("cascade@example.com");
        player.GoogleLogin = new PlayerGoogleLoginEntity { PlayerId = player.Id, GoogleId = "google-cascade" };
        await using (var context = _db.CreateContext())
        {
            context.Players.Add(player);
            context.RefreshTokens.Add(new RefreshTokenEntity
            {
                Id = Guid.NewGuid(),
                PlayerId = player.Id,
                TokenHash = "hash_cascade",
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                IsRevoked = false,
                CreatedByIp = "127.0.0.1"
            });
            await context.SaveChangesAsync();
        }

        await using (var context = _db.CreateContext())
        {
            var deleted = await new EfPlayerRepository(context).Delete(player.Id);
            Assert.True(deleted);
        }

        await using var readContext = _db.CreateContext();
        Assert.False(await readContext.Players.AnyAsync());
        Assert.False(await readContext.RefreshTokens.AnyAsync());
        Assert.False(await readContext.PlayerGoogleLogins.AnyAsync());
    }

    // Видалення неіснуючого гравця повертає false і нічого не змінює.
    [DockerFact]
    public async Task Delete_WhenPlayerMissing_ReturnsFalse()
    {
        await using var context = _db.CreateContext();

        var deleted = await new EfPlayerRepository(context).Delete(Guid.NewGuid());

        Assert.False(deleted);
    }
}
