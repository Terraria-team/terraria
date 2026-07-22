using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Lobby.Application.Entities;
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

    private HttpClient CreateAuthorizedClient()
    {
        var client = _api.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _api.CreateAccessToken(Guid.NewGuid()));
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
        await _api.WithDb(async db =>
        {
            // Оновлено для використання ініціалізатора об'єкта (class) замість позиційного конструктора (record)
            db.ServerInstances.Add(new ServerInstanceEntity
            {
                Id = Guid.NewGuid(),
                WorldId = Guid.NewGuid(), // Додано нове поле
                ContainerId = "container-1",
                Image = "terraria-server:latest",
                Name = "server_instance_7778",
                Port = 7778,
                PlayerCount = 0,
                EmptySince = null,
                PendingSince = null, // Додано нове поле
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null,
                Status = ServerInstanceStatus.Running
            });
            await db.SaveChangesAsync();
        });
        var client = CreateAuthorizedClient();

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
        var client = CreateAuthorizedClient();

        var response = await client.PostAsync("/api/server-instances", content: null);

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