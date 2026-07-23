namespace Lobby.Application.UseCases;

public class ServerInstanceServiceSettings
{
    public const string SettingsName = "ServerInstanceServiceSettings";
    public int ServerInstanceMaxCount { get; set; } = 13;
}