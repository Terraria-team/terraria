using Lobby.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Lobby.Tests.Integration;

/// <summary>
/// Спільна фікстура інтеграційних тестів: один PostgreSQL-контейнер (Testcontainers) на всю колекцію. 
/// </summary>
public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
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
        // Порядок важливий: спершу залежні таблиці, бо світи прив'язані до інстансів
        // через Restrict, а сховища — до світів.
        
        await context.RefreshTokens.ExecuteDeleteAsync();
        await context.PlayerExternalIdentities.ExecuteDeleteAsync();
        await context.ServerInstances.ExecuteDeleteAsync();
        await context.TerrariaWorlds.ExecuteDeleteAsync();
        await context.TerrariaWorldStorages.ExecuteDeleteAsync();
        await context.Players.ExecuteDeleteAsync();
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
