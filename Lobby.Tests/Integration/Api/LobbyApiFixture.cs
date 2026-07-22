using Lobby.Application.Contracts;
using Lobby.Application.Entities;
using Lobby.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Lobby.Tests.Integration.Api;

/// <summary>
/// Спільна фікстура API-тестів: власний Postgres-контейнер + один запущений застосунок на всю колекцію. 
/// Міграції застосовує сам застосунок під час старту (як у проді). 
/// Між тестами таблиці очищаються, а фейк Google скидається.
/// </summary>
public sealed class LobbyApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    public LobbyApiFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        Factory = new LobbyApiFactory(_container.GetConnectionString());
        _ = Factory.Server;
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
            await Factory.DisposeAsync();
        await _container.DisposeAsync();
    }

    public async Task ResetAsync()
    {
        Factory.GoogleAuth.NextResult = null;
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LobbyDbContext>();
        await db.RefreshTokens.ExecuteDeleteAsync();
        await db.PlayerGoogleLogins.ExecuteDeleteAsync();
        await db.Players.ExecuteDeleteAsync();
        await db.ServerInstances.ExecuteDeleteAsync();
    }

    /// <summary>Виконує дію над LobbyDbContext застосунку (сідінг/перевірки).</summary>
    public async Task WithDb(Func<LobbyDbContext, Task> action)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LobbyDbContext>();
        await action(db);
    }

    /// <summary>Створює гравця в БД застосунку.</summary>
    public async Task<PlayerEntity> SeedPlayer(string email = "player@example.com")
    {
        var player = new PlayerEntity
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = "Player",
            Role = "User"
        };
        await WithDb(async db =>
        {
            db.Players.Add(player);
            await db.SaveChangesAsync();
        });
        return player;
    }

    /// <summary>Зберігає refresh-токен для гравця; у БД кладеться хеш від rawToken>.</summary>
    public async Task SeedRefreshToken(Guid playerId, string rawToken)
    {
        var tokenHash = HashToken(rawToken);
        await WithDb(async db =>
        {
            db.RefreshTokens.Add(new RefreshTokenEntity
            {
                Id = Guid.NewGuid(),
                PlayerId = playerId,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                IsRevoked = false,
                CreatedByIp = "127.0.0.1"
            });
            await db.SaveChangesAsync();
        });
    }

    /// <summary>Видає валідний access-токен тим самим IJwtService, що й застосунок.</summary>
    public string CreateAccessToken(Guid playerId)
    {
        using var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IJwtService>()
            .GenerateToken(playerId, "player@example.com", "User", "Player");
    }

    /// <summary>Хешує токен тим самим ITokenService, що й застосунок.</summary>
    public string HashToken(string rawToken)
    {
        using var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ITokenService>().HashToken(rawToken);
    }
}

/// <summary>
/// Колекція xUnit, що ділить один застосунок і один Postgres-контейнер між API-тестами.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<LobbyApiFixture>
{
    public const string Name = "Api";
}
