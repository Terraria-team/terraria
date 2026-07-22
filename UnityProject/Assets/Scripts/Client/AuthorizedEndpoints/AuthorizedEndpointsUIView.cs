using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Client.Auth;
using LobbyUnityShared.DTOs;

namespace Client.AuthorizedEndpoints
{
    public class AuthorizedEndpointsUIView : MonoBehaviour, IAuthorizedEndpointsView
    {
        [Header("UI Button References")]
        [SerializeField] private Button getServersButton;
        [SerializeField] private Button createServerButton;
        [SerializeField] private Button logoutButton;
        [SerializeField] private Button logoutAllButton;

        [Header("Server List References")]
        [SerializeField] private Transform serverListContainer;
        [SerializeField] private ServerItemUI serverItemPrefab;

        [Header("Feedback References")]
        [SerializeField] private TextMeshProUGUI feedbackText;

        private AuthorizedEndpointsPresenter _presenter;
        private Coroutine _hideFeedbackCoroutine;
        
        public event Action OnGetServersClicked;
        public event Action OnCreateServerClicked;
        public event Action OnLogoutClicked;
        public event Action OnLogoutAllClicked;

        private void Awake()
        {
            _presenter = new AuthorizedEndpointsPresenter(this, BackendApiService.Instance, AuthService.Instance);

            getServersButton.onClick.AddListener(() => OnGetServersClicked?.Invoke());
            createServerButton.onClick.AddListener(() => OnCreateServerClicked?.Invoke());
            logoutButton.onClick.AddListener(() => OnLogoutClicked?.Invoke());
            logoutAllButton.onClick.AddListener(() => OnLogoutAllClicked?.Invoke());

            SetLoadingState(false);
        }

        public void TriggerServerListRefresh()
        {
            OnGetServersClicked?.Invoke();
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
        }

        public void DisplayServerResponse(string rawJson)
        {
            //if (serverListContainer == null || serverItemPrefab == null) return;
        }

        public void SetLoadingState(bool isLoading)
        {
            getServersButton.interactable = !isLoading;
            createServerButton.interactable = !isLoading;
            logoutButton.interactable = !isLoading;
            logoutAllButton.interactable = !isLoading;
        }

        public void ShowRetrievedServerElements(List<ServerInstanceDto> servers)
        {
            if (serverItemPrefab == null || serverListContainer == null) return;

            foreach (Transform child in serverListContainer)
            {
                Destroy(child.gameObject);
            }
            
            Debug.Log($"Retrieved server elements: {servers.Count}");
            
            foreach (var server in servers)
            {
                var item = Instantiate(serverItemPrefab, serverListContainer, false);
                item.gameObject.SetActive(true);
                item.Setup(server.Id, server.Name, server.PlayerCount, 8, server.Port);
            }
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
        
        private System.Collections.IEnumerator HideFeedbackAfterDelay(float delay)
        {
            yield return new UnityEngine.WaitForSeconds(delay);
            if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        }

    }
}
