namespace Lobby.Application.UseCases;

public class RefreshTokenSettings
{
    public const string SettingsName = "Authentication:Jwt";
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
