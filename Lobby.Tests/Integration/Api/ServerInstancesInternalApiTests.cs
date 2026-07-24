using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lobby.Application.Domain;
using Lobby.Settings;
using LobbyUnityShared;
using LobbyUnityShared.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lobby.Tests.Integration.Api;

/// <summary>
/// Інтеграційні тести внутрішнього ендпоінта /api/internal/server-instances через реальний HTTP-пайплайн.
/// Перевіряють, що LobbyToServerAuthFilter енфорсить серверний API-ключ (немає / невірний / валідний),
/// а UpdatePlayerCount проходить увесь ланцюжок до збереження в БД, включно з валідацією та NotFound.
/// </summary>
[Collection(ApiCollection.Name)]
public class ServerInstancesInternalApiTests : IAsyncLifetime
{
    private readonly LobbyApiFixture _api;

    public ServerInstancesInternalApiTests(LobbyApiFixture api) => _api = api;

    public Task InitializeAsync() => _api.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private string ApiKey() => _api.Factory.Services.GetRequiredService<ApiSecuritySettings>().ApiKey;

    private HttpClient CreateServerClient(string? apiKey)
    {
        var client = _api.Factory.CreateClient();
        if (apiKey is not null)
            client.DefaultRequestHeaders.Add(ApiConstants.ServerApiKeyHeader, apiKey);
        return client;
    }

    private async Task<Guid> SeedServerInstance(int port, int playerCount)
    {
        var owner = await _api.SeedPlayer($"owner_{port}@example.com");
        var worldId = Guid.NewGuid();
        var instanceId = Guid.NewGuid();
        await _api.WithDb(async db =>
        {
            db.Set<TerrariaWorldEntity>().Add(new TerrariaWorldEntity
            {
                Id = worldId,
                OwnerId = owner.Id,
                Name = "Test World",
                StorageId = null
            });
            db.ServerInstances.Add(new ServerInstanceEntity
            {
                Id = instanceId,
                WorldId = worldId,
                ContainerId = $"container-{port}",
                Image = "terraria-server:latest",
                Name = $"server_instance_{port}",
                Port = port,
                PlayerCount = playerCount,
                EmptySince = null,
                PendingSince = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null,
                Status = ServerInstanceStatus.Running
            });
            await db.SaveChangesAsync();
        });
        return instanceId;
    }

    private static string Url(Guid id) => $"/api/internal/server-instances/{id}/player-count";

    // Без заголовка серверного API-ключа фільтр закриває ендпоінт → 401.
    [DockerFact]
    public async Task UpdatePlayerCount_WithoutApiKey_ReturnsUnauthorized()
    {
        var client = CreateServerClient(apiKey: null);

        var response = await client.PostAsJsonAsync(Url(Guid.NewGuid()), new UpdatePlayerCountDto { PlayerCount = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // З невірним серверним API-ключем → 401.
    [DockerFact]
    public async Task UpdatePlayerCount_WithWrongApiKey_ReturnsUnauthorized()
    {
        var client = CreateServerClient("wrong-key");

        var response = await client.PostAsJsonAsync(Url(Guid.NewGuid()), new UpdatePlayerCountDto { PlayerCount = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Валідний ключ, але від'ємна кількість гравців → 400 (валідація до сервісу).
    [DockerFact]
    public async Task UpdatePlayerCount_WithNegativeCount_ReturnsBadRequest()
    {
        var client = CreateServerClient(ApiKey());

        var response = await client.PostAsJsonAsync(Url(Guid.NewGuid()), new UpdatePlayerCountDto { PlayerCount = -1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Валідний ключ, але невідомий інстанс → 404.
    [DockerFact]
    public async Task UpdatePlayerCount_WhenInstanceMissing_ReturnsNotFound()
    {
        var client = CreateServerClient(ApiKey());

        var response = await client.PostAsJsonAsync(Url(Guid.NewGuid()), new UpdatePlayerCountDto { PlayerCount = 2 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Валідний ключ + засіяний інстанс → 200, у БД оновлюється кількість гравців.
    [DockerFact]
    public async Task UpdatePlayerCount_WithValidRequest_UpdatesInstanceAndReturnsOk()
    {
        var instanceId = await SeedServerInstance(port: 7778, playerCount: 0);
        var client = CreateServerClient(ApiKey());

        var response = await client.PostAsJsonAsync(Url(instanceId), new UpdatePlayerCountDto { PlayerCount = 4 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(4, json.RootElement.GetProperty("playerCount").GetInt32());

        await _api.WithDb(async db =>
        {
            var instance = await db.ServerInstances.SingleAsync(i => i.Id == instanceId);
            Assert.Equal(4, instance.PlayerCount);
        });
    }
}
