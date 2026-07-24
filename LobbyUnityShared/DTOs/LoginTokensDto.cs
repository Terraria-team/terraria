using System;

namespace LobbyUnityShared.DTOs
{
    [Serializable]
    public class LoginTokensDto
    {
        public string RefreshToken { get; set; } = string.Empty;

        public string AccessToken { get; set; }= string.Empty;
    }
}