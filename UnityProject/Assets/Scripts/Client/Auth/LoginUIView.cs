using System;
using UnityEngine;
using UnityEngine.UI;
// using TMPro;

namespace Client.Auth
{
    public class LoginUIView : MonoBehaviour, ILoginView
    {
        [Header("UI References")]
        [SerializeField] private Button loginButton;
        // [SerializeField] private GameObject loadingSpinner;
        // [SerializeField] private TextMeshProUGUI statusText;
        // [SerializeField] private TextMeshProUGUI errorText;

        private LoginPresenter _presenter;
        public event Action OnLoginClicked;

        private void Awake()
        {
            _presenter = new LoginPresenter(this, AuthService.Instance);
            loginButton.onClick.AddListener(() => OnLoginClicked?.Invoke());
            
            SetLoadingState(false);
            ClearMessages();
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
        }

        public void SetLoadingState(bool isLoading)
        {
            loginButton.interactable = !isLoading;
            // if (loadingSpinner != null) loadingSpinner.SetActive(isLoading);
        }

        public void UpdateStatus(string message)
        {
            /*
            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = message;
            }
            if (errorText != null) errorText.gameObject.SetActive(false);
            */
        }

        public void ShowError(string errorMessage)
        {
            SetLoadingState(false);
            /*
            if (statusText != null) statusText.gameObject.SetActive(false);
            if (errorText != null)
            {
                errorText.gameObject.SetActive(true);
                errorText.text = errorMessage;
            }
            */
        }

        public void ClearMessages()
        {
            /*
            if (statusText != null) statusText.gameObject.SetActive(false);
            if (errorText != null) errorText.gameObject.SetActive(false);
            */
        }
    }
}
