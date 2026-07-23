namespace Lobby.Application.Domain;

public class PlayerEntity
{
    public required Guid Id { get; set; }
    public required string Email { get; set; } 
    public required string Name { get; set; } 
    public required string Role { get; set; }
    
    public ICollection<PlayerExternalIdentityEntity> ExternalIdentities { get; set; } = new List<PlayerExternalIdentityEntity>();
    public IEnumerable<RefreshTokenEntity>? RefreshTokens { get; set; }
    public ICollection<TerrariaWorldEntity> OwnedWorlds { get; set; } = new List<TerrariaWorldEntity>();
}