namespace Lobby.Application.Entities;

public class RefreshTokenEntity
{
    public required Guid Id { get; set; }
    public required Guid PlayerId { get; set; }
    public required string TokenHash { get; set; }
    public required DateTime ExpiresAt { get; set; } 
    public required bool IsRevoked { get; set; } 
    public required string CreatedByIp { get; set; }
    
    public PlayerEntity Player { get; set; } = null!;
}