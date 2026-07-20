using System.Threading.Tasks;
using Client.Api;
using Client.Auth;
using Client.AuthorizedEndpoints;
using UnityEngine;

namespace Client
{
    public class GameFlowCoordinator : MonoBehaviour
    {
        [Tooltip("The root GameObject containing your LoginUIView")]
        [SerializeField] private GameObject loginScreenRoot;
        
        [Tooltip("The root GameObject containing your AuthorizedEndpointsUIView")]
        [SerializeField] private GameObject dashboardScreenRoot;

        private const string RefreshTokenKey = "refresh_token";

        private void Awake()
        {
            AuthService.Instance.OnAuthSuccess += HandleAuthenticationSuccess;
            BackendApiService.Instance.OnSessionExpired += HandleSessionExpired;

            loginScreenRoot.SetActive(false);
            dashboardScreenRoot.SetActive(false);
        }

        private async void Start()
        {
            await ExecuteSilentBootAsync();
        }

        private void OnDestroy()
        {
            if (AuthService.Instance != null)
                AuthService.Instance.OnAuthSuccess -= HandleAuthenticationSuccess;

            if (BackendApiService.Instance != null)
                BackendApiService.Instance.OnSessionExpired -= HandleSessionExpired;
        }
        

        private async Task ExecuteSilentBootAsync()
        {
            
            string savedRefreshToken = PlayerPrefs.GetString(RefreshTokenKey, null);

            if (!string.IsNullOrEmpty(savedRefreshToken))
            {
                
                AuthService.Instance.SetRefreshToken(savedRefreshToken);
                
                bool silentLoginSuccess = await AuthService.Instance.TrySilentRefreshAsync();

                if (silentLoginSuccess)
                {
                    ShowDashboardScreen();
                    return;
                }
            }
            
            ShowLoginScreen();
        }

        private void HandleAuthenticationSuccess(string newJwtAccessToken)
        {
            ShowDashboardScreen();
        }

        private void HandleSessionExpired()
        {
            
            PlayerPrefs.DeleteKey(RefreshTokenKey);
            PlayerPrefs.Save();
            
            ShowLoginScreen();
        }

        private void ShowLoginScreen()
        {
            dashboardScreenRoot.SetActive(false);
            loginScreenRoot.SetActive(true);
        }

        private void ShowDashboardScreen()
        {
            loginScreenRoot.SetActive(false);
            dashboardScreenRoot.SetActive(true);
            // Explicitly trigger server list load AFTER auth token is guaranteed to be set
            GetComponent<AuthorizedEndpointsUIView>()?.TriggerServerListRefresh();
        }
    }
}   
