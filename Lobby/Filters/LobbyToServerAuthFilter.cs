using Lobby.Infrastructure.Settings;
using LobbyUnityShared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lobby.Filters;

public class LobbyToServerAuthFilter : ActionFilterAttribute
{
    private readonly ServerToLobbyAuthSettings _settings;

    public LobbyToServerAuthFilter(ServerToLobbyAuthSettings settings)
    {
        _settings = settings;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiConstants.ServerApiKeyHeader, out var extractedApiKey))
        {
            context.Result = new UnauthorizedObjectResult("Missing Server API Key");
            return;
        }
        

        if (_settings.ApiKey != extractedApiKey.ToString())
        {
            context.Result = new UnauthorizedObjectResult("Invalid Server API Key");
            return;
        }

        await next();
    }
}