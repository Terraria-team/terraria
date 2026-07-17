using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Google.Apis.Auth;
using Lobby.Application.Contracts;
using Lobby.Application.Models;
using Lobby.Infrastructure.Settings;
using Microsoft.Extensions.Logging;

namespace Lobby.Infrastructure.Services;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly GoogleSettings _settings;
    private readonly ILogger<GoogleAuthService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public GoogleAuthService(
        GoogleSettings settings,
        ILogger<GoogleAuthService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _settings = settings;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ResultModel<PlayerGoogleLoginModel>> ExchangeCode(string code, string redirectUri)
    {
        string? idToken;
        try
        {
            idToken = await ExchangeCodeForIdToken(code, redirectUri);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google token endpoint call failed.");
            return ErrorModel.Validation($"Google token exchange failed: {ex.Message}");
        }

        if (idToken is null)
            return ErrorModel.Validation("Google did not return an id_token");

        try
        {
            var validationSettings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _settings.ClientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);

            return new PlayerGoogleLoginModel
            {
                GoogleId = payload.Subject,
                Email = payload.Email,
                Name = payload.Name,
                Role = "Player"
            };
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogError(ex, "Google ID token validation failed after code exchange.");
            return ErrorModel.Validation("Invalid Google token signature");
        }
    }

    private async Task<string?> ExchangeCodeForIdToken(string code, string redirectUri)
    {
        using var client = _httpClientFactory.CreateClient();

        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "code",          code },
            { "client_id",     _settings.ClientId },
            { "client_secret", _settings.ClientSecret },
            { "redirect_uri",  redirectUri },
            { "grant_type",    "authorization_code" }
        });

        var response = await client.PostAsync("https://oauth2.googleapis.com/token", formContent);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Google /token returned {StatusCode}: {Body}", response.StatusCode, body);
            throw new HttpRequestException($"Google token endpoint failed: {response.StatusCode}");
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>();
        return tokenResponse?.IdToken;
    }

    private sealed class GoogleTokenResponse
    {
        [JsonPropertyName("id_token")]
        public string? IdToken { get; set; }

        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }
}