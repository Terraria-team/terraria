using Google.Apis.Auth;
using Lobby.Application.Contracts;
using Lobby.Application.Models;
using Lobby.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lobby.Infrastructure.Services;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly GoogleSettings _settings;
    private readonly ILogger<GoogleAuthService> _logger;
    
    public GoogleAuthService(GoogleSettings settings, ILogger<GoogleAuthService> logger)
    {
        _logger = logger;
        _settings = settings;
    }

    public async Task<ResultModel<PlayerGoogleLoginModel>> ValidateToken(string idToken)
    {
        try
        {
            var validationSettings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _settings.ClientId }
            };
            
            GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);
            
            var profile = new PlayerGoogleLoginModel
            {
                GoogleId = payload.Subject,
                Email = payload.Email,
                Name = payload.Name,
                Role = "Player"
            };

            return profile;
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogError(ex, "Google ID token validation failed during authentication.");
            return new ErrorModel("Invalid Google token signature", ErrorType.Validation);
        }
    }
}