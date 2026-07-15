using Lobby.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Lobby.Tests.Integration;

/// <summary>
/// Спільна фікстура інтеграційних тестів: один PostgreSQL-контейнер (Testcontainers) на всю колекцію. 
/// </summary>
public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public LobbyDbContext CreateContext() => new(
        new DbContextOptionsBuilder<LobbyDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options);

    public async Task ResetAsync()
    {
        await using var context = CreateContext();
        await context.RefreshTokens.ExecuteDeleteAsync();
        await context.PlayerGoogleLogins.ExecuteDeleteAsync();
        await context.Players.ExecuteDeleteAsync();
        await context.ServerInstances.ExecuteDeleteAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}

/// <summary>
/// Колекція xUnit, що ділить один Postgres-контейнер між усіма тестовими класами.
/// </summary>
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresDatabaseFixture>
{
    public const string Name = "Postgres";
}
