namespace Lobby.Application.Models;

public class LoginTokensModel
{
    public required string sessionToken { get; set; }
    public required string accessToken { get; set; }

}