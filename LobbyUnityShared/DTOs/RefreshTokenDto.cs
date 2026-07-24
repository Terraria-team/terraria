using System;

namespace LobbyUnityShared.DTOs
{
    [Serializable]
    public class RefreshTokenDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}