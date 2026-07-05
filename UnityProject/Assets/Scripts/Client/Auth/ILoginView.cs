using System;

namespace Client.Auth
{
    public interface ILoginView
    {
        event Action OnLoginClicked;

        void SetLoadingState(bool isLoading);
        void UpdateStatus(string message);
        void ShowError(string errorMessage);
        void ClearMessages();
    }
}
