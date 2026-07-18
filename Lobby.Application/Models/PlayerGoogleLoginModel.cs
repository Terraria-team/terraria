namespace Lobby.Application.Models;

public class PlayerGoogleLoginModel
{
    public required string GoogleId { get; set; }
    public required string Email { get; set; }
    public required string Name { get; set; }
    public required string Role { get; set; }
}