namespace Lobby.DTOs;

public class GoogleLoginRecord
{
    public string Code { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
}

public class RefreshTokenRecord
{
    public string RefreshToken { get; set; } = string.Empty;
}