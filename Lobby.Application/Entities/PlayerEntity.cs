namespace Lobby.Application.Entities;

public class PlayerEntity
{
    public required Guid Id { get; set; }
    public required string Email { get; set; } 
    public required string Name { get; set; } 
    public required string Role { get; set; }
    
    public PlayerGoogleLoginEntity? GoogleLogin { get; set; }
    public IEnumerable<RefreshTokenEntity>? RefreshTokens { get; set; }
}