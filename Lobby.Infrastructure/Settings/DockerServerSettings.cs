namespace Lobby.Infrastructure.Settings;

public class DockerServerSettings
{
    public static string SettingsName = "DockerServerSettings";
    public string ImageName { get; set; } = string.Empty;
}