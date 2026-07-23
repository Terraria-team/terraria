using System;

namespace LobbyUnityShared.DTOs
{
    [Serializable]
    public class UpdatePlayerCountDto
    {
        public int PlayerCount { get; set; } = -1;
    }
}