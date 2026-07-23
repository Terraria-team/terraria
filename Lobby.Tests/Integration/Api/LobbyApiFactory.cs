using System.Net;
using Lobby.Application.Contracts;
using Lobby.Application.Entities;
using Lobby.Application.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lobby.Tests.Integration.Api;

/// <summary>
/// Фабрика застосунку для інтеграційних тестів API (реальний HTTP-пайплайн у TestServer).
/// </summary>
public sealed class LobbyApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public FakeGoogleAuthService GoogleAuth { get; } = new();

    public LobbyApiFactory(string connectionString) => _connectionString = connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:LobbyDb", _connectionString);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IGoogleAuthService>();
            services.AddSingleton<IGoogleAuthService>(GoogleAuth);

            services.RemoveAll<IServerInstanceSpawner>();
            services.AddSingleton<IServerInstanceSpawner, FakeServerInstanceSpawner>();

            services.AddSingleton<IStartupFilter, FakeClientIpStartupFilter>();
        });
    }
}

/// <summary>
/// Фейковий обмін Google-коду: повертає заздалегідь заданий результат,
/// а якщо його не задано — помилку авторизації, як робить справжній сервіс на невалідному коді.
/// </summary>
public sealed class FakeGoogleAuthService : IGoogleAuthService
{
    public ResultModel<PlayerGoogleLoginModel>? NextResult { get; set; }

    public Task<ResultModel<PlayerGoogleLoginModel>> ExchangeCode(string code, string redirectUri) =>
        Task.FromResult(NextResult ?? ErrorModel.Unauthorized("fake: invalid google code"));
}

/// <summary>
/// Фейковий спавнер інстансів: не чіпає Docker, повертає детерміновану інформацію про «контейнер».
/// </summary>
public sealed class FakeServerInstanceSpawner : IServerInstanceSpawner
{
    public const string ContainerIdPrefix = "fake-container-";

    public Task<ServerInstanceSpawnInfoEntity> CreateNewServerInstance(Guid id, string name)
    {
        // Додано порт, оскільки раніше змінна не була оголошена
        const int port = 7777; 
        
        return Task.FromResult(new ServerInstanceSpawnInfoEntity(
            ContainerId: $"{ContainerIdPrefix}{port}",
            Image: "fake/terraria-server:test",
            Name: name,
            Port: port,
            Status: ServerInstanceStatus.Running));
    }
}

/// <summary>
/// Проставляє <c>RemoteIpAddress</c> на початку пайплайна:
/// у TestServer він <c>null</c>, а контролери авторизації читають його без перевірки.
/// </summary>
internal sealed class FakeClientIpStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, nextMiddleware) =>
        {
            context.Connection.RemoteIpAddress = IPAddress.Loopback;
            await nextMiddleware();
        });
        next(app);
    };
}