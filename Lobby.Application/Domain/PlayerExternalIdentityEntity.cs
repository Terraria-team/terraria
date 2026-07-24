namespace Lobby.Application.Domain;

public class PlayerExternalIdentityEntity
{
    public required Guid PlayerId { get; set; }
    public required string Provider { get; set; }
    public required string ExternalId { get; set; }

    public PlayerEntity Player { get; set; } = null!;
}
