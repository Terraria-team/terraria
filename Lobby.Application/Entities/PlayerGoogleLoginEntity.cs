namespace Lobby.Application.Entities;

public class PlayerGoogleLoginEntity
{
    public required Guid PlayerId { get; set; }
    public required string GoogleId { get; set; }

    public PlayerEntity Player { get; set; } = null!;
}