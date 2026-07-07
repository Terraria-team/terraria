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
        private GameObject _lobbyScreenRoot;
        private GameObject _gameScreenRoot;

        private void Awake()
        {
            if (joinButton != null)
            {
                joinButton.onClick.AddListener(OnJoinClicked);
            }
        }

        public void Setup(string serverId, string name, int currentPlayers, int maxPlayers, int port, GameObject lobbyRoot, GameObject gameRoot)
        {
            _serverId = serverId;
            _port = port;
            _lobbyScreenRoot = lobbyRoot;
            _gameScreenRoot = gameRoot;
            
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

            if (NetworkManager.singleton.isNetworkActive)
            {
                Debug.LogWarning("[ServerItemUI] Already connected to a server.");
                return;
            }

            Debug.Log($"[ServerItemUI] Connecting to server: {_serverId} at {NetworkManager.singleton.networkAddress}:{_port}");

            if (Transport.active is PortTransport portTransport)
                portTransport.Port = (ushort)_port;

            NetworkManager.singleton.StartClient();

            // Hide Lobby UI, show game world
            if (_lobbyScreenRoot != null) _lobbyScreenRoot.SetActive(false);
            if (_gameScreenRoot != null) _gameScreenRoot.SetActive(true);
        }
    }
}
