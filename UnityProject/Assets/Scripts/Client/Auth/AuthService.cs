using System;
using System.Threading.Tasks;
using Client.Api;
using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient;
using LobbyUnityShared.DTOs;
using Client.Config;
using UnityEngine;

namespace Client.Auth
{
    public class AuthService
    {
        private static AuthService _instance;
        public static AuthService Instance => _instance ??= new AuthService();
        
        private readonly string _googleClientId;
        private readonly string _googleClientSecret;
        private readonly string _googleLoginEndpoint;
        private readonly string _refreshEndpoint;
        private readonly string _logoutEndpoint;

        private readonly string _baseUrl;

        public string CurrentAccessToken { get; private set; }
        public string CurrentRefreshToken { get; private set; }

        public event Action OnAuthStarted;
        public event Action<string> OnAuthStatusUpdated;
        public event Action<string> OnAuthSuccess;
        public event Action<string> OnAuthFailed;

        private AuthService()
        {
            var config = Resources.Load<ClientConfig>("ClientConfig");
            if (config != null)
            {
                _googleClientId = config.googleClientId;
                _googleClientSecret = config.googleClientSecret;
                _googleLoginEndpoint = config.googleLoginEndpoint;
                _refreshEndpoint = config.refreshEndpoint;
                _logoutEndpoint = config.logoutEndpoint;
                
                _baseUrl = config.baseUrl;
            }
            else
            {
                Debug.LogError("[AuthService] CRITICAL: ClientConfig not found in Resources folder! Falling back to localhost defaults.");
                _googleClientId = "859856222839-qjfks5pbv25osu3ks8pirl994llfkt4p.apps.googleusercontent.com";
                _googleClientSecret = "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";
                _googleLoginEndpoint = "/api/auth/google-login";
                _refreshEndpoint = "/api/auth/refresh";
                _logoutEndpoint = "/api/auth/logout";
                
                _baseUrl = "http://localhost:5241";
            }
            LoadRefreshTokenFromDisk();
        }

        public async Task StartGoogleLoginFlow()
        {
            OnAuthStarted?.Invoke();
            OnAuthStatusUpdated?.Invoke("Opening OS browser for Google authentication...");

            try
            {
                var browser = new Browser();
                int freePort = browser.Port;
                
                var options = new OidcClientOptions
                {
                    Authority = "https://accounts.google.com",
                    ClientId = _googleClientId,
                    ClientSecret = _googleClientSecret,
                    Scope = "openid profile email",
                    RedirectUri = $"http://127.0.0.1:{freePort}/",
                    Browser = browser,
                    Policy = new Policy { Discovery = new DiscoveryPolicy { ValidateEndpoints = false } }
                };
                
                var oidcClient = new OidcClient(options);
                LoginResult loginResult = await oidcClient.LoginAsync();

                if (loginResult.IsError)
                {
                    OnAuthFailed?.Invoke($"Google OAuth Error: {loginResult.Error}");
                    return;
                }

                OnAuthStatusUpdated?.Invoke("Google authentication complete! Connecting to server");
                bool success = await ExchangeTokenWithBackendAsync(loginResult.IdentityToken);

                if (success)
                {
                    OnAuthStatusUpdated?.Invoke("Logged in successfully!");
                    OnAuthSuccess?.Invoke(CurrentAccessToken);
                }
            }
            catch (Exception ex)
            {
                OnAuthFailed?.Invoke($"Fatal login exception: {ex.Message}");
            }
        }

        private async Task<bool> ExchangeTokenWithBackendAsync(string googleIdToken)
        {
            string url = _baseUrl + _googleLoginEndpoint;
    
            var tokens = await HttpUtil.SendAsync<LoginTokensDto>(url, "POST", new { googleIdToken });

            if (tokens == null || string.IsNullOrEmpty(tokens.accesstoken))
            {
                OnAuthFailed?.Invoke("Backend token exchange failed or returned an empty access token.");
                return false;
            }

            UpdateTokens(tokens);
            return true;
        }

        public async Task<bool> TrySilentRefreshAsync()
        {
            if (string.IsNullOrEmpty(CurrentRefreshToken)) return false;

            string url = _baseUrl + _refreshEndpoint;
            var (content, statusCode, error) = await HttpUtil.SendRawAsync(url, "POST", new { refreshToken = CurrentRefreshToken });

            if (statusCode == 400 || statusCode == 401)
            {
                Debug.LogWarning($"[AuthService] Refresh token rejected by server ({statusCode}). Wiping saved session.");
                ClearTokens();
                return false;
            }

            if (error != null || string.IsNullOrEmpty(content)) return false;

            try
            {
                var tokens = Newtonsoft.Json.JsonConvert.DeserializeObject<LoginTokensDto>(content);
                if (tokens == null || string.IsNullOrEmpty(tokens.accesstoken)) return false;

                UpdateTokens(tokens);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            if (string.IsNullOrEmpty(CurrentRefreshToken)) return;

            string url = _baseUrl + _logoutEndpoint;
            await HttpUtil.SendRawAsync(url, "POST", new { refreshToken = CurrentRefreshToken }, CurrentAccessToken);
        
            ClearTokens();
        }
        
        private void UpdateTokens(LoginTokensDto tokens)
        {
            
            CurrentAccessToken = tokens.accesstoken;
            CurrentRefreshToken = tokens.sessiontoken;
            
            PlayerPrefs.SetString("refresh_token", CurrentRefreshToken);
            PlayerPrefs.Save();
        }

        private void ClearTokens()
        {
            CurrentAccessToken = null;
            CurrentRefreshToken = null;
            
            PlayerPrefs.DeleteKey("refresh_token");
            PlayerPrefs.Save();
        }
        
        public void SetRefreshToken(string token)
        {
            CurrentRefreshToken = token;
        }
        
        public bool LoadRefreshTokenFromDisk()
        {
            if (PlayerPrefs.HasKey("refresh_token"))
            {
                CurrentRefreshToken = PlayerPrefs.GetString("refresh_token");
                return true;
            }
            return false;
        }
    } 
}