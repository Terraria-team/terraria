using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
// using TMPro;
using Client.Api;
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

        [Header("Feedback References")]
        // [SerializeField] private GameObject loadingSpinner;
        // [SerializeField] private TextMeshProUGUI statusText;
        // [SerializeField] private TextMeshProUGUI errorText;
        // [SerializeField] private TextMeshProUGUI responseDataText;

        [Header("Server List UI")]
        [SerializeField] private ServerItemUI serverItemPrefab;
        [SerializeField] private Transform serverListContainer;

        private AuthorizedEndpointsPresenter _presenter;
        
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
            // if (responseDataText != null) responseDataText.text = "";
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
        }

        public void DisplayServerResponse(string rawJson)
        {
            // if (responseDataText != null) responseDataText.text = rawJson;
        }

        public void SetLoadingState(bool isLoading)
        {
            getServersButton.interactable = !isLoading;
            createServerButton.interactable = !isLoading;
            logoutButton.interactable = !isLoading;
            logoutAllButton.interactable = !isLoading;
            // if (loadingSpinner != null) loadingSpinner.SetActive(isLoading);
        }

        public void ShowRetrievedServerElements(List<ServerInstanceDto> servers)
        {
            if (serverItemPrefab == null || serverListContainer == null) return;

            foreach (Transform child in serverListContainer)
            {
                Destroy(child.gameObject);
            }
            
            foreach (var server in servers)
            {
                var item = Instantiate(serverItemPrefab, serverListContainer, false);
                item.gameObject.SetActive(true);
                item.Setup(server.Id, server.Port, server.Name, server.PlayerCount, 8);
            }
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

    }
}
