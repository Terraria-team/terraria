using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Client.Api;
using Client.Auth;
using System.Collections.Generic;
using Newtonsoft.Json;

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

        [Header("Scene Root References")]
        [SerializeField] private GameObject lobbyScreenRoot;
        [SerializeField] private GameObject gameScreenRoot;

        [Header("Feedback References")]
        [SerializeField] private TextMeshProUGUI feedbackText;

        [Header("Server List UI")]
        [SerializeField] private ServerItemUI serverItemPrefab;
        [SerializeField] private Transform serverListContainer;

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

        private void OnDestroy()
        {
            _presenter?.Dispose();
        }

        public void DisplayServerResponse(string rawJson)
        {
            if (serverListContainer == null || serverItemPrefab == null) return;

            // Handle both single objects (from Create) and arrays (from GetServers)
            try
            {
                string trimmedJson = rawJson.TrimStart();
                if (trimmedJson.StartsWith("{"))
                {
                    // It's a single object (e.g., from Create Server)
                    var server = JsonConvert.DeserializeObject<ServerInstanceDto>(rawJson);
                    if (server != null)
                    {
                        var item = Instantiate(serverItemPrefab, serverListContainer);
                        item.Setup(server.id, server.name, server.playerCount, 8,
                             server.port, lobbyScreenRoot, gameScreenRoot);
                    }
                }
                else if (trimmedJson.StartsWith("["))
                {
                    // It's an array (e.g., from Refresh Servers)
                    // Clear previous items only when refreshing the full list
                    foreach (Transform child in serverListContainer)
                    {
                        Destroy(child.gameObject);
                    }

                    var servers = JsonConvert.DeserializeObject<List<ServerInstanceDto>>(rawJson);
                    if (servers != null)
                    {
                        foreach (var server in servers)
                        {
                            var item = Instantiate(serverItemPrefab, serverListContainer);
                            item.Setup(server.id, server.name, server.playerCount, 8,
                                server.port, lobbyScreenRoot, gameScreenRoot);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthorizedEndpointsUIView] Failed to parse server instances: {ex.Message}");
            }
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
            
            foreach (var server in servers)
            {
                var item = Instantiate(serverItemPrefab, serverListContainer, false);
                item.gameObject.SetActive(true);
                item.Setup(server.Id, server.Port, server.Name, server.PlayerCount, 8);
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
