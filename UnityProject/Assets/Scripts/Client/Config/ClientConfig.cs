using UnityEngine;

namespace Client.Config
{
    [CreateAssetMenu(fileName = "ClientConfig", menuName = "Client/Client Config")]
    public class ClientConfig : ScriptableObject
    {
        [Header("Backend Configuration")]
        [Tooltip("The base URL of your API server.")]
        public string baseUrl = "http://127.0.0.1:5241";

        [Header("Endpoints")]
        public string serverInstancesEndpoint = "/api/server-instances";
        public string logoutAllEndpoint = "/api/auth/logoutAll";
        public string googleLoginEndpoint = "/api/auth/google-login";
        public string refreshEndpoint = "/api/auth/refresh";
        public string logoutEndpoint = "/api/auth/logout";

        [Header("Google OAuth")]
        public string googleClientId = "859856222839-qjfks5pbv25osu3ks8pirl994llfkt4p.apps.googleusercontent.com";
        public string googleClientSecret = "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";
    }
}
