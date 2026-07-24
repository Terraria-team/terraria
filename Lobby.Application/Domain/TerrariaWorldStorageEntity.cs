namespace Lobby.Application.Domain;

public class TerrariaWorldStorageEntity
{
    public required Guid Id { get; set; }
    public required string AbsolutePathOnTheDisk { get; set; }
    
    public TerrariaWorldEntity? World { get; set; }
}