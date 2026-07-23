namespace Lobby.Application.Entities;

public class TerrariaWorldEntity
{
    public required Guid Id { get; set; }
    public required Guid OwnerId { get; set; }
    public required string Name { get; set; } // if none specifed defaults to the id
    public required Guid? StorageId{ get; set; } 
    // I keep this nullable becasue there is a period of time when the container and the world have been created but not saved once to the disk
    
    public TerrariaWorldStorageEntity? Storage { get; set; }
    public PlayerEntity? Owner { get; set; }
    public ICollection<ServerInstanceEntity> ServerInstances { get; set; } = new List<ServerInstanceEntity>();
}