using System;
using UnityEngine;

namespace Client.Auth
{
    public class LoginPresenter : IDisposable
    {
        private readonly ILoginView _view;
        private readonly AuthService _authService;

        public LoginPresenter(ILoginView view, AuthService authService)
        {
            _view = view;
            _authService = authService;

            _view.OnLoginClicked += HandleLoginClicked;
            _authService.OnAuthStarted += HandleAuthStarted;
            _authService.OnAuthStatusUpdated += HandleStatusUpdated;
            _authService.OnAuthSuccess += HandleAuthSuccess;
            _authService.OnAuthFailed += HandleAuthFailed;
        }

        private async void HandleLoginClicked()
        {
            try
            {
                _view.ClearMessages();
                await _authService.StartGoogleLoginFlow();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LoginPresenter] Unexpected exception during login: {ex.Message}");
                _view.ShowError($"Unexpected error: {ex.Message}");
                _view.SetLoadingState(false);
            }
        }

        private void HandleAuthStarted() => _view.SetLoadingState(true);
        private void HandleStatusUpdated(string status) => _view.UpdateStatus(status);
        private void HandleAuthFailed(string error)
        {
            _view.ShowError(error);
            _view.SetLoadingState(false);
        }

        private void HandleAuthSuccess(string jwtToken)
        {
            _view.UpdateStatus("Success!");
            _view.SetLoadingState(false);
        }

        public void Dispose()
        {
            _view.OnLoginClicked -= HandleLoginClicked;
            _authService.OnAuthStarted -= HandleAuthStarted;
            _authService.OnAuthStatusUpdated -= HandleStatusUpdated;
            _authService.OnAuthSuccess -= HandleAuthSuccess;
            _authService.OnAuthFailed -= HandleAuthFailed;
        }
    }
}
