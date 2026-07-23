using Lobby.Application.Domain;
using Lobby.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lobby.Tests.Integration;

/// <summary>
/// Тести, що перевіряють реальні LINQ-запити, фільтр очистки протермінованих/відкликаних токенів
/// та унікальний індекс на хеші токена.
/// </summary>
[Collection(PostgresCollection.Name)]
public class EfRefreshTokenRepositoryTests : IAsyncLifetime
{
    private readonly PostgresDatabaseFixture _db;

    public EfRefreshTokenRepositoryTests(PostgresDatabaseFixture db) => _db = db;

    public Task InitializeAsync() => _db.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static PlayerEntity Player() => new()
    {
        Id = Guid.NewGuid(),
        Email = $"{Guid.NewGuid():N}@example.com",
        Name = "Player",
        Role = "User"
    };

    private static RefreshTokenEntity Token(
        Guid playerId, string hash, bool revoked = false, DateTime? expiresAt = null) => new()
    {
        Id = Guid.NewGuid(),
        PlayerId = playerId,
        TokenHash = hash,
        ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(1),
        IsRevoked = revoked,
        CreatedByIp = "127.0.0.1"
    };

    private async Task<PlayerEntity> SeedPlayer()
    {
        await using var context = _db.CreateContext();
        var player = Player();
        context.Players.Add(player);
        await context.SaveChangesAsync();
        return player;
    }

    // Доданий токен читається назад за хешем через окреме з'єднання.
    [DockerFact]
    public async Task AddRefreshToken_PersistsAndIsReadableByHash()
    {
        var player = await SeedPlayer();

        await using (var context = _db.CreateContext())
        {
            await new EfRefreshTokenRepository(context)
                .AddRefreshToken(Token(player.Id, "hash_a"));
        }

        await using var readContext = _db.CreateContext();
        var found = await new EfRefreshTokenRepository(readContext).GetRefreshToken("hash_a");

        Assert.NotNull(found);
        Assert.Equal(player.Id, found.PlayerId);
        Assert.Equal("127.0.0.1", found.CreatedByIp);
    }

    // Пошук за неіснуючим хешем повертає null.
    [DockerFact]
    public async Task GetRefreshToken_WhenHashUnknown_ReturnsNull()
    {
        await using var context = _db.CreateContext();

        var found = await new EfRefreshTokenRepository(context).GetRefreshToken("missing");

        Assert.Null(found);
    }

    // RevokeToken видаляє лише токен із заданим хешем, інші лишаються.
    [DockerFact]
    public async Task RevokeToken_RemovesOnlyMatchingToken()
    {
        var player = await SeedPlayer();
        await using (var context = _db.CreateContext())
        {
            context.RefreshTokens.AddRange(Token(player.Id, "hash_a"), Token(player.Id, "hash_b"));
            await context.SaveChangesAsync();
        }

        await using (var context = _db.CreateContext())
        {
            await new EfRefreshTokenRepository(context).RevokeToken("hash_a");
        }

        await using var readContext = _db.CreateContext();
        var remaining = await readContext.RefreshTokens.Select(t => t.TokenHash).ToListAsync();
        Assert.Equal(["hash_b"], remaining);
    }

    // RevokeAllTokens видаляє всі токени гравця, не зачіпаючи токени інших гравців.
    [DockerFact]
    public async Task RevokeAllTokens_RemovesOnlyTokensOfGivenPlayer()
    {
        var target = await SeedPlayer();
        var other = await SeedPlayer();
        await using (var context = _db.CreateContext())
        {
            context.RefreshTokens.AddRange(
                Token(target.Id, "hash_t1"),
                Token(target.Id, "hash_t2"),
                Token(other.Id, "hash_o1"));
            await context.SaveChangesAsync();
        }

        await using (var context = _db.CreateContext())
        {
            await new EfRefreshTokenRepository(context).RevokeAllTokens(target.Id);
        }

        await using var readContext = _db.CreateContext();
        var remaining = await readContext.RefreshTokens.Select(t => t.TokenHash).ToListAsync();
        Assert.Equal(["hash_o1"], remaining);
    }

    // Очистка видаляє протерміновані та відкликані токени, а чинні лишає.
    [DockerFact]
    public async Task DeleteAllExpiredOrRevokedTokens_KeepsOnlyActiveTokens()
    {
        var player = await SeedPlayer();
        await using (var context = _db.CreateContext())
        {
            context.RefreshTokens.AddRange(
                Token(player.Id, "hash_expired", expiresAt: DateTime.UtcNow.AddMinutes(-1)),
                Token(player.Id, "hash_revoked", revoked: true),
                Token(player.Id, "hash_active"));
            await context.SaveChangesAsync();
        }

        await using (var context = _db.CreateContext())
        {
            await new EfRefreshTokenRepository(context)
                .DeleteAllExpiredOrRevokedTokens(CancellationToken.None);
        }

        await using var readContext = _db.CreateContext();
        var remaining = await readContext.RefreshTokens.Select(t => t.TokenHash).ToListAsync();
        Assert.Equal(["hash_active"], remaining);
    }

    // Унікальний індекс на TokenHash не дозволяє зберегти два токени з однаковим хешем.
    [DockerFact]
    public async Task AddRefreshToken_WithDuplicateHash_ThrowsDbUpdateException()
    {
        var player = await SeedPlayer();
        await using (var context = _db.CreateContext())
        {
            await new EfRefreshTokenRepository(context)
                .AddRefreshToken(Token(player.Id, "hash_dup"));
        }

        await using var secondContext = _db.CreateContext();
        var repository = new EfRefreshTokenRepository(secondContext);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => repository.AddRefreshToken(Token(player.Id, "hash_dup")));
    }
}
