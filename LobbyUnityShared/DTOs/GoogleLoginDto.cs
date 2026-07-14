using System;

namespace LobbyUnityShared.DTOs
{
    [Serializable]
    public class GoogleLoginDto
    {
        public string Code { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
    }
}