using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Lobby.Application.Domain;
using Microsoft.EntityFrameworkCore;

namespace Lobby.Tests.Integration.Api;

/// <summary>
/// Інтеграційні тести ендпоінтів /api/server-instances через реальний HTTP-пайплайн:
/// перевіряють, що [Authorize] на контролері справді енфорситься, і що створення
/// інстансу проходить увесь ланцюжок (порт → фейковий спавнер → збереження в БД).
/// </summary>
[Collection(ApiCollection.Name)]
public class ServerInstancesApiTests : IAsyncLifetime
{
    private readonly LobbyApiFixture _api;

    public ServerInstancesApiTests(LobbyApiFixture api) => _api = api;

    public Task InitializeAsync() => _api.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient CreateAuthorizedClient(Guid? playerId = null)
    {
        var client = _api.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _api.CreateAccessToken(playerId ?? Guid.NewGuid()));
        return client;
    }

    // Без access-токена контролер закритий → 401 (ключова перевірка безпеки).
    [DockerFact]
    public async Task GetAll_WithoutAccessToken_ReturnsUnauthorized()
    {
        var client = _api.Factory.CreateClient();

        var response = await client.GetAsync("/api/server-instances");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // З валідним токеном → 200 і список раніше збережених інстансів.
    [DockerFact]
    public async Task GetAll_WithAccessToken_ReturnsSeededInstances()
    {
        var player = await _api.SeedPlayer();
        var worldId = Guid.NewGuid();
        
        await _api.WithDb(async db =>
        {
            db.Set<TerrariaWorldEntity>().Add(new TerrariaWorldEntity
            {
                Id = worldId,
                OwnerId = player.Id,
                Name = "Test World",
                StorageId = null
            });
            db.ServerInstances.Add(new ServerInstanceEntity
            {
                Id = Guid.NewGuid(),
                WorldId = worldId, 
                ContainerId = "container-1",
                Image = "terraria-server:latest",
                Name = "server_instance_7778",
                Port = 7778,
                PlayerCount = 0,
                EmptySince = null,
                PendingSince = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null,
                Status = ServerInstanceStatus.Running
            });
            await db.SaveChangesAsync();
        });
        var client = CreateAuthorizedClient(player.Id);

        var response = await client.GetAsync("/api/server-instances");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var instance = Assert.Single(json.RootElement.EnumerateArray().ToList());
        Assert.Equal(7778, instance.GetProperty("port").GetInt32());
    }

    // POST створює інстанс: фейковий спавнер + реальне збереження в БД → 201.
    [DockerFact]
    public async Task Create_WithAccessToken_PersistsInstanceFromSpawner()
    {
        var player = await _api.SeedPlayer();
        var client = CreateAuthorizedClient(player.Id);

        var response = await client.PostAsJsonAsync("/api/server-instances", new LobbyUnityShared.DTOs.CreateServerDto { Name = "test" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await _api.WithDb(async db =>
        {
            var instance = await db.ServerInstances.SingleAsync();
            Assert.StartsWith(FakeServerInstanceSpawner.ContainerIdPrefix, instance.ContainerId);
            Assert.Equal(0, instance.PlayerCount);
            Assert.Equal(ServerInstanceStatus.Running, instance.Status);
        });
    }
}