using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Client.AuthorizedEndpoints
{
    public class ServerItemUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI serverNameText;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private Button joinButton;

        private string _serverId;

        private void Awake()
        {
            if (joinButton != null)
            {
                joinButton.onClick.AddListener(OnJoinClicked);
            }
        }

        // TODO:
        // Once you have the ServerInstanceDto and the JSON parsing ready in AuthorizedEndpointsUIView.cs,
        // iterate over your DTO list, Instantiate this prefab, and call this Setup() method for each item 
        // to populate the UI with the real data.
        public void Setup(string serverId, string name, int currentPlayers, int maxPlayers)
        {
            _serverId = serverId;
            
            if (serverNameText != null) 
                serverNameText.text = name;
                
            if (playerCountText != null) 
                playerCountText.text = $"{currentPlayers}/{maxPlayers}";
        }

        private void OnJoinClicked()
        {
            Debug.Log($"[ServerItemUI] Join button clicked for server: {_serverId}");
            
            // TODO:
            // Implement the connection logic here. You should pass the _serverId to the NetworkManager 
            // or the appropriate service to initiate the connection to the game server.
        }
    }
}
