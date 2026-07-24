namespace Lobby.Infrastructure.ExternalServices.Settings;

public class GoogleSettings
{
    public const string SettingsName = "Authentication:Google";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
