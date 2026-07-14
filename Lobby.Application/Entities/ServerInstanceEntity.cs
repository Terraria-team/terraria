namespace Lobby.Application.Entities;

public class ServerInstanceEntity
{
    public Guid Id { get; set; }
    public Guid WorldId { get; set; }
    public string ContainerId { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Port { get; set; }

    public ServerInstanceStatus Status { get; set; }
    public int PlayerCount { get; set; }
    public DateTime? EmptySince { get; set; }
    public DateTime? PendingSince { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public TerrariaWorldEntity? World { get; set; }
}