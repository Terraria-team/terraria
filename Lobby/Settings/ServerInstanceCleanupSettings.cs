namespace Lobby.Settings;

public class ServerInstanceCleanupSettings
{
    public const string SettingsName = "ServerInstanceCleanupSettings";
    public int CleanupIntervalSeconds { get; set; }
    public int PendingTimeoutSeconds { get; set; }
    public int IdleTimeoutSeconds { get; set; }

    public string ServerInstanceImageName { get; set; } = "terraria-server:latest";

}