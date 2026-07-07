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
        private int _serverPort;

        private void Awake()
        {
            if (joinButton != null)
            {
                joinButton.onClick.AddListener(OnJoinClicked);
            }
        }
        
        public void Setup(string serverId, int port, string name, int currentPlayers, int maxPlayers)
        {
            _serverId = serverId;
            _serverPort = port;
            
            if (serverNameText != null) 
                serverNameText.text = name;
                
            if (playerCountText != null) 
                playerCountText.text = $"{currentPlayers}/{maxPlayers}";
        }

        private void OnJoinClicked()
        {
            Debug.Log($"[ServerItemUI] Join button clicked for server: {_serverId} on port: {_serverPort}");
            
            // TODO:
            // Implement the connection logic here. You should pass the _serverId to the NetworkManager 
            // or the appropriate service to initiate the connection to the game server.
        }
    }
}
