namespace Lobby.Infrastructure.Settings;

public class GoogleSettings
{
    public const string SettingsName = "Authentication:Google";
    public string ClientId { get; set; } = string.Empty;
}