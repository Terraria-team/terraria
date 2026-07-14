using System;
using System.Text;
using System.Threading.Tasks;
using LobbyUnityShared.DTOs;
using Client.Config;
using Shared.Api;
using UnityEngine;

namespace Client.Auth
{
    public class AuthService
    {
        private static AuthService _instance;
        public static AuthService Instance => _instance ??= new AuthService();

        private readonly string _googleClientId;
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
                _googleClientId      = config.googleClientId;
                _googleLoginEndpoint = config.googleLoginEndpoint;
                _refreshEndpoint     = config.refreshEndpoint;
                _logoutEndpoint      = config.logoutEndpoint;
                _baseUrl             = config.baseUrl;
            }
            else
            {
                Debug.LogError("[AuthService] CRITICAL: ClientConfig not found in Resources folder! Falling back to localhost defaults.");
                _googleClientId      = "YOUR_GOOGLE_CLIENT_ID_HERE";
                _googleLoginEndpoint = "/api/auth/google-login";
                _refreshEndpoint     = "/api/auth/refresh";
                _logoutEndpoint      = "/api/auth/logout";
                _baseUrl             = "http://localhost:5241";
            }
            LoadRefreshTokenFromDisk();
        }

        public async Task StartGoogleLoginFlow()
        {
            OnAuthStarted?.Invoke();
            OnAuthStatusUpdated?.Invoke("Opening OS browser for Google authentication...");

            try
            {
                using var browser = new Browser();
                string redirectUri = $"http://127.0.0.1:{browser.Port}/";
                string authUrl     = BuildGoogleAuthUrl(redirectUri);

                Application.OpenURL(authUrl);

                string redirectResponse = await browser.WaitForRedirectAsync();

                if (string.IsNullOrEmpty(redirectResponse))
                {
                    OnAuthFailed?.Invoke("Login timed out or browser was closed. Please try again.");
                    return;
                }

                string error = ParseQueryParam(redirectResponse, "error");
                if (!string.IsNullOrEmpty(error))
                {
                    OnAuthFailed?.Invoke($"Google OAuth Error: {error}");
                    return;
                }

                string code = ParseQueryParam(redirectResponse, "code");
                if (string.IsNullOrEmpty(code))
                {
                    OnAuthFailed?.Invoke("No authorization code received from Google.");
                    return;
                }

                OnAuthStatusUpdated?.Invoke("Google authentication complete! Connecting to server...");
                bool success = await ExchangeCodeWithBackendAsync(code, redirectUri);

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

        private string BuildGoogleAuthUrl(string redirectUri)
        {
            var sb = new StringBuilder("https://accounts.google.com/o/oauth2/v2/auth?");
            sb.Append($"client_id={Uri.EscapeDataString(_googleClientId)}");
            sb.Append($"&redirect_uri={Uri.EscapeDataString(redirectUri)}");
            sb.Append("&response_type=code");
            sb.Append("&scope=openid%20profile%20email");
            sb.Append("&access_type=offline");
            return sb.ToString();
        }

        private static string ParseQueryParam(string url, string param)
        {
            int queryStart = url.IndexOf('?');
            if (queryStart < 0) return null;

            string query = url.Substring(queryStart + 1);
            foreach (var part in query.Split('&'))
            {
                int eqIndex = part.IndexOf('=');
                if (eqIndex < 0) continue;
                string key = Uri.UnescapeDataString(part.Substring(0, eqIndex));
                if (key == param)
                    return Uri.UnescapeDataString(part.Substring(eqIndex + 1));
            }
            return null;
        }

        private async Task<bool> ExchangeCodeWithBackendAsync(string code, string redirectUri)
        {
            string url = _baseUrl + _googleLoginEndpoint;
            var tokens = await HttpUtil.SendAsync<LoginTokensDto>(url, "POST", new GoogleLoginDto { Code = code, RedirectUri = redirectUri });

            if (tokens == null || string.IsNullOrEmpty(tokens.AccessToken))
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
            var (content, statusCode, error) = await HttpUtil.SendRawAsync(url, "POST", new RefreshTokenDto { RefreshToken = CurrentRefreshToken });
            
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
                if (tokens == null || string.IsNullOrEmpty(tokens.AccessToken)) return false;

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
            await HttpUtil.SendRawAsync(url, "POST", new RefreshTokenDto { RefreshToken = CurrentRefreshToken }, CurrentAccessToken);

            ClearTokens();
        }

        private void UpdateTokens(LoginTokensDto tokens)
        {
            CurrentAccessToken  = tokens.AccessToken;
            CurrentRefreshToken = tokens.RefreshToken;

            PlayerPrefs.SetString("refresh_token", CurrentRefreshToken);
            PlayerPrefs.Save();
        }

        private void ClearTokens()
        {
            CurrentAccessToken  = null;
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