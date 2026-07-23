namespace Lobby.Settings;

public class ApiSecuritySettings
{
    public const string SettingsName = "Authentication:ServerToLobbyAuth";
    public string ApiKey { get; set; } = string.Empty;
}
