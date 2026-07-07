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
        private string _ipAddress = "localhost"; // Default for local testing
        private int _port = 7777;

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
        public void Setup(string serverId, string name, int currentPlayers, int maxPlayers, string ip = "localhost", int port = 7777)
        {
            _serverId = serverId;
            _ipAddress = ip;
            _port = port;
            
            if (serverNameText != null) 
                serverNameText.text = name;
                
            if (playerCountText != null) 
                playerCountText.text = $"{currentPlayers}/{maxPlayers}";
        }

        private void OnJoinClicked()
        {
            Debug.Log($"[ServerItemUI] Join button clicked for server: {_serverId} at {_ipAddress}:{_port}");
            
            if (NetworkManager.singleton != null)
            {
                NetworkManager.singleton.networkAddress = _ipAddress;
                
                // Try to set port if transport supports it
                if (Transport.active is PortTransport portTransport)
                {
                    portTransport.Port = (ushort)_port;
                }
                
                NetworkManager.singleton.StartClient();
            }
            else
            {
                Debug.LogWarning("[ServerItemUI] NetworkManager instance not found!");
            }
            
            //when merged, change to the latest scene(scene with collisions)
            UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
        }
    }
}
