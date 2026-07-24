using Lobby.Application.Contracts.Repositories;
using Lobby.Settings;
using Microsoft.Extensions.Options;

namespace Lobby.HostedServices;
public class TokenCleanupBackgroundService : BackgroundService
{
    private readonly ILogger<TokenCleanupBackgroundService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    
    public TokenCleanupBackgroundService(
        ILogger<TokenCleanupBackgroundService> logger,
        IServiceScopeFactory scopeFactory
        )
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TokenCleanupBackgroundService: starting...");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var settings = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<TokenCleanupSettings>>().Value;
                
                await Task.Delay(TimeSpan.FromSeconds(settings.CleanupIntervalSeconds), stoppingToken);

                try
                {
                    _logger.LogInformation("Removing expired or revoked refresh tokens...");
                    var refreshTokenRepository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
                    await refreshTokenRepository.DeleteAllExpiredOrRevokedTokens(stoppingToken);
                    _logger.LogInformation("Successfully removed expired or revoked refresh tokens");
                }
                catch (Exception e)
                {
                  _logger.LogError(e, "Failed to cleanup refresh tokens");  
                }
            }
        }
    }
}