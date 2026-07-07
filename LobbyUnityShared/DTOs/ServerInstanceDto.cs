using System;

namespace LobbyUnityShared.DTOs
{
    [Serializable]
    public class ServerInstanceDto
    {
        public string Id { get; set; } = string.Empty;
        public string ContainerId { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Port { get; set; }
        public int PlayerCount { get; set; }
        public DateTime? EmptySince { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int Status { get; set; }
    }
}
