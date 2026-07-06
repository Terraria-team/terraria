using System;
using System.Threading.Tasks;
using Client.Api;
using Client.Auth;
using Client.Config;
using UnityEngine;

// AI GENERATED TEST TO CHECK IF AUTH FUNCTIONALITY WORKS WITH LOBBY ENDPOINTS
// Auth flow (as of refactor): Client opens browser → captures ?code= from loopback redirect
// → POSTs {code, redirectUri} to /api/auth/google-login → server exchanges code with Google
// → server issues JWT + refresh token. The OidcClient / client-secret are no longer used.
namespace Client
{
    public class AuthWorkflowTest : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("If true, the Full Typical Workflow will run automatically when you hit Play.")]
        public bool runWorkflowOnStart = true;
        
        private string _serverInstancesUrl;

        private void Start()
        {
            var config = Resources.Load<ClientConfig>("ClientConfig");
            _serverInstancesUrl = config != null ? config.serverInstancesEndpoint : "/api/server-instances";

            // Subscribe to Auth events to monitor login
            AuthService.Instance.OnAuthStarted += () => Debug.Log("[Auth] Started...");
            AuthService.Instance.OnAuthStatusUpdated += (status) => Debug.Log($"[Auth] Status: {status}");
            
            AuthService.Instance.OnAuthFailed += (err) =>
            {
                Debug.LogError($"[Auth] Failed: {err}");
            };
            
            AuthService.Instance.OnAuthSuccess += async (jwt) =>
            {
                Debug.Log($"<color=#4CAF50>[Auth] Success! JWT received: {jwt}</color>");
                
                // REACTIVE STEP: As soon as login succeeds, immediately run the protected endpoint test!
                Debug.Log("Login successful. Proceeding to call protected endpoint...");
                await TestProtectedEndpointsAsync();
                Debug.Log("=== WORKFLOW COMPLETE (PATH: Manual Login) ===");
            };

            if (runWorkflowOnStart)
            {
                RunFullWorkflow();
            }
        }

        [ContextMenu("1. Test Disk Storage & Memory")]
        public void TestDiskStorage()
        {
            Debug.Log("--- Test Disk Storage ---");
            string testToken = "test_refresh_token_123";
            
            // NOTE: Ensure AuthService uses this exact string "refresh_token" internally!
            PlayerPrefs.SetString("refresh_token", testToken);
            PlayerPrefs.Save();
            
            bool loaded = AuthService.Instance.LoadRefreshTokenFromDisk();
            if (loaded && AuthService.Instance.CurrentRefreshToken == testToken)
            {
                Debug.Log("Success: Loaded token from disk into memory.");
            }
            else
            {
                Debug.LogError("Failed to load token from disk. Check if AuthService uses the key 'refresh_token'.");
            }

            PlayerPrefs.DeleteKey("refresh_token");
            PlayerPrefs.Save();
            AuthService.Instance.SetRefreshToken(null);
            
            bool reLoaded = AuthService.Instance.LoadRefreshTokenFromDisk();
            if (!reLoaded && string.IsNullOrEmpty(AuthService.Instance.CurrentRefreshToken))
            {
                Debug.Log("Success: Token wiped cleanly.");
            }
            else
            {
                Debug.LogError("Failed to wipe token cleanly.");
            }
        }

        [ContextMenu("2. Test Google OAuth Initial Login")]
        public async void TestGoogleLogin()
        {
            Debug.Log("--- Test Google Login ---");
            await AuthService.Instance.StartGoogleLoginFlow();
        }

        [ContextMenu("3. Test Protected GET & POST Endpoints")]
        public async void TestProtectedEndpoints()
        {
            await TestProtectedEndpointsAsync();
        }

        [ContextMenu("4. Test Silent Token Refresh")]
        public async void TestSilentRefresh()
        {
            Debug.Log("--- Test Silent Refresh ---");
            AuthService.Instance.LoadRefreshTokenFromDisk();
            
            if (string.IsNullOrEmpty(AuthService.Instance.CurrentRefreshToken))
            {
                Debug.LogError("No Refresh Token on disk. Please login first.");
                return;
            }

            try
            {
                bool success = await AuthService.Instance.TrySilentRefreshAsync();
                if (success)
                {
                    Debug.Log($"Silent Refresh Success! New Access Token: {AuthService.Instance.CurrentAccessToken}");
                }
                else
                {
                    Debug.LogError("Silent Refresh Failed.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception during Silent Refresh: {ex.Message}");
            }
        }

        [ContextMenu("5. Test Logout & Revocation")]
        public async void TestLogout()
        {
            Debug.Log("--- Test Logout ---");
            AuthService.Instance.LoadRefreshTokenFromDisk();
            
            try
            {
                await AuthService.Instance.LogoutAsync();
                
                if (string.IsNullOrEmpty(AuthService.Instance.CurrentAccessToken) && 
                    string.IsNullOrEmpty(AuthService.Instance.CurrentRefreshToken) &&
                    !PlayerPrefs.HasKey("refresh_token"))
                {
                    Debug.Log("Logout successful: Memory and Disk cleared.");
                }
                else
                {
                    Debug.LogError("Logout failed: Tokens remain in memory or disk.");
                }
                
                Debug.Log("Verifying rejection on protected endpoint...");
                string result = await BackendApiService.Instance.GetAsync(_serverInstancesUrl);
                if (result == null) Debug.Log("Verified: Server rejected the request correctly.");
                else Debug.LogError("Failure: Server accepted the request after logout!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception during Logout: {ex.Message}");
            }
        }

        [ContextMenu("6. Full Typical Workflow")]
        public async void RunFullWorkflow()
        {
            Debug.Log("=== RUNNING FULL WORKFLOW ===");
            
            try
            {
                // 1. Try to load from disk
                bool hasDiskToken = AuthService.Instance.LoadRefreshTokenFromDisk();
                
                if (hasDiskToken)
                {
                    Debug.Log("Found token on disk. Attempting silent refresh...");
                    bool refreshed = await AuthService.Instance.TrySilentRefreshAsync();
                    
                    if (refreshed)
                    {
                        Debug.Log("Silent Refresh successful. Calling protected endpoint...");
                        await TestProtectedEndpointsAsync();
                        
                        Debug.Log("=== WORKFLOW COMPLETE (PATH: Silent Refresh) ===");
                        return;
                    }
                    else
                    {
                        Debug.LogWarning("Silent refresh failed. Proceeding to manual login.");
                    }
                }
                else
                {
                    Debug.Log("No token on disk. Proceeding to manual login.");
                }

                // 2. Manual Login -> Just fire it and let Start()'s OnAuthSuccess event handle the rest!
                Debug.Log("Starting Google Login Flow...");
                await AuthService.Instance.StartGoogleLoginFlow();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Fatal workflow exception: {ex.Message}");
            }
        }

        private async Task TestProtectedEndpointsAsync()
        {
            Debug.Log("--- Testing Protected Endpoints Internal ---");
            if (string.IsNullOrEmpty(AuthService.Instance.CurrentAccessToken))
            {
                Debug.LogError("No Access Token available to make API requests.");
                return;
            }

            try
            {
                Debug.Log($"Testing GET to: {_serverInstancesUrl}");
                string getResult = await BackendApiService.Instance.GetAsync(_serverInstancesUrl);
                if (getResult != null) Debug.Log($"<color=#4CAF50>GET Success: {getResult}</color>");
                else Debug.LogError("GET Failed.");

                Debug.Log($"Testing POST to: {_serverInstancesUrl}");
                string postResult = await BackendApiService.Instance.PostAsync(_serverInstancesUrl);
                if (postResult != null) Debug.Log($"<color=#4CAF50>POST Success: {postResult}</color>");
                else Debug.LogError("POST Failed.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception during API request execution: {ex.Message}");
            }
        }
    }
}