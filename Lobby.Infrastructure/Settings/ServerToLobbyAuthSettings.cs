namespace Lobby.Infrastructure.Settings;

public class ServerToLobbyAuthSettings
{
    public const string SettingsName = "Authentication:ServerToLobbyAuth";
    public string ApiKey { get; set; } = string.Empty;
}