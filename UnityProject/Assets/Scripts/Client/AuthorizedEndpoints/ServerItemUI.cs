using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

namespace Client.AuthorizedEndpoints
{
    public class ServerItemUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI serverNameText;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private Button joinButton;

        private string _serverId;
        private int _port = 7777;

        private void Awake()
        {
            if (joinButton != null)
            {
                joinButton.onClick.AddListener(OnJoinClicked);
            }
        }

        public void Setup(string serverId, string name, int currentPlayers, int maxPlayers, int port)
        {
            _serverId = serverId;
            _port = port;
            
            if (serverNameText != null) 
                serverNameText.text = name;
                
            if (playerCountText != null) 
                playerCountText.text = $"{currentPlayers}/{maxPlayers}";
        }

        private void OnJoinClicked()
        {
            if (NetworkManager.singleton == null)
            {
                Debug.LogWarning("[ServerItemUI] NetworkManager.singleton is null — make sure NetworkManager is in LobbyScene.");
                return;
            }

            // If the user clicks join while it's already trying to connect (but hung), 
            // force stop the old attempt so we can start fresh.
            if (NetworkManager.singleton.isNetworkActive)
            {
                Debug.Log("[ServerItemUI] Stopping previous hung connection attempt...");
                NetworkManager.singleton.StopClient();
            }

            Debug.Log($"[ServerItemUI] Connecting to server: {_serverId} at {NetworkManager.singleton.networkAddress}:{_port}");
            
            if (Transport.active is PortTransport portTransport)
                portTransport.Port = (ushort)_port;

            NetworkManager.singleton.StartClient();
        }
    }
}
