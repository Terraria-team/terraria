namespace Lobby.Application.Domain;

public class RefreshTokenEntity
{
    public required Guid Id { get; set; }
    public required Guid PlayerId { get; set; }
    public required string TokenHash { get; set; }
    public required DateTime ExpiresAt { get; set; } 
    public required bool IsRevoked { get; set; } 
    public required string CreatedByIp { get; set; }
    
    public bool IsExpired => ExpiresAt < DateTime.UtcNow;
    
    public PlayerEntity Player { get; set; } = null!;
}