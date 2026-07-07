using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Client.Auth
{
    public class LoginUIView : MonoBehaviour, ILoginView
    {
        [Header("UI References")]
        [SerializeField] private Button loginButton;
        [SerializeField] private TextMeshProUGUI feedbackText;
        
        private Coroutine _hideFeedbackCoroutine;

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
        }

        public void UpdateStatus(string message)
        {
            if (feedbackText != null)
            {
                feedbackText.gameObject.SetActive(true);
                feedbackText.color = Color.blue;
                feedbackText.text = message;
                
                if (_hideFeedbackCoroutine != null) StopCoroutine(_hideFeedbackCoroutine);
                _hideFeedbackCoroutine = StartCoroutine(HideFeedbackAfterDelay(3f));
            }
        }

        public void ShowError(string errorMessage)
        {
            SetLoadingState(false);
            if (feedbackText != null)
            {
                feedbackText.gameObject.SetActive(true);
                feedbackText.color = Color.red;
                feedbackText.text = errorMessage;
                
                // Stop any pending hide from a previous status message so the error stays visible
                if (_hideFeedbackCoroutine != null) StopCoroutine(_hideFeedbackCoroutine);
            }
        }

        public void ClearMessages()
        {
            if (_hideFeedbackCoroutine != null) StopCoroutine(_hideFeedbackCoroutine);
            if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        }
        
        private System.Collections.IEnumerator HideFeedbackAfterDelay(float delay)
        {
            yield return new UnityEngine.WaitForSeconds(delay);
            if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        }
    }
}
