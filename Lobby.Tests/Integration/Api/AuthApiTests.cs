using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Lobby.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace Lobby.Tests.Integration.Api;

/// <summary>
/// Інтеграційні тести ендпоінтів /api/auth через реальний HTTP-пайплайн
/// (TestServer + справжній Postgres): маршрутизація, JWT-автентифікація,
/// коди відповідей і побічні ефекти в БД.
/// </summary>
[Collection(ApiCollection.Name)]
public class AuthApiTests : IAsyncLifetime
{
    private readonly LobbyApiFixture _api;

    public AuthApiTests(LobbyApiFixture api) => _api = api;

    public Task InitializeAsync() => _api.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient CreateAuthorizedClient(Guid playerId)
    {
        var client = _api.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _api.CreateAccessToken(playerId));
        return client;
    }

    // Google відхиляє код → 400, гравець у БД не з'являється.
    [DockerFact]
    public async Task GoogleLogin_WhenGoogleRejectsCode_ReturnsBadRequestAndCreatesNoPlayer()
    {
        var client = _api.Factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/google-login", new { code = "bad-code", redirectUri = "https://cb" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await _api.WithDb(async db => Assert.False(await db.Players.AnyAsync()));
    }

    // Перший вхід через Google → 200 із парою токенів, гравець і refresh-токен у БД.
    [DockerFact]
    public async Task GoogleLogin_FirstLogin_ReturnsTokensAndCreatesPlayer()
    {
        _api.Factory.GoogleAuth.NextResult = new PlayerGoogleLoginModel
        {
            GoogleId = "google-1",
            Email = "new@example.com",
            Name = "New Player",
            Role = "User"
        };
        var client = _api.Factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/google-login", new { code = "ok-code", redirectUri = "https://cb" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrEmpty(json.RootElement.GetProperty("accessToken").GetString()));
        Assert.False(string.IsNullOrEmpty(json.RootElement.GetProperty("refreshToken").GetString()));

        await _api.WithDb(async db =>
        {
            var player = await db.Players.Include(p => p.GoogleLogin).SingleAsync();
            Assert.Equal("new@example.com", player.Email);
            Assert.Equal("google-1", player.GoogleLogin!.GoogleId);
            Assert.Equal(1, await db.RefreshTokens.CountAsync());
        });
    }

    // Валідний refresh-токен → 200 із новою парою, старий токен замінено новим у БД.
    [DockerFact]
    public async Task Refresh_WithValidToken_ReturnsNewPairAndRotatesToken()
    {
        var player = await _api.SeedPlayer();
        await _api.SeedRefreshToken(player.Id, "raw_refresh");
        var oldHash = _api.HashToken("raw_refresh");
        var client = _api.Factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/refresh", new { refreshToken = "raw_refresh" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrEmpty(json.RootElement.GetProperty("accessToken").GetString()));
        Assert.False(string.IsNullOrEmpty(json.RootElement.GetProperty("refreshToken").GetString()));

        await _api.WithDb(async db =>
        {
            var remaining = await db.RefreshTokens.SingleAsync();
            Assert.NotEqual(oldHash, remaining.TokenHash);
            Assert.Equal(player.Id, remaining.PlayerId);
        });
    }

    // Невідомий refresh-токен → 400 без видачі нової пари.
    [DockerFact]
    public async Task Refresh_WithUnknownToken_ReturnsBadRequest()
    {
        var client = _api.Factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/refresh", new { refreshToken = "unknown" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Logout без access-токена → 401 (JWT-автентифікація реально ввімкнена).
    [DockerFact]
    public async Task Logout_WithoutAccessToken_ReturnsUnauthorized()
    {
        var client = _api.Factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/logout", new { refreshToken = "whatever" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Logout із валідним access-токеном → 204, refresh-токен зникає з БД.
    [DockerFact]
    public async Task Logout_WithValidAccessToken_Returns204AndRemovesToken()
    {
        var player = await _api.SeedPlayer();
        await _api.SeedRefreshToken(player.Id, "raw_refresh");
        var client = CreateAuthorizedClient(player.Id);

        var response = await client.PostAsJsonAsync(
            "/api/auth/logout", new { refreshToken = "raw_refresh" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await _api.WithDb(async db => Assert.False(await db.RefreshTokens.AnyAsync()));
    }

    // LogoutAll → 204, зникають усі токени гравця, а токени інших гравців лишаються.
    [DockerFact]
    public async Task LogoutAll_RemovesOnlyTokensOfCaller()
    {
        var caller = await _api.SeedPlayer("caller@example.com");
        var other = await _api.SeedPlayer("other@example.com");
        await _api.SeedRefreshToken(caller.Id, "caller_t1");
        await _api.SeedRefreshToken(caller.Id, "caller_t2");
        await _api.SeedRefreshToken(other.Id, "other_t1");
        var client = CreateAuthorizedClient(caller.Id);

        var response = await client.PostAsync("/api/auth/logoutAll", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await _api.WithDb(async db =>
        {
            var remaining = await db.RefreshTokens.SingleAsync();
            Assert.Equal(other.Id, remaining.PlayerId);
        });
    }
}
