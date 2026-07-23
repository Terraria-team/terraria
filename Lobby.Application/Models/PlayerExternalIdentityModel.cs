namespace Lobby.Application.Models;

public class PlayerExternalIdentityModel
{
    public required string ExternalId { get; set; }
    public required string Provider { get; set; }
    public required string Email { get; set; }
    public required string Name { get; set; }
    public required string Role { get; set; }
}
