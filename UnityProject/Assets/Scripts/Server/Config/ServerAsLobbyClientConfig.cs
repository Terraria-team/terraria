using UnityEngine;

namespace Server.Config
{
    [CreateAssetMenu(fileName = "ServerAsLobbyClientConfig", menuName = "Server/LobbyClient Config")]
    public class ServerAsLobbyClientConfig : ScriptableObject
    {
        [Header("ServerAsLobbyClient Configuration")]
        [Tooltip("The base URL of your API server.")]
        public string baseUrl = "http://lobby-api:5241";
        
        [Header("Endpoints")]
        public string serverInstancesEndpoint = "/api/internal/server-instances";
        
    }
}