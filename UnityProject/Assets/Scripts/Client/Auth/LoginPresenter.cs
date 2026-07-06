using System;

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
            _view.ClearMessages();
            await _authService.StartGoogleLoginFlow();
        }

        private void HandleAuthStarted() => _view.SetLoadingState(true);
        private void HandleStatusUpdated(string status) => _view.UpdateStatus(status);
        private void HandleAuthFailed(string error) => _view.ShowError(error);

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
